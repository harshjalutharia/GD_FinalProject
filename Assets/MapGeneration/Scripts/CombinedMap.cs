using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombinedMap : NoiseMap
{
    public enum ApplyMethod { Off, Generate, Apply, GenerateApply }
    public enum CombineMethod { Set, Add, Subtract, Multiply, Divide }
    
    [System.Serializable]
    public class CombineItem {
        public NoiseMap map;
        public CombineMethod combineMethod = CombineMethod.Multiply;
        public ApplyMethod apply = ApplyMethod.GenerateApply;
    }

    [Header("=== Combined Map Settings ===")]
    [SerializeField] protected List<CombineItem> m_maps;

    public override void GenerateMap() {
        m_prng = new System.Random(m_seed);
        m_noiseMap = Generators.GenerateFloatMap(m_mapChunkSize, m_mapChunkSize, 0f);
        m_heightMap = Generators.GenerateFloatMap(m_mapChunkSize, m_mapChunkSize, 0f);

        foreach(CombineItem ci in m_maps) {
            if (ci.apply == ApplyMethod.Off) continue;
            if (ci.apply != ApplyMethod.Apply) {
                ci.map.SetSeed(m_seed);
                ci.map.SetChunkSize(m_mapChunkSize);
                ci.map.SetLevelOfDetail(m_levelOfDetail);
                ci.map.SetOffset(m_offset);
                ci.map.SetNormalizeMode(m_normalizeMode);
                ci.map.GenerateMap();
            }
            if (ci.apply == ApplyMethod.Generate) continue;
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

        // Normalize noise map to between 0 and 1
        m_noiseMap = NormalizeNoiseMap(m_noiseMap);
        // With the normalized noise map, we can now base the height map such that the smallest possible height is set to 0.
        m_heightMap = RecenterHeightMap(m_heightMap);
        // Now draw
        if (m_drawMode != DrawMode.None) RenderMap();
        // On generation end
        m_onGenerationEnd?.Invoke();
    }

    // This is just a carbon-copy of GenerateMap above, except compatible with coroutine logic
    public override IEnumerator GenerateMapCoroutine() {
        m_prng = new System.Random(m_seed);
        m_noiseMap = Generators.GenerateFloatMap(m_mapChunkSize, m_mapChunkSize, 0f);
        yield return null;
        m_heightMap = Generators.GenerateFloatMap(m_mapChunkSize, m_mapChunkSize, 0f);
        yield return null;

        foreach(CombineItem ci in m_maps) {
            if (ci.apply == ApplyMethod.Off) continue;
            if (ci.apply != ApplyMethod.Apply) {
                ci.map.SetSeed(m_seed);
                ci.map.SetChunkSize(m_mapChunkSize);
                ci.map.SetLevelOfDetail(m_levelOfDetail);
                ci.map.SetOffset(m_offset);
                ci.map.SetNormalizeMode(m_normalizeMode);
                ci.map.GenerateMap();
            }
            if (ci.apply == ApplyMethod.Generate) {
                yield return null;
                continue;
            }
            switch(ci.combineMethod) {
                case CombineMethod.Multiply:
                    m_noiseMap = Generators.MultiplyMap(m_mapChunkSize, m_mapChunkSize, m_noiseMap, ci.map.noiseMap);
                    yield return null;
                    m_heightMap = Generators.MultiplyMap(m_mapChunkSize, m_mapChunkSize, m_heightMap, ci.map.heightMap);
                    yield return null;
                    break;
                case CombineMethod.Add:
                    m_noiseMap = Generators.AddMap(m_mapChunkSize, m_mapChunkSize, m_noiseMap, ci.map.noiseMap);
                    yield return null;
                    m_heightMap = Generators.AddMap(m_mapChunkSize, m_mapChunkSize, m_heightMap, ci.map.heightMap);
                    yield return null;
                    break;
                case CombineMethod.Subtract:
                    m_noiseMap = Generators.SubtractMap(m_mapChunkSize, m_mapChunkSize, m_noiseMap, ci.map.noiseMap);
                    yield return null;
                    m_heightMap = Generators.SubtractMap(m_mapChunkSize, m_mapChunkSize, m_heightMap, ci.map.heightMap);
                    yield return null;
                    break;
                case CombineMethod.Divide:
                    m_noiseMap = Generators.DivideMap(m_mapChunkSize, m_mapChunkSize, m_noiseMap, ci.map.noiseMap);
                    yield return null;
                    m_heightMap = Generators.DivideMap(m_mapChunkSize, m_mapChunkSize, m_heightMap, ci.map.heightMap);
                    yield return null;
                    break;
                default: 
                    m_noiseMap = ci.map.noiseMap;
                    m_heightMap = ci.map.heightMap;
                    yield return null;
                    break;
            }
            yield return null;
        }

        // Normalize noise map to between 0 and 1
        m_noiseMap = NormalizeNoiseMap(m_noiseMap);
        // With the normalized noise map, we can now base the height map such that the smallest possible height is set to 0.
        m_heightMap = RecenterHeightMap(m_heightMap);
        // Now draw
        if (m_drawMode != DrawMode.None) RenderMap();
        // On generation end
        m_onGenerationEnd?.Invoke();
    }

    private float[,] NormalizeNoiseMap(float[,] data) {
        int width = data.GetLength(0);
        int height = data.GetLength(1);

        // Determine noise range based on min max logic
        MinMax noiseRange = GetHeightRange(m_noiseMap);

        // With min and max determined, normalize data
        float[,] newNoiseMap = new float[width,height];
        float divisor = noiseRange.max - noiseRange.min;
        if (divisor == 0) divisor = 0.0001f;
        for(int y = 0; y < height; y++) {
            for(int x = 0; x < width; x++) {
                newNoiseMap[x,y] = (data[x,y] - noiseRange.min) / divisor;
            }
        }

        return newNoiseMap;
    }

    private float[,] RecenterHeightMap(float[,] data) {
        int width = data.GetLength(0);
        int height = data.GetLength(1);
        
        // Get min and max of height
        m_heightRange = GetHeightRange(m_heightMap);

        // With min and max determined, normalize data
        float[,] newHeightMap = new float[width,height];
        for(int y = 0; y < height; y++) {
            for(int x = 0; x < width; x++) {
                newHeightMap[x,y] = (data[x,y] - m_heightRange.min) * m_textureHeightMultiplier * m_textureHeightCurve.Evaluate(m_noiseMap[x,y]);
            }
        }

        // re-calculate the min and max again to account for the change.
        m_heightRange = GetHeightRange(newHeightMap);

        return newHeightMap;
    }
}
