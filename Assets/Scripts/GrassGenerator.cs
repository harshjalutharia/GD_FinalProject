using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;


public class GrassGenerator : MonoBehaviour
{
    /***
    [System.Serializable]
    public class VegetationPrefab {
        public GameObject prefab;
        public int mapRenderSize;
        public Color mapColor;
    }
    
    public class ToSpawn {
        public VegetationPrefab prefab;
        public Vector3 position;
        public Quaternion rotation;
    }
    
    [Header("=== References ===")]
    [SerializeField, Tooltip("Reference to the noise map used for terrain generation")]                                     private NoiseMap m_terrainGenerator;
    [SerializeField, Tooltip("Reference to the voronoi map used for region segmentation")]                                  private  VoronoiMap m_voronoiMap;
    [SerializeField, Tooltip("Prefabs used for this generator. Allows for modding of their appearance on the held map")]    private List<VegetationPrefab> m_prefabs;
    [SerializeField, Tooltip("The parent of the generated objects. If unset, will auto-set to this game object.")]          private Transform m_vegetationParent;
    
    [SerializeField, Tooltip("Player Reference")]          private Transform m_player;
    
    [Header("=== Pre-Generation Settings ===")]
    [SerializeField, Tooltip("The seed used to control psuedo-rng for this map.")]                                                  private int m_seed;
    [SerializeField, Tooltip("Use a coroutine to spawn vegetation objects. May improve initial runtime")]                           private bool m_useCoroutine = false;
    [SerializeField, Tooltip("If using a coroutine, the spawn delay between spawning.")]                                            private float m_coroutineSpawnDelay = 0.05f;
    [SerializeField, Tooltip("If using a coroutine, how many objects do you dequeue at a single time?")]                            private int m_coroutineNumItems = 3;

    [Header("=== Generation Settings ===")]
    [SerializeField, Tooltip("Max number of vegetation objects we want to spawn.")]                                                 private int m_maxNumVegetation = 200;
    [SerializeField, Tooltip("For each pixel, will spawn vegetation object if m_prng number is below this threshold"), Range(0f, 1f)] private float m_generationThreshold = 0.5f;
    [SerializeField, Tooltip("How much away (relatively) from border edge do we want to not spawn objects?"), Range(0f, 0.5f)]      private float m_edgeBuffer;    
    [SerializeField, Tooltip("Min noise map threshold to consider a pixel cell to generate in"), Range(0f,1f)]                      private float m_minHeight = 0.3f;
    [SerializeField, Tooltip("Max noise map threshold to consider a pixel cell to generate in"), Range(0f,1f)]                      private float m_maxHeight = 0.6f;
    [SerializeField, Tooltip("Rotation Offset for rotating the prefab"), Range(0,30)]                                               private int m_rotationOffset;
    [SerializeField, Tooltip("Distance around the player to generate grass"), Range(0,100)]                                         private int m_playerAroundDistance = 20;
    
    [Header("=== Outputs - READ ONLY ===")]
    public List<GameObject> generatedTree;
    private IEnumerator spawnCoroutine;
    private Queue<ToSpawn> m_coroutineSpawnQueue;

    private void Awake() {
        if (m_vegetationParent == null) m_vegetationParent = this.transform;
        if (m_player == null) Debug.Log("Grass Generator Player Reference Not Found!");
    }

    
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

    public void GenerateVegetation() {
        // Get Player's Position
        if (m_player == null) {
            Debug.LogWarning("Player Transform is not assigned!");
            return; // Exit if no player is assigned
        }

        Vector3 playerPosition = m_player.position;

        Debug.Log($"Player Position: {playerPosition}");



        // Determine random number generator based on seed value
        System.Random m_prng = new System.Random(m_seed);

        // Determine some constants, counters, and the output array
        int mapChunkSize = m_terrainGenerator.mapChunkSize;
        int edgeDistance  = Mathf.FloorToInt(mapChunkSize* m_edgeBuffer);
        int treeCount = 0;
        generatedTree= new List<GameObject>();

        // If we want to use a coroutine, then initialize the spawn coroutine
        if (m_useCoroutine) {
            // initialize the spawn queue
            m_coroutineSpawnQueue = new Queue<ToSpawn>();
            spawnCoroutine = SpawnCoroutine();
            StartCoroutine(spawnCoroutine);
        }



        // Iterate through each pixel
       for (int y = (int) playerPosition.y - m_playerAroundDistance; y < (int) playerPosition.y + m_playerAroundDistance; y++){
            
            // Ignore this pixel cell if the y-coord is within buffer zone
            if (y < edgeDistance || y > mapChunkSize - edgeDistance) continue;

            for (int x = (int) playerPosition.x - m_playerAroundDistance; x < (int) playerPosition.x + m_playerAroundDistance; x++){

                // Ignore this pixel cell if the x-coord is within buffer zone
                if (x < edgeDistance || x > mapChunkSize - edgeDistance) continue;


                
                // x and y are coordinate for the map.
                // Get the noise value from the voronoi map
                //float noise = m_voronoiMap.QueryNoiseAtCoords(x ,y, out Vector3 dummy);

                // If the noise value is not within the range determined by min and max height, ignore this pixel.
                //if (noise < m_minHeight || noise > m_maxHeight) continue;

                // Check if we should actually plant this...
                //float shouldPlant = (float)m_prng.Next(0, 100) / 100f;
                //if (shouldPlant > m_generationThreshold) continue;

                // Select which prefab to use, and get its characteristics
                int prefabIndex = m_prng.Next(0, m_prefabs.Count);
                GameObject prefab = m_prefabs[prefabIndex].prefab;
                int mapRadius = m_prefabs[prefabIndex].mapRenderSize;
                Color mapColor = m_prefabs[prefabIndex].mapColor;

                // Get the world position of the new object to be spawned.
                float heightNoise = m_terrainGenerator.QueryHeightAtCoords(x, y, out Vector3 pixelPosition);
                float posOffsetX = (float)m_prng.Next(0, 100) / 100f;
                float posOffsetZ = (float)m_prng.Next(0, 100) / 100f;
                Vector3 pos = pixelPosition + new Vector3 (posOffsetX, 0f, posOffsetZ);

                // Get the orientation of the planted tree
                float rotOffsetX = m_prng.Next(0,m_rotationOffset);
                float rotOffsetY = m_prng.Next(0,360);
                float rotOffsetZ = m_prng.Next(0,m_rotationOffset);
                Quaternion rot = Quaternion.Euler(rotOffsetX, rotOffsetY, rotOffsetZ);

                

                // Increase the ticker for the number of generated trees
                treeCount += 1;

                // if we are using a coroutine to spawn, then we initialize a `ToSpawn` object, populate it, and add it to the spawn queue
                if (m_useCoroutine) {
                    ToSpawn toSpawn = new ToSpawn { prefab=m_prefabs[prefabIndex], position=pos, rotation=rot };
                    m_coroutineSpawnQueue.Enqueue(toSpawn);
                }
                // Otherwise, just spawn now
                else {
                    // instantiate the object, store its reference in `generatedTree`, and add its circle to held map
                    GameObject t = Instantiate (prefab, pos , rot, m_vegetationParent);
                    generatedTree.Add(t);
                    m_terrainGenerator.DrawCircleOnHeldMap(pos.x, pos.z, mapRadius, mapColor);
                }

                // Break early of the total # of generated trees already has reached the max number possible.
                if (treeCount >= m_maxNumVegetation) break;
            }
            // Break early of the total # of generated trees already has reached the max number possible.
            if (treeCount >= m_maxNumVegetation) break;
        }

    }

    private IEnumerator SpawnCoroutine() {
        // Initialize queue, if it doesn't exist yet
        if (m_coroutineSpawnQueue == null) m_coroutineSpawnQueue = new Queue<ToSpawn>();

        // Initialize waitforseconds delay
        WaitForSeconds spawnDelay = new WaitForSeconds(m_coroutineSpawnDelay);

        // Use a counter to restrict the number of times we wait for the spawn queue to fill up. If the counter goes beyond this value, we auto-end the spawn coroutine
        int waitCounter = 0;

        // initiate loop
        while(waitCounter < 100) {
            // Check if we still have something to spawn. If not, skip
            if (m_coroutineSpawnQueue.Count == 0) {
                waitCounter += 1;
                yield return null;
                continue;
            }

            // Execute a loop to restrict the number of simultaneous items to spawn
            for(int i = 0; i < m_coroutineNumItems; i++) {
                // If nothing to dequeue, just break
                if (m_coroutineSpawnQueue.Count == 0) break;

                // Get the first object in the queue
                ToSpawn toSpawn = m_coroutineSpawnQueue.Dequeue();

                // Instantiate the necessary prefab with the given position and rotation
                GameObject t = Instantiate (toSpawn.prefab.prefab, toSpawn.position , toSpawn.rotation, m_vegetationParent);
                generatedTree.Add(t);
                m_terrainGenerator.DrawCircleOnHeldMap(toSpawn.position.x, toSpawn.position.z, toSpawn.prefab.mapRenderSize, toSpawn.prefab.mapColor);
            }


            // Yield return the delay
            waitCounter = 0;
            yield return spawnDelay;
        }
    }

    void OnValidate() {
        if (m_coroutineNumItems <= 0 ) m_coroutineNumItems = 1;
    }

    // Ensure that if the coroutine still exists, end it safely
    void OnDestroy() {
        StopAllCoroutines();
    }
    ***/

    [Header("=== References ===")]
    [SerializeField, Tooltip("Player Reference")]          private Transform m_player;
    [SerializeField, Tooltip("Radius around the player"), Range(0,40)]          private int radius = 10;
    [SerializeField, Tooltip("Reference to the noise map used for terrain generation")]  private NoiseMap m_terrainGenerator;

    [SerializeField, Tooltip("Reference to the query Mask")] private LayerMask m_queryMask;
    [SerializeField, Tooltip("The parent of the generated objects. If unset, will auto-set to this game object.")]          private Transform m_grassParent;
    [SerializeField, Tooltip("Prefab used for this generator. Allows for modding of their appearance on the held map")]    private GameObject m_prefab;

    [Header("=== Pre-Generation Settings ===")]
    [SerializeField, Tooltip("The seed used to control psuedo-rng for this map.")]                  private int m_seed;
    [SerializeField, Tooltip("Use a coroutine to spawn vegetation objects. May improve initial runtime")]           private bool m_useCoroutine = false;
    [SerializeField, Tooltip("If using a coroutine, the spawn delay between spawning.")]                            private float m_coroutineSpawnDelay = 0.05f;
    [SerializeField, Tooltip("If using a coroutine, how many objects do you dequeue at a single time?")]            private int m_coroutineNumItems = 3;

    private bool m_initialized = false;
    private System.Random m_prng;

    [SerializeField, Tooltip("Rotation Offset for rotating the prefab"), Range(0,30)]                               private int m_rotationOffset;

    [SerializeField, Tooltip("Age for Grass to die of"), Range(0,3)]                               private int m_ageUntilDie;

    class GrassObject {
        public GameObject grass;

        public Vector2Int pos;

        public int age;

    }
    private Dictionary<Vector2Int, GrassObject> m_grass;

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

    public void Initialize (){

        m_prng = new System.Random(m_seed);

        m_grass = new Dictionary<Vector2Int, GrassObject>();
        m_initialized = true;

    }

    private void Update(){
        if (!m_initialized) return;
        
        // Get player's position, both in world and in terrain map coordinates
        Vector3 playerPos = m_player.position;
        Vector2Int playerCoords = m_terrainGenerator.QueryCoordsAtWorldPos(playerPos.x, playerPos.z);

        // Generate list of grass we need to track that existed before
        List<Vector2Int> existingGrass = m_grass.Keys.ToList();

        // Iterate through Y
        for (int y = playerCoords.y - radius; y <= playerCoords.y + radius; y++){
            
            // If y-coord is outside range, ignore
            if (y < 0 || y >= m_terrainGenerator.heightMap.GetLength(1)) continue;

            // Iterate through X
            for (int x = playerCoords.x - radius; x <= playerCoords.x + radius; x++){

                // If x-coord is outside range, ignore
                if (x < 0 || x >= m_terrainGenerator.heightMap.GetLength(0)) continue;

                // Generate Vector2Int for these specific coords
                Vector2Int queryCords = new Vector2Int(x, y);
                
                // check: do we have a record of these coords
                if (! m_grass.ContainsKey(queryCords)){

                    // Get the world position of the new object to be spawned.
                    float heightNoise = m_terrainGenerator.QueryHeightAtCoords(x, y, out Vector3 pixelPosition);
                    float posOffsetX = (float)m_prng.Next(0, 100) / 100f;
                    float posOffsetZ = (float)m_prng.Next(0, 100) / 100f;
                    Vector3 pos = pixelPosition + new Vector3 (posOffsetX, 0f, posOffsetZ);

                    // Get the orientation of the planted tree
                    float rotOffsetX = m_prng.Next(0,m_rotationOffset);
                    float rotOffsetY = m_prng.Next(0,360);
                    float rotOffsetZ = m_prng.Next(0,m_rotationOffset);
                    Quaternion rot = Quaternion.Euler(rotOffsetX, rotOffsetY, rotOffsetZ);

                    // calculate the  normal of the current point
                    
                    Vector3 m_currentNormal = m_terrainGenerator.QueryMapNormalAtWorldPos(pos.x, pos.z, m_queryMask, out int dum_x, out int dum_y, out float worldY);

                    // Calculate rotation to align the up vector with the normal
                    Quaternion normalRotation = Quaternion.FromToRotation(Vector3.up, m_currentNormal);

                    // Combine the random rotation offset with the normal alignment
                    Quaternion finalRotation = normalRotation * rot;

                    // Instantiate the GameObject with the aligned rotation
                    GameObject newGrass = Instantiate(m_prefab, pos, finalRotation, m_grassParent);


                    m_grass.Add(queryCords, new GrassObject {grass= newGrass, pos =queryCords, age = 0});                
                }
                else{
                    m_grass [queryCords].age =0;
                    existingGrass.Remove(queryCords);
                } 
            }

        }

        foreach (Vector2Int agedCoords in existingGrass){

            m_grass [agedCoords].age++;


            if (m_grass[agedCoords].age >= m_ageUntilDie){

                Destroy(m_grass[agedCoords].grass);
                m_grass.Remove(agedCoords);
            }
        }

    }

}
