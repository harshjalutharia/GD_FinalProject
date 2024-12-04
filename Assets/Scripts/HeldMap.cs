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
        public FilterMode filterMode;
        public TextureWrapMode wrapMode;
        public Color baseColor = Color.clear;
        [Space]
        // public Renderer renderer;
        public List<RawImage> images = new List<RawImage>();
        //[HideInInspector] public Material material;
        [HideInInspector] public Texture2D texture;
        
        public void SetTexture(Texture2D texture) {
            this.texture = texture;
            //if (this.renderer != null)  this.renderer.sharedMaterial.mainTexture = FlipTexture(texture, true, true);
            foreach(RawImage image in this.images) image.texture = texture;
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

            //if (this.renderer != null) this.renderer.sharedMaterial.mainTexture = FlipTexture(this.texture, true, true);
            foreach(RawImage image in this.images) image.texture = this.texture;
        }
        public void AddCircleToTexture(float x, float y, float radius, Color color) {
            this.AddCircleToTexture(Mathf.RoundToInt(x), Mathf.RoundToInt(y), Mathf.RoundToInt(radius), color);
        }
        public void AddCircleToTexture(float x, float y, int radius, Color color) {
            this.AddCircleToTexture(Mathf.RoundToInt(x), Mathf.RoundToInt(y), radius, color);
        }

        public void AddBoxToTexture(int x, int y, int w, int h, Color color) {
            int width = this.texture.width;
            int rows = this.texture.height;
    
            for (int u = x - w; u < x + w + 1; u++) {
                for (int v = y - h; v < y + h + 1; v++) {
                    if (u >= 0 && u < width && v >= 0 && v < rows) this.texture.SetPixel(u, v, color);
                }
            }
            this.texture.Apply();

            //if (this.renderer != null) this.renderer.sharedMaterial.mainTexture = FlipTexture(this.texture, true, true);
            foreach (RawImage image in this.images) image.texture = this.texture;
        }
    }

    [Header("=== Global settings ===")]
    [SerializeField, Tooltip("Output texture width")]   private int m_width;
    [SerializeField, Tooltip("Output texture height")]  private int m_height;
    //[SerializeField, Tooltip("The material used as a baseline")]    private Material m_baseMaterial;
    [SerializeField, Tooltip("The canvas parent of all images")]    private RectTransform m_imageParent;
    private Vector2 m_pivotPosition;
    

    [Header("=== Smoothdamp Lerp ===")]
    [SerializeField, Tooltip("Should we lerp the images with respect to the player's speed?")]  private bool m_lerpWithPlayerSpeed = true;
    [SerializeField, Tooltip("Animation curve to control the position of the image parent w.r.t. speed")]   private AnimationCurve m_lerpCurve;
    [SerializeField, Tooltip("The max speed threshold to consider for the lerp")]   private float m_maxLerpSpeed = 15f;
    [SerializeField, Tooltip("Because Player Movement hides everything aobut speed, we need to check ourselves")]    private Rigidbody m_playerRigidbody;
    [SerializeField, Tooltip("Smoother transition by weighted averaging between previous speed and current speed. 0 = previous speed, 1 = current speed"), Range(0f,1f)]    private float m_lerpWeight = 0.5f;
    private float m_prevRatio = 0f;

    [Header("=== Rotation ===")]
    [SerializeField, Tooltip("Should the map rotate to always point in the same direction as the player's orientation?")]   private bool m_rotateWithPlayer = false;
    [SerializeField, Tooltip("The transform reference that represents the forward direction")]                              private Transform m_forwardRef;

    [Header("=== Maps ===")]
    [SerializeField, Tooltip("The list of maps we want to render")] private List<MapGroup> m_mapGroups;
    private Dictionary<string, MapGroup> m_groupDictionary;
    private List<RawImage> m_images;

    private void Awake() {
        current = this;
    }

    private void OnEnable() {
        // Set up the dictionary that stores all our map groups for reference
        m_groupDictionary = new Dictionary<string, MapGroup>();
        m_images = new List<RawImage>();

        // Get the pivot position of the parent of the images
        m_pivotPosition = m_imageParent.anchoredPosition;

        foreach(MapGroup group in m_mapGroups) {
            if (!m_groupDictionary.ContainsKey(group.name)) {
                m_groupDictionary.Add(group.name, group);
                InitializeMapGroup(group);
                m_images.AddRange(group.images);
            }
        }
    }

    private void InitializeMapGroup(MapGroup group) {
        // Initialize material and texture
        //group.material = new Material(m_baseMaterial);
        group.texture = GenerateColorTexture(m_width, m_height, group.baseColor, group.filterMode, group.wrapMode);
        // Populate renderer and image, if present
        /*
        if (group.renderer != null) {
            group.renderer.sharedMaterial = group.material;
            group.renderer.sharedMaterial.mainTexture = group.texture;
        }
        */
        if (group.images.Count > 0) foreach(RawImage image in group.images) image.texture = group.texture;
    }

    private void LateUpdate() {
        if (m_lerpWithPlayerSpeed) {
            Vector2 horizontalVelocity = new Vector2(m_playerRigidbody.velocity.x, m_playerRigidbody.velocity.z);
            float currentSpeed = horizontalVelocity.magnitude;
            float currentRatio = m_lerpCurve.Evaluate(currentSpeed/m_maxLerpSpeed);
            m_imageParent.anchoredPosition = Vector2.Lerp(m_pivotPosition, Vector2.zero, currentRatio*m_lerpWeight + m_prevRatio*(1f-m_lerpWeight));
            m_prevRatio = currentRatio;
        }
        if (m_rotateWithPlayer) {
            Quaternion rotation = Quaternion.Euler(0f, 0f, Vector3.SignedAngle(Vector3.forward, m_forwardRef.forward, Vector3.up));
            foreach(RawImage image in m_images) image.rectTransform.rotation = rotation;
        }
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

    public bool TryAddCircleToMap(string groupName, Vector3 position, int radius, Color color) {
        if (!m_groupDictionary.ContainsKey(groupName)) {
            Debug.Log($"Cannot add circle to group {groupName}");
            return false;
        }
        m_groupDictionary[groupName].AddCircleToTexture(position.x, position.z, radius, color);
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
    public static Texture2D FlipTexture(Texture2D originalTexture, bool horizontal, bool vertical) {
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
