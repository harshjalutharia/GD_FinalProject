using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class HeldMap : MonoBehaviour 
{
    public static HeldMap current;
    const TextureFormat transparentTextureFormat = TextureFormat.ARGB32;

    [Header("=== Global settings ===")]
    [SerializeField, Tooltip("The width of the generated texture")]     private int m_width;
    [SerializeField, Tooltip("The height of the generated texture")]    private int m_height;
    [SerializeField, Tooltip("The material used as a baseline")]        private Material m_baseMaterial;
    [Space]
    [SerializeField, Tooltip("Filter mode for the generated texture")]  private FilterMode m_filterMode;
    [SerializeField, Tooltip("The base texture to be generated")]       private Texture2D m_baseTexture;
    
    [Header("=== Terrain Map ===")]
    [SerializeField, Tooltip("Renderer for the terrain map")]               private Renderer m_terrainMap;
    [SerializeField, Tooltip("The texture generated for the terrain")]      private Texture2D m_terrainTexture;

    [Header("=== Landmark Map ===")]
    [SerializeField, Tooltip("Renderer for the landmark map")]              private Renderer m_landmarkMap;
    [SerializeField, Tooltip("The texture generated for the landmarks")]    private Texture2D m_landmarkTexture;

    [Header("=== Gem Map ===")]
    [SerializeField, Tooltip("Renderer for the gem map")]                   private Renderer m_gemMap;
    [SerializeField, Tooltip("The texture generated for the gems")]         private Texture2D m_gemTexture;

    private void Awake() {
        current = this;
    }

    private void OnEnable() {
        // For each renderer, we generate the base texture. Originally, it's a transparent texture.
        m_baseTexture = new Texture2D(m_width, m_height);
        m_baseTexture.filterMode = m_filterMode;
        m_baseTexture.wrapMode = TextureWrapMode.Clamp;
        //texture.SetPixels(colorMap);
        m_baseTexture.Apply();
    }

    public static Texture2D GenerateTransparentTexture(int width, int height, FilterMode filterMode) {
        Color fillColor = Color.clear;
		Color[] fillPixels = new Color[width * height];
		for (int i = 0; i < fillPixels.Length; i++) fillPixels[i] = fillColor;

        Texture2D texture = new Texture2D (width, height, transparentTextureFormat, false);
        texture.filterMode = filterMode;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels(fillPixels);
        texture.Apply();
        return texture;
    }

    public static Texture2D GenerateTextureFromColormap(int width, int height,FilterMode filterMode, Color[] colorMap) {        
        Texture2D texture = new Texture2D (width, height, transparentTextureFormat, false);
        texture.filterMode = filterMode;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels(colorMap);
        texture.Apply();
        return texture;
    }
}
