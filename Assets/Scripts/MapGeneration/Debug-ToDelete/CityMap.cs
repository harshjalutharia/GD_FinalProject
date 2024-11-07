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

    [Header("=== City Map Outputs - READ ONLY ===")]
    [SerializeField] private RegionCell[,] m_regionMap;

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

        // Step 1: Generate the region map
        m_regionMap = GenerateVoidMap(m_prng, m_mapChunkSize, m_mapChunkSize, m_neighborhoodType);

        // Step 2: Iterate and allow the CA to work
        RegionCell[,] iteratedRegionMap = IterateRegionMap(m_regionMap, m_numIterations);

        m_noiseMap = ConvertRegionToTerrain(iteratedRegionMap);

        /*

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
        */

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

    protected virtual RegionCell[,] GenerateVoidMap(System.Random prng, int width, int height, CA_Neighborhood neighborhoodType) {
        // Identify the seeds
        Vector2Int  duneSeed = Vector2Int.zero, 
                    forestSeed = Vector2Int.zero;

        duneSeed = new Vector2Int(
            prng.Next(0, width),
            prng.Next(0, height)
        );
        do {
            forestSeed = new Vector2Int(
                prng.Next(0, width),
                prng.Next(0, height)
            );
        } while (forestSeed == duneSeed);

        RegionCell[,] voidMap = new RegionCell[width,height];
        for(int y = 0; y < height; y++) {
            for(int x = 0; x < width; x++) {
                voidMap[x,y] = new RegionCell();
                int regionType = 0;

                if (x == duneSeed.x && y == duneSeed.y) regionType = 1;
                else if (x == forestSeed.x && y == forestSeed.y) regionType = 2;
                voidMap[x,y].InitializeCell(x,y,regionType,neighborhoodType);
            }
        }

        voidMap[duneSeed.x,duneSeed.y].cellTypeIndex = 1;
        voidMap[duneSeed.x,duneSeed.y].neighborhoodType = CA_Neighborhood.Moore;
        voidMap[forestSeed.x,forestSeed.y].cellTypeIndex = 2;
        voidMap[forestSeed.x,forestSeed.y].neighborhoodType = CA_Neighborhood.Neumann;

        return voidMap;
    } 

    protected virtual RegionCell[,] IterateRegionMap(RegionCell[,] data, int numIterations) {
        int width = data.GetLength(0);
        int height = data.GetLength(1);

        RegionCell[,] iteratedRegionMap = data;
        for(int iter = 0; iter < numIterations; iter++) {
            for(int y = 0; y < height; y++) {
                for(int x = 0; x < width; x++) {
                    iteratedRegionMap[x,y].UpdateCell(data);
                }
            }
        }
        return iteratedRegionMap;
    } 

    protected virtual float[,] ConvertRegionToTerrain(RegionCell[,] data) {
        int width = data.GetLength(0);
        int height = data.GetLength(1);
        float[,] converted = new float[width, height];
        for(int y = 0; y < height; y++) {
            for(int x = 0; x < width; x++) {
                converted[x,y] = m_terrainTypes[data[x,y].cellTypeIndex].height;
            }
        }
        return converted;
    }


    // Types:
    //  0: void
    //  1: dune
    //  2: forest 

    [System.Serializable]
    public class RegionCell {
        public int x, y;                            // The XY coordiantes on the grid
        public int cellTypeIndex;                   // The corresponding index to the cell type
        public CA_Neighborhood neighborhoodType;    // What kind of neighborhood do we want to consider?
        public int age;                             // how many iterations has this cell existed?

        public virtual void InitializeCell(int x, int y, int cellTypeIndex, CA_Neighborhood neighborhoodType) {
            this.x = x;
            this.y = y;
            this.cellTypeIndex = cellTypeIndex;
            this.neighborhoodType = neighborhoodType;
            this.age = 0;
        } 

        public virtual void UpdateCell(RegionCell[,] data) {
            switch(this.cellTypeIndex) {
                case 1:
                    UpdateDuneCell(data);
                    break;
                case 2:
                    UpdateForestCell(data);
                    break;
                default: break;
            }
            this.age += 1;
        }

        public virtual void UpdateDuneCell(RegionCell[,] data) {
            Vector2Int[] neighborIndices = this.neighborhoodType == CA_Neighborhood.Moore ? m_mooreIndices : m_neumannIndices;
            int width = data.GetLength(0);
            int height = data.GetLength(1);
            foreach(Vector2Int ni in neighborIndices) {
                int nx = this.x+ni.x;
                int ny = this.y+ni.y;
                if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;
                if (data[nx,ny].cellTypeIndex == 0) data[nx,ny].SetCellType(1);
            }
        }
        
        public virtual void UpdateForestCell(RegionCell[,] data) {
            if (this.age < 10) return;
            Vector2Int[] neighborIndices = this.neighborhoodType == CA_Neighborhood.Moore ? m_mooreIndices : m_neumannIndices;
            int width = data.GetLength(0);
            int height = data.GetLength(1);
            foreach(Vector2Int ni in neighborIndices) {
                int nx = this.x+ni.x;
                int ny = this.y+ni.y;
                if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;
                float number = UnityEngine.Random.Range(0.0f, 1.0f);
                if (number > 0.1f) continue;
                if (data[nx,ny].cellTypeIndex == 1 && data[nx,ny].age > 50) data[nx,ny].SetCellType(2);
            }
        }

        public virtual void SetCellType(int newIndex) {
            this.cellTypeIndex = newIndex;
            this.age = 0;
        }
    }
}
