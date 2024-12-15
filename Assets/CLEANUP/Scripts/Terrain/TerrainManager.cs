using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TerrainManager : MonoBehaviour
{
    public static TerrainManager current;

    const int textureSize = 512;
    const TextureFormat textureFormat = TextureFormat.RGB565;
    
    public enum FalloffType { Box, NorthSouth, EastWest, Circle }
    public enum LODMethod { Off, Grid, Angle, AngleDistance }

    [Header("=== Terrain Grid Settings ===")]
    [SerializeField, Tooltip("The seed used for generation. Passed down to all terrain chunks")]    private int m_seed;
    public int seed => m_seed;
    [SerializeField, Tooltip("The number of columns (along X-axis) along the chunk grid")]          private int m_numCols = 4;
    [SerializeField, Tooltip("The number of rows (along Z-axis) along the chunk grid")]             private int m_numRows = 4;
    [SerializeField, Tooltip("The width (along X-axis) of each individual chunk")]                  private int m_cellWidth = 150;
    [SerializeField, Tooltip("The height (along Z-axis) of each individual chunk")]                 private int m_cellHeight = 150;
    public float width => (float)m_numCols * m_cellWidth;
    public float height => (float)m_numRows * m_cellHeight;
    public Vector3 worldCenter => new Vector3(width/2f, (m_noiseRange.max-m_noiseRange.min)/2f + m_noiseRange.min, height/2f);
    [SerializeField, Tooltip("Generate on start?")] private bool m_generateOnStart = false;
    [Space]
    [SerializeField, Tooltip("The maximum LOD we want to enforce for terrain chunks"), Range(0,6)]  private int m_maxLOD = 6;
    [SerializeField, Tooltip("How do chunks determine their LOD? If 'Off', then will default to max LOD level")]    private LODMethod m_lodMethod = LODMethod.Grid;
    [SerializeField, Tooltip("If using angle-based LOD determiantion, what is the angle discretization?")]          private float m_lodAngleThreshold = 30f;
    [Space]
    [SerializeField, Tooltip("Do we want to randomize the offsets used for terrain generation?")]   private bool m_randomizeOffsets = true;
    [SerializeField, Tooltip("The offsets for perlin noise generation")]                            private Vector2 m_offsets = Vector2.zero;
    private System.Random m_prng;

    [Header("=== Core References ===")]
    [SerializeField, Tooltip("The TerrainChunk prefab")]    private TerrainChunk m_chunkPrefab;
    [SerializeField, Tooltip("Reference to the player")]    private Transform m_playerRef; 
    [SerializeField, Tooltip("What layer should we assign to each child?")] private LayerMask m_terrainLayer;
    [SerializeField, Tooltip("Collider prefab for generating boundaries")]  private Transform m_boundariesPrefab;

    [Header("=== Material Generation ===")]
    [SerializeField] private Material m_meshMaterialSrc;
    private Material m_meshMaterial;
    [SerializeField] private List<TextureLayer> m_meshMaterialLayers;

    [Header("=== Generation Layers ===")]
    [SerializeField] private bool m_applyValleys = true;
    [SerializeField, Tooltip("The falloff representation for base height")]         private Falloff m_baseFalloff;
    [SerializeField, Tooltip("The perlin noise representation for base valleys")]   private PerlinNoise m_valleysPerlin;
    [SerializeField] private bool m_applyDunes = true;
    [SerializeField, Tooltip("The perlin noise representation for base dunes")]     private PerlinNoise m_dunesPerlin;
    [SerializeField, Tooltip("The falloff representation for traversable terrain")] private Falloff m_traversableFalloff;
    [SerializeField, Tooltip("The falloff representation for border terrain")]      private Falloff m_borderFalloff;
    [SerializeField, Tooltip("READ ONLY")]  private TerrainChunk.MinMax m_noiseRange;
    public TerrainChunk.MinMax noiseRange => m_noiseRange;

    [Header("=== On Completion ===")]
    [SerializeField] private int m_numInitializedChunks = 0;
    private int m_totalNumChunks => m_numCols * m_numRows;
    [SerializeField, Tooltip("Is the terrain generated?")]  private bool m_generated = false;
    public bool generated => m_generated;
    [Space]
    public UnityEvent onGenerationEnd;

    private Dictionary<Vector2Int, TerrainChunk> m_chunks;

    public void SetSeed(string newSeed) {
        if (newSeed.Length > 0 && int.TryParse(newSeed, out int validNewSeed)) {   
            m_seed = validNewSeed;
            return;
        }
        Debug.Log($"Provided seed {newSeed} not viable. Auto-generating new seed...");
        m_seed = UnityEngine.Random.Range(0, 1000001);
    }
    public void SetSeed(int newSeed) { m_seed = newSeed; }

    private void Awake() {
        current = this;
    }

    private void Start() {
        if (m_generateOnStart) Generate();
    }

    public void Generate() {
        // For now, set the viewer position to 0,0. If we actually DO have a player, then we get its current position.
        Vector2Int viewerCoords = Vector2Int.zero;
        if (m_playerRef != null) viewerCoords = GetIndicesFromWorldPosition(m_playerRef.position);

        // Initialize our randomization engine
        m_prng = new System.Random(m_seed);
        if (m_randomizeOffsets) m_offsets = new Vector2(m_prng.Next(-100000, 100000), m_prng.Next(-100000, 100000));

        // Start Coroutine to generate materials
        StartCoroutine(GenerateMaterial());
    }

    public IEnumerator GenerateMaterial() {
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

        // Start Generating Chunks
        yield return null;
        GenerateChunks();
    }
    public virtual Texture2DArray GenerateTextureArray(Texture2D[] textures) {
        Texture2DArray textureArray = new Texture2DArray(textureSize, textureSize, textures.Length, textureFormat, true);
        for(int i =0; i < textures.Length; i++) textureArray.SetPixels(textures[i].GetPixels(), i);
        textureArray.Apply();
        return textureArray;
    }

    public void GenerateChunks() {
        // CLear all existing chunks, and initialize the chunk collection
        ClearChildrenChunks();
        m_chunks = new Dictionary<Vector2Int, TerrainChunk>();
        //m_noiseRange = new TerrainChunk.MinMax { min=float.MaxValue, max=float.MinValue };
        // Loop through chunks
        m_numInitializedChunks = 0;
        for(int x = 0; x < m_numCols; x++) {
            for(int y = 0; y < m_numRows; y++) {
                // Generate Vector2Int index for this chunk
                Vector2Int index = new Vector2Int(x,y);
                // The world position of this chunk is x=(x*m_cellWidth), y=(y*m_cellHeight);
                Vector3 worldPosition = new Vector3(x*m_cellWidth, 0f, y*m_cellHeight);
                // Instantiate new chunk at this location.
                TerrainChunk chunk = Instantiate(m_chunkPrefab, worldPosition, Quaternion.identity, this.transform) as TerrainChunk;
                // Initialize its m_seed, layer, width and height, offsets, LODs, etc.
                chunk.gameObject.name = $"CHUNK {x},{y}";
                chunk.gameObject.layer = (int)Mathf.Log(m_terrainLayer.value, 2);
                chunk.SetParent(this);
                chunk.SetDimensions(m_cellWidth, m_cellHeight);
                chunk.SetLevelOfDetail(0);
                chunk.SetOffset(x,y);
                chunk.SetMaterial(m_meshMaterial);
                // Initialize its coroutine
                chunk.Initialize(true,true);
                // Save a reference to it in our chunks dictionary
                m_chunks.Add(index, chunk);
            }
        }
        StartCoroutine(GenerateMap());
    }

    private void Update() {
        if (m_playerRef == null || !m_generated || m_lodMethod == LODMethod.Off) return;

        // Get the current chunk coords of the viewer
        Vector2Int playerChunkCoords = GetIndicesFromWorldPosition(m_playerRef.position);
        TerrainChunk playerChunk = m_chunks[playerChunkCoords];
        
        // For each Terrain Chunk, calculate the intended LOD level. If the LOD level ahs changed, we run the coroutien to update it
        foreach(KeyValuePair<Vector2Int, TerrainChunk> kvp in m_chunks) {
            
            // Get chunk details
            Vector2Int chunkCoords = kvp.Key;
            TerrainChunk chunk = kvp.Value;
            int chunkLOD = chunk.levelOfDetail;

            // Distance. Clamp between 0 and max LOD
            int clampedDistance = 0;

            // If the current chunk is the same as the player's chunk, then we just set the distance to 0
            if (playerChunk == chunk) {
                clampedDistance = 0;
            } else {
                // Distance. Clamp between 0 and max LOD
                int distance;
                float angle;
                Vector3 playerForward, towardsChunk;
                switch(m_lodMethod) {
                    case LODMethod.AngleDistance:
                        playerForward = m_playerRef.forward;
                        towardsChunk = new Vector3(chunk.chunkCenter.x - m_playerRef.position.x, 0f, chunk.chunkCenter.z - m_playerRef.position.z);
                        angle = Mathf.Abs(Vector3.SignedAngle(playerForward, towardsChunk.normalized, Vector3.up));
                        int angleDistance = Mathf.FloorToInt(angle/m_lodAngleThreshold);
                        int gridDistance = Mathf.Max(Mathf.Abs(chunkCoords.x - playerChunkCoords.x), Mathf.Abs(chunkCoords.y - playerChunkCoords.y)) - 1;
                        distance = Mathf.Min(angleDistance, gridDistance);
                        break;
                    case LODMethod.Angle:
                        playerForward = m_playerRef.forward;
                        towardsChunk = new Vector3(chunk.chunkCenter.x - m_playerRef.position.x, 0f, chunk.chunkCenter.z - m_playerRef.position.z);
                        angle = Mathf.Abs(Vector3.SignedAngle(playerForward, towardsChunk.normalized, Vector3.up));
                        distance = Mathf.FloorToInt(angle/m_lodAngleThreshold);
                        break;
                    default:
                        // Grid
                        distance = Mathf.Max(Mathf.Abs(chunkCoords.x - playerChunkCoords.x), Mathf.Abs(chunkCoords.y - playerChunkCoords.y)) - 1;
                        break;
                }
                clampedDistance = Mathf.Clamp(distance, 0, m_maxLOD);
            }

            // Check if the LOD is different or not. If so, update
            // We don't need a coroutine because we already generated it. It's just about switching the model, which is a quick function call.
            if (chunkLOD != clampedDistance) chunk.SetLODMesh(clampedDistance);
        
        }
    }
    
    public float GetTerrainValueFromWorldPosition(float x, float z) {
        //Start by generating the base based on falloff
        float start = m_baseFalloff.Evaluate(x,z,width,height);
 
        // Add together valleys and dunes as "base" map
        float valley = (m_applyValleys) ? m_valleysPerlin.Evaluate(x, z, m_offsets.x, m_offsets.y) : 30f;
        float dune = (m_applyDunes) ? m_dunesPerlin.Evaluate(x, z, m_offsets.x, m_offsets.y) : 5f;
        float baseValue = start + valley + dune;

        // Then, separately, get the traversible and border values
        float traversibleFalloff = m_traversableFalloff.Evaluate(x,z,width,height);
        float traversible = traversibleFalloff * baseValue;
        float borderFalloff = m_borderFalloff.Evaluate(x,z,width,height);
        float border = borderFalloff * baseValue;

        // Finally, combine the traversible and border values
        float terrainValue = traversible + border;

        // Return the value.
        return terrainValue;
    }

    private IEnumerator GenerateMap() {
        // For each chunk in `m_chunks`, we need to run through the process of genreate noise, material, then mesh
        // Firstly, really ensure that the # of initialized chunks is set to 0
        m_numInitializedChunks = 0;
        // Then iterate through each chunk
        foreach(TerrainChunk chunk in m_chunks.Values) {
            // Attach a listener that detects when the chunk has finished generating the noise map
            chunk.onNoiseGenerationEnd.AddListener(this.OnChunkNoiseCompletion);
            // Tell the chunk to initialize noise. The listener we attached will detect when the chunk is completed.
            StartCoroutine(chunk.GenerateNoiseCoroutine());
            yield return null;
        }
    } 

    public void OnChunkNoiseCompletion(TerrainChunk chunk) {
        // Increment our counter.
        m_numInitializedChunks += 1;
        // If our # of initialized chunks is done, then we can move onto the next step.
        if (m_numInitializedChunks != m_chunks.Count) return;

        Debug.Log($"Terrain Manager: Noise Generation Completed");
        m_numInitializedChunks = 0;
        foreach(TerrainChunk c in m_chunks.Values) {
            // Remove the listener for noise generation
            c.onNoiseGenerationEnd.RemoveListener(this.OnChunkNoiseCompletion);
            // Set this chunk's noise range to that determiend when calcualteing terrain values
            c.SetMinMax(m_noiseRange);
            // Add the listener for chunk adjustment
            c.onNoiseAdjustmentEnd.AddListener(this.OnChunkNoiseAdjustment);
            // And start the coroutine
            StartCoroutine(c.AdjustHeightToFloor());
        }
    }

    public void OnChunkNoiseAdjustment(TerrainChunk chunk) {
        // In this case, we don't need to check the counter for incrementation. We can just make a beeline!
        // Firstly, remove the listener for noise adjustment
        chunk.onNoiseAdjustmentEnd.RemoveListener(this.OnChunkNoiseAdjustment);
        // Now, tell it to generate the materials
        chunk.onMeshGenerationEnd.AddListener(this.OnChunkMeshCompletion);
        StartCoroutine(chunk.GenerateMeshDataCoroutine());
    }

    public void OnChunkMaterialCompletion(TerrainChunk chunk) {
        // In this case, we don't need to check the counter for incrementation. We can just make a beeline!
        // Firstly, remove the listener for noise adjustment
        chunk.onMaterialGenerationEnd.RemoveListener(this.OnChunkMaterialCompletion);
        // Now, tell it to generate the mesh
        chunk.onMeshGenerationEnd.AddListener(this.OnChunkMeshCompletion);
        StartCoroutine(chunk.GenerateMeshDataCoroutine());
    }

    public void OnChunkMeshCompletion(TerrainChunk chunk) {
        Debug.Log("Mesh Completion");
        // This time, we DO need the counter
        m_numInitializedChunks += 1;
        // Remove the listener
        chunk.onMeshGenerationEnd.RemoveListener(this.OnChunkMeshCompletion);

        // Cannot do anything until all chunks are complete.
        if (m_numInitializedChunks < m_chunks.Count) return;
        Debug.Log($"Terrain Manager: Mesh Generation Completed");

        // If we have an inverse box boundary, let's instantiate one
        Transform boundary = Instantiate(m_boundariesPrefab, new Vector3(width/2f, m_noiseRange.min, height/2f), Quaternion.identity, this.transform) as Transform;
        boundary.localScale = new Vector3(width-1f, (m_noiseRange.max-m_noiseRange.min)*2f, height-1f);

        // Assuming teh check passes, then we can safely declare that we're initialized
        m_generated = true;
        // If any events are tied to on generation end, invoke them
        onGenerationEnd?.Invoke();
    }


    public Vector2Int GetIndicesFromWorldPosition(Vector3 queryPosition) {
        return new Vector2Int(
            Mathf.FloorToInt(queryPosition.x/m_cellWidth), 
            Mathf.FloorToInt(queryPosition.z/m_cellHeight)
        );
    }

    public bool TryGetPointOnTerrain(float queryX, float queryZ, out Vector3 point, out Vector3 normal, out float steepness) {
        // We need to do a raycast. We COULD use mesh.triangles and all that hullaballoo, but it's easier to use physics for this one.
        float verticalDistance = m_noiseRange.max+10f;
        Vector3 start = new Vector3(queryX, verticalDistance, queryZ);
        if (Physics.Raycast(start, Vector3.down, out RaycastHit hit, verticalDistance, m_terrainLayer)) {
            point = hit.point;
            normal = hit.normal;
            steepness = Vector3.Angle(Vector3.up, normal);
            return true;
        }
        // In the case where we can't get a raycast hit, we'll just return upward normal and start
        point = start;
        normal = Vector3.up;
        steepness = 0f;
        return false;
    }
    public bool TryGetPointOnTerrain(int queryX, int queryZ, out Vector3 point, out Vector3 normal, out float steepness) {   return TryGetPointOnTerrain((float)queryX, (float)queryZ, out point, out normal, out steepness); }
    public bool TryGetPointOnTerrain(Vector3 queryPosition, out Vector3 point, out Vector3 normal, out float steepness) {    return TryGetPointOnTerrain(queryPosition.x, queryPosition.z, out point, out normal, out steepness); }

    public bool TryGetNormalOnTerrain(float queryX, float queryZ, out Vector3 normal, out float steepness) {
         // We need to do a raycast. We COULD use mesh.triangles and all that hullaballoo, but it's easier to use physics for this one.
        float verticalDistance = m_noiseRange.max+10f;
        Vector3 start = new Vector3(queryX, verticalDistance, queryZ);
        if (Physics.Raycast(start, Vector3.down, out RaycastHit hit, verticalDistance, m_terrainLayer)) {
            normal = hit.normal;
            steepness = Vector3.Angle(Vector3.up, normal);
            return true;
        }
        // In the case where we can't get a raycast hit, we'll just return upward normal and start
        normal = Vector3.up;
        steepness = 0f;
        return false;
    }
    public bool TryGetNormalOnTerrain(int queryX, int queryZ, out Vector3 normal, out float steepness) { return TryGetNormalOnTerrain((float)queryX, (float)queryZ, out normal, out steepness); }
    public bool TryGetNormalOnTerrain(Vector3 query, out Vector3 normal, out float steepness) { return TryGetNormalOnTerrain(query.x, query.z, out normal, out steepness); }

    public void ToggleLODMethod(string setTo) {
        switch(setTo) {
            case "Off":             m_lodMethod = LODMethod.Off;    break;
            case "Grid":            m_lodMethod = LODMethod.Grid;   break;
            case "Angle":           m_lodMethod = LODMethod.Angle;  break;
            case "AngleDistance":   m_lodMethod = LODMethod.AngleDistance;  break;
            default:
                Debug.LogError($"Terrain Manager: Cannot set LOD Method to \"{setTo}\" - no corresponding LOD Method");
                break;
        }
    }
    public void ToggleLODMethod(LODMethod setTo) {
        m_lodMethod = setTo;
    }

    public void ClearChunks() {
        if (m_chunks != null && m_chunks.Count > 0) foreach(TerrainChunk chunk in m_chunks.Values) if (chunk != null) DestroyImmediate(chunk.gameObject);
    }
    public void ClearChildrenChunks() {
        TerrainChunk[] chunks = GetComponentsInChildren<TerrainChunk>();
        if (chunks.Length > 0) foreach(TerrainChunk chunk in chunks) DestroyImmediate(chunk.gameObject);
    }

    public void TerminateCoroutines() {
        StopAllCoroutines();
    }

    private void OnDisable() {
        StopAllCoroutines();
        ClearChunks();
        ClearChildrenChunks();
    }
}

[System.Serializable]
public class HeightCurve {
    public AnimationCurve curve;
    public float multiplier;
    public float Evaluate(float v) {
        return curve.Evaluate(v) * multiplier;
    }
}

[System.Serializable]
public class Falloff {
    public enum FalloffType { Box, NorthSouth, EastWest, Circle }
    public FalloffType falloffType;
    [Range(0f,1f)] public float centerX = 0.5f, centerY = 0.5f;
    [Range(0f,2f)] public float falloffStart = 0.25f, falloffEnd = 0.5f;
    public HeightCurve heightCurve;

    public float Evaluate(float x, float y, float width, float height) {
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
        return heightCurve.Evaluate(v);
    }
}

[System.Serializable]
public class PerlinNoise {
    public float scale;
    public HeightCurve heightCurve;
    
    public float Evaluate(float x, float y, float offsetX, float offsetY) {
        // x and y are expected to be in world coordinates
        float xCoord = (x+offsetX) / scale;
        float yCoord = (y+offsetY) / scale;
        return heightCurve.Evaluate(Mathf.PerlinNoise(xCoord, yCoord));
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