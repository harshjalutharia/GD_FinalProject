using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class TestFinalTerrainManager : MonoBehaviour
{
    public enum FalloffType { Box, NorthSouth, EastWest, Circle }

    [Header("=== Terrrain Grid Settings ===")]
    [SerializeField, Tooltip("The seed used for generation. Passed down to all terrain chunks")]    private int m_seed;
    [SerializeField, Tooltip("The number of columns (along X-axis) along the chunk grid")]          private int m_numCols = 4;
    [SerializeField, Tooltip("The number of rows (along Z-axis) along the chunk grid")]             private int m_numRows = 4;
    [SerializeField, Tooltip("The width (along X-axis) of each individual chunk")]                  private int m_cellWidth = 150;
    [SerializeField, Tooltip("The height (along Z-axis) of each individual chunk")]                 private int m_cellHeight = 150;
    public float width => (float)m_numCols * m_cellWidth;
    public float height => (float)m_numRows * m_cellHeight;
    [SerializeField, Tooltip("The maximum LOD we want to enforce for terrain chunks"), Range(0,6)]  private int m_maxLOD = 6;

    [Header("=== Core References ===")]
    [SerializeField, Tooltip("The TerrainChunk prefab")]    private TerrainChunk m_chunkPrefab;
    [SerializeField, Tooltip("Reference to the player")]    private Transform m_playerRef; 

    [Header("=== Falloff Map Influence ===")]
    [SerializeField, Tooltip("Do we even apply the falloff map?")]  private bool m_applyFalloff = true;
    [SerializeField, Tooltip("The global falloff map settings")]    private Falloff m_globalFalloff;

    [Header("=== Region Determination ===")]
    [SerializeField, Tooltip("Do we even apply the voronoi tessellation?")]             private bool m_applyVoronoi = false;
    [SerializeField, Tooltip("Voronoi region tessellation for region determination")]   private Voronoi m_voronoi;

    private Dictionary<Vector2Int, TerrainChunk> m_chunks;
    private bool chunksInitialized = false;

    #if UNITY_EDITOR
    void OnDrawGizmos() {
        if (!chunksInitialized || m_voronoi == null || m_playerRef == null) return;
        Voronoi.DBScanCluster closestCluster = m_voronoi.QueryCluster(m_playerRef.position);
        Vector3 clusterPos = new Vector3(closestCluster.centroid.x * width, 0f, closestCluster.centroid.z * height);
        Gizmos.color = Color.black;
        Gizmos.DrawCube(clusterPos, Vector3.one * 20);
    }
    #endif

    private void Start() {
        // CLear all existing chunks, and initialize the chunk collection
        ClearChildrenChunks();
        m_chunks = new Dictionary<Vector2Int, TerrainChunk>();

        // For now, set the viewer position to 0,0. If we actually DO have a player, then we get its current position.
        Vector2Int viewerCoords = Vector2Int.zero;
        if (m_playerRef != null) viewerCoords = GetIndicesFromWorldPosition(m_playerRef.position);

        // Initialize falloff width and height
        m_globalFalloff.width = width;
        m_globalFalloff.height = height;

        // If we have a voronoi tessellation set up, then we generate
        if (m_voronoi != null) {
            m_voronoi.SetSeed(m_seed);
            m_voronoi.SetScale(width, height);
            m_voronoi.GenerateTessellation();
        }

        for(int x = 0; x < m_numCols; x++) {
            for(int y = 0; y < m_numRows; y++) {
                // Generate Vector2Int index for this chunk
                Vector2Int index = new Vector2Int(x,y);
                // The world position of this chunk is x=(x*m_cellWidth), y=(y*m_cellHeight);
                Vector3 worldPosition = new Vector3(x*m_cellWidth, 0f, y*m_cellHeight);
                // Instantiate new chunk at this location.
                TerrainChunk chunk = Instantiate(m_chunkPrefab, worldPosition, Quaternion.identity, this.transform) as TerrainChunk;
                // Initialize its m_seed, width and height, offsets, LODs, etc.
                chunk.gameObject.name = $"CHUNK {x},{y}";
                chunk.SetSeed(m_seed);
                chunk.SetDimensions(m_cellWidth, m_cellHeight);
                int chunkLOD = (m_playerRef != null) 
                    ? Mathf.Clamp(Mathf.Max(Mathf.Abs(index.x-viewerCoords.x), Mathf.Abs(index.y - viewerCoords.y)) - 1, 0, m_maxLOD) 
                    : m_maxLOD;
                chunk.SetLevelOfDetail(chunkLOD);
                chunk.SetOffset(x,y);
                if (m_applyVoronoi) chunk.SetVoronoi(m_voronoi);
                if (m_applyFalloff) chunk.SetFalloff(m_globalFalloff);
                // Initialize its coroutine
                chunk.GenerateMap(true);
                // Save a reference to it in our chunks dictionary
                m_chunks.Add(index, chunk);
            }
        }

        chunksInitialized = true;
    }

    private void Update() {
        if (m_playerRef == null || !chunksInitialized) return;

        // Get the current chunk coords of the viewer
        Vector2Int playerChunkCoords = GetIndicesFromWorldPosition(m_playerRef.position);
        
        // For each Terrain Chunk, calculate the intended LOD level. If the LOD level ahs changed, we run the coroutien to update it
        foreach(KeyValuePair<Vector2Int, TerrainChunk> kvp in m_chunks) {
            
            // Get chunk details
            Vector2Int chunkCoords = kvp.Key;
            TerrainChunk chunk = kvp.Value;
            int chunkLOD = chunk.levelOfDetail;

            // Distance. Clamp between 0 and max LOD
            int distance = Mathf.Max(Mathf.Abs(chunkCoords.x - playerChunkCoords.x), Mathf.Abs(chunkCoords.y - playerChunkCoords.y)) - 1;
            int clampedDistance = Mathf.Clamp(distance, 0, m_maxLOD);

            // Check if the LOD is different or not. If so, update
            // We don't need a coroutine because we already generated it. It's just about switching the model, which is a quick function call.
            if (chunkLOD != clampedDistance) chunk.SetLODMesh(clampedDistance);
        
        }
    }

    public Vector2Int GetIndicesFromWorldPosition(Vector3 queryPosition) {
        return new Vector2Int(
            Mathf.FloorToInt(queryPosition.x/m_cellWidth), 
            Mathf.FloorToInt(queryPosition.z/m_cellHeight)
        );
    }

    public void ClearChunks() {
        if (m_chunks != null && m_chunks.Count > 0) foreach(TerrainChunk chunk in m_chunks.Values) if (chunk != null) DestroyImmediate(chunk.gameObject);
    }
    public void ClearChildrenChunks() {
        TerrainChunk[] chunks = GetComponentsInChildren<TerrainChunk>();
        if (chunks.Length > 0) foreach(TerrainChunk chunk in chunks) DestroyImmediate(chunk.gameObject);
    }

    private void OnDisable() {
        StopAllCoroutines();
        ClearChunks();
        ClearChildrenChunks();
    }
}

[System.Serializable]
public class Falloff {
    public enum FalloffType { Box, NorthSouth, EastWest, Circle }
    public FalloffType falloffType;
    public float width, height;
    [Range(0f,1f)] public float centerX = 0.5f, centerY = 0.5f;
    [Range(0f,2f)] public float falloffStart = 0.25f, falloffEnd = 0.5f;
    public AnimationCurve falloffCurve;
    public float GetFalloff(float x, float y) {
        Vector2 coords = new Vector2( x/width, y/height);
        float t = 0f;
        switch(falloffType) {
            case FalloffType.NorthSouth:    t = Mathf.Abs(coords.y-centerY);  break;
            case FalloffType.EastWest:      t = Mathf.Abs(coords.x-centerX);  break;
            case FalloffType.Circle:        t = Vector2.Distance(new Vector2(centerX, centerY), coords);                break;
            default:                        t = Mathf.Max(Mathf.Abs(coords.x-centerX), Mathf.Abs(coords.y-centerY));    break;
        }
        float v = (t < falloffStart) 
            ? 1f 
            : (t > falloffEnd) 
                ? 0f 
                : Mathf.SmoothStep(1f,0f,Mathf.InverseLerp(falloffStart, falloffEnd, t));
        return falloffCurve.Evaluate(v);
    }
}