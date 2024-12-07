using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class TestFinalTerrainManager : MonoBehaviour
{
    public int seed;
    public int numCols = 4;
    public int numRows = 4;
    public int cellWidth = 150;
    public int cellHeight = 150;
    private int m_halfWidth, m_halfHeight;
    [Space]
    public TerrainChunk chunkPrefab;
    /*
    public TerrainChunk m_northChunkPrefab;
    public TerrainChunk m_southChunkPrefab;
    public TerrainChunk m_eastChunkPrefab;
    public TerrainChunk m_westChunkPrefab;
    public TerrainChunk m_middleChunkPrefab;
    [Space]
    public TerrainChunk m_northEastChunkPrefab;
    public TerrainChunk m_southEastChunkPrefab;
    public TerrainChunk m_northWestChunkPrefab;
    public TerrainChunk m_southWestChunkPrefab;
    */
    [Space]
    public Transform viewerRef; 
    [Range(0,6)] public int maxLOD = 6;
    public float m_timeToChangeLOD = 5f;

    private Dictionary<Vector2Int, TerrainChunk> m_chunkPrefabByDirection;
    private Dictionary<Vector2Int, TerrainChunk> m_chunks;
    private Dictionary<Vector2Int, List<Vector2Int>> m_halfChunks;
    private Dictionary<Vector2Int, bool> m_chunkUpdating;
    public List<HalfChunk> m_halfChunksDebug;
    public Vector2Int m_prevChunkCoords = Vector2Int.zero, m_prevHalfCoords = Vector2Int.zero;
    private float m_timeSinceLastChange = 0f;
    private bool chunksInitialized = false;

    #if UNITY_EDITOR
    private void OnDrawGizmos() {
        if (!Application.isPlaying) return; 

        if (m_chunks != null && m_chunks.Count > 0 && m_halfChunks != null && m_halfChunks.Count > 0) {
            Gizmos.color = Color.yellow;
            if (!m_halfChunks.ContainsKey(m_prevHalfCoords)) return;
            foreach(Vector2Int chunk in m_halfChunks[m_prevHalfCoords]) {
                Gizmos.DrawWireCube(m_chunks[chunk].transform.position + new Vector3(cellWidth/2, 0, cellHeight/2), new Vector3(cellWidth, cellWidth, cellHeight));
            }
        }
    }
    #endif

    private void Awake() {
        /*
        m_chunkPrefabByDirection = new Dictionary<Vector2Int, TerrainChunk>() {
            {new Vector2Int(0,0), m_southWestChunkPrefab},
            {new Vector2Int(0,1), m_westChunkPrefab},
            {new Vector2Int(0,2), m_northWestChunkPrefab},
            {new Vector2Int(1,0), m_southChunkPrefab},
            {new Vector2Int(1,1), m_middleChunkPrefab},
            {new Vector2Int(1,2), m_northChunkPrefab},
            {new Vector2Int(2,0), m_southEastChunkPrefab},
            {new Vector2Int(2,1), m_eastChunkPrefab},
            {new Vector2Int(2,2), m_northEastChunkPrefab}
        };
        */
    }

    private void Start() {
        if (viewerRef != null) {
            GetIndicesFromWorldPosition(viewerRef.position, out m_prevChunkCoords, out m_prevHalfCoords);
        }

        ClearChildrenChunks();
        m_chunks = new Dictionary<Vector2Int, TerrainChunk>();
        m_chunkUpdating = new Dictionary<Vector2Int, bool>();

        Vector2Int viewerCoords = Vector2Int.zero;
        if (viewerRef != null) {
            viewerCoords = GetIndicesFromWorldPosition(viewerRef.position);
        }

        for(int x = 0; x < numCols; x++) {
            int prefabXIndex = (x == 0) ? 0 : (x == numCols-1) ? 2 : 1;
            for(int y = 0; y < numRows; y++) {
                // Generate the prefab index we'll be using, based on our position in our grid.
                int prefabYIndex = (y == 0) ? 0 : (y == numRows-1) ? 2 : 1;
                Vector2Int prefabIndex = new Vector2Int(prefabXIndex, prefabYIndex);
                //TerrainChunk prefab = m_chunkPrefabByDirection[prefabIndex];
                // Generate Vector2Int index for this chunk
                Vector2Int index = new Vector2Int(x,y);
                // The world position of this chunk is x=(x*cellWidth), y=(y*cellHeight);
                Vector3 worldPosition = new Vector3(x*cellWidth, 0f, y*cellHeight);
                // Instantiate new chunk at this location.
                //TerrainChunk chunk = Instantiate(prefab, worldPosition, Quaternion.identity, this.transform) as TerrainChunk;
                TerrainChunk chunk = Instantiate(chunkPrefab, worldPosition, Quaternion.identity, this.transform) as TerrainChunk;
                // Initialize its seed, width and height, offsets, LODs, etc.
                chunk.gameObject.name = $"CHUNK {x},{y}";
                chunk.SetSeed(seed);
                chunk.SetDimensions(cellWidth, cellHeight);
                int chunkLOD = (viewerRef != null) 
                    ? Mathf.Clamp(Mathf.Max(Mathf.Abs(index.x-viewerCoords.x), Mathf.Abs(index.y - viewerCoords.y)) - 1, 0, maxLOD) 
                    : maxLOD;
                chunk.SetLevelOfDetail(chunkLOD);
                chunk.SetOffset(x,y);
                // Initialize its coroutine
                chunk.GenerateMap(true);
                // Save a reference to it in our chunks dictionary
                m_chunks.Add(index, chunk);
                // Update update dict
                m_chunkUpdating.Add(index, false);
            }
        }

        /*
        // Generate a sub-graph, given half the cell size
        m_halfWidth = cellWidth / 2;
        m_halfHeight = cellHeight / 2;
        int numHalfCols = (numCols*cellWidth)/m_halfWidth + 1;
        int numHalfRows = (numRows*cellHeight)/m_halfHeight + 1;

        // Generate dictionary for half chunks
        m_halfChunks = new Dictionary<Vector2Int, List<Vector2Int>>();
        m_halfChunksDebug = new List<HalfChunk>();
        for(int xi = 0; xi < numHalfCols; xi++) {
            for(int yi = 0; yi < numHalfRows; yi++) {
                // Half Cell Index
                Vector2Int halfIndex = new Vector2Int(xi,yi);
                m_halfChunks.Add(halfIndex, new List<Vector2Int>());
                HalfChunk hc = new HalfChunk{indices=halfIndex};
                HalfToChunkIndexConversion(halfIndex, out Vector2Int xRange, out Vector2Int yRange);
                for(int i = xRange.x; i <= xRange.y; i++) {
                    for(int j = yRange.x; j <= yRange.y; j++) {
                        Vector2Int chunkIndex = new Vector2Int(i,j);
                        m_halfChunks[halfIndex].Add(chunkIndex);
                        hc.chunkIndices.Add(chunkIndex);
                    }
                }
                m_halfChunksDebug.Add(hc);
            }
        }
        */

        chunksInitialized = true;
    }

    private void Update() {
        if (viewerRef == null || !chunksInitialized) return;

        // Get the current chunk coords of the viewer
        Vector2Int playerChunkCoords = GetIndicesFromWorldPosition(viewerRef.position);
        
        // For each Terrain Chunk, calculate the intended LOD level. If the LOD level ahs changed, we run the coroutien to update it
        foreach(KeyValuePair<Vector2Int, TerrainChunk> kvp in m_chunks) {
            // Get chunk details
            Vector2Int chunkCoords = kvp.Key;
            TerrainChunk chunk = kvp.Value;
            int chunkLOD = chunk.levelOfDetail;
            // Distance. Clamp between 0 and max LOD
            int distance = Mathf.Max(Mathf.Abs(chunkCoords.x - playerChunkCoords.x), Mathf.Abs(chunkCoords.y - playerChunkCoords.y)) - 1;
            int clampedDistance = Mathf.Clamp(distance, 0, maxLOD);

            // Check if the LOD is different or not. If so, run the coroutine to update
            if (chunkLOD != clampedDistance && !m_chunkUpdating[chunkCoords]) {
                // If we're currently updating, we must stop it and re-update it
                //StartCoroutine(UpdateChunk(chunkCoords, chunk, clampedDistance));
                chunk.SetLevelOfDetail(clampedDistance);
                chunk.GenerateMeshData();
            }
        }
        /*
        GetIndicesFromWorldPosition(viewerRef.position, out Vector2Int chunkCoords, out Vector2Int halfCoords);
        if (m_prevHalfCoords != halfCoords) {
            m_timeSinceLastChange += Time.deltaTime;
            if (m_timeSinceLastChange >= m_timeToChangeLOD) {
                //RecalculateLODs(chunkCoords, m_halfChunks[halfCoords]);
                m_prevChunkCoords = chunkCoords;
                m_prevHalfCoords = halfCoords;
                m_timeSinceLastChange = 0f;
                Debug.Log($"Changed to Half Coords = {halfCoords.ToString()}");
            }
        } else {
            m_timeSinceLastChange = 0f;
        }
        */
    }

    /*
    public void RecalculateLODs(Vector2Int chunkCoords, List<Vector2Int> forceLOD0Coords) {
        foreach(KeyValuePair<Vector2Int, TerrainChunk> kvp in m_chunks) {
            int newLevelOfDetail;
            if (forceLOD0Coords.Contains(kvp.Key)) newLevelOfDetail = 0;
            else {
                newLevelOfDetail = Mathf.Clamp(
                    Mathf.FloorToInt(Mathf.Max(Mathf.Abs(kvp.Key.x - chunkCoords.x), Mathf.Abs(kvp.Key.y - chunkCoords.y))), 
                    0, maxLOD
                );
            }
            if (kvp.Value.levelOfDetail != newLevelOfDetail) {
                kvp.Value.SetLevelOfDetail(newLevelOfDetail);
                StartCoroutine(kvp.Value.GenerateMapCoroutine(false));
            }
        }
    }
    */

    private IEnumerator UpdateChunk(Vector2Int coord, TerrainChunk chunk, int lod) {
        m_chunkUpdating[coord] = true;
        chunk.SetLevelOfDetail(lod);
        yield return chunk.GenerateMeshDataCoroutine();
        m_chunkUpdating[coord] = false;
    }

    public void HalfToChunkIndexConversion(Vector2Int halfIndices, out Vector2Int xRange, out Vector2Int yRange) {
        int hhWidth = m_halfWidth/2;
        int hhHeight = m_halfHeight/2;

        // X-axis
        int hhX = Mathf.FloorToInt(halfIndices.x/2);
        xRange = ((float)halfIndices.x % 2f == 0)
            ? new Vector2Int(Mathf.Clamp(hhX-1,0,numCols-1), Mathf.Clamp(hhX,0,numCols-1))  // Even
            : new Vector2Int(Mathf.Clamp(hhX,0,numCols-1), Mathf.Clamp(hhX,0,numCols-1));   // Odd
        
        // Y-axis
        int hhY = Mathf.FloorToInt(halfIndices.y/2);
        yRange = ((float)halfIndices.y % 2f == 0) 
            ? new Vector2Int(Mathf.Clamp(hhY-1,0,numRows-1), Mathf.Clamp(hhY,0,numRows-1))  // Even
            : new Vector2Int(Mathf.Clamp(hhY,0,numRows-1), Mathf.Clamp(hhY,0,numRows-1));   // Odd
    }

    public Vector2Int GetIndicesFromWorldPosition(Vector3 queryPosition) {
        return new Vector2Int(
            Mathf.FloorToInt(queryPosition.x/cellWidth), 
            Mathf.FloorToInt(queryPosition.z/cellHeight)
        );
    }
    public void GetIndicesFromWorldPosition(Vector3 queryPosition, out Vector2Int chunk, out Vector2Int half) {
        int hhWidth = m_halfWidth/2;
        int hhHeight = m_halfHeight/2;

        chunk = new Vector2Int(Mathf.FloorToInt(queryPosition.x/cellWidth), Mathf.FloorToInt(queryPosition.z/cellHeight));
        half = new Vector2Int(Mathf.FloorToInt((queryPosition.x+hhWidth)/m_halfWidth), Mathf.FloorToInt((queryPosition.z+hhHeight)/m_halfHeight));
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
public class HalfChunk {
    public Vector2Int indices;
    public List<Vector2Int> chunkIndices = new List<Vector2Int>();
}