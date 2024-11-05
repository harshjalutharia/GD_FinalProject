using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CityMap : NoiseMap
{
    //https://antoniosliapis.com/articles/pcgbook_dungeons.php
    public enum CA_Neighborhood { Moore, Neumann }
    
    [Header("=== City Map Generation ===")]
    [SerializeField] private CA_Neighborhood m_neighborhoodType = CA_Neighborhood.Moore;
    [SerializeField] private int m_neighborhoodThreshold = 5;
    [SerializeField] private int m_numIterations = 2;

    public override void GenerateMap() {
        // Step 0: Generate the prng system
        m_prng = new System.Random(m_seed);

        // Step 1: Generate a random noise map.
        m_noiseMap = Generators.GenerateRandomMap(m_mapChunkSize, m_mapChunkSize, m_prng);

        // Step 2: We classify the different regions using the terrain map system
        int[,] regionMap = GenerateRegions(m_noiseMap, m_terrainTypes);

        // Step 3: We use cellular automata processing to generate regions for real - the original regionMap is a random distribution


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

    /*
    public static void MooreNeighborIndices(int x, int y, int maxX, int maxY) {
        List<Vector2Int> indices = new List<Vector2Int>();
        for(int yi = y-1; yi <= y+1; yi++) {
            for(int xi = x-1; xi <= x+1; xi++) {
                if (xi < )
            }
        }
    }
    */
}
