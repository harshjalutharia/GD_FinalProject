using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[RequireComponent(typeof(Renderer))]
public class TerrainChunk : MonoBehaviour
{
    public enum LayerType { Random, Perlin, Falloff, PerlinFalloff }
    public enum FalloffType { Box, NorthSouth, EastWest, Circle }
    public enum ApplicationType { Off, Set, Add, Subtract, Multiply, Divide }

    const int textureSize = 512;
    const TextureFormat textureFormat = TextureFormat.RGB565;

    [System.Serializable]
    public class TerrainLayer {
        public string name;
        public LayerType layerType;
        public ApplicationType applicationType;
        [Space]
        public AnimationCurve heightCurve;
        public float heightMultiplier;

        [Header("Perlin Settings")]
        public float scale = 20f;

        [Header("Falloff Settings")]
        public FalloffType falloffType;
        [Range(0f,1f)] public float centerX = 0.5f;
        [Range(0f,1f)] public float centerY = 0.5f;
        [Range(0f,2f)] public float falloffStart;
        [Range(0f,2f)] public float falloffEnd;
        public Gradient falloffGradient;

        public virtual float GeneratePoint(System.Random prng, int x, int y, int width, int height, int offsetX, int offsetY) { 
            float value;
            switch(layerType) {
                case LayerType.Perlin:
                    value = GeneratePerlin(x, y, width, height, offsetX, offsetY);
                    break;
                case LayerType.Falloff:
                    value = GenerateFalloff(x, y, width, height);
                    break;
                case LayerType.PerlinFalloff:
                    value = GeneratePerlin(x, y, width, height, offsetX, offsetY);
                    value *= GenerateFalloff(x, y, width, height);
                    break;
                default:
                    value = GenerateRandom(prng);
                    break;
            }
            return heightCurve.Evaluate(value) * heightMultiplier;
        }

        public virtual float GenerateRandom(System.Random prng) {
            return (float)prng.Next(0,100)/100f;
        }

        public virtual float GeneratePerlin(int x, int y, int width, int height, int offsetX, int offsetY) {
            float xCoord = ((float)x / width + offsetX) * scale;
            float yCoord = ((float)y / height + offsetY) * scale;
            return Mathf.PerlinNoise(xCoord, yCoord);
        }

        public virtual float GenerateFalloff(int x, int y, int width, int height) {
            float xCoord = (float)x / width;
            float yCoord = (float)y / height;
            float t = 0f;
            switch(falloffType) {
                case FalloffType.NorthSouth:
                    t = Mathf.Abs(yCoord-centerY);
                    break;
                case FalloffType.EastWest:
                    t = Mathf.Abs(xCoord-centerX);
                    break;
                case FalloffType.Circle:
                    t = Vector2.Distance(new Vector2(centerX, centerY), new Vector2(xCoord, yCoord));
                    break;
                default:
                    t = Mathf.Max(Mathf.Abs(xCoord-centerX), Mathf.Abs(yCoord-centerY));
                    break;
            }
            float v = (t < falloffStart) 
                ? 1f : (t > falloffEnd) 
                    ? 0f : Mathf.SmoothStep(1f,0f,Mathf.InverseLerp(falloffStart, falloffEnd, t));
            return falloffGradient.Evaluate(v).r;
        }
    }

    [System.Serializable]
    public class TextureLayer {
        public string name;
        public Texture2D texture;
        public Color tint;
        [Range(0f,1f)] public float tintStrength;
        [Range(0f,1f)] public float blendStrength;
        public float startHeight;
        public float textureScale;
    }

    [System.Serializable]
    public class MinMax {
        public float min;
        public float max;
    }

    [Header("=== Map Settings ===")]
    [SerializeField] private int m_seed;
    [SerializeField, Tooltip("The visual (world) width of this map")]   private int m_width = 256;
    [SerializeField, Tooltip("The visual (world) height of this map")]  private int m_height = 256;
    public int width => m_width;
    public int height => m_height;
    public int gridWidth => m_width+1;    // The grid map width; +1 of world width due to vertices requiring one more point at the end
    public int gridHeight => m_height+1;  // The grid map height;
    [SerializeField, Tooltip("The LOD level used to control the mesh fidelity."), Range(0,6)] private int m_levelOfDetail;
    public int levelOfDetail => m_levelOfDetail;
    [SerializeField, Tooltip("The horizontal offset. Also is equivalent to its column index in the terrain grid")]  private int m_offsetX = 0;
    [SerializeField, Tooltip("The vertical offset. Also is equivalent to its row index in the terrain grid")]       private int m_offsetY = 0;
    [SerializeField, Tooltip("The terrain layers that generate the resulting nosie map")] private List<TerrainLayer> m_layers;
    [SerializeField, Tooltip("Do we use a custom noise map range or allow the system to estimate?")]    private bool m_manualNoiseRange = false;
    private System.Random m_prng;

    [Header("=== Renderer Settings ===")]
    [SerializeField] private Material m_meshMaterialSrc;
    private Material m_meshMaterial;
    [SerializeField] private List<TextureLayer> m_meshMaterialLayers;
    [Space]
    [SerializeField] private MeshFilter m_meshFilter;
    [SerializeField] private Renderer m_renderer;
    [SerializeField] private MeshCollider m_collider;
    [SerializeField] private FilterMode m_filterMode;

    [Header("=== Outputs - READ ONLY ==")]
    [SerializeField] private float[,] m_noiseMap;
    [SerializeField] private MinMax m_noiseRange;
    [SerializeField] private Texture2D m_textureData;
    [SerializeField] private MeshData m_meshData;

    [Header("=== Debug Settings ===")]
    [SerializeField] private bool m_generateOnStart = false;
    [SerializeField] private bool m_repositionOnGenerate = true;
    [SerializeField] private int m_coroutineNumThreshold = 20;
    [SerializeField] private bool m_autoUpdate = false;
    public bool autoUpdate => m_autoUpdate;

    public void SetSeed(string newSeed) {
        if (newSeed.Length > 0 && int.TryParse(newSeed, out int validNewSeed)) {
            m_seed = validNewSeed;
            return;
        }
        Debug.Log($"Provided seed {newSeed} not viable. Auto-generating new seed...");
        m_seed = UnityEngine.Random.Range(0, 1000001);
    }
    public void SetSeed(int newSeed) {
        m_seed = newSeed;
    }

    public void SetDimensions(Vector2Int wh) {
        m_width = wh.x;
        m_height = wh.y;
    }
    public void SetDimensions(int w, int h) {
        m_width = w;
        m_height = h;
    }

    public void SetLevelOfDetail(int newLOD) {
        m_levelOfDetail = Mathf.Clamp(newLOD, 0, 6);
    }

    public void SetOffset(Vector2Int newOffset) {
        m_offsetX = newOffset.x;
        m_offsetY = newOffset.y;
    }
    public void SetOffset(int x, int y) {
        m_offsetX = x;
        m_offsetY = y;
    }

    private void Start() {
        if (m_generateOnStart) StartCoroutine(GenerateMapCoroutine());
    }

    public void GenerateMap() {
        Debug.Log("Generating map via Function");

        // Initialize randomization seed
        m_prng = new System.Random(m_seed);

        // Generating the noise map
        GenerateNoise();
        Debug.Log("Noise Map Generated");

        // Generate texture
        GenerateMaterial();
        Debug.Log("Texture Generated");

        // Generate mesh data
        GenerateMeshData();
        Debug.Log("Mesh Generated");

        // If toggled to reposition, do so
        if (m_repositionOnGenerate) {
            transform.position = new Vector3(m_offsetX*m_width, 0f, m_offsetY*m_height);
        }

        if (m_meshMaterial != null && m_renderer != null) {
            m_renderer.sharedMaterial = m_meshMaterial;
        }

        if (m_meshFilter != null) {
            m_meshFilter.sharedMesh = m_meshData.CreateMesh();
            if (m_collider != null) m_collider.sharedMesh = m_meshFilter.sharedMesh;
        }
    }

    public IEnumerator GenerateMapCoroutine() {
        Debug.Log("Generating map via Coroutine");

        // Initialize randomization seed
        m_prng = new System.Random(m_seed);

        // Generating the noise map
        yield return GenerateNoiseCoroutine();
        Debug.Log("Noise Map Generated");

        // Generate texture
        yield return GenerateMaterialCoroutine();
        Debug.Log("Texture Generated");

        // Generate mesh data
        yield return GenerateMeshDataCoroutine();
        Debug.Log("Mesh Generated");

        if (m_meshMaterial != null && m_renderer != null) {
            m_renderer.sharedMaterial = m_meshMaterial;
        }

        if (m_meshFilter != null) {
            m_meshFilter.sharedMesh = m_meshData.CreateMesh();
            if (m_collider != null) m_collider.sharedMesh = m_meshFilter.sharedMesh;
        }

        yield return null;
    }

    private void GenerateNoise() {
        float[,] data = new float[gridWidth,gridHeight];
        MinMax dataRange = (!m_manualNoiseRange) ? new MinMax { min=float.MaxValue, max=float.MinValue } : m_noiseRange;
        for (int x = 0; x < gridWidth; x++) {
            for (int y = 0; y < gridHeight; y++) {
                float value = 0f;
                for(int i = 0; i < m_layers.Count; i++) {
                    TerrainLayer layer = m_layers[i];
                    if (layer.applicationType != ApplicationType.Off) {
                        float v = layer.GeneratePoint(m_prng, x, y, m_width, m_height, m_offsetX, m_offsetY);
                        switch(layer.applicationType) {
                            case ApplicationType.Add:
                                value += v;
                                break;
                            case ApplicationType.Subtract:
                                value -= v;
                                break;
                            case ApplicationType.Multiply:
                                value *= v;
                                break;
                            case ApplicationType.Divide:
                                if (v == 0f) v = 0.00001f;
                                value /= v;
                                break;
                            default: // Set
                                value = v;
                                break;
                        }
                    }
                }
                data[x,y] = value;
                if (!m_manualNoiseRange) {
                    if (value > dataRange.max) dataRange.max = value;
                    if (value < dataRange.min) dataRange.min = value;
                }
            }
        }
        m_noiseMap = data;
        m_noiseRange = dataRange;
    }

    private IEnumerator GenerateNoiseCoroutine() {
        // Initialize the noise map array
        float[,] data = new float[gridWidth,gridHeight];

        // Initialize the minmax
        MinMax dataRange = (!m_manualNoiseRange) ? new MinMax { min=float.MaxValue, max=float.MinValue } : m_noiseRange;

        // Initialize the loop. We use a counter due to this being a coroutine
        int counter = 0;
        for (int x = 0; x < gridWidth; x++) {
            for (int y = 0; y < gridHeight; y++) {
                
                // With an initial value of 0, we consecutively add layers on top of it based on the terrain layers
                float value = 0f;
                for(int i = 0; i < m_layers.Count; i++) {
                    TerrainLayer layer = m_layers[i];
                    if (layer.applicationType != ApplicationType.Off) {
                        float v = layer.GeneratePoint(m_prng, x, y, m_width, m_height, m_offsetX, m_offsetY);
                        switch(layer.applicationType) {
                            case ApplicationType.Add:
                                value += v;
                                break;
                            case ApplicationType.Subtract:
                                value -= v;
                                break;
                            case ApplicationType.Multiply:
                                value *= v;
                                break;
                            case ApplicationType.Divide:
                                if (v == 0f) v = 0.00001f;
                                value /= v;
                                break;
                            default: // Set
                                value = v;
                                break;
                        }
                    }
                }
                data[x,y] = value;

                // Set min and max values
                if (!m_manualNoiseRange) {
                    if (value > dataRange.max) dataRange.max = value;
                    if (value < dataRange.min) dataRange.min = value;
                }
                
                // Coroutine logic
                counter++;
                if (counter % m_coroutineNumThreshold == 0) {
                    yield return null;
                    counter = 0;
                }
            }
        }

        // End
        m_noiseMap = data;
        m_noiseRange = dataRange;
        yield return null;

        /*
        heightMap = new float[gridWidth, gridHeight];
        for (int x = 0; x < gridWidth; x++) {
            for (int y = 0; y < gridHeight; y++) {
                noiseMap[x,y] /= maxValue;
            }
        }
        */
    }

    private void GenerateMaterial() {
        m_meshMaterial = new Material(m_meshMaterialSrc);
        m_meshMaterial.SetInt("layerCount", m_meshMaterialLayers.Count);
        m_meshMaterial.SetColorArray("baseColors", m_meshMaterialLayers.Select(x => x.tint).ToArray());
        m_meshMaterial.SetFloatArray("baseStartHeights", m_meshMaterialLayers.Select(x => x.startHeight).ToArray());
        m_meshMaterial.SetFloatArray("baseBlends", m_meshMaterialLayers.Select(x => x.blendStrength).ToArray());
        m_meshMaterial.SetFloatArray("baseColorStrength", m_meshMaterialLayers.Select(x => x.tintStrength).ToArray());
        m_meshMaterial.SetFloatArray("baseTextureScales", m_meshMaterialLayers.Select(x => x.textureScale).ToArray());

        Texture2DArray texturesArray = GenerateTextureArray(m_meshMaterialLayers.Select(x => x.texture).ToArray());
        m_meshMaterial.SetTexture("baseTextures", texturesArray);
        m_meshMaterial.SetFloat("minHeight", m_noiseRange.min);
        m_meshMaterial.SetFloat("maxHeight", m_noiseRange.max);

        m_textureData = new Texture2D(gridWidth, gridHeight);
        m_textureData.filterMode = m_filterMode;
        m_textureData.wrapMode = TextureWrapMode.Clamp;

        for (int x = 0; x < gridWidth; x++) {
            for (int y = 0; y < gridHeight; y++) {
                float currentHeight = m_noiseMap[x,y];
                Color currentColor = Color.white;
                for(int i = m_meshMaterialLayers.Count-1; i >= 0 ; i--) {
                    if (currentHeight >= Mathf.Lerp(m_noiseRange.min, m_noiseRange.max, m_meshMaterialLayers[i].startHeight)) {
                        currentColor = m_meshMaterialLayers[i].tint;
                        break;
                    }
                }
                m_textureData.SetPixel(x, y, currentColor);
            }
        }
        m_textureData.Apply();
    }

    private IEnumerator GenerateMaterialCoroutine() {
        // Initialize the material by making an instance of it, so that we don't accidentally override it.
        m_meshMaterial = new Material(m_meshMaterialSrc);

        // We prep its variables
        m_meshMaterial.SetInt("layerCount", m_meshMaterialLayers.Count);
        m_meshMaterial.SetColorArray("baseColors", m_meshMaterialLayers.Select(x => x.tint).ToArray());
        m_meshMaterial.SetFloatArray("baseStartHeights", m_meshMaterialLayers.Select(x => x.startHeight).ToArray());
        m_meshMaterial.SetFloatArray("baseBlends", m_meshMaterialLayers.Select(x => x.blendStrength).ToArray());
        m_meshMaterial.SetFloatArray("baseColorStrength", m_meshMaterialLayers.Select(x => x.tintStrength).ToArray());
        m_meshMaterial.SetFloatArray("baseTextureScales", m_meshMaterialLayers.Select(x => x.textureScale).ToArray());

        // Initialize the texture array
        Texture2DArray texturesArray = GenerateTextureArray(m_meshMaterialLayers.Select(x => x.texture).ToArray());
        m_meshMaterial.SetTexture("baseTextures", texturesArray);
        m_meshMaterial.SetFloat("minHeight", m_noiseRange.min);
        m_meshMaterial.SetFloat("maxHeight", m_noiseRange.max);

        // Initialize the texture we'll be applying to this material
        m_textureData = new Texture2D(gridWidth, gridHeight);
        m_textureData.filterMode = m_filterMode;
        m_textureData.wrapMode = TextureWrapMode.Clamp;

        // We need to apply pixels and colors to the texture
        int counter = 0;
        for (int x = 0; x < gridWidth; x++) {
            for (int y = 0; y < gridHeight; y++) {
                
                // What's the noise map value here?
                float currentHeight = m_noiseMap[x,y];
                Color currentColor = Color.white;

                // Colors are determined by start height. As long as the noise map is above the start height, we apply it
                for(int i = m_meshMaterialLayers.Count-1; i >= 0 ; i--) {
                    if (currentHeight >= Mathf.Lerp(m_noiseRange.min, m_noiseRange.max, m_meshMaterialLayers[i].startHeight)) {
                        currentColor = m_meshMaterialLayers[i].tint;
                        break;
                    }
                }

                // Given the color, let's apply it
                m_textureData.SetPixel(x, y, currentColor);

                // Coroutine stuff
                counter++;
                if (counter % m_coroutineNumThreshold == 0) {
                    yield return null;
                    counter = 0;
                }
            }
        }

        // Apply the texture to set its colors
        m_textureData.Apply();
        yield return null;

        // Now apply the texture
        /*
        int counter = 0;
        for (int x = 0; x < gridWidth; x++) {
            for (int y = 0; y < gridHeight; y++) {
                float sample = m_noiseMap[x,y];
                Color color = new Color(sample,sample,sample);
                m_textureData.SetPixel(x, y, color);
                counter++;
                if (counter % m_coroutineNumThreshold == 0) {
                    yield return null;
                    counter = 0;
                }
            }
            counter++;
            if (counter % m_coroutineNumThreshold == 0) {
                yield return null;
                counter = 0;
            }
        }
        */
    }

    private void GenerateMeshData() {
        float topLeftX = (float)m_width / -2f;
        float topLeftZ = (float)m_height / 2f;

        int meshSimplificationIncrement = (m_levelOfDetail == 0) ? 1 : m_levelOfDetail * 2;
        int verticesPerLine = m_width / meshSimplificationIncrement + 1;

        m_meshData = new MeshData(verticesPerLine, verticesPerLine);
        int vertexIndex = 0;

        for(int y = 0; y < gridHeight; y+=meshSimplificationIncrement) {
            for(int x = 0; x < gridWidth; x+=meshSimplificationIncrement) {
                m_meshData.vertices[vertexIndex] = new Vector3(x, m_noiseMap[x,y], y);
                m_meshData.uvs[vertexIndex] = new Vector2(x/(float)gridWidth, y/(float)gridHeight);
                if (x < m_width && y < m_height) {
                    m_meshData.AddTriangle(vertexIndex, vertexIndex+verticesPerLine, vertexIndex+verticesPerLine+1);
                    m_meshData.AddTriangle(vertexIndex+verticesPerLine+1, vertexIndex+1, vertexIndex);
                }
                vertexIndex++;
            }
        }
    }

    private IEnumerator GenerateMeshDataCoroutine() {
        float topLeftX = (float)m_width / -2f;
        float topLeftZ = (float)m_height / 2f;

        int meshSimplificationIncrement = (m_levelOfDetail == 0) ? 1 : m_levelOfDetail * 2;
        int verticesPerLine = m_width / meshSimplificationIncrement + 1;

        m_meshData = new MeshData(verticesPerLine, verticesPerLine);
        int vertexIndex = 0;

        for(int y = 0; y < gridHeight; y+=meshSimplificationIncrement) {
            for(int x = 0; x < gridWidth; x+=meshSimplificationIncrement) {
                m_meshData.vertices[vertexIndex] = new Vector3(x, m_noiseMap[x,y], y);
                m_meshData.uvs[vertexIndex] = new Vector2(x/(float)gridWidth, y/(float)gridHeight);
                if (x < m_width && y < m_height) {
                    m_meshData.AddTriangle(vertexIndex, vertexIndex+verticesPerLine, vertexIndex+verticesPerLine+1);
                    m_meshData.AddTriangle(vertexIndex+verticesPerLine+1, vertexIndex+1, vertexIndex);
                }
                vertexIndex++;
                if (vertexIndex % m_coroutineNumThreshold == 0) yield return null;
            }
            if (vertexIndex % m_coroutineNumThreshold == 0) yield return null;
        }
        yield return null;
    }

    public virtual Texture2DArray GenerateTextureArray(Texture2D[] textures) {
        Texture2DArray textureArray = new Texture2DArray(textureSize, textureSize, textures.Length, textureFormat, true);
        for(int i =0; i < textures.Length; i++) {
            textureArray.SetPixels(textures[i].GetPixels(), i);
        }
        textureArray.Apply();
        return textureArray;
    }

    private void OnValidate() {
        foreach(TerrainLayer layer in m_layers) if (layer.scale <= 1f) layer.scale = 1f;
    }
}
