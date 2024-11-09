using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoronoiMap : NoiseMap
{
    // Voronoi tessellation requires that we define a set number of centroids within some XxY space.
    // We then use Lloyd Relaxation to make these clusters a bit more rounded and clumpy


    [Header("=== Voronoi Map Generation ===")]
    [SerializeField, Tooltip("The number of intended segments we want to generate.")]                                           private int m_numSegments;
    [SerializeField, Tooltip("Designate a buffer zone from the map edges where centroids cannot be spawned.")]                  private int m_edgeBuffer = 20;
    [SerializeField, Tooltip("The number of times Lloyd Relaxation is conducted to 'clump' the regions"), Range(0,10)]          private int m_lloydRelaxationIterations = 2;
    [SerializeField, Tooltip("Do we make the edge regions feel like cliffs?")]                                                  private bool m_edgeBorder = true;

    [Header("=== Voronoi Outputs - READ-ONLY ===")]
    [SerializeField, Tooltip("The different voronoi segments generated.")]          private TerrainType[] m_voronoiSegments;
    [SerializeField, Tooltip("The centroids of the generated voronoi segments.")]   private Vector2Int[] m_voronoiCentroids;
    [SerializeField, Tooltip("The lookup dictionary to map segments to pixels")]    private Dictionary<int, List<Vector2Int>> m_voronoiSegmentToPixelsMap;

    public override void GenerateMap() {
        // Initialize the pseudo-random number generator
        m_prng = new System.Random(m_seed);

        // Conduct some checks: 1) the number of segments is at least 1, and 2) the edge buffer is smaller than half the map chunk size
        if (m_numSegments <= 0) m_numSegments = 1;
        if ((float)m_edgeBuffer >= (float)m_mapChunkSize/2f) m_edgeBuffer = Mathf.FloorToInt((float)(m_mapChunkSize-1)/2f);

        // generate the terrain types
        float heightDenom = (m_edgeBorder) ? (float)(m_numSegments+1) : (float)m_numSegments;
        m_voronoiSegments = new TerrainType[m_numSegments];
        for(int i = 0; i < m_numSegments; i++) {
            string name = $"Region {i+1}";
            float height = (float)(i+1)/heightDenom;
            Color color =  new Color(
                (float)m_prng.Next(0, 100)/100f, 
                (float)m_prng.Next(0, 100)/100f,
                (float)m_prng.Next(0, 100)/100f
            );
            m_voronoiSegments[i] = new TerrainType { name=name, height=height, color=color };
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
        if (m_edgeBorder) {
            for(int y = 0; y < m_mapChunkSize; y++) {
                m_voronoiSegments[voronoiMap[0,y]].height = 1f;
                m_voronoiSegments[voronoiMap[m_mapChunkSize-1,y]].height = 1f;
            }
            for(int x = 0; x < m_mapChunkSize; x++) {
                m_voronoiSegments[voronoiMap[x,0]].height = 1f;
                m_voronoiSegments[voronoiMap[x,m_mapChunkSize-1]].height = 1f;
            }
        }

        // At this point, it's a good idea to generate a lookup table of some kind - namely, one that'll
        //  allow us to associate voronoi segments with their pixels
        m_voronoiSegmentToPixelsMap = GenerateRegionLookup(voronoiMap);
        
        // Finalize the voronoi centroids
        m_voronoiCentroids = centroids;

        // The generated noisemap needs to be converted from an int[,] to a float[,] based on region
        m_noiseMap = GenerateNoiseMapFromVoronoi(voronoiMap);
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

    private Dictionary<int, List<Vector2Int>> GenerateRegionLookup(int[,] data) {
        Dictionary<int, List<Vector2Int>> lookupTable = new Dictionary<int, List<Vector2Int>>();
        for(int i = 0; i < m_numSegments; i++) lookupTable.Add(i, new List<Vector2Int>());
        for(int y = 0; y < m_mapChunkSize; y++) {
            for(int x = 0; x < m_mapChunkSize; x++) {
                Vector2Int cellIndex = new Vector2Int(x,y);
                int regionIndex = data[x,y];
                lookupTable[regionIndex].Add(cellIndex);
            }
        }
        return lookupTable;
    }

    private float[,] GenerateNoiseMapFromVoronoi(int[,] data) {
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

}
