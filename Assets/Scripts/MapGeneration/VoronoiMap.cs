using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoronoiMap : NoiseMap
{

    [Header("=== Voronoi Map Generation ===")]
    [SerializeField] private Vector2Int[] m_voronoiCenters;
    [SerializeField] private int m_edgeBuffer = 20;
    [SerializeField, Range(0,10)] private int m_lloydRelaxationIterations = 2;

    public override void GenerateMap() {
        m_prng = new System.Random(m_seed);

        // generate the terrain types
        for(int i = 0; i < m_terrainTypes.Length; i++) {
            string name = $"Region {i+1}";
            float height = (float)(i+1)/(float)m_terrainTypes.Length;
            Color color =  new Color(
                (float)m_prng.Next(0, 100)/100f, 
                (float)m_prng.Next(0, 100)/100f,
                (float)m_prng.Next(0, 100)/100f
            );
            m_terrainTypes[i] = new TerrainType { name=name, height=height, color=color };
        }

        // In this first run, we generate the voronoi regions first, using pure randomness.
        Vector2Int[] centers = GenerateVoronoiPoints();
        float[,] voronoiMap = GenerateVoronoiRegions(centers);

        // Then we iteratively perform Lloyd Relaxation N times to make the regions a bit more... clumpy
        for (int i = 0; i < m_lloydRelaxationIterations; i++) {
            centers = LloydRelaxation(centers.Length, voronoiMap);
            voronoiMap = GenerateVoronoiRegions(centers);
        }

        // Finalize the voronoi centers and noise map
        m_voronoiCenters = centers;
        m_noiseMap = voronoiMap; 
        m_heightMap = Generators.GenerateHeightMap(m_noiseMap, m_textureHeightCurve, m_textureHeightMultiplier);
        if (m_drawMode != DrawMode.None) RenderMap();
    }

    private Vector2Int[] GenerateVoronoiPoints() {        
        List<Vector2Int> existingPoints = new List<Vector2Int>();
        while(existingPoints.Count < m_terrainTypes.Length) {
            Vector2Int pointIndices = new Vector2Int(
                m_prng.Next(m_edgeBuffer, m_mapChunkSize-m_edgeBuffer),
                m_prng.Next(m_edgeBuffer, m_mapChunkSize-m_edgeBuffer)
            );
            if (!existingPoints.Contains(pointIndices)) existingPoints.Add(pointIndices);
        }

        return existingPoints.ToArray();
    }

    private Vector2Int[] LloydRelaxation(int numRegions, float[,] data) {
        Vector2Int[] regionPositionSums = new Vector2Int[numRegions];
        int[] regionCounts = new int[numRegions];

        for(int y = 0; y < m_mapChunkSize; y++) {
            for(int x = 0; x < m_mapChunkSize; x++) {
                // Iterate through regions, check which region index we're actually in
                float currentHeight = data[x,y];
                int regionIndex = 0;
                for(int i = 0; i < m_terrainTypes.Length; i++) {
                    if (currentHeight <= m_terrainTypes[i].height) {
                        regionIndex = i;
                        break;
                    }
                }
                // Knowing the region index, we can cumulate the height and count of that region
                regionPositionSums[regionIndex] += new Vector2Int(x,y);
                regionCounts[regionIndex] += 1;        
            }
        }

        // Now, re-position the centers
        Vector2Int[] newCenters = new Vector2Int[numRegions];
        for(int j = 0; j < numRegions; j++) {
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

    private float[,] GenerateVoronoiRegions(Vector2Int[] centers) {
        float[,] regionMap = new float[m_mapChunkSize, m_mapChunkSize];
        for(int y = 0; y < m_mapChunkSize; y++) {
            for(int x = 0; x < m_mapChunkSize; x++) {
                Vector2Int index = new Vector2Int(x,y);
                int closestIndex = 0;
                float closestDistance = Vector2Int.Distance(index, centers[0]);
                for(int i = 1; i < centers.Length; i++) {
                    float dist = Vector2Int.Distance(index, centers[i]);
                    if (dist < closestDistance) {
                        closestIndex = i;
                        closestDistance = dist;
                    }
                }
                regionMap[x,y] = m_terrainTypes[closestIndex].height;          
            }
        }
        return regionMap;
    }

}
