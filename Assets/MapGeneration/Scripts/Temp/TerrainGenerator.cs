using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class TerrainGenerator : MonoBehaviour
{
    public enum DrawMode { Noise, Falloff, Combined, Color, Mesh}
    public enum ColorMode { Gradient, TerrainTypes }

    [Header("=== Generation Settings ===")]
    [SerializeField] private MeshFilter m_meshFilter;
    [SerializeField] private Renderer m_renderer;
    [SerializeField] private MeshCollider m_meshCollider;
    [SerializeField] private Renderer m_heldMapRenderer;
    [SerializeField] private Vector2Int m_mapDimensions = new Vector2Int(100,100);
    [SerializeField] private DrawMode m_drawMode = DrawMode.Noise;
    [SerializeField] private bool m_autoUpdate;
    public bool autoUpdate => m_autoUpdate;
    private System.Random m_prng;

    [Header("=== Perlin Noise Settings")]
                                    public int seed;
    [SerializeField]                private float m_noiseScale = 15f;
    [SerializeField]                private int m_octaves = 4;
    [SerializeField, Range(0f,1f)]  private float m_persistance = 0.5f;
    [SerializeField]                private float m_lacunarity;
    [SerializeField]                private Vector2 m_offset;
    [SerializeField]                private Generators.NormalizeMode m_normalizeMode;

    [Header("=== Falloff Settings ===")]
    [SerializeField, Range(0f,1f)]  private float m_falloffStart = 1f;
    [SerializeField, Range(0f,1f)]  private float m_falloffEnd = 1f;

    [Header("=== Height Map Settings ===")]
    [SerializeField] private FilterMode m_textureFilterMode = FilterMode.Point;
    [SerializeField] private ColorMode m_colorMode = ColorMode.TerrainTypes;
    [SerializeField] private AnimationCurve m_textureHeightCurve;
    [SerializeField] private float m_textureHeightMultiplier = 2000f;
    public TerrainType[] m_terrainTypes;
    [SerializeField] private Gradient m_terrainGradient;

    [Header("=== Vegetation Settings ===")]
    [SerializeField] private bool m_generateVegetation = true;
    [SerializeField] private int m_treeDensity = 200;
    [SerializeField] private float m_minVegetationHeight = 0.5f;
    [SerializeField] private float m_maxVegetationHeight = 10f;
    [SerializeField] private GameObject m_treePrefab;
    [SerializeField] private List<GameObject> m_trees = new List<GameObject>();

    [Header("=== Playtime Settings ===")]
    private float[,] m_combinedMap;
    private Color[] m_colorMap;
    [SerializeField] private LayerMask m_waypointLayerMask;

    #if UNITY_EDITOR
    [Header("== DEBUG ===")]
    [SerializeField] private bool m_showGrid = false;
    [SerializeField] private Color m_gridColor = Color.white;
    [SerializeField] private int m_gridCellSize;
    
    private void OnDrawGizmos() {
        if (!m_showGrid) return;
        Gizmos.color = m_gridColor;

        Vector3 extents = new Vector3((float)m_mapDimensions.x/2f, 0f, (float)m_mapDimensions.y/2f);
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
        
        
        
    }
    #endif

    private void Awake() {
        // We create a mesh and set it to our mesh filter's mesh
        //mesh = new Mesh(); 
        //m_meshFilter.mesh = mesh;

        // The mesh defines our PCG mesh. Let's initialize our list of vertices
        GenerateMap();
    }

    public void GenerateMap() {
        // Before anything, we must generate our random generator based on our seed value
        m_prng = new System.Random(seed);

        // We first generate the noise map from Perlin Noise
        float[,] noiseMap = Generators.GenerateNoiseMap(
            m_mapDimensions.x, m_mapDimensions.y, m_noiseScale,
            m_prng, 
            m_octaves, m_persistance, m_lacunarity, m_offset,
            m_normalizeMode
        );

        // We also create a falloff map that controls the edges of the map, creating islands
        float[,] falloffMap = Generators.GenerateFalloffMap(
            m_mapDimensions.x, m_mapDimensions.y, 
            m_falloffStart, m_falloffEnd
        );
        
        // We multiply the two maps together to create a combined island-based falloff map.
        m_combinedMap = Generators.MultiplyMap(
            m_mapDimensions.x, m_mapDimensions.y,
            noiseMap, falloffMap
        );

        // We want to attribute each pixel in our height map with terrain details
        m_colorMap = new Color[m_mapDimensions.x * m_mapDimensions.y];
        for(int y = 0; y < m_mapDimensions.y; y++) {
            for(int x = 0; x < m_mapDimensions.x; x++) {
                float currentHeight = m_combinedMap[x,y];
                switch(m_colorMode) {
                    case ColorMode.TerrainTypes:
                        for(int i = 0; i < m_terrainTypes.Length; i++) {
                            if (currentHeight <= m_terrainTypes[i].height) {
                                m_colorMap[y*m_mapDimensions.x + x] = m_terrainTypes[i].color;
                                break;
                            }
                        }
                        break;
                    default:
                        m_colorMap[y*m_mapDimensions.x+x] = m_terrainGradient.Evaluate(currentHeight);
                        break;
                }
            }
        }

        // We then draw the texture of the map
        switch(m_drawMode) {
            case DrawMode.Color:
                DrawMesh(Generators.GenerateTerrainMesh(m_combinedMap, m_textureHeightCurve, 0f), Generators.TextureFromColorMap(m_mapDimensions.x, m_mapDimensions.y, m_colorMap, m_textureFilterMode));
                ClearVegetation();
                break;
            case DrawMode.Falloff:
                DrawMesh(Generators.GenerateTerrainMesh(falloffMap, m_textureHeightCurve, 0f), Generators.TextureFromHeightMap(falloffMap, m_textureFilterMode));
                ClearVegetation();
                break;
            case DrawMode.Combined:
                DrawMesh(Generators.GenerateTerrainMesh(m_combinedMap, m_textureHeightCurve, 0f), Generators.TextureFromHeightMap(m_combinedMap, m_textureFilterMode));
                ClearVegetation();
                break;
            case DrawMode.Mesh:
                DrawMesh(Generators.GenerateTerrainMesh(m_combinedMap, m_textureHeightCurve, m_textureHeightMultiplier), Generators.TextureFromColorMap(m_mapDimensions.x, m_mapDimensions.y, m_colorMap, m_textureFilterMode));
                // If prompted, we can generate vegetation!
                if (!m_generateVegetation) return;
                GenerateVegetation();
                break;
            default:
                DrawMesh(Generators.GenerateTerrainMesh(noiseMap, m_textureHeightCurve, 0f), Generators.TextureFromHeightMap(noiseMap, m_textureFilterMode));
                ClearVegetation();
                break;
        }
    }

    public void DrawTexture(Texture2D texture) {
        m_renderer.sharedMaterial.mainTexture = texture;
        //m_renderer.transform.localScale = new Vector3(texture.width, 1f, texture.height);
    }

    public void DrawMesh(MeshData meshData, Texture2D texture) {
        m_meshFilter.sharedMesh = meshData.CreateMesh();
        m_meshCollider.sharedMesh = m_meshFilter.sharedMesh;
        m_renderer.sharedMaterial.mainTexture = texture;
        if (m_heldMapRenderer != null) m_heldMapRenderer.sharedMaterial.mainTexture = Generators.FlipTextureHorizontally(texture);
    }

    public void DrawDestinationOnMap(int x, int y, int radius, Color color) {
        if (m_heldMapRenderer == null) return;
        // Get the texture already drawon onto the held map
        Texture2D original = (Texture2D)m_heldMapRenderer.sharedMaterial.mainTexture;
        m_heldMapRenderer.sharedMaterial.mainTexture = Generators.DrawCircleOnTexture(original, x, y, radius, color);
    }

    public void GenerateVegetation() {
        // Firstly, clear any existing vegetation
        ClearVegetation();

        int halfWidth = m_mapDimensions.x / 2;
        int halfHeight = m_mapDimensions.y / 2;
        Vector2Int vegetationXRange = new Vector2Int(-halfWidth, halfWidth);
        Vector2Int vegetationYRange = new Vector2Int(-halfHeight, halfHeight);

        for(int i = 0; i < m_treeDensity; i++) {
            // Generate a random position within our map
            float sampleX = m_prng.Next(vegetationXRange.x, vegetationXRange.y);
            float sampleY = m_prng.Next(vegetationYRange.x, vegetationYRange.y);
            Vector3 rayStart = new Vector3(sampleX, m_maxVegetationHeight, sampleY);
            RaycastHit hit;

            // Use raycast to check if the ground unerneath is viable. If not, continue;
            if (!Physics.Raycast(rayStart, Vector3.down, out hit, Mathf.Infinity, m_waypointLayerMask))  continue;
            int hitX = Mathf.FloorToInt(hit.point.x + halfWidth);
            int hitY = Mathf.FloorToInt(hit.point.z + halfHeight);
            if (m_combinedMap[hitX,hitY] < 0.3f)                                    continue;
            if (hit.collider != m_meshCollider)                                     continue;
            if (Vector3.Angle(hit.normal, Vector3.up) > 60f)                        continue;
            
            // Instantiate the object 
            GameObject instantiatedTree;
            instantiatedTree = Instantiate(m_treePrefab, hit.point, Quaternion.identity, this.transform);
            m_trees.Add(instantiatedTree);
        }
    }

    public void ClearVegetation() {
        while(m_trees.Count > 0) {
            GameObject go = m_trees[0];
            m_trees.RemoveAt(0);
            DestroyImmediate(go);
        }

        // Maybe there might be stragglers..
        List<GameObject> remainingVegetation = new List<GameObject>(GameObject.FindGameObjectsWithTag("Vegetation"));
        if (remainingVegetation.Count == 0) return;
        while(remainingVegetation.Count > 0) {
            GameObject go = remainingVegetation[0];
            remainingVegetation.RemoveAt(0);
            DestroyImmediate(go);
        }
    }

    public void SetSeed(string seedInput) {
        if (seedInput.Length > 0 && int.TryParse(seedInput, out int newSeed)) {
            seed = newSeed;
            return;
        }
        seed = UnityEngine.Random.Range(0, 1000001);
    }

    public void GetStartAndEnd(float minDistanceBetween, out Vector3 pointA, out Vector3 pointB) {
        // Initialize foundA and foundB, which are our flags
        bool foundA = false, foundB = false;
        pointA = Vector3.zero;
        pointB = Vector3.zero;

        // get the extents of the map
        float widthHalf = m_mapDimensions.x/2f;
        float heightHalf = m_mapDimensions.y/2f;

        RaycastHit hit;
        int hitX, hitY;
        float normHitY = 0f;

        // Find the start position
        while(!foundA) {
            float x = UnityEngine.Random.Range(-widthHalf, widthHalf);
            float y = UnityEngine.Random.Range(-heightHalf, heightHalf);
            Vector3 rayStart = new Vector3(x,100f,y);
            if (Physics.Raycast(rayStart, Vector3.down, out hit, Mathf.Infinity, m_waypointLayerMask)) {
                // Hit something. Check if it's safe
                hitX = Mathf.FloorToInt(hit.point.x + widthHalf);
                hitY = Mathf.FloorToInt(hit.point.z + heightHalf);
                if (hitX < 0) hitX = 0;
                if (hitY < 0) hitY = 0;
                normHitY = m_combinedMap[hitX,hitY];
                // Check if this is above water
                if (normHitY > 0.3f) {
                    pointA = new Vector3(x,hit.point.y, y);
                    foundA = true;
                }
            }
        }

        // Find the destination point. Similar... but requires an additional distance check
        while(!foundB) {
            float x = UnityEngine.Random.Range(-widthHalf, widthHalf);
            float y = UnityEngine.Random.Range(-heightHalf, heightHalf);
            Vector3 rayStart = new Vector3(x,100f,y);
            if (Physics.Raycast(rayStart, Vector3.down, out hit, Mathf.Infinity, m_waypointLayerMask)) {
                // Hit something. Check if it's safe
                hitX = Mathf.FloorToInt(hit.point.x + widthHalf);
                hitY = Mathf.FloorToInt(hit.point.z + heightHalf);
                if (hitX < 0) hitX = 0;
                if (hitY < 0) hitY = 0;
                normHitY = m_combinedMap[hitX,hitY];
                // Check if this is above water
                if (normHitY > 0.3f) {
                    pointB = new Vector3(x,hit.point.y, y);
                    // Check once more - is the distance between A and B long enough?
                    if (Vector3.Distance(pointA, pointB) >= minDistanceBetween) foundB = true;
                }
            }
        }

        // Take the time to draw the destination (pointB) onto the map
        // We need to re-draw the map first, to prevent double-destination-drawing
        if (m_heldMapRenderer == null) {
            Debug.Log("Cannot render on missing held map renderer reference");
            return;
        }
        m_heldMapRenderer.sharedMaterial.mainTexture = Generators.FlipTextureHorizontally(Generators.TextureFromColorMap(m_mapDimensions.x, m_mapDimensions.y, m_colorMap, m_textureFilterMode));
        int destX = m_mapDimensions.x - Mathf.FloorToInt(pointB.x + widthHalf);
        int destY = m_mapDimensions.y - Mathf.FloorToInt(pointB.z + heightHalf);
        DrawDestinationOnMap(destX, destY, 6, Color.black);
        DrawDestinationOnMap(destX, destY, 5, Color.red);

        // Can return safely
        return;
    }

    void OnValidate() {
        if (m_mapDimensions.x < 1) m_mapDimensions.x = 1;
        if (m_mapDimensions.y < 1) m_mapDimensions.y = 1;
        if (m_lacunarity < 1) m_lacunarity = 1;
        if (m_octaves < 0) m_octaves = 0;
    }
}

[System.Serializable]
public class TerrainType {
    public string name;
    public float height;
    public Color color;
}