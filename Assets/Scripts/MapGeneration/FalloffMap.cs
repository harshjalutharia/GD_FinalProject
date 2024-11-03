using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FalloffMap : NoiseMap
{
    [Header("=== Falloff Settings ===")]
    [SerializeField, Range(0f,1f)]  protected float m_falloffStart = 1f;
    [SerializeField, Range(0f,1f)]  protected float m_falloffEnd = 1f;

    public override void GenerateMap() {
        m_prng = new System.Random(m_seed);

        m_noiseMap = Generators.GenerateFalloffMap(
            m_mapChunkSize, m_mapChunkSize, 
            m_falloffStart, m_falloffEnd
        );
        m_heightMap = Generators.GenerateHeightMap(m_noiseMap, m_textureHeightCurve, m_textureHeightMultiplier);

        if (m_drawMode != DrawMode.None) RenderMap();
    }
}
