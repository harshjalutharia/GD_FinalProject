using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombinedMap : NoiseMap
{
    public enum CombineMethod { Set, Add, Subtract, Multiply, Divide }
    
    [System.Serializable]
    public class CombineItem {
        public NoiseMap map;
        public CombineMethod combineMethod = CombineMethod.Multiply;
        public bool apply = true;
    }

    [Header("=== Combined Map Settings ===")]
    [SerializeField] protected List<CombineItem> m_maps;

    public override void GenerateMap() {
        m_prng = new System.Random(m_seed);
        m_noiseMap = Generators.GenerateFloatMap(m_mapChunkSize, m_mapChunkSize, 0f);
        m_heightMap = Generators.GenerateFloatMap(m_mapChunkSize, m_mapChunkSize, 0f);

        foreach(CombineItem ci in m_maps) {
            if (!ci.apply) continue;
            ci.map.SetSeed(m_seed);
            ci.map.SetDimensions(m_dimensions);
            ci.map.SetChunkSize(m_mapChunkSize);
            ci.map.SetLevelOfDetail(m_levelOfDetail);
            ci.map.GenerateMap();
            switch(ci.combineMethod) {
                case CombineMethod.Multiply:
                    m_noiseMap = Generators.MultiplyMap(m_mapChunkSize, m_mapChunkSize, m_noiseMap, ci.map.noiseMap);
                    m_heightMap = Generators.MultiplyMap(m_mapChunkSize, m_mapChunkSize, m_heightMap, ci.map.heightMap);
                    break;
                case CombineMethod.Add:
                    m_noiseMap = Generators.AddMap(m_mapChunkSize, m_mapChunkSize, m_noiseMap, ci.map.noiseMap);
                    m_heightMap = Generators.AddMap(m_mapChunkSize, m_mapChunkSize, m_heightMap, ci.map.heightMap);
                    break;
                case CombineMethod.Subtract:
                    m_noiseMap = Generators.SubtractMap(m_mapChunkSize, m_mapChunkSize, m_noiseMap, ci.map.noiseMap);
                    m_heightMap = Generators.SubtractMap(m_mapChunkSize, m_mapChunkSize, m_heightMap, ci.map.heightMap);
                    break;
                case CombineMethod.Divide:
                    m_noiseMap = Generators.DivideMap(m_mapChunkSize, m_mapChunkSize, m_noiseMap, ci.map.noiseMap);
                    m_heightMap = Generators.DivideMap(m_mapChunkSize, m_mapChunkSize, m_heightMap, ci.map.heightMap);
                    break;
                default: 
                    m_noiseMap = ci.map.noiseMap;
                    m_heightMap = ci.map.heightMap;
                    break;
            }
        }

        if (m_drawMode != DrawMode.None) RenderMap();

        LandmarkGenerator landmarkGenerator = GetComponent<LandmarkGenerator>();
        if (landmarkGenerator != null)
            landmarkGenerator.GenerateLandmarks();
    }
}
