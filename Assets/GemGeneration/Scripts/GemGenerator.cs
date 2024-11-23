using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class GemGenerator : MonoBehaviour
{
    public static GemGenerator current;

    [System.Serializable]
    public class GemData {
        public Gem gem;
        public int id;
        public Vector3 position;
        public bool detected;
        public bool collected;
        public bool isDestination;
    }

    [Header("=== References ===")]
    [SerializeField, Tooltip("Ref. to landmark generator of the scene.")]                               private LandmarkGenerator m_landmarkGenerator;
    [SerializeField, Tooltip("Ref. to the terrain generator of the scene.")]                            private NoiseMap m_terrainGenerator;
    [SerializeField, Tooltip("The prefab used for small gems")]                                         private Gem m_smallGemPrefab;
    [SerializeField, Tooltip("The prefab used for the destination gem")]                                private Gem m_destinationGemPrefab;
    [SerializeField, Tooltip("The transform parent where gems are generated. If unset, set to self.")]  private Transform m_gemParent = null;

    [Header("=== Check In View Settings ===")]
    [SerializeField, Tooltip("Toggle to start checking for gems")]  private bool m_checkViewInitialized = false;

    [Header("=== Gem Settings ===")]
    [SerializeField, Tooltip("The seed int. used to set the pseudo-rng system")]    private int m_seed;
    [SerializeField, Tooltip("Max # of small gems. Capped by landmark generators' weenie count")]   
                                                                                    private int m_maxSmallGems = 20;
    [SerializeField, Tooltip("Size of small gems on the held map")]                 private int m_smallGemMapSize = 5;
    [SerializeField, Tooltip("Color of small gems on the held map")]                private Color m_smallGemMapColor = Color.yellow;
    [SerializeField, Tooltip("Color of collected small gems on the held map")]      private Color m_smallGemCollectedColor = Color.blue;
    [SerializeField, Tooltip("Max height offset off the ground of the small gem")]  private float m_smallGemHeightOffset = 0.5f;
    [SerializeField, Tooltip("Size of the destination gem on the held map")]        private int m_destGemMapSize = 10;
    [SerializeField, Tooltip("Color of thedestination gem on the held map")]        private Color m_destGemMapColor = Color.yellow;
    [SerializeField, Tooltip("Max height offset off the ground of the dest. gem")]  private float m_destGemHeightOffset = 0.5f;

    [Header("=== Outputs - READ ONLY ===")]
    [SerializeField, Tooltip("The list of the data on small gems generated.")] private Dictionary<Gem, GemData> m_gemData = null;
    public Dictionary<Gem, GemData> gemData => m_gemData;
    [SerializeField, Tooltip("The destination gem generated")]  private Gem m_destinationGem;
    public Gem destinationGem => m_destinationGem;
    private System.Random m_prng;

    // This is a singleton/
    private void Awake() {
        current = this;
        if (m_gemParent == null) m_gemParent = this.transform;
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

    public void GenerateSmallGems() {
        // Check: is the landmark generator set?
        if (m_landmarkGenerator == null) {
            Debug.LogError("Cannot generate gems: Landmark Generator reference not set");
            return;
        }

        // Initialize the prng system
        if (m_prng == null) m_prng = new System.Random(m_seed);

        // Check: are the gem prefabs set?
        if (m_smallGemPrefab == null) {
            Debug.LogError("Cannot generate gems: prefabs for small gems not set");
            return;
        }

        // Cap the max count of small gems to the total number of weenies
        if (m_maxSmallGems > m_landmarkGenerator.weeniePositions.Count) {
            m_maxSmallGems = m_landmarkGenerator.weeniePositions.Count;
        }

        // Generate list of weenie indices that'll track which weenies we want to place the small gems at.
        List<int> weenieIndices = new List<int>();
        for(int i = 0; i < m_landmarkGenerator.weeniePositions.Count; i++) weenieIndices.Add(i);
        
        // Iterate among weenies. Generate as many small gems as there are weenies... or at least up to the max count of small gems
        if (m_gemData == null) m_gemData = new Dictionary<Gem, GemData>();
        int startID = m_gemData.Count;
        for(int i = 0; i < m_maxSmallGems; i++) {
            // Which weenie should we choose this time?
            int weenieIndex = m_prng.Next(0, weenieIndices.Count);

            // Get the position of the 
            Vector3 weeniePosition = m_landmarkGenerator.weeniePositions[weenieIndices[weenieIndex]];
            weenieIndices.RemoveAt(weenieIndex);
            
            // Let's add noise to the position and rotation of the gem itself
            Vector3 pos = weeniePosition + new Vector3(0f, m_smallGemHeightOffset, 0f);
            Quaternion rot = Quaternion.EulerAngles(
                m_prng.Next(0, 30),
                m_prng.Next(0,360),
                m_prng.Next(0, 30)
            );
            
            // Instantiate
            Gem generatedGem = Instantiate(m_smallGemPrefab, pos, rot, m_gemParent) as Gem;

            // Save a record of this gem
            GemData newGem = new GemData { 
                gem=generatedGem, 
                id=startID+i,
                position=pos,
                detected=false,
                collected=false,
                isDestination=false
            };
            m_gemData.Add(generatedGem, newGem);
        }     
    }

    public void GenerateDestinationGem(Vector3 destination) {
        // Initialize randomization engine
        if (m_prng == null) m_prng = new System.Random(m_seed);

        // Check, does the destination gem prefab exist? Exit early if not.
        if (m_destinationGemPrefab == null) {
            Debug.LogError("Cannot generate gems: destination gem prefab is not set");
            return;
        }   

        // Create the gem data dictionary if it doesn't exist
        if (m_gemData == null) m_gemData = new Dictionary<Gem, GemData>();

        // Get the resulting destination and rotation
        Vector3 pos = destination + new Vector3(0f, m_destGemHeightOffset, 0f);
        Quaternion rot = Quaternion.Euler(0f, m_prng.Next(0,360), 0f);

        // Instantiate
        m_destinationGem = Instantiate(m_destinationGemPrefab, pos, rot, m_gemParent) as Gem;

        // Save
        GemData destinationData = new GemData { 
            gem=m_destinationGem, 
            id=m_gemData.Count,
            position=pos,
            detected=false,
            collected=false,
            isDestination=true
        };
        m_gemData.Add(m_destinationGem, destinationData);
    }

    public void AddGemToMap(Gem gem) {
        if (m_gemData == null || !m_gemData.ContainsKey(gem)) {
            Debug.LogError("Cannot add gem to map: either gemdata not initialized or gem is not contained in map");
            return;
        }
        GemData data = m_gemData[gem];

        // If the data is already detected... so don't do anything
        if (data.detected) return;

        // Data isn't detected yet. Add it to map, and note it as detected
        if (data.isDestination) AddDestinationGemToMap(data.position);
        else AddSmallGemToMap(data.position);
        data.detected = true;
        Debug.Log($"Gem ID {data.id.ToString()} is detected");
    }
    public void AddGemToMap(Gem gem, GemData data) {
        // If the data is already detected... so don't do anything
        if (data.collected || data.detected) return;

        // Data isn't detected yet. Add it to map, and note it as detected
        if (data.isDestination) AddDestinationGemToMap(data.position);
        else AddSmallGemToMap(data.position);
        data.detected = true;
        Debug.Log($"Gem ID {data.id.ToString()} is detected");
    }

    public void AddSmallGemToMap(Vector3 pos) {
        m_terrainGenerator.DrawCircleOnHeldMap(pos.x, pos.z, m_smallGemMapSize+1, Color.black);
        m_terrainGenerator.DrawCircleOnHeldMap(pos.x, pos.z, m_smallGemMapSize, m_smallGemMapColor);
    }
    public void AddDestinationGemToMap(Vector3 pos) {
        m_terrainGenerator.DrawCircleOnHeldMap(pos.x, pos.z, m_destGemMapSize+1, Color.black);
        m_terrainGenerator.DrawCircleOnHeldMap(pos.x, pos.z, m_destGemMapSize, m_destGemMapColor);
    }

    public void CollectSmallGemToMap(Vector3 pos) {
        m_terrainGenerator.DrawCircleOnHeldMap(pos.x, pos.z, m_smallGemMapSize+1, Color.black);
        m_terrainGenerator.DrawCircleOnHeldMap(pos.x, pos.z, m_smallGemMapSize, m_smallGemCollectedColor);
    }

    public void CollectGem(Gem gem) {
        if (m_gemData == null || !m_gemData.ContainsKey(gem)) {
            Debug.LogError("Cannot add gem to map: either gemdata not initialized or gem is not contained in map");
            return;
        }
        GemData data = m_gemData[gem];

        // If the data is already collected... don't do anything
        // Otherwise, make sure to 
        if (!data.collected) {
            data.collected = true;
            CollectSmallGemToMap(data.position);
            // ADD EFFECT HERE
        }
        gem.gameObject.SetActive(false);

    }

    public void ToggleViewCheck(bool setTo) {
        m_checkViewInitialized = setTo;
    }

    private void LateUpdate() {
        // Don't do anything if we haven't initialized yet or if our dictionary of gems is unset
        if (!m_checkViewInitialized) return;
        if (m_gemData == null || m_gemData.Count == 0) return;

        // Loop through all gems. All gems must have a collider. If not, then we skip.
        foreach(KeyValuePair<Gem, GemData> kvp in m_gemData) {
            Collider col = kvp.Key._collider;
            if (col == null) continue;
            if (FustrumManager.current.gemFustrumCamera.CheckInFustrum(col) && !kvp.Value.detected) AddGemToMap(kvp.Key, kvp.Value);
        }
    }

    public static bool TestPlanesAABB(Plane[] planes, Bounds bounds) {
        for (int i = 0; i < planes.Length; i++) {
            Plane plane = planes[i];
            float3 normal_sign = math.sign(plane.normal);
            float3 test_point = (float3)bounds.center + (((float3)bounds.extents)*normal_sign);
            float dot = math.dot(test_point, plane.normal);
            if (dot + plane.distance < 0) return false;
        }
        return true;
    }
}
