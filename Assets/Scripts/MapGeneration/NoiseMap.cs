using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoiseMap : MonoBehaviour
{
    public enum DrawMode { None, Noise, Color, Mesh}
    public enum ColorMode { Gradient, TerrainTypes }

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

    [Header("=== Map Settings ===")]
    [SerializeField] protected int m_seed;
    [SerializeField] protected Vector2Int m_dimensions = new Vector2Int(100,100);
    [SerializeField] protected int m_mapChunkSize = 241;
    [SerializeField, Range(0,6)] protected int m_levelOfDetail;
    protected System.Random m_prng;

    [Header("=== Render Settings ===")]
    [SerializeField] protected MeshFilter m_filter;
    [SerializeField] protected MeshRenderer m_renderer;
    [SerializeField] protected MeshCollider m_collider;
    [SerializeField] protected MeshRenderer m_heldRenderer;
    [SerializeField] protected DrawMode m_drawMode = DrawMode.Noise;
    [SerializeField] protected FilterMode m_textureFilterMode = FilterMode.Point;
    [SerializeField] protected ColorMode m_colorMode = ColorMode.TerrainTypes;
    [SerializeField] protected AnimationCurve m_textureHeightCurve;
    [SerializeField] protected float m_textureHeightMultiplier = 2000f;
    [SerializeField] protected TerrainType[] m_terrainTypes;
    [SerializeField] protected Gradient m_terrainColorGradient;
    [SerializeField] protected Material m_meshMaterial;

    [Header("=== Debug Settings ===")]
    [SerializeField] private bool m_autoUpdate;
    public bool autoUpdate => m_autoUpdate;

    [Header("=== Outputs - READ ONLY ===")]
    [SerializeField] protected float[,] m_noiseMap;
    public float[,] noiseMap => m_noiseMap;
    [SerializeField] protected float[,] m_heightMap;
    public float[,] heightMap => m_heightMap;
    [SerializeField] protected MinMax m_heightRange;
    public MinMax heightRange => m_heightRange;
    [SerializeField] protected Color[] m_colorMap;
    public Color[] colorMap => m_colorMap;
    

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

    public virtual void SetDimensions(int x, int y) {
        m_dimensions = new Vector2Int(x,y);
    }
    public virtual void SetDimensions(Vector2Int xy) {
        m_dimensions = xy;
    }

    public virtual void SetChunkSize(int newSize) {
        m_mapChunkSize = newSize;
    }

    public virtual void SetLevelOfDetail(int newLOD) {
        m_levelOfDetail = Mathf.Clamp(newLOD, 0, 6);
    }

    public virtual void GenerateMap() {
        m_prng = new System.Random(m_seed);
        m_noiseMap = Generators.GenerateRandomMap(m_mapChunkSize, m_mapChunkSize, m_prng);
        m_heightMap = Generators.GenerateHeightMap(m_noiseMap, m_textureHeightCurve, m_textureHeightMultiplier);
        m_heightRange = GetHeightRange(m_heightMap);
        if (m_drawMode != DrawMode.None) RenderMap();
    }

    public virtual void GenerateColorMap() {
        // We want to attribute each pixel in our height map with terrain details
        m_colorMap = new Color[m_mapChunkSize * m_mapChunkSize];
        for(int y = 0; y < m_mapChunkSize; y++) {
            for(int x = 0; x < m_mapChunkSize; x++) {
                float currentHeight = m_noiseMap[x,y];
                switch(m_colorMode) {
                    case ColorMode.TerrainTypes:
                        for(int i = 0; i < m_terrainTypes.Length; i++) {
                            if (currentHeight <= m_terrainTypes[i].height) {
                                m_colorMap[y*m_mapChunkSize + x] = m_terrainTypes[i].color;
                                break;
                            }
                        }
                        break;
                    default:
                        m_colorMap[y*m_mapChunkSize+x] = m_terrainColorGradient.Evaluate(currentHeight);
                        break;
                }
            }
        }
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
                meshData.vertices[vertexIndex] = new Vector3(topLeftX + x, mapData[x,y], topLeftZ - y);
                meshData.uvs[vertexIndex] = new Vector2(x/(float)width, y/(float)height);
                if (x < m_mapChunkSize-1 && y < m_mapChunkSize-1) {
                    meshData.AddTriangle(vertexIndex, vertexIndex+verticesPerLine+1, vertexIndex+verticesPerLine);
                    meshData.AddTriangle(vertexIndex+verticesPerLine+1, vertexIndex, vertexIndex+1);
                }
                vertexIndex++;
            }
        }

        return meshData;
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

        m_meshMaterial.SetFloat("inHeight", m_heightRange.min);
        m_meshMaterial.SetFloat("maxHeight", m_heightRange.max);

        if (m_renderer != null)     m_renderer.material = m_meshMaterial;
        if (m_heldRenderer != null) m_heldRenderer.material = m_meshMaterial;
    }

    public virtual void DrawMap() {
        // We then draw the texture of the map
        switch(m_drawMode) {
            case DrawMode.Color:
                DrawMesh(GenerateTerrainMesh(m_noiseMap), Generators.TextureFromColorMap(m_mapChunkSize, m_mapChunkSize, m_colorMap, m_textureFilterMode));
                break;
            case DrawMode.Mesh:
                DrawMesh(GenerateTerrainMesh(m_heightMap), Generators.TextureFromColorMap(m_mapChunkSize, m_mapChunkSize, m_colorMap, m_textureFilterMode));
                break;
            default:
                DrawMesh(GenerateTerrainMesh(m_noiseMap), Generators.TextureFromHeightMap(m_noiseMap, m_textureFilterMode));
                break;
        }
    }

    public virtual void RenderMap() {
        GenerateColorMap();
        InitializeMaterials();
        DrawMap();
    }

    public virtual void DrawTexture(Texture2D texture) {
        if (m_renderer != null) m_renderer.sharedMaterial.mainTexture = texture;
    }
    public virtual void DrawMesh(MeshData meshData, Texture2D texture) {
        if (m_filter != null)       m_filter.sharedMesh = meshData.CreateMesh();
        if (m_collider != null)     m_collider.sharedMesh = m_filter.sharedMesh;
        if (m_renderer != null)     m_renderer.sharedMaterial.mainTexture = texture;
        if (m_heldRenderer != null) m_heldRenderer.sharedMaterial.mainTexture = Generators.FlipTextureHorizontally(texture);
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

        float worldX = (float)x - mapExtent;
        float worldY = m_heightMap[x,y];
        float worldZ = mapWidth - (float)y - mapExtent;

        worldPosition = new Vector3(worldX, worldY, worldZ);
        return worldY;
    }

    public virtual float QueryHeightAtWorldPos(float worldX, float worldZ, out int x, out int y) {
        Vector3 worldPos = new Vector3(worldX, transform.position.y, worldZ) - transform.position;
        float mapWidth = (float)m_mapChunkSize - 1f;
        float mapExtent = mapWidth / 2f;

        x = Mathf.Clamp(Mathf.RoundToInt(worldPos.x + mapExtent), 0, m_mapChunkSize-1);
        y = Mathf.Clamp(Mathf.RoundToInt(worldPos.z + mapExtent - mapWidth)*-1, 0, m_mapChunkSize-1);
        return m_heightMap[x,y];
    }

    public virtual float QueryNoiseAtCoords(int x, int y, out Vector3 worldPosition) {
        float mapWidth = (float)m_mapChunkSize - 1f;
        float mapExtent = mapWidth / 2f;

        float noiseX = (float)x - mapExtent;
        float noiseY = m_noiseMap[x,y];
        float noiseZ = mapWidth - (float)y - mapExtent;

        worldPosition = new Vector3(noiseX, noiseY, noiseZ);
        return noiseY;
    }
    public virtual float QueryNoiseAtWorldPos(float worldX, float worldZ, out int x, out int y) {
        float mapWidth = (float)m_mapChunkSize - 1f;
        float mapExtent = mapWidth / 2f;

        x = Mathf.Clamp(Mathf.RoundToInt(worldX + mapExtent), 0, m_mapChunkSize-1);
        y = Mathf.Clamp(Mathf.RoundToInt(worldZ + mapExtent - mapWidth)*-1, 0, m_mapChunkSize-1);
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
        for(int i = 0; i < m_terrainTypes.Length; i++) {
            if (noiseY <= m_terrainTypes[i].height) {
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
        for(int i = 0; i < m_terrainTypes.Length; i++) {
            if (noiseY <= m_terrainTypes[i].height) {
                regionIndex = i;
                break;
            }
        }
        return regionIndex;
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

