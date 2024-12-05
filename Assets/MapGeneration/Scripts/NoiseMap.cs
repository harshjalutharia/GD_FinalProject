using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Linq;

public class NoiseMap : MonoBehaviour
{
    public enum DrawMode { None, Noise, Color, Mesh}
    public enum ColorMode { Gradient, TerrainTypes, Shader }
    const int textureSize = 512;
    const TextureFormat textureFormat = TextureFormat.RGB565;

    [System.Serializable]
    public class TerrainType {
        public string name;
        public float height;
        public Color color;
    }

    [System.Serializable]
    public class MinMax {
        public float min;
        public float max;
    }

    [System.Serializable]
    public class TextureLayer {
        public string name;
        public Texture2D texture;
        public Color tint;
        [Range(0f,1f)] public float tintStrength;
        [Range(0f,1f)] public float startHeight;
        [Range(0f,1f)] public float blendStrength;
        public float textureScale;
    }


    [Header("=== Map Settings ===")]
    [SerializeField, Tooltip("The seed used to control psuedo-rng for this map.")]                                  protected int m_seed;
    [SerializeField, Tooltip("The length and width of the generated map, in Unity meters.")]                        protected int m_mapChunkSize = 241;
    public int mapChunkSize => m_mapChunkSize;
    public Vector2 mapCenter => new Vector2((m_mapChunkSize-1)/2f, (m_mapChunkSize-1)/2f);
    public Vector3 mapCenter3D => new Vector3((m_mapChunkSize-1)/2f, 0f, (m_mapChunkSize-1)/2f);
    [SerializeField, Range(0,6), Tooltip("The LOD level used to control the mesh fidelity.")]                       protected int m_levelOfDetail;
    [SerializeField, Tooltip("The offset of the noise map in either X or Y direction. Not all variants will use this.")]    protected Vector2 m_offset;
    [SerializeField, Tooltip("When generating perlin noise, how do we normalize the noise map")]                            protected Generators.NormalizeMode m_normalizeMode;
    [SerializeField, Tooltip("If set to TRUE, Unity will call `GenerateMap()` with every inspector value change.")] protected bool m_autoUpdate;
    public bool autoUpdate => m_autoUpdate;
    protected System.Random m_prng;

    [Header("=== Render References ===")]
    [SerializeField, Tooltip("The Mesh Filter component.")]                                                                 protected MeshFilter m_filter;
    [SerializeField, Tooltip("The Mesh Renderer component.")]                                                               protected MeshRenderer m_renderer;
    [SerializeField, Tooltip("The Mesh Collider component.")]                                                               protected MeshCollider m_collider;
    [SerializeField, Tooltip("The Mesh Renderer for the held map.")]                                                        protected MeshRenderer m_heldRenderer;
    [SerializeField, Tooltip("The UI Raw Image element for presenting on a canvas")]                                        protected RawImage m_canvasImage;
    [SerializeField, Tooltip("The Held Map component that we pass the texture to")]                                         protected HeldMap m_heldMap;
    [SerializeField, Tooltip("If set, will replace the materials in the Mesh Renderer and Collider with this material.")]   protected Material m_meshMaterial;

    [Header("=== Global Render Settings ===")]
    [SerializeField, Tooltip("How should the mesh be drawn?")]                                                  protected DrawMode m_drawMode = DrawMode.Noise;
    [SerializeField, Tooltip("The filter mode of the texture drawn onto the mesh.")]                            protected FilterMode m_textureFilterMode = FilterMode.Point;
    [SerializeField, Tooltip("The base multiplier applied to each pixel of the texture.")]                      protected float m_textureHeightMultiplier = 2000f;
    [SerializeField, Tooltip("Controls how the base multiplier is applied to each pixel, based on noise map.")] protected AnimationCurve m_textureHeightCurve;

    [Header("=== Color Settings ===")]
    [SerializeField, Tooltip("How the colors are printed onto the mesh. If using either TerrainTypes or Gradient, make sure the material uses the 'Standard' shader.")] protected ColorMode m_colorMode = ColorMode.TerrainTypes;
    [SerializeField, Tooltip("The colors rendered onto the mesh material. If Color Mode is set to 'TerrainTypes', will use only the colors and startHeight")]           protected TextureLayer[] m_meshMaterialLayers;
    [SerializeField, Tooltip("TO BE DEPRECATED")]                                                                                                                       protected TerrainType[] m_terrainTypes;
    [SerializeField, Tooltip("The color gradient used to color the mesh. Only used if Color Mode is set to 'Gradient'.")]                                               protected Gradient m_terrainColorGradient;

    [Header("=== Post Generation ===")]
    [SerializeField, Tooltip("Events to call after noise map generation")]  protected UnityEvent m_onGenerationEnd;

    [Header("=== Outputs - READ ONLY ===")]
    [SerializeField] protected float[,] m_noiseMap;
    public float[,] noiseMap => m_noiseMap;
    [SerializeField] protected float[,] m_heightMap;
    public float[,] heightMap => m_heightMap;
    [SerializeField] protected MinMax m_heightRange;
    public MinMax heightRange => m_heightRange;

    #if UNITY_EDITOR
    [Header("== DEBUG ===")]
    [SerializeField] private bool m_showGrid = false;
    [SerializeField] private Color m_gridColor = Color.white;
    [SerializeField] private int m_gridCellSize;
    [SerializeField] private Color m_coordColor = Color.blue;
    [SerializeField] private Vector2Int m_debugGridCoord = new Vector2Int(0,0);
    
    protected virtual void OnDrawGizmos() {
        if (!m_showGrid) return;
        Gizmos.color = m_gridColor;

        Vector3 extents = new Vector3((float)(m_mapChunkSize-1)/2f, 0f, (float)(m_mapChunkSize-1)/2f);
        Vector3 gridDimensions = extents*2f;
        Gizmos.DrawWireCube(transform.position, gridDimensions);
        
        if (m_gridCellSize == 0f) return;
        Vector3 gridStart = transform.position - extents;
        Vector3 gridCellDimensions = new Vector3(m_gridCellSize, 0f, m_gridCellSize);
        Vector2Int numGridCells = new Vector2Int(Mathf.FloorToInt(gridDimensions.x/m_gridCellSize), Mathf.FloorToInt(gridDimensions.z/m_gridCellSize));
        for(int x = 0; x < numGridCells.x; x++) {
            for(int y = 0; y < numGridCells.y; y++) {
                Vector3 cellCenter = gridStart + new Vector3(x*m_gridCellSize+m_gridCellSize/2f, 0f, y*m_gridCellSize+m_gridCellSize/2f);
                Gizmos.DrawWireCube(cellCenter, gridCellDimensions);
            }
        }

        if (m_heightMap != null && m_heightMap.Length > 0) {
            Gizmos.color = m_coordColor;
            float debugCoordY = m_heightMap[m_debugGridCoord.x, m_debugGridCoord.y];
            Gizmos.DrawSphere(new Vector3((float)m_debugGridCoord.x - extents.x, debugCoordY, gridDimensions.z - (float)m_debugGridCoord.y - extents.z), 1f);
        }
    }
    #endif

    public virtual void SetSeed(string newSeed) {
        if (newSeed.Length > 0 && int.TryParse(newSeed, out int validNewSeed)) {
            m_seed = validNewSeed;
            return;
        }
        m_seed = UnityEngine.Random.Range(0, 1000001);
    }
    public virtual void SetSeed(int newSeed) {
        m_seed = newSeed;
    }

    public virtual void SetChunkSize(int newSize) {
        m_mapChunkSize = newSize;
    }

    public virtual void SetLevelOfDetail(int newLOD) {
        m_levelOfDetail = Mathf.Clamp(newLOD, 0, 6);
    }

    public virtual void SetOffset(Vector2 newOffset) {
        m_offset = newOffset;
    }

    public virtual void SetNormalizeMode(Generators.NormalizeMode newMode) {
        m_normalizeMode = newMode;
    }

    public virtual void GenerateMap() {
        m_prng = new System.Random(m_seed);
        m_noiseMap = Generators.GenerateRandomMap(m_mapChunkSize, m_mapChunkSize, m_prng);
        m_heightMap = Generators.GenerateHeightMap(m_noiseMap, m_textureHeightCurve, m_textureHeightMultiplier);
        m_heightRange = GetHeightRange(m_heightMap);
        if (m_drawMode != DrawMode.None) RenderMap();
        m_onGenerationEnd?.Invoke();
    }
    public virtual IEnumerator GenerateMapCoroutine() {
        GenerateMap();
        yield return null;
    }

    public virtual Color[] GenerateColorMap() {
        // We need to mod our behavior based on what kind of color mode we're using.
        //  - Gradient = we evaluate the color based on noise map value, which should be between 0 to 1 for each pixel.
        //  - TerrainType = we use the tint and startHeight to color each pixel. Simpler than the shader, but leads to jagged edges due to applying the color in a grid pattern.
        //  - Shader = Only applies if a mesh material is set

        // 0. Set the components to use the materials we are using
        if (m_meshMaterial != null && m_renderer != null) m_renderer.material = m_meshMaterial;

        // 1. Check if ColorMode is set to Shader... because if there is no material to set, we can't do anything. We must manually change to `ColorMode.TerrainType`
        else if (m_colorMode == ColorMode.Shader) {
            Debug.LogWarning($"{gameObject.name}: Unable to use ColorMode.Shader due to missing mesh material. Setting to ColorMode.TerrainTypes manually.");
            m_colorMode = ColorMode.TerrainTypes;
        }

        // 2. Based on color mode, render the different color types
        Color[] cMap = new Color[m_mapChunkSize * m_mapChunkSize];
        for(int y = 0; y < m_mapChunkSize; y++) {
            for(int x = 0; x < m_mapChunkSize; x++) {
                float currentHeight = m_heightMap[x,y];
                for(int i = m_meshMaterialLayers.Length-1; i >= 0 ; i--) {
                    float materialHeight = m_meshMaterialLayers[i].startHeight * m_heightRange.max;
                    if (m_colorMode == ColorMode.Gradient) {
                        cMap[y*m_mapChunkSize+x] = m_terrainColorGradient.Evaluate(currentHeight);
                    } else if (currentHeight >= materialHeight) {
                        cMap[y*m_mapChunkSize + x] = m_meshMaterialLayers[i].tint;
                        break;
                    }
                }
            }
        }

        if (m_colorMode == ColorMode.Shader) {
            m_meshMaterial.SetInt("layerCount", m_meshMaterialLayers.Length);
            m_meshMaterial.SetColorArray("baseColors", m_meshMaterialLayers.Select(x => x.tint).ToArray());
            m_meshMaterial.SetFloatArray("baseStartHeights", m_meshMaterialLayers.Select(x => x.startHeight).ToArray());
            m_meshMaterial.SetFloatArray("baseBlends", m_meshMaterialLayers.Select(x => x.blendStrength).ToArray());
            m_meshMaterial.SetFloatArray("baseColorStrength", m_meshMaterialLayers.Select(x => x.tintStrength).ToArray());
            m_meshMaterial.SetFloatArray("baseTextureScales", m_meshMaterialLayers.Select(x => x.textureScale).ToArray());
            Texture2DArray texturesArray = GenerateTextureArray(m_meshMaterialLayers.Select(x => x.texture).ToArray());
            m_meshMaterial.SetTexture("baseTextures", texturesArray);
            m_meshMaterial.SetFloat("inHeight", m_heightRange.min);
            m_meshMaterial.SetFloat("maxHeight", m_heightRange.max);
        }

        return cMap;
    }

    protected virtual MeshData GenerateTerrainMesh(float[,] mapData) {
        int width = mapData.GetLength(0);
        int height = mapData.GetLength(1);
        float topLeftX = (width-1) / -2f;
        float topLeftZ = (height-1) / 2f;

        int meshSimplificationIncrement = (m_levelOfDetail == 0) ? 1 : m_levelOfDetail * 2;
        int verticesPerLine = (width-1) / meshSimplificationIncrement + 1;

        MeshData meshData = new MeshData(verticesPerLine, verticesPerLine);
        int vertexIndex = 0;

        for(int y = 0; y < height; y+=meshSimplificationIncrement) {
            for(int x = 0; x < width; x+=meshSimplificationIncrement) {
                //meshData.vertices[vertexIndex] = new Vector3(topLeftX + x, mapData[x,y], topLeftZ - y);
                meshData.vertices[vertexIndex] = new Vector3(x, mapData[x,y], y);
                meshData.uvs[vertexIndex] = new Vector2(x/(float)width, y/(float)height);
                if (x < m_mapChunkSize-1 && y < m_mapChunkSize-1) {
                    /*
                    meshData.AddTriangle(vertexIndex, vertexIndex+verticesPerLine+1, vertexIndex+verticesPerLine);
                    meshData.AddTriangle(vertexIndex+verticesPerLine+1, vertexIndex, vertexIndex+1);
                    */
                    meshData.AddTriangle(vertexIndex, vertexIndex+verticesPerLine, vertexIndex+verticesPerLine+1);
                    meshData.AddTriangle(vertexIndex+verticesPerLine+1, vertexIndex+1, vertexIndex);
                }
                vertexIndex++;
            }
        }

        return meshData;
    }

    public virtual Texture2DArray GenerateTextureArray(Texture2D[] textures) {
        Texture2DArray textureArray = new Texture2DArray(textureSize, textureSize, textures.Length, textureFormat, true);
        for(int i =0; i < textures.Length; i++) {
            textureArray.SetPixels(textures[i].GetPixels(), i);
        }
        textureArray.Apply();
        return textureArray;
    }

    public virtual void InitializeMaterials() {
        if (m_meshMaterial == null) {
            Debug.Log("Unable to initialize mesh material due to null material.");
            return;
        }

        // Tell the material about our min and max height, which should be set by this point
        if (m_heightRange == null) {
            Debug.Log("Unable to initialize mesh materials due to null height range");
            return;
        }

        m_meshMaterial.SetInt("layerCount", m_meshMaterialLayers.Length);
        m_meshMaterial.SetColorArray("baseColors", m_meshMaterialLayers.Select(x => x.tint).ToArray());
        m_meshMaterial.SetFloatArray("baseStartHeights", m_meshMaterialLayers.Select(x => x.startHeight).ToArray());
        m_meshMaterial.SetFloatArray("baseBlends", m_meshMaterialLayers.Select(x => x.blendStrength).ToArray());
        m_meshMaterial.SetFloatArray("baseColorStrength", m_meshMaterialLayers.Select(x => x.tintStrength).ToArray());
        m_meshMaterial.SetFloatArray("baseTextureScales", m_meshMaterialLayers.Select(x => x.textureScale).ToArray());
        Texture2DArray texturesArray = GenerateTextureArray(m_meshMaterialLayers.Select(x => x.texture).ToArray());
        m_meshMaterial.SetTexture("baseTextures", texturesArray);

        m_meshMaterial.SetFloat("inHeight", m_heightRange.min);
        m_meshMaterial.SetFloat("maxHeight", m_heightRange.max);

        if (m_renderer != null)     m_renderer.material = m_meshMaterial;
    }

    public virtual void DrawMap(Color[] cMap) {
        // We need two things to use DrawMesh():
        //  1. MeshData meshData - built either with noisemap or heightmap
        //  2. Texture2D texture - built based on several conditions
        MeshData mData = (m_drawMode == DrawMode.Mesh) ? GenerateTerrainMesh(m_heightMap) : GenerateTerrainMesh(m_noiseMap);
        Texture2D tData = null;
        switch(m_drawMode) {
            case DrawMode.Color:
                tData = Generators.TextureFromColorMap(m_mapChunkSize, m_mapChunkSize, cMap, m_textureFilterMode);
                break;
            case DrawMode.Mesh:
                tData = Generators.TextureFromColorMap(m_mapChunkSize, m_mapChunkSize, cMap, m_textureFilterMode);
                break;
            default:
                tData = Generators.TextureFromHeightMap(m_noiseMap, m_textureFilterMode);
                break;
        }
        DrawMesh(mData, tData);
    }

    public virtual void RenderMap() {
        Color[] cMap = GenerateColorMap();
        DrawMap(cMap);
    }

    public virtual void DrawTexture(Texture2D texture) {
        if (m_renderer != null) m_renderer.sharedMaterial.mainTexture = texture;
    }
    public virtual void DrawMesh(MeshData meshData, Texture2D texture=null) {
        if (m_filter != null)       m_filter.sharedMesh = meshData.CreateMesh();
        if (m_collider != null)     m_collider.sharedMesh = m_filter.sharedMesh;
        if (texture != null) {
            if (m_renderer != null)     m_renderer.sharedMaterial.mainTexture = texture;
            if (m_heldRenderer != null) m_heldRenderer.sharedMaterial.mainTexture = Generators.FlipTextureHorizontally(texture);
            if (m_canvasImage != null) m_canvasImage.texture = Generators.FlipTextureHorizontally(texture);
            if (m_heldMap != null) m_heldMap.SetMapGroupTexture("Terrain", texture);
        }
    }

    public virtual void DrawCircleOnHeldMap(int x, int y, int radius, Color color) {
        Texture2D original, modified;
        if (m_heldRenderer != null) {
            original = (Texture2D)m_heldRenderer.sharedMaterial.mainTexture;
            modified = Generators.DrawCircleOnTexture(original, x, y, radius, color);
            m_heldRenderer.sharedMaterial.mainTexture = modified;
        }
        if (m_heldMap != null && m_heldMap.TryGetMapGroup("Gems", out HeldMap.MapGroup group)) {
            group.AddCircleToTexture(x,y,radius,color);
        }
    }
    public virtual void DrawCircleOnHeldMap(float x, float z, int radius, Color color) {
        Texture2D original, modified;
        Vector2Int coords;
        if (m_heldRenderer != null) {
            coords = QueryCoordsAtWorldPos(x, z, true);
            original = (Texture2D)m_heldRenderer.sharedMaterial.mainTexture;
            modified = Generators.DrawCircleOnTexture(original, coords.x, coords.y, radius, color);
            m_heldRenderer.sharedMaterial.mainTexture = modified;
        }
        if (m_heldMap != null && m_heldMap.TryGetMapGroup("Gems", out HeldMap.MapGroup group)) {
            coords = QueryCoordsAtWorldPos(x, z, false, false);
            group.AddCircleToTexture(coords.x, coords.y, radius, color);
        }
    }
    public virtual void DrawBoxOnHeldMap(float x, float z, float minX, float minZ, float maxX, float maxZ, Color color) {
        Texture2D original, modified;
        Vector2Int coords, minCoords, maxCoords;
        int w,h;
        if (m_heldRenderer != null) {
            coords = QueryCoordsAtWorldPos(x, z, true);
            minCoords = QueryCoordsAtWorldPos(minX, minZ, true);
            maxCoords = QueryCoordsAtWorldPos(maxX, maxZ, true);
            w = Mathf.Abs(maxCoords.x - minCoords.x);
            h = Mathf.Abs(maxCoords.y - minCoords.y);
            original = (Texture2D)m_heldRenderer.sharedMaterial.mainTexture;
            modified = Generators.DrawBoxOnTexture(original, coords.x, coords.y, w, h, color);
            m_heldRenderer.sharedMaterial.mainTexture = modified;
        }
        if (m_heldMap != null && m_heldMap.TryGetMapGroup("Landmarks", out HeldMap.MapGroup group)) {
            coords = QueryCoordsAtWorldPos(x, z, false, false);
            minCoords = QueryCoordsAtWorldPos(minX, minZ, false, false);
            maxCoords = QueryCoordsAtWorldPos(maxX, maxZ, false, false);
            w = Mathf.Abs(maxCoords.x - minCoords.x);
            h = Mathf.Abs(maxCoords.y - minCoords.y);
            group.AddBoxToTexture(coords.x, coords.y, w, h, color);
        }
    }

    public virtual Vector2 GetMapExtents() {
        float l = (float)(m_mapChunkSize-1)/2f;
        return new Vector2(l,l);
    }

    public virtual MinMax GetHeightRange(float[,] data) {
        int width = data.GetLength(0);
        int height = data.GetLength(1);
        MinMax mm = new MinMax { min=float.MaxValue, max=float.MinValue};
        for(int y = 0; y < height; y++) {
            for(int x = 0; x < width; x++) {
                float v = data[x,y];
                if (v < mm.min) mm.min = v;
                if (v > mm.max) mm.max = v;
            }
        }
        return mm;
    }

    public virtual Vector3 GetRandomPosition(bool useSeedEngine = false, int edgeBuffer = 50) {
        int coordX = (useSeedEngine)
            ? m_prng.Next(0,m_mapChunkSize)
            : UnityEngine.Random.Range(0,m_mapChunkSize);
        int coordY = (useSeedEngine)
            ? m_prng.Next(0,m_mapChunkSize)
            : UnityEngine.Random.Range(0,m_mapChunkSize);
        Debug.Log($"Raw Coords: ["+coordX.ToString()+","+coordY.ToString()+"]");
        
        coordX = Mathf.Clamp(coordX, edgeBuffer, m_mapChunkSize-1-edgeBuffer);
        coordY = Mathf.Clamp(coordY, edgeBuffer, m_mapChunkSize-1-edgeBuffer);
        Debug.Log($"Coords: ["+coordX.ToString()+","+coordY.ToString()+"]");
        
        Vector3 worldPos;
        QueryHeightAtCoords(coordX, coordY, out worldPos);
        Debug.Log("["+coordX.ToString() + ","+coordY.ToString() + "] => " + worldPos.ToString());
        return worldPos;
    }

    public virtual float QueryHeightAtCoords(int x, int y, out Vector3 worldPosition) {
        float mapWidth = (float)m_mapChunkSize - 1f;
        float mapExtent = mapWidth / 2f;

        float worldX = (float)x;
        float worldY = m_heightMap[x,y];
        float worldZ = (float)y;

        worldPosition = new Vector3(worldX, worldY, worldZ);
        return worldY;
    }

    public virtual float QueryHeightAtWorldPos(float worldX, float worldZ, out int x, out int y) {
        x = Mathf.Clamp(Mathf.RoundToInt(worldX), 0, m_mapChunkSize-1);
        y = Mathf.Clamp(Mathf.RoundToInt(worldZ), 0, m_mapChunkSize-1);
        return m_heightMap[x,y];
    }
    public virtual float QueryHeightAtWorldPos(float worldX, float worldZ) {
        int x = Mathf.Clamp(Mathf.RoundToInt(worldX), 0, m_mapChunkSize-1);
        int y = Mathf.Clamp(Mathf.RoundToInt(worldZ), 0, m_mapChunkSize-1);
        return m_heightMap[x,y];
    }

    public virtual float QueryNoiseAtCoords(int x, int y, out Vector3 worldPosition) {
        float noiseX = (float)x;
        float noiseY = m_noiseMap[x,y];
        float noiseZ = (float)y;

        worldPosition = new Vector3(noiseX, noiseY, noiseZ);
        return noiseY;
    }
    public virtual float QueryNoiseAtWorldPos(float worldX, float worldZ, out int x, out int y) {
        x = Mathf.Clamp(Mathf.RoundToInt(worldX), 0, m_mapChunkSize-1);
        y = Mathf.Clamp(Mathf.RoundToInt(worldZ), 0, m_mapChunkSize-1);
        return m_noiseMap[x,y];
    }

    public virtual int QueryRegionAtCoords(int x, int y, out Vector3 worldPosition) {
        float mapWidth = (float)m_mapChunkSize - 1f;
        float mapExtent = mapWidth / 2f;

        float worldX = (float)x - mapExtent;
        float worldY = m_heightMap[x,y];
        float worldZ = mapWidth - (float)y - mapExtent;
        worldPosition = new Vector3(worldX, worldY, worldZ);

        float noiseY = m_noiseMap[x,y];
        int regionIndex = 0;
        for(int i = m_meshMaterialLayers.Length-1; i >= 0 ; i--) {
            if (noiseY >= m_meshMaterialLayers[i].startHeight) {
                regionIndex = i;
                break;
            }
        }
        return regionIndex;
    }
    public virtual int QueryRegionAtWorldPos(float worldX, float worldZ, out int x, out int y) {
        float mapWidth = (float)m_mapChunkSize - 1f;
        float mapExtent = mapWidth / 2f;

        x = Mathf.Clamp(Mathf.RoundToInt(worldX + mapExtent), 0, m_mapChunkSize-1);
        y = Mathf.Clamp(Mathf.RoundToInt(worldZ + mapExtent - mapWidth)*-1, 0, m_mapChunkSize-1);
        
        float noiseY = m_noiseMap[x,y];
        int regionIndex = 0;
        for(int i = m_meshMaterialLayers.Length-1; i >= 0 ; i--) {
            if (noiseY >= m_meshMaterialLayers[i].startHeight) {
                regionIndex = i;
                break;
            }
        }
        return regionIndex;
    }

    public virtual Vector2Int QueryCoordsAtWorldPos(float worldX, float worldZ, bool flipX=false, bool flipY=false) {
        int x = Mathf.Clamp(Mathf.RoundToInt(worldX), 0, m_mapChunkSize-1);
        int y = Mathf.Clamp(Mathf.RoundToInt(worldZ), 0, m_mapChunkSize-1);
        if (flipX) x = (m_mapChunkSize-1) - x;
        if (flipY) y = (m_mapChunkSize-1) - y;
        return new Vector2Int(x,y);
    }

    public virtual Vector3 QueryMapNormalAtWorldPos(float worldX, float worldZ, out int x, out int y, out float worldY) {
        LayerMask queryMask = this.gameObject.layer;
        return QueryMapNormalAtWorldPos(worldX, worldZ, queryMask, out x, out y, out worldY);
    }
    public virtual Vector3 QueryMapNormalAtWorldPos(float worldX, float worldZ, LayerMask mask, out int x, out int y, out float worldY) {
        x = Mathf.Clamp(Mathf.RoundToInt(worldX), 0, m_mapChunkSize-1);
        y = Mathf.Clamp(Mathf.RoundToInt(worldZ), 0, m_mapChunkSize-1);
        worldY = m_heightMap[x,y];

        Vector3 rayStart = new Vector3(worldX, 100f, worldZ);
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 100f, mask)) return hit.normal;

        // If nothing else, return up
        return Vector3.up;
    }


    protected virtual void OnValidate() {}
}

namespace Extensions {
    public static class VectorExtensions {
        public static Vector2 ToVector2(this Vector3 v0) {
            return new Vector2(v0.x, v0.z);
        }
        public static Vector3 ToVector3(this Vector2 v0, float y = 0f) {
            return new Vector3(v0.x, y, v0.y);
        } 
    }
}

