using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FalloffMap : NoiseMap
{
    [Header("=== Falloff Settings ===")]
    [SerializeField, Tooltip("The centroid X position where the falloff centers around, relative to -1f to 1f"), Range(-1f, 1f)]    private float m_centroidX = 0f;
    [SerializeField, Tooltip("The centroid Y position where the falloff centers around, relative to -1f to 1f"), Range(-1f, 1f)]    private float m_centroidY = 0f;
    [SerializeField, Tooltip("The type of shape the falloff map should be")]                            private Generators.FalloffShape m_falloffShape = Generators.FalloffShape.Box;
    [SerializeField, Range(0f,2f)]  protected float m_falloffStart = 1f;
    [SerializeField, Range(0f,2f)]  protected float m_falloffEnd = 1f;
    [SerializeField]                protected bool m_invert = false;

    public override void GenerateMap() {
        // Set the random number generator
        m_prng = new System.Random(m_seed);

        m_noiseMap = Generators.GenerateFalloffMap(
            m_mapChunkSize, m_mapChunkSize,
            m_centroidX, m_centroidY, m_falloffShape, 
            m_falloffStart, m_falloffEnd, m_invert
        );
        m_heightMap = Generators.GenerateHeightMap(m_noiseMap, m_textureHeightCurve, m_textureHeightMultiplier);
        m_heightRange = GetHeightRange(m_heightMap);
        if (m_drawMode != DrawMode.None) RenderMap();
    }

}
