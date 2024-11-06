using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class CityMap : NoiseMap
{
    //https://antoniosliapis.com/articles/pcgbook_dungeons.php
    public enum CA_Neighborhood { Moore, Neumann }
    
    [Header("=== City Map Generation ===")]
    [SerializeField] private CA_Neighborhood m_neighborhoodType = CA_Neighborhood.Moore;
    [SerializeField] private int m_neighborhoodThreshold = 5;
    [SerializeField] private int m_numIterations = 2;

    private static Vector2Int[] m_mooreIndices = new Vector2Int[8] {
        new Vector2Int(-1,-1),
        new Vector2Int(0,-1),
        new Vector2Int(1,-1),
        new Vector2Int(-1,0),
        new Vector2Int(1,0),
        new Vector2Int(-1,1),
        new Vector2Int(0,1),
        new Vector2Int(1,1)
    };
    private static Vector2Int[] m_neumannIndices = new Vector2Int[4] {
        new Vector2Int(0,-1),
        new Vector2Int(-1,0),
        new Vector2Int(1,0),
        new Vector2Int(0,1)
    };

    public override void GenerateMap() {
        // Step 0: Generate the prng system
        m_prng = new System.Random(m_seed);

        // Step 1: Generate a random noise map.
        m_noiseMap = Generators.GenerateRandomMap(m_mapChunkSize, m_mapChunkSize, m_prng);

        // Step 2: We classify the different regions using the terrain map system
        int[,] regionMap = GenerateRegions(m_noiseMap, m_terrainTypes);

        // Step 3: We use cellular automata processing to generate regions for real - the original regionMap is a random distribution
        int[,] combinedMap = regionMap;
        for(int i = 0; i < m_numIterations; i++) {
            combinedMap = CombineRegions(combinedMap, m_neighborhoodType, m_neighborhoodThreshold);
        }

        // Finally, need to convert from combinedMap back to noiseMap
        m_noiseMap = ConvertCombinedToTerrain(combinedMap);

        // Step Final: Render
        m_heightMap = Generators.GenerateHeightMap(m_noiseMap, m_textureHeightCurve, m_textureHeightMultiplier);
        if (m_drawMode != DrawMode.None) RenderMap();
    }

    private int[,] GenerateRegions(float[,] data, TerrainType[] types) {
        int width = data.GetLength(0);
        int height = data.GetLength(1);
        int[,] regionMap = new int[width, height];
        for(int y = 0; y < height; y++) {
            for(int x = 0; x < width; x++) {
                float currentHeight = data[x,y];
                for(int i = 0; i < types.Length; i++) {
                    if (currentHeight <= types[i].height) {
                        regionMap[x,y] = i;
                        break;
                    }
                }
            }
        }
        return regionMap;
    }

    private int[,] CombineRegions(int[,] data, CA_Neighborhood neighborhoodType, int threshold) {
        int width = data.GetLength(0);
        int height = data.GetLength(1);
        int[,] combined = new int[width, height];
        Vector2Int[] neighborIndices = neighborhoodType == CA_Neighborhood.Moore ? m_mooreIndices : m_neumannIndices;
        for(int y = 0; y < height; y++) {
            for(int x = 0; x < width; x++) {
                int cityCount = 0;
                foreach(Vector2Int ni in neighborIndices) {
                    int nx = x + ni.x;
                    int ny = y + ni.y;
                    if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;
                    if (data[nx,ny] == 1) cityCount += 1;
                }
                combined[x,y] = (cityCount >= threshold) ? 1 : 0;
            }
        }
        return combined;
    }

    private float[,] ConvertCombinedToTerrain(int[,] data) {
        int width = data.GetLength(0);
        int height = data.GetLength(1);
        float[,] converted = new float[width, height];
        for(int y = 0; y < height; y++) {
            for(int x = 0; x < width; x++) {
                converted[x,y] = m_terrainTypes[data[x,y]].height;
            }
        }
        return converted;
    }
}
