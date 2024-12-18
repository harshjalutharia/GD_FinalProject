using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PerlinNoiseMap : NoiseMap
{
    [Header("=== Perlin Noise Settings ===")]
    [SerializeField]                protected float m_noiseScale = 15f;
    [SerializeField]                protected int m_octaves = 4;
    [SerializeField, Range(0f,1f)]  protected float m_persistance = 0.5f;
    [SerializeField]                protected float m_lacunarity;

    public override void GenerateMap() {
        m_prng = new System.Random(m_seed);

        m_noiseMap = Generators.GenerateNoiseMap(
            m_mapChunkSize, m_mapChunkSize, m_noiseScale, m_prng, 
            m_octaves, m_persistance, m_lacunarity, m_offset,
            m_normalizeMode
        );
        m_heightMap = Generators.GenerateHeightMap(m_noiseMap, m_textureHeightCurve, m_textureHeightMultiplier);
        m_heightRange = GetHeightRange(m_heightMap);
        if (m_drawMode != DrawMode.None) RenderMap();
        m_onGenerationEnd?.Invoke();
    }

    protected override void OnValidate() {
        base.OnValidate();
        if (m_lacunarity < 1) m_lacunarity = 1;
        if (m_octaves < 0) m_octaves = 0;
    }
}
