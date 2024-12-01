using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class TestFinalTerrainGenerator : MonoBehaviour
{
    public enum LayerType { Random, Perlin, Falloff }
    public enum FalloffType { Box, NorthSouth, EastWest, Circle }
    public enum ApplicationType { Off, Set, Add, Subtract, Multiply, Divide }

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
        public float offsetX = 0f;
        public float offsetY = 0f;

        [Header("Falloff Settings")]
        public FalloffType falloffType;
        [Range(0f,1f)] public float centerX = 0.5f;
        [Range(0f,1f)] public float centerY = 0.5f;
        [Range(0f,2f)] public float falloffStart;
        [Range(0f,2f)] public float falloffEnd;
        public Gradient falloffGradient;
        public bool invert;

        public virtual float GeneratePoint(System.Random prng, int x, int y, int width, int height) { 
            float value;
            switch(layerType) {
                case LayerType.Perlin:
                    value = GeneratePerlin(x, y, width, height);
                    break;
                case LayerType.Falloff:
                    value = GenerateFalloff(x, y, width, height);
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

        public virtual float GeneratePerlin(int x, int y, int width, int height) {
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

    [Header("=== Map Settings ===")]
    [SerializeField] private int m_seed;
    [SerializeField, Tooltip("The visual (world) width of this map")]   private int width = 256;
    [SerializeField, Tooltip("The visual (world) height of this map")]  private int height = 256;
    public int gridWidth => width+1;    // The grid map width; +1 of world width due to vertices requiring one more point at the end
    public int gridHeight => height+1;  // The grid map height;
    [SerializeField, Range(0,6), Tooltip("The LOD level used to control the mesh fidelity.")] private int m_levelOfDetail;
    [Space]
    [SerializeField] private AnimationCurve m_heightCurve;
    public List<TerrainLayer> m_layers;
    public System.Random m_prng;

    [Header("=== Renderer Settings ===")]
    [SerializeField] private MeshFilter m_meshFilter;
    [SerializeField] private Renderer m_renderer;
    [SerializeField] private MeshCollider m_collider;
    [SerializeField] private float[,] m_noiseMap;
    //[SerializeField] private float[,] m_heightMap;
    [SerializeField] private FilterMode m_filterMode;

    [Header("=== Debug Settings ===")]
    [SerializeField] private bool m_autoUpdate = false;
    public bool autoUpdate => m_autoUpdate;

    public void GenerateMap() {
        StartCoroutine(GenerateMapCoroutine());
    }

    public IEnumerator GenerateMapCoroutine() {
        Debug.Log("Generating map");

        // Initialize randomization seed
        m_prng = new System.Random(m_seed);

        // Generating the noise map
        GenerateNoise(out m_noiseMap);

        // Generate texture and mesh data
        Texture2D tData = GenerateTexture();
        MeshData mData = GenerateMeshData();

        if (m_renderer != null) {
            //m_meshFilter.sharedMesh = mData.CreateMesh();
            //if (m_collider != null) m_collider.sharedMesh = m_meshFilter.sharedMesh;
            var tempMaterial = new Material(m_renderer.sharedMaterial);
            tempMaterial.mainTexture = tData;
            m_renderer.sharedMaterial = tempMaterial;
        }

        if (m_meshFilter != null) {
            m_meshFilter.sharedMesh = mData.CreateMesh();
            //if (m_collider != null) m_collider.sharedMesh = m_meshFilter.sharedMesh;
        }

        yield return null;
    }

    private void GenerateNoise(out float[,] noiseMap) {
        noiseMap = new float[gridWidth,gridHeight];
        float maxValue = float.MinValue;
        for (int x = 0; x < gridWidth; x++) {
            for (int y = 0; y < gridHeight; y++) {
                float value = 0f;
                for(int i = 0; i < m_layers.Count; i++) {
                    TerrainLayer layer = m_layers[i];
                    if (layer.applicationType != ApplicationType.Off) {
                        float v = layer.GeneratePoint(m_prng, x, y, width, height);
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
                noiseMap[x,y] = value;
                if (value > maxValue) maxValue = value;
                //noiseMap[x,y] = CalculateNoise(x,y,gridWidth,gridHeight);
            }
        }

        /*
        heightMap = new float[gridWidth, gridHeight];
        for (int x = 0; x < gridWidth; x++) {
            for (int y = 0; y < gridHeight; y++) {
                noiseMap[x,y] /= maxValue;
            }
        }
        */
    }
    
    private MeshData GenerateMeshData() {
        float topLeftX = (float)width / -2f;
        float topLeftZ = (float)height / 2f;

        int meshSimplificationIncrement = (m_levelOfDetail == 0) ? 1 : m_levelOfDetail * 2;
        int verticesPerLine = width / meshSimplificationIncrement + 1;

        MeshData meshData = new MeshData(verticesPerLine, verticesPerLine);
        int vertexIndex = 0;

        for(int y = 0; y < gridHeight; y+=meshSimplificationIncrement) {
            for(int x = 0; x < gridWidth; x+=meshSimplificationIncrement) {
                meshData.vertices[vertexIndex] = new Vector3(x, m_noiseMap[x,y], y);
                meshData.uvs[vertexIndex] = new Vector2(x/(float)gridWidth, y/(float)gridHeight);
                if (x < width && y < height) {
                    meshData.AddTriangle(vertexIndex, vertexIndex+verticesPerLine, vertexIndex+verticesPerLine+1);
                    meshData.AddTriangle(vertexIndex+verticesPerLine+1, vertexIndex+1, vertexIndex);
                }
                vertexIndex++;
            }
        }

        return meshData;
    }

    private Texture2D GenerateTexture() {
        Texture2D texture = new Texture2D(gridWidth, gridHeight);
        texture.filterMode = m_filterMode;
        texture.wrapMode = TextureWrapMode.Clamp;

        for (int x = 0; x < gridWidth; x++) {
            for (int y = 0; y < gridHeight; y++) {
                float sample = m_noiseMap[x,y];
                Color color = new Color(sample,sample,sample);
                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();
        return texture;
    }
    /*
    private float CalculateNoise(int x, int y, int width, int height)
    {
        float xCoord = (float)x / width * scale + offsetX;
        float yCoord = (float)y / height * scale + offsetY;
        Debug.Log(xCoord);
        return Mathf.PerlinNoise(xCoord, yCoord);
    }
    */

    private void OnValidate() {
        foreach(TerrainLayer layer in m_layers) if (layer.scale <= 1f) layer.scale = 1f;
    }
}
