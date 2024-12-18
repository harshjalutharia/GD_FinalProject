using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class FustrumManager : MonoBehaviour
{
    public static FustrumManager current;

    [Header("=== References ===")]
    [SerializeField, Tooltip("Ref. to the NoiseMap used for world generation")]         private NoiseMap m_terrainGenerator;
    [SerializeField, Tooltip("Ref. to prefab that represents map chunk")]               private FustrumGroup m_mapChunkPrefab;
    [SerializeField, Tooltip("The parent transform to store chunks under")]             private Transform m_parent;

    [Header("=== Fustrum Cameras ===")]
    [SerializeField, Tooltip("The main camera representing the primary perspective")]   private FustrumCamera m_mainFustrumCamera;
    public FustrumCamera mainFustrumCamera => m_mainFustrumCamera;
    [SerializeField, Tooltip("The fustrum camera used when pinpointing landmarks")]          private FustrumCamera m_landmarkFustrumCamera;
    public FustrumCamera landmarkFustrumCamera => m_landmarkFustrumCamera;
    [SerializeField, Tooltip("The fustrum camera used when pinpointing gems")]          private FustrumCamera m_gemFustrumCamera;
    public FustrumCamera gemFustrumCamera => m_gemFustrumCamera;

    [Header("=== Map dimensions & Sizes ===")]
    [SerializeField, Tooltip("The map chunk size")]                         private float m_mapSize;
    [SerializeField, Tooltip("The size of each chunk")]                     private float m_chunkSize;
    [SerializeField, Tooltip("How tall should map chunks be?")]             private float m_chunkHeight = 20f;

    [Header("=== Outputs - READ ONLY ===")]
    [SerializeField, Tooltip("The resulting grid-specific grid size")]              private int m_gridSize;
    [SerializeField, Tooltip("The resulting world space size of the grid")]         private float m_gridDimensions;
    [SerializeField, Tooltip("The resulting world space extents of the grid")]      private float m_gridExtents;
    [SerializeField, Tooltip("The resulting world space anchor point of the grid")] private Vector3 m_anchor;
    private Dictionary<Vector2Int, FustrumGroup> m_coordToChunkMap = new Dictionary<Vector2Int, FustrumGroup>();
    public Dictionary<Vector2Int, FustrumGroup> coordToChunkMap => m_coordToChunkMap;
    [SerializeField, Tooltip("Has this manager been initialized?")]                 private bool m_initialized = false;
    public bool initialized => m_initialized;

    #if UNITY_EDITOR
    [SerializeField] private bool m_showGizmos = false;
    private void OnDrawGizmos() {
        if (!m_showGizmos) return;
        if (m_coordToChunkMap.Count == 0) return;
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(m_anchor, 5f);
    }
    #endif

    private void Awake() {
        current = this;
        if (m_parent == null) m_parent = this.transform;
    }
    
    public void Initialize() {
        // Don't do anything if we ourselves are disabled
        if (!gameObject.activeSelf) return;

        // Let's count the grid that we'll generate. Assuming a square grid, we'll calcualte the grid dimensions by calculating how many chunks will fit inside the provided map size
        m_gridSize = Mathf.CeilToInt(m_mapSize / m_chunkSize);
        
        // Let's now calculate the total grid width, given that we rounded to the closest ceiling
        m_gridDimensions = m_chunkSize * (float)m_gridSize;
        m_gridExtents = m_gridDimensions / 2f;

        // Let's calculate the anchor point, which is the bottom-left if we look at cardinal coordinates
        m_anchor = transform.position - new Vector3(m_gridExtents, 0f, m_gridExtents);

        // Generate our mapper
        m_coordToChunkMap = new Dictionary<Vector2Int, FustrumGroup>();

        // Starting from the anchor point, let's begin generating fustrum groups in the world
        for(int x = 0; x < m_gridSize; x++) {
            for(int y = 0; y < m_gridSize; y++) {
                Vector3 chunkPosition = GetWorldPositionFromCoords(x, y);
                Vector2Int coords = new Vector2Int(x,y);
                // instantiate new chunk
                FustrumGroup group = Instantiate(m_mapChunkPrefab, chunkPosition, Quaternion.identity) as FustrumGroup;
                group.transform.parent = m_parent;
                group.transform.localScale = new Vector3(m_chunkSize, m_chunkHeight, m_chunkSize);
                // Add chunk to our dictionary of chunks
                m_coordToChunkMap.Add(coords, group);
            }
        }

        // Initialize this game object
        m_initialized = true;
    }

    // Technically, each fustrumgroup has its own collider, as well as its own list of children.
    // We don't need to do ANYTHING else here. The fustrum groups themselves will manage themselves via their own update loop.
    // This component therefore only needs to be here to help fustrum groups identify what major fustrum chunk they effectively are in.

    public Vector3 GetWorldPositionFromCoords(int x, int y) {
        // To map from grid coords to world position, we need to understand what's going on with the grid system here.
        // The world scale dimension is defined as `m_gridDimension`. The `m_anchor` is where, in the world scale, the "start" of the grid is.
        // We simply just need to return the anchor's position, plus the offset generated by m_chunkSize * (coord * 0.5). 
        // Keep in mind outliers where x or y are outside the grid range...
        int queryX = Mathf.Clamp(x, 0, m_gridSize);
        int queryY = Mathf.Clamp(y, 0, m_gridSize);

        float worldX = m_anchor.x + m_chunkSize * ((float)queryX + 0.5f);
        float worldZ = m_anchor.z + m_chunkSize * ((float)queryY + 0.5f);
        Debug.Log($"({queryX},{queryY}) -> ({worldX},{worldZ})");
        float worldY = m_terrainGenerator.QueryHeightAtWorldPos(worldX, worldZ, out int dumX, out int dumY);

        return new Vector3(worldX, worldY, worldZ);
    }

    public Vector2Int GetCoordsFromWorldPosition(Vector3 position) {
        // To map from world position to grid coords, we have to identify where, relative to our grid's `m_anchor`, we are.
        // We only care about the provided position's X and Z coordinates in this case.
        // Keep in mind we need to clamp the provided position values by the grid extents
        float queryX = Mathf.Clamp(position.x, m_anchor.x, transform.position.x+m_gridExtents);
        float queryZ = Mathf.Clamp(position.z, m_anchor.z, transform.position.z+m_gridExtents);

        // Given queryX and queryZ, we simply have to calculate the difference between the position and the anchor position
        float diffX = queryX - m_anchor.x;
        float diffZ = queryZ - m_anchor.z;

        // Then it's simply a matter of dividing the difference by chunk size, flooring to ensure we stay within grid dimensions
        return new Vector2Int(Mathf.FloorToInt(diffX/m_chunkSize), Mathf.FloorToInt(diffZ/m_chunkSize));
    }

    public void SetMainFustrumCamera(FustrumCamera newMain) {
        m_mainFustrumCamera = newMain;
    }
    public void SetMainFustrumCamera(Camera newMain) {
        FustrumCamera fustrumCam = newMain.gameObject.GetComponent<FustrumCamera>();
        if (fustrumCam != null) m_mainFustrumCamera = fustrumCam;
    }
}
