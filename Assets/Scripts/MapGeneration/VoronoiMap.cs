using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class VoronoiMap : NoiseMap
{
    // Voronoi tessellation requires that we define a set number of centroids within some XxY space.
    // We then use Lloyd Relaxation to make these clusters a bit more rounded and clumpy

    [System.Serializable]
    public class VoronoiSegment : TerrainType {
        public bool isBorder = false;
    }

    [Header("=== Voronoi Map Generation ===")]
    [SerializeField, Tooltip("The number of intended segments we want to generate.")]                                           private int m_numSegments;
    [SerializeField, Tooltip("Designate a buffer zone from the map edges where centroids cannot be spawned.")]                  private int m_edgeBuffer = 20;
    [SerializeField, Tooltip("The number of times Lloyd Relaxation is conducted to 'clump' the regions"), Range(0,10)]          private int m_lloydRelaxationIterations = 2;
    [SerializeField, Tooltip("Do we make the edge regions feel like cliffs?")]                                                  private bool m_edgeBorder = true;
    [SerializeField, Tooltip("Do we use a falloff noise map for border determination? If so, set here. Otherwise, just uses regions next to edge")] private FalloffMap m_falloffMap;
    [SerializeField, Tooltip("The number of iterations to smoothen the heights of regions."), Range(0,10)]                      private int m_heightSmoothenIterations = 2;
    [SerializeField, Tooltip("The weight factor applied to the new smoothened height, when smoothening region heights"), Range(0f, 1f)] private float m_heightSmoothenWeight = 0.25f;

    [Header("=== Voronoi Outputs - READ-ONLY ===")]
    [SerializeField, Tooltip("The different voronoi segments generated.")]          private VoronoiSegment[] m_voronoiSegments;
    [SerializeField, Tooltip("The centroids of the generated voronoi segments.")]   private Vector2Int[] m_voronoiCentroids;
    [SerializeField, Tooltip("The lookup dictionary to map segments to pixels")]    private Dictionary<int, List<Vector2Int>> m_voronoiSegmentToPixelsMap;
    [SerializeField, Tooltip("Neighbor lookup for each voronoi segment")]           private Dictionary<int, List<int>> m_voronoiSegmentNeighborMap;
    [SerializeField] private int[] m_voronoiSegmentNeighborCount;

    #if UNITY_EDITOR
    [Header("=== Debug Settings ===")]
    [SerializeField, Tooltip("Should we show the voronoi centroids in gizmos?")]  private bool m_showVoronoiCentroids = true;
    [SerializeField, Tooltip("Should we show the voronoi stats in gizmos?")]  private bool m_showVoronoiStats = true;
    private bool m_showVoronoiDebug => m_showVoronoiCentroids || m_showVoronoiStats;

    protected override void OnDrawGizmos() {
        base.OnDrawGizmos();
        if (!m_showVoronoiDebug) return;

        float worldHalfWidth = Mathf.Floor((float)(m_mapChunkSize-1)/2f);
        float worldWidth = worldHalfWidth * 2f;
        Gizmos.color = Color.blue;
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.black;
        style.alignment = TextAnchor.MiddleLeft;
        try {
            for(int i = 0; i < m_voronoiCentroids.Length; i++) {
                Vector2Int centroidPos = m_voronoiCentroids[i];
                float height = QueryHeightAtCoords(centroidPos.x, centroidPos.y, out Vector3 relWorldPos);
                Vector3 worldPos = transform.position + relWorldPos; 
                int neighborCount = m_voronoiSegmentNeighborCount[i];
                string borderStatus = m_voronoiSegments[i].isBorder ? "Border" : "Not Border";

                if (m_showVoronoiCentroids) Gizmos.DrawSphere(worldPos, 1f);
                if (m_showVoronoiStats)     Handles.Label(worldPos + new Vector3(1.5f, 0f, 0f), $"R{i+1}\n{neighborCount} neighbors\n{borderStatus}", style);
            }
        } catch(Exception e) {}
    }
    #endif

    public override void GenerateMap() {
        // Initialize the pseudo-random number generator
        m_prng = new System.Random(m_seed);

        // Conduct some checks: 1) the number of segments is at least 1, and 2) the edge buffer is smaller than half the map chunk size
        if (m_numSegments <= 0) m_numSegments = 1;
        if ((float)m_edgeBuffer >= (float)m_mapChunkSize/2f) m_edgeBuffer = Mathf.FloorToInt((float)(m_mapChunkSize-1)/2f);

        // generate the terrain types
        float heightDenom = (m_edgeBorder) ? (float)(m_numSegments+1) : (float)m_numSegments;
        m_voronoiSegments = new VoronoiSegment[m_numSegments];
        for(int i = 0; i < m_numSegments; i++) {
            string name = $"Region {i+1}";
            float height = (float)(i+1)/heightDenom;
            Color color =  new Color(
                (float)m_prng.Next(0, 100)/100f, 
                (float)m_prng.Next(0, 100)/100f,
                (float)m_prng.Next(0, 100)/100f
            );
            m_voronoiSegments[i] = new VoronoiSegment { name=name, height=height, color=color, isBorder = false };
        }

        // In this first run, we generate the voronoi regions first, using pure randomness.
        Vector2Int[] centroids = GenerateVoronoiCentroids();
        int[,] voronoiMap = GenerateVoronoiRegions(centroids);

        // Then we iteratively perform Lloyd Relaxation N times to make the regions a bit more... clumpy
        for (int i = 0; i < m_lloydRelaxationIterations; i++) {
            centroids = LloydRelaxation(voronoiMap);
            voronoiMap = GenerateVoronoiRegions(centroids);
        }

        // If we want to define an edge border, then we have to do a double-check along the edge pixels
        // Any regions that have an edge pixel is considered a border segment.
        // For those segments, we have to set the height to 1.
        SetBorderRegions(voronoiMap);

        // At this point, it's a good idea to generate a lookup table of some kind - namely, one that'll
        //  allow us to associate voronoi segments with their pixels
        m_voronoiSegmentToPixelsMap = GenerateRegionLookup(voronoiMap, out m_voronoiSegmentNeighborMap);
        
        // Finalize the voronoi centroids
        m_voronoiCentroids = centroids;

        // The generated noisemap needs to be converted from an int[,] to a float[,] based on region
        // We want to smoothen the terrain regions so that we don't have these weird pocket areas.\
        // Basically, we take each noise map and then smoothen them height-wise via averaging of current height and non-border neighbor heights
        m_noiseMap = GenerateNoiseMapFromVoronoi(voronoiMap);

        // We can generate the height map afterwards
        m_heightMap = Generators.GenerateHeightMap(m_noiseMap, m_textureHeightCurve, m_textureHeightMultiplier);
        if (m_drawMode != DrawMode.None) RenderMap();
    }

    private Vector2Int[] GenerateVoronoiCentroids() {        
        List<Vector2Int> existingPoints = new List<Vector2Int>();
        while(existingPoints.Count < m_numSegments) {
            Vector2Int pointIndices = new Vector2Int(
                m_prng.Next(m_edgeBuffer, m_mapChunkSize-m_edgeBuffer),
                m_prng.Next(m_edgeBuffer, m_mapChunkSize-m_edgeBuffer)
            );
            if (!existingPoints.Contains(pointIndices)) existingPoints.Add(pointIndices);
        }
        return existingPoints.ToArray();
    }

    private Vector2Int[] LloydRelaxation(int[,] data) {
        Vector2Int[] regionPositionSums = new Vector2Int[m_numSegments];
        int[] regionCounts = new int[m_numSegments];

        for(int y = 0; y < m_mapChunkSize; y++) {
            for(int x = 0; x < m_mapChunkSize; x++) {
                // Check which region index we're actually in
                int regionIndex = data[x,y];
                // Knowing the region index, we can cumulate the position and count of that region
                regionPositionSums[regionIndex] += new Vector2Int(x,y);
                regionCounts[regionIndex] += 1;        
            }
        }

        // Now, re-position the centers
        Vector2Int[] newCenters = new Vector2Int[m_numSegments];
        for(int j = 0; j < m_numSegments; j++) {
            Vector2 originalCenter = new Vector2(
                (float)regionPositionSums[j].x,
                (float)regionPositionSums[j].y
            );
            float regionCountf = (float)regionCounts[j];
            if (regionCountf == 0f) regionCountf = 0.0001f;
            Vector2Int newCenter = new Vector2Int(
                Mathf.RoundToInt(originalCenter.x / regionCountf),
                Mathf.RoundToInt(originalCenter.y / regionCountf)
            );
            newCenters[j] = newCenter;
        }

        return newCenters;
    }

    private int[,] GenerateVoronoiRegions(Vector2Int[] centroids) {
        int[,] regionMap = new int[m_mapChunkSize, m_mapChunkSize];
        for(int y = 0; y < m_mapChunkSize; y++) {
            for(int x = 0; x < m_mapChunkSize; x++) {
                Vector2Int index = new Vector2Int(x,y);
                int closestIndex = 0;
                float closestDistance = Vector2Int.Distance(index, centroids[0]);
                for(int i = 1; i < m_numSegments; i++) {
                    float dist = Vector2Int.Distance(index, centroids[i]);
                    if (dist < closestDistance) {
                        closestIndex = i;
                        closestDistance = dist;
                    }
                }
                regionMap[x,y] = closestIndex;          
            }
        }
        return regionMap;
    }

    private Dictionary<int, List<Vector2Int>> GenerateRegionLookup(int[,] data, out Dictionary<int, List<int>> neighborMap) {
        Dictionary<int, List<Vector2Int>> lookupTable = new Dictionary<int, List<Vector2Int>>();
        neighborMap = new Dictionary<int, List<int>>();
        m_voronoiSegmentNeighborCount = new int[m_numSegments];

        for(int i = 0; i < m_numSegments; i++) {
            lookupTable.Add(i, new List<Vector2Int>());
            neighborMap.Add(i, new List<int>());
        }
        for(int y = 0; y < m_mapChunkSize; y++) {
            for(int x = 0; x < m_mapChunkSize; x++) {
                Vector2Int cellIndex = new Vector2Int(x,y);
                int regionIndex = data[x,y];
                lookupTable[regionIndex].Add(cellIndex);
                // Get neighbors using moore neighbors, which are the 8 grid cells around a given grid
                for(int ny = y-1; ny <= y+1; ny++) {
                    for(int nx = x-1; nx <= x+1; nx++) {
                        if (ny < 0 || ny >= m_mapChunkSize || nx < 0 || nx >= m_mapChunkSize) continue;
                        // Neighbor region
                        int neighborRegionIndex = data[nx,ny];
                        if (neighborRegionIndex == regionIndex) continue;
                        if (!neighborMap[regionIndex].Contains(neighborRegionIndex)) {
                            neighborMap[regionIndex].Add(neighborRegionIndex);
                            m_voronoiSegmentNeighborCount[regionIndex] += 1;
                        }
                    }
                }
            }
        }
        return lookupTable;
    }

    private float[,] GenerateNoiseMapFromVoronoi(int[,] data) {
        // Before anything, we have to mod the heights of each region 
        if (m_heightSmoothenIterations > 0) {
            float origWeight = 1f - m_heightSmoothenWeight;
            for(int i = 0; i < m_heightSmoothenIterations; i++) {
                for(int j = 0; j < m_numSegments; j++) {
                    if (m_voronoiSegments[j].isBorder) continue;
                    List<int> neighbors = m_voronoiSegmentNeighborMap[j];
                    float origHeight = m_voronoiSegments[j].height;
                    float height = origHeight;
                    int heightCount = 1;
                    foreach(int neighborIndex in neighbors) {
                        if (m_voronoiSegments[neighborIndex].isBorder) continue;
                        height += m_voronoiSegments[neighborIndex].height;
                        heightCount += 1;
                    }
                    // Set the new height
                    m_voronoiSegments[j].height = origHeight*origWeight + (height / (float)heightCount)*m_heightSmoothenWeight;
                }
            }
        }

        float[,] noiseMap = new float[m_mapChunkSize, m_mapChunkSize];
        for(int y = 0; y < m_mapChunkSize; y++) {
            for(int x = 0; x < m_mapChunkSize; x++) {
                int regionIndex = data[x,y];
                float height = m_voronoiSegments[data[x,y]].height;
                // If we want to define an edge border, then we have to 
                noiseMap[x,y] = height;
            }
        }

        return noiseMap;
    }

    private void SetBorderRegions(int[,] voronoiMap) {
        if (!m_edgeBorder) return;
        
        if (m_falloffMap != null) {
            for(int y = 0; y < m_mapChunkSize; y++) {
                for(int x = 0; x < m_mapChunkSize; x++) {
                    if (m_falloffMap.noiseMap[x,y] < 1f) {
                        m_voronoiSegments[voronoiMap[x,y]].height = 1f;
                        m_voronoiSegments[voronoiMap[x,y]].isBorder = true;
                    }
                }
            }
            return;
        }

        for(int y = 0; y < m_mapChunkSize; y++) {
            m_voronoiSegments[voronoiMap[0,y]].height = 1f;
            m_voronoiSegments[voronoiMap[0,y]].isBorder = true;
            m_voronoiSegments[voronoiMap[m_mapChunkSize-1,y]].height = 1f;
            m_voronoiSegments[voronoiMap[m_mapChunkSize-1,y]].isBorder = true;
        }
        for(int x = 0; x < m_mapChunkSize; x++) {
            m_voronoiSegments[voronoiMap[x,0]].height = 1f;
            m_voronoiSegments[voronoiMap[x,0]].isBorder = true;
            m_voronoiSegments[voronoiMap[x,m_mapChunkSize-1]].height = 1f;
            m_voronoiSegments[voronoiMap[x,m_mapChunkSize-1]].isBorder = true;
        }

    }

    public List<Vector3> GetCentroidsInWorldPos()
    {
        List<Vector3> centroidWorldPos = new List<Vector3>();
        for (int i = 0; i < m_voronoiCentroids.Length; i++)
        {
            Vector2Int centroidPos = m_voronoiCentroids[i];
            float height = QueryHeightAtCoords(centroidPos.x, centroidPos.y, out Vector3 relWorldPos);
            Vector3 worldPos = transform.position + relWorldPos;
            centroidWorldPos.Add(worldPos);
        }
        return centroidWorldPos;
    }

    public Dictionary<int, List<int>> GetNeighbourMap()
    {
        return m_voronoiSegmentNeighborMap;
    }

}
