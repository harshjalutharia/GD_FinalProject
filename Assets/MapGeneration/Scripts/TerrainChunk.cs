using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Linq;

[RequireComponent(typeof(Renderer))]
public class TerrainChunk : MonoBehaviour
{
    const int textureSize = 512;
    const TextureFormat textureFormat = TextureFormat.RGB565;

    [System.Serializable]
    public class MinMax {
        public float min;
        public float max;
    }

    [Header("=== Map Settings ===")]
    [SerializeField] private TerrainManager m_parent;
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

    [Header("=== Layering & Noise Calculation ===")]
    [SerializeField, Tooltip("Do we use a custom noise map range or allow the system to estimate?")]                private bool m_manualNoiseRange = false;

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
    [SerializeField] private Vector3 m_chunkCenter;
    public Vector3 chunkCenter => m_chunkCenter;
    [SerializeField] private int[] m_lodLevels;
    [SerializeField] private float[,] m_noiseMap;
    [SerializeField] private MinMax m_noiseRange;
    //[SerializeField] private Texture2D m_textureData;
    [SerializeField] private Mesh[] m_meshes;

    [Header("=== Debug Settings ===")]
    [SerializeField] private bool m_repositionOnGenerate = true;
    [SerializeField] private int m_coroutineNumThreshold = 20;
    [SerializeField] private bool m_autoUpdate = false;
    public bool autoUpdate => m_autoUpdate;

    public UnityEvent<TerrainChunk> onNoiseGenerationEnd;
    public UnityEvent<TerrainChunk> onNoiseAdjustmentEnd;
    public UnityEvent<TerrainChunk> onMaterialGenerationEnd;
    public UnityEvent<TerrainChunk> onMeshGenerationEnd;
    public UnityEvent<TerrainChunk> onMapGenerationEnd;

    public void SetParent(TerrainManager parent = null) {
        m_parent = parent;
    }

    public void SetDimensions(int w, int h) {
        m_width = w;
        m_height = h;
    }
    public void SetDimensions(Vector2Int wh) { SetDimensions(wh.x, wh.y); }

    public void SetLevelOfDetail(int newLOD) {
        m_levelOfDetail = Mathf.Clamp(newLOD, 0, 6);
    }

    public void SetOffset(int x, int y) {
        m_offsetX = x;
        m_offsetY = y;
    }
    public void SetOffset(Vector2Int newOffset) { SetOffset(newOffset.x, newOffset.y); }

    public void SetMinMax(MinMax newRange) {
        m_noiseRange = newRange;
    }

    public void SetMaterial(Material mat = null) {
        m_meshMaterial = mat; 
        if (m_renderer != null) m_renderer.sharedMaterial = mat;
    }

    private void Awake() {
        // This MUST be called, no matter how this is used.
        CalculateLODLevels();
    }

    public void Initialize(bool reposition = true, bool recalculateLODs = true) {
        // If we do not have a parent... then what is there to do? Can't do anything!
        if (m_parent == null) {
            Debug.LogError($"{gameObject.name} cannot generate map: missing parent");
            return;
        }

        // If toggled to reposition, do so
        if (reposition) transform.position = new Vector3(m_offsetX*m_width, 0f, m_offsetY*m_height);
        // If we are to recalculate LOD levels, then sure
        if (recalculateLODs)        CalculateLODLevels();

        // Determine center
        m_chunkCenter = new Vector3(((float)m_offsetX + 0.5f)*m_width, 0f, ((float)m_offsetY + 0.5f)*m_height);
    }

    public void GenerateMap(bool recalculateLODs=true) {
        // If toggled to reposition, do so
        if (m_repositionOnGenerate) transform.position = new Vector3(m_offsetX*m_width, 0f, m_offsetY*m_height);
        // If we are to recalculate LOD levels, then sure
        if (recalculateLODs)        CalculateLODLevels();

        // If we do not have a parent... then what is there to do? Can't do anything!
        if (m_parent == null) {
            Debug.LogError($"{gameObject.name} cannot generate map: missing parent");
            return;
        }

        GenerateNoise();                        // Generating the noise map
        GenerateMaterial();                     // Generate texture
        GenerateMeshData();                     // Generate mesh data
        onMapGenerationEnd?.Invoke(this);
    }

    public IEnumerator GenerateMapCoroutine(bool recalculateLODs=true) {
        // If toggled to reposition, do so
        if (m_repositionOnGenerate) transform.position = new Vector3(m_offsetX*m_width, 0f, m_offsetY*m_height);
        // If we are to recalculate LOD levels, then sure
        if (recalculateLODs)        CalculateLODLevels();
        
        // If we do not have a parent... then what is there to do? Can't do anything!
        if (m_parent == null) {
            Debug.LogError($"{gameObject.name} cannot generate map: missing parent");
            yield break;
        }

        yield return GenerateNoiseCoroutine();      // Generating the noise map
        yield return GenerateMaterialCoroutine();   // Generate texture
        yield return GenerateMeshDataCoroutine();   // Generate mesh data
        onMapGenerationEnd?.Invoke(this);
        yield return null;
    }

    public void GenerateNoise() {
        float[,] data = new float[gridWidth,gridHeight];    // Initialize the noise map array
        MinMax dataRange = (!m_manualNoiseRange)            // Initialize the minmax
            ? new MinMax { min=float.MaxValue, max=float.MinValue } 
            : m_noiseRange;

         // Initialize the loop.
        for (int x = 0; x < gridWidth; x++) {
            for (int y = 0; y < gridHeight; y++) {
                float worldX = x + m_offsetX * m_width;
                float worldZ = y + m_offsetY * m_height;
                float value = m_parent.GetTerrainValueFromWorldPosition(worldX, worldZ);
                data[x,y] = value;
                if (!m_manualNoiseRange) {
                    if (value > dataRange.max) dataRange.max = value;
                    if (value < dataRange.min) dataRange.min = value;
                }
            }
        }
        m_noiseMap = data;
        m_noiseRange = dataRange;
        onNoiseGenerationEnd?.Invoke(this);
    }

    public IEnumerator GenerateNoiseCoroutine() {
        float[,] data = new float[gridWidth,gridHeight];    // Initialize the noise map array
        MinMax dataRange = (!m_manualNoiseRange)            // Initialize the minmax
            ? new MinMax { min=float.MaxValue, max=float.MinValue } 
            : m_noiseRange;

        // Initialize the loop. We use a counter due to this being a coroutine
        int counter = 0;
        for (int x = 0; x < gridWidth; x++) {
            for (int y = 0; y < gridHeight; y++) {
                float worldX = x + m_offsetX * m_width;
                float worldZ = y + m_offsetY * m_height;
                float value = m_parent.GetTerrainValueFromWorldPosition(worldX, worldZ);
                data[x,y] = value;
                if (!m_manualNoiseRange) {
                    if (value > dataRange.max) dataRange.max = value;
                    if (value < dataRange.min) dataRange.min = value;
                }
                
                counter++;
                if (counter % m_coroutineNumThreshold == 0) {
                    yield return null;
                    counter = 0;
                }
            }
        }

        m_noiseMap = data;
        m_noiseRange = dataRange;
        onNoiseGenerationEnd?.Invoke(this);
        yield return null;
    }

    public IEnumerator AdjustHeightToFloor() {
        // All this function does is, for each value in our noise map, move so that the smallest value in our noise map is set to the floor
        int counter = 0;
        for (int x = 0; x < gridWidth; x++) {
            for (int y = 0; y < gridHeight; y++) {
                float originalNoise = m_noiseMap[x,y];
                m_noiseMap[x,y] = originalNoise - m_noiseRange.min;
                
                counter++;
                if (counter >= m_coroutineNumThreshold) {
                    yield return null;
                    counter = 0;
                }
            }
        }
        onNoiseAdjustmentEnd?.Invoke(this);
        yield return null;
    }

    public void GenerateMaterial() {
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

        /*
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
                //m_textureData.SetPixel(x, y, currentColor);
            }
        }
        //m_textureData.Apply();
        */
        if (m_meshMaterial != null && m_renderer != null) m_renderer.sharedMaterial = m_meshMaterial;
        onMaterialGenerationEnd?.Invoke(this);
    }

    public IEnumerator GenerateMaterialCoroutine() {
        Debug.Log("Started generating material");
        // Initialize the material by making an instance of it, so that we don't accidentally override it.
        m_meshMaterial = new Material(m_meshMaterialSrc);
        Debug.Log("Creating new material");

        // We prep its variables
        Color[] baseColors = new Color[m_meshMaterialLayers.Count];
        float[] startHeights = new float[m_meshMaterialLayers.Count];
        float[] blendStrengths = new float[m_meshMaterialLayers.Count];
        float[] tintStrengths = new float[m_meshMaterialLayers.Count];
        float[] textureScales = new float[m_meshMaterialLayers.Count];
        Texture2D[] textures = new Texture2D[m_meshMaterialLayers.Count];
        for (int i = 0; i < m_meshMaterialLayers.Count; i++) {
            TextureLayer tl = m_meshMaterialLayers[i];
            baseColors[i] = tl.tint;
            startHeights[i] = tl.startHeight;
            blendStrengths[i] = tl.blendStrength;
            tintStrengths[i] = tl.tintStrength;
            textureScales[i] = tl.textureScale;
            textures[i] = tl.texture;
            yield return null;
        }

        m_meshMaterial.SetInt("layerCount", m_meshMaterialLayers.Count);
        m_meshMaterial.SetColorArray("baseColors", baseColors);             yield return null;
        m_meshMaterial.SetFloatArray("baseStartHeights", startHeights);     yield return null;
        m_meshMaterial.SetFloatArray("baseBlends", blendStrengths);         yield return null;
        m_meshMaterial.SetFloatArray("baseColorStrength", tintStrengths);   yield return null;
        m_meshMaterial.SetFloatArray("baseTextureScales", textureScales);   yield return null;

        // Initialize the texture array
        Texture2DArray texturesArray = GenerateTextureArray(textures);      yield return null;
        m_meshMaterial.SetTexture("baseTextures", texturesArray);           yield return null;
        m_meshMaterial.SetFloat("minHeight", m_noiseRange.min);
        m_meshMaterial.SetFloat("maxHeight", m_noiseRange.max);

        /*
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
                //m_textureData.SetPixel(x, y, currentColor);

                // Coroutine stuff
                counter++;
                if (counter >= m_coroutineNumThreshold) {
                    yield return null;
                    counter = 0;
                }
            }
        }

        // Apply the texture to set its colors
        //m_textureData.Apply();
        */
        if (m_meshMaterial != null && m_renderer != null) m_renderer.sharedMaterial = m_meshMaterial;
        onMaterialGenerationEnd?.Invoke(this);
        yield return null;
    }

    public void GenerateMeshData() {
        for(int i = 0; i < m_meshes.Length; i++) {
            int meshSimplificationIncrement = m_lodLevels[i];
            int verticesPerLine = m_width / meshSimplificationIncrement + 1;
            MeshData meshData = new MeshData(verticesPerLine, verticesPerLine);
            int vertexIndex = 0;
            for(int y = 0; y < gridHeight; y+=meshSimplificationIncrement) {
                for(int x = 0; x < gridWidth; x+=meshSimplificationIncrement) {
                    meshData.vertices[vertexIndex] = new Vector3(x, m_noiseMap[x,y], y);
                    meshData.uvs[vertexIndex] = new Vector2(x/(float)gridWidth, y/(float)gridHeight);
                    if (x < m_width && y < m_height) {
                        meshData.AddTriangle(vertexIndex, vertexIndex+verticesPerLine, vertexIndex+verticesPerLine+1);
                        meshData.AddTriangle(vertexIndex+verticesPerLine+1, vertexIndex+1, vertexIndex);
                    }
                    vertexIndex++;
                }
            }

            m_meshes[i] = meshData.CreateMesh();
        }

        SetLODMesh(m_levelOfDetail);
        onMeshGenerationEnd?.Invoke(this);
    }

    public IEnumerator GenerateMeshDataCoroutine() {
        for(int i = 0; i < m_meshes.Length; i++) {
            int meshSimplificationIncrement = m_lodLevels[i];
            int verticesPerLine = m_width / meshSimplificationIncrement + 1;
            MeshData meshData = new MeshData(verticesPerLine, verticesPerLine);
            int vertexIndex = 0;
            for(int y = 0; y < gridHeight; y+=meshSimplificationIncrement) {
                for(int x = 0; x < gridWidth; x+=meshSimplificationIncrement) {
                    meshData.vertices[vertexIndex] = new Vector3(x, m_noiseMap[x,y], y);
                    meshData.uvs[vertexIndex] = new Vector2(x/(float)gridWidth, y/(float)gridHeight);
                    if (x < m_width && y < m_height) {
                        meshData.AddTriangle(vertexIndex, vertexIndex+verticesPerLine, vertexIndex+verticesPerLine+1);
                        meshData.AddTriangle(vertexIndex+verticesPerLine+1, vertexIndex+1, vertexIndex);
                    }
                    vertexIndex++;
                    if ((float)vertexIndex % m_coroutineNumThreshold == 0) yield return null;
                }
            }

            m_meshes[i] = meshData.CreateMesh();
            yield return null;
        }

        SetLODMesh(m_levelOfDetail);
        onMeshGenerationEnd?.Invoke(this);
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

    private void CalculateLODLevels() {
        m_lodLevels = new int[7];
        m_lodLevels[0] = 1;
        int previousFactor = 1;
        for(int i = 1; i < 7; i++) {
            // The issue with LODs is that there is no guarantee that LOD2, which typically responds to (width/(LOD*2) + 1) vertices per line,
            // will actually BE divible by the intended mesh size.
            // For example, let's say you have a 50x50 mesh. This means your grid dimensions are 51x51. LOD0, which produces (50/1)+1=51 vertices per line, will still fit within 51x51.
            //  Similarly, for LOD1, there will be (50/(1*2) + 1) = 26 vertices per line, will still align with 50x50 because your mesh will ultimately occupy 25x25, 25 neatly dividing 50 by 2.
            //  However, if you have LOD2, there will be (50/(2*2)+1) = 13.5 vertices per line, which rounds to 14 by integer rounding. 14 does NOT divide 50 neatly. This becomes an issue...
            //  Therefore, an alternative is to, for each LOD level, determine what's the next optimal amount of vertices you can maintain in your given mesh size that will still end up neatly dividing your intended dimensions.
            //  The best chance you have is to look at factors of your intended dimension size. In this case, the factors of 50 are 1,2,5,25,50... which only gives you LOD levels of upwards to 3 lods (0-2), with LOD2 = divisor of 5.
            //  Another example: consider 500x500. The factors of this are 1-500, 2-250, 4-125, 5-100, 10-50, and 20-25
            
            // Case: if we're STILL going on an we've passed the halfway mark for factors, then it's no good. Just increment based on the previous LOD
            if (previousFactor > Mathf.Min(m_width, m_height)) {
                m_lodLevels[i] = m_lodLevels[i-1];
                continue;
            }

            // What's the current factor we want to check?
            int factor = previousFactor+1;
            // If successful check, add to `m_lodLevels` and increment `i`
            if (m_width % factor == 0 && m_height % factor == 0) {
                m_lodLevels[i] = factor;
            } 
            // Otherwise, decrement `i` so that we move onto the next factor without incrementing LOD
            else {
                i--;
            }
            previousFactor = factor;
        }
        // By this point, LOD levels should be set between 0 to 6.
        // Last thing we do is instantiate the meshes array
        m_meshes = new Mesh[7];
    }

    public void SetLODMesh(int setTo) {
        m_levelOfDetail = setTo;
        if (m_meshFilter != null) {
            m_meshFilter.sharedMesh = m_meshes[setTo];
            if (m_collider != null) m_collider.sharedMesh = m_meshes[setTo];
        }
    }

    private void OnValidate() {
        CalculateLODLevels();
    }
}
