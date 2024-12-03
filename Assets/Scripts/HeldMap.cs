using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class HeldMap : MonoBehaviour 
{
    public static HeldMap current;
    const TextureFormat transparentTextureFormat = TextureFormat.RGBA32;

    [System.Serializable]
    public class MapGroup {
        public string name;
        public int width, height;
        public FilterMode filterMode;
        public TextureWrapMode wrapMode;
        public Color baseColor = Color.clear;
        [Space]
        public Renderer renderer;
        public RawImage image;
        public Material material;
        public Texture2D texture;
        
        public void SetTexture(Texture2D texture) {
            this.texture = texture;
            if (this.renderer != null)  this.renderer.sharedMaterial.mainTexture = FlipTexture(texture, true, true);
            if (this.image != null)     this.image.texture = texture;
        }

        public void AddCircleToTexture(int x, int y, int radius, Color color) {
            int width = this.texture.width;
            int rows = this.texture.height;
            int rSquared = radius*radius;
    
            for (int u = x - radius; u < x + radius + 1; u++) {
                for (int v = y - radius; v < y + radius + 1; v++) {
                    if ((x - u) * (x - u) + (y - v) * (y - v) < rSquared) this.texture.SetPixel(u, v, color);
                }
            }
            this.texture.Apply();

            if (this.renderer != null) this.renderer.sharedMaterial.mainTexture = FlipTexture(this.texture, true, true);
            if (this.image != null) this.image.texture = this.texture;
        }
        public void AddCircleToTexture(float x, float y, float radius, Color color) {
            this.AddCircleToTexture(Mathf.RoundToInt(x), Mathf.RoundToInt(y), Mathf.RoundToInt(radius), color);
        }
        public void AddCircleToTexture(float x, float y, int radius, Color color) {
            this.AddCircleToTexture(Mathf.RoundToInt(x), Mathf.RoundToInt(y), radius, color);
        }

        public void AddBoxToTexture(int x, int y, int w, int h, Color color) {
            var width = this.texture.width;
            var rows = this.texture.height;
    
            for (int u = x - w; u < x + w + 1; u++) {
                for (int v = y - h; v < y + h + 1; v++) {
                    if (u >= 0 && u < width && v >= 0 && v < rows) this.texture.SetPixel(u, v, color);
                }
            }
            this.texture.Apply();

            if (this.renderer != null) this.renderer.sharedMaterial.mainTexture = FlipTexture(this.texture, true, true);
            if (this.image != null) this.image.texture = this.texture;
        }
    }

    [Header("=== Global settings ===")]
    [SerializeField, Tooltip("The material used as a baseline")]        private Material m_baseMaterial;

    [Header("=== Maps ===")]
    [SerializeField, Tooltip("The list of maps we want to render")] private List<MapGroup> m_mapGroups;
    private Dictionary<string, MapGroup> m_groupDictionary;

    private void Awake() {
        current = this;
    }

    private void OnEnable() {
        // Set up the dictionary that stores all our map groups for reference
        m_groupDictionary = new Dictionary<string, MapGroup>();

        foreach(MapGroup group in m_mapGroups) {
            if (!m_groupDictionary.ContainsKey(group.name)) {
                m_groupDictionary.Add(group.name, group);
                InitializeMapGroup(group);
            }
        }
    }

    private void InitializeMapGroup(MapGroup group) {
        // Initialize material and texture
        group.material = new Material(m_baseMaterial);
        group.texture = GenerateColorTexture(group.width, group.height, group.baseColor, group.filterMode, group.wrapMode);
        // Populate renderer and image, if present
        if (group.renderer != null) {
            group.renderer.sharedMaterial = group.material;
            group.renderer.sharedMaterial.mainTexture = group.texture;
        }
        if (group.image != null) group.image.texture = group.texture;
    }

    public void SetMapGroupTexture(string groupName, Texture2D texture) {
        if (!m_groupDictionary.ContainsKey(groupName)) {
            Debug.LogError($"Cannot render to map group {groupName} - no group name");
            return;
        }
        m_groupDictionary[groupName].SetTexture(texture);
    }

    public bool TryGetMapGroup(string groupName, out MapGroup group) {
        if (!m_groupDictionary.ContainsKey(groupName)) {
            group = null;
            return false;
        }
        group = m_groupDictionary[groupName];
        return true;
    }

    public static Texture2D GenerateTransparentTexture(int width, int height, FilterMode filterMode = FilterMode.Bilinear, TextureWrapMode wrapMode = TextureWrapMode.Clamp) {
        Color fillColor = Color.clear;
		Color[] fillPixels = new Color[width * height];
		for (int i = 0; i < fillPixels.Length; i++) fillPixels[i] = fillColor;

        Texture2D texture = new Texture2D (width, height, transparentTextureFormat, false);
        texture.filterMode = filterMode;
        texture.wrapMode = wrapMode;
        texture.SetPixels(fillPixels);
        texture.Apply();
        return texture;
    }

    public static Texture2D GenerateColorTexture(int width, int height, Color color, FilterMode filterMode = FilterMode.Bilinear, TextureWrapMode wrapMode = TextureWrapMode.Clamp) {
		Color[] fillPixels = new Color[width * height];
		for (int i = 0; i < fillPixels.Length; i++) fillPixels[i] = color;

        Texture2D texture = new Texture2D (width, height, transparentTextureFormat, false);
        texture.filterMode = filterMode;
        texture.wrapMode = wrapMode;
        texture.SetPixels(fillPixels);
        texture.Apply();
        return texture;
    }

    public static Texture2D GenerateColorMapTexture(int width, int height, Color[] colorMap, FilterMode filterMode = FilterMode.Bilinear, TextureWrapMode wrapMode = TextureWrapMode.Clamp) {        
        Texture2D texture = new Texture2D (width, height, transparentTextureFormat, false);
        texture.filterMode = filterMode;
        texture.wrapMode = wrapMode;
        texture.SetPixels(colorMap);
        texture.Apply();
        return texture;
    }

    // Create a new texture with the same dimensions as the original texture
    public static Texture2D FlipTexture(Texture2D originalTexture, bool horizontal, bool vertical)
    {
        // Create a new texture with the same dimensions as the original texture
        Texture2D flippedTexture = new Texture2D(originalTexture.width, originalTexture.height, originalTexture.format, false);

        // Loop through the pixels of the original texture
        for (int y = 0; y < originalTexture.height; y++) {
            for (int x = 0; x < originalTexture.width; x++) {
                // Get coords based on flip
                int newX = x;
                if (horizontal) newX = originalTexture.width - x - 1;
                int newY = y;
                if (vertical)   newY = originalTexture.height - y - 1;

                // Get the pixel from the original texture and set it in the new texture
                flippedTexture.SetPixel(newX, newY, originalTexture.GetPixel(x, y));
            }
        }

        // Apply the changes to the new texture
        flippedTexture.Apply();
        return flippedTexture;
    }
}
