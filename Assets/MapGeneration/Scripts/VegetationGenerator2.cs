using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using DataStructures.ViliWonka.KDTree;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class VegetationGenerator2 : MonoBehaviour
{
    public static VegetationGenerator2 current;

    [Header("=== References ===")]
    [SerializeField] private Transform m_vegetationParent;

    [Header("=== Generation Settings ===")]
    [SerializeField, Tooltip("The seed value for randomization.")] private int m_seed;
    public int seed => m_seed;
    [SerializeField, Tooltip("The map width")]  private float m_width = 100f;
    [SerializeField, Tooltip("The map height")] private float m_height = 100f;
    public float width => m_width;
    public float height => m_height;
    [Space]
    [SerializeField, Tooltip("Do we auto-generate the max number of generated vegeation items?")]   private bool m_autoCalculateMax = true;
    [SerializeField, Tooltip("The total number of vegetation items we want to generate")]           private int m_maxGeneratedItems;                
    [Space]
    [SerializeField, Tooltip("When iterating across the map, how much do we advance the steps by?")]    private int m_iterationIncrement = 2;
    [SerializeField, Tooltip("The spawn delay between spawning.")]                                      private float m_coroutineSpawnDelay = 0.05f;
    [SerializeField, Tooltip("How many vegetation items do you spawn at a single time?")]               private int m_coroutineNumItems = 3;
    
    [Header("=== Outputs - READ ONLY ===")]
    [SerializeField, Tooltip("The generated vegetation items")] private List<GameObject> m_generatedVegetation;
    public List<GameObject> generatedVegetation => m_generatedVegetation;
    private List<Vector3> m_generatedPoints;

    [Header("=== On Generation End")]
    [SerializeField, Tooltip("Has vegetation been generated?")] private bool m_generated = false;
    public bool generated => m_generated;
    public UnityEvent onGenerationEnd;

    private IEnumerator m_spawnCoroutine;
    private Queue<ToSpawn> m_coroutineSpawnQueue;
    private int m_generatedItemsCount;

    private KDQuery m_vegetationQuery;
    private KDTree m_vegetationTree;
    private System.Random m_prng;

    public class ToSpawn {
        public GameObject prefab;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
    }

    #if UNITY_EDITOR
    [Header("=== Debug Tree Indice Radius Search ===")]
    [SerializeField] private Transform m_debugPositionRef;
    [SerializeField] private float m_debugRadius = 10f;
    void OnDrawGizmos() {
        if (!m_generated || m_debugPositionRef == null) return;
        List<int> treeIndices = QueryTreeIndicesInRadius(m_debugPositionRef.position, m_debugRadius);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(m_debugPositionRef.position, m_debugRadius);
        foreach(int i in treeIndices) Gizmos.DrawSphere(m_generatedPoints[i], 10f);
    }
    #endif

    private void Awake() {
        current = this;
        if (m_vegetationParent == null) m_vegetationParent = this.transform;
    }

    public virtual void SetSeed(string newSeed) {
        if (newSeed.Length > 0 && int.TryParse(newSeed, out int validNewSeed)) {    m_seed = validNewSeed;  return; }
        m_seed = UnityEngine.Random.Range(0, 1000001);
    }
    public virtual void SetSeed(int newSeed) {
        m_seed = newSeed;
    }

    public void SetDimensions(float w, float h) {
        m_width = w;
        m_height = h;
        if (m_autoCalculateMax) m_maxGeneratedItems = Mathf.FloorToInt(m_width * m_height);
    }
    public void SetDimensions(Vector2 dimensions) { SetDimensions(dimensions.x, dimensions.y); }
    
    public void Generate() {
        // Check if both voronoi and terrain map are generated, then we can proceed. However, if we have neither, then we cannot do anything
        if (TerrainManager.current == null || !TerrainManager.current.generated) {
            Debug.Log("Vegetation Generator 2: Cannot start generating until terrian is generated.");
            return;
        }
        if (Voronoi.current == null || !Voronoi.current.generated) {
            Debug.Log("Vegetation Generator 2: Cannot start generating until voronoi is generated.");
            return;
        }
        // Set the seed engine
        m_prng = new System.Random(m_seed);

        // Initialize the spawn coroutine. This requires:
        m_generatedItemsCount = 0;                      // Keeping a count of the total number of generated items
        m_coroutineSpawnQueue = new Queue<ToSpawn>();   // The queue when instantiating the generated items
        m_spawnCoroutine = SpawnCoroutine();            // The coroutine for spawning the vegetation items
        StartCoroutine(m_spawnCoroutine);

        // We want to iterate across the entire map. At most, the total number of possible vegetation elements we can add is m_width*m_height.
        // We first start by iterating through our map
        for(int x = 0; x <= (int)m_width; x+=m_iterationIncrement) {
            for(int z = 0; z <= (int)m_height; z+=m_iterationIncrement) {
                // What's the world position coordinates? We randomize the position just a bit, to add some noise
                float worldX = (float)x + (float)m_prng.Next(-5,5)/10f;
                float worldZ = (float)z + (float)m_prng.Next(-5,5)/10f;

                // Get the point on the terrain where this point is
                if (!TerrainManager.current.TryGetPointOnTerrain(worldX, worldZ, out Vector3 point, out Vector3 normal, out float steepness)) {
                    continue;   // unfortunately, looks like we couldn't get a point. Skip.
                }

                // Similarly, query which region we are currently in. This region has a list of prefabs we want to refer to.
                Region currentRegion = Voronoi.current.QueryRegion(point);
                List<VegetationGenerator.VegetationPrefab> prefabs = currentRegion.attributes.vegetationPrefabs;

                //  Given the position on the map and the region, we now have to check if the point matches the criteria defined by the region attributes
                if ((float)m_prng.Next(0,1000)/1000f < currentRegion.attributes.vegetationSpawnThreshold) continue;
                if (steepness > currentRegion.attributes.vegetationSteepnessThreshold) continue;
                if (point.y > currentRegion.attributes.vegetationHeightRange.max || point.y < currentRegion.attributes.vegetationHeightRange.min) continue;
                
                // Select which prefab to use, and get its characteristics
                int prefabIndex = m_prng.Next(0, prefabs.Count);
                GameObject prefab = prefabs[prefabIndex].prefab;

                // Determine if we should rotate the vegetation item to follow the curvature of the terrain
                Quaternion normalRotation = Quaternion.identity;
                if ( prefabs[prefabIndex].alignWithNormal) {
                    normalRotation = Quaternion.FromToRotation(Vector3.up, normal);
                }

                // Get the world position and rotation of the new object to be spawned.
                Vector3 pos = point;
                Quaternion rot = normalRotation * Quaternion.Euler(0f, m_prng.Next(0,360), 0f);
                Vector3 scale = Vector3.one * (float)m_prng.Next(Mathf.RoundToInt(prefabs[prefabIndex].scaleRange.x*1000f), Mathf.RoundToInt(prefabs[prefabIndex].scaleRange.y*1000f))/1000f;

                // Initialize a `ToSpawn` object, populate it, and add it to the spawn queue for our spawn coroutine
                ToSpawn toSpawn = new ToSpawn { prefab=prefab, position=pos, rotation=rot, scale=scale };
                m_coroutineSpawnQueue.Enqueue(toSpawn);

                // Break early of the total # of generated trees already has reached the max number possible.
                m_generatedItemsCount++;
                if (m_generatedItemsCount >= m_maxGeneratedItems) break;
            }
            if (m_generatedItemsCount >= m_maxGeneratedItems) break;
        }
    }

    private IEnumerator SpawnCoroutine() {
        // Initialize waitforseconds delay
        WaitForSeconds spawnDelay = new WaitForSeconds(m_coroutineSpawnDelay);

        // Let's actually wait for the spawn delay first, to give the system enough time to generate some number of trees to generate
        yield return spawnDelay;

        // Because we want to form a KDTree for easy vegetation search, we must initialize a list o positions here
        m_generatedPoints = new List<Vector3>();

        // initiate loop
        while(m_generatedVegetation.Count < m_generatedItemsCount) {
            // Check if we still have something to spawn. If not, skip
            if (m_coroutineSpawnQueue.Count == 0) {
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
                GameObject t = Instantiate (toSpawn.prefab, toSpawn.position , toSpawn.rotation, m_vegetationParent);
                t.transform.localScale = toSpawn.scale;
                
                // Add it to our list of generated vegetation
                m_generatedVegetation.Add(t);
                m_generatedPoints.Add(toSpawn.position);
            }

            // Yield return the delay
            yield return spawnDelay;
        }

        // When reaching this point, we have generated all our trees. 
        m_vegetationQuery = new KDQuery();
        m_vegetationTree = new KDTree(m_generatedPoints.ToArray(), 32);

        // If there's an event callback we want to call, we do so here.
        m_generated = true;
        onGenerationEnd?.Invoke();
    }

    public List<int> QueryTreeIndicesInRadius(Vector3 query, float radius) {
        if (!m_generated) return null;
        List<int> results = new List<int>();
        m_vegetationQuery.Radius(m_vegetationTree, query, radius, results);
        return results;
    }

    public void DeactivateTreesInRadius(Vector3 query, float radius) {
        if (!m_generated) return;
        List<int> results = QueryTreeIndicesInRadius(query, radius);
        if (results == null) return;
        foreach(int i in results) m_generatedVegetation[i].SetActive(false);
    }

    private void OnValidate() {
        if (m_autoCalculateMax) m_maxGeneratedItems = (int)m_width * (int)m_height;
    }
}
