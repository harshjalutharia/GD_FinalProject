using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class GemGenerator2 : MonoBehaviour
{
    [SerializeField, Tooltip("The parent transform where all gems are contained.")] private Transform m_gemParent;
    [SerializeField, Tooltip("The seed int. used to set the pseudo-rng system")]    private int m_seed;
    public int seed => m_seed;
    [SerializeField, Tooltip("The map width")]  private float m_width = 100f;
    [SerializeField, Tooltip("The map height")] private float m_height = 100f;
    public float width => m_width;
    public float height => m_height;
    [Space]
    [SerializeField, Range(0f,0.5f)]    private float m_widthBorderRange = 0.1f;
    [SerializeField, Range(0f,0.5f)]    private float m_heightBorderRange = 0.1f;
    private float widthBorderRange => m_widthBorderRange * m_width;
    private float heightBorderRange => m_heightBorderRange * m_height;
    [SerializeField] private float m_maxRadiusFromGems = 20f;
    [SerializeField, Range(0f,90f)] private float m_maxSteepnessThreshold = 45f; 
    [SerializeField] private int m_maxGemsPerRegion = 10;
    [SerializeField, Tooltip("The prefab used for small gems")]                                         private Gem m_smallGemPrefab;
    [SerializeField, Tooltip("The prefab used for the destination gem")]                                private Gem m_destinationGemPrefab;
    private System.Random m_prng;

    [SerializeField] private List<Vector3> m_majorGemLocations = new List<Vector3>();
    [SerializeField] private List<Vector3> m_minorGemLocations = new List<Vector3>();

    [Header("=== Post-Generation ===")]
    [SerializeField, Tooltip("Event to call when small gems are generated")]            private UnityEvent m_onSmallGemGenerationEnd;
    [SerializeField, Tooltip("Event to call when the destination gem is generated")]    private UnityEvent m_onDestinationGemGenerationEnd;

    #if UNITY_EDITOR
    void OnDrawGizmos() {
        if (m_majorGemLocations.Count > 0) {
            Gizmos.color = Color.blue;
            foreach(Vector3 p in m_majorGemLocations) Gizmos.DrawSphere(p, 10f);
        }

        if (m_minorGemLocations.Count > 0) {
            Gizmos.color = Color.yellow;
            foreach(Vector3 p in m_minorGemLocations) Gizmos.DrawSphere(p, 10f);
        }
    }
    #endif

    private void Awake() {
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

    public void SetDimensions(float w, float h) {
        m_width = width;
        m_height = height;
    }
    public void SetDimensions(Vector2 dimensions) { SetDimensions(dimensions.x, dimensions.y); }

    public void Generate() {
        // Check if both voronoi and terrain map are generated, then we can proceed. However, if we have neither, then we cannot do anything
        if (TerrainManager.current == null || !TerrainManager.current.generated) {
            Debug.Log("Gem Generator 2: Cannot start generating until terrian is generated.");
            return;
        }
        if (Voronoi.current == null || !Voronoi.current.generated) {
            Debug.Log("Gem Generator 2: Cannot start generating until voronoi is generated.");
            return;
        }

        // Set seed and dimensions
        SetSeed(TerrainManager.current.seed);
        m_width = TerrainManager.current.width;
        m_height = TerrainManager.current.height;
        Debug.Log($"{TerrainManager.current.width}, {TerrainManager.current.height} VS {m_width}, {m_height}");

        // Initialize the prng system
        m_prng = new System.Random(m_seed);

        // Need to generate for reach region
        foreach(Region region in Voronoi.current.regions) GenerateRegion(region);
    }

    private void GenerateRegion(Region region) {
        // We must place a major gem and other smaller gems around where the region's clusters' points are positioned. We will place the major gem in one of the core cluster's points, and all points in either the core or sub clusters are fair game.
        // Not all placements are suitable. For example, it'll be hard to find a gem that's lodged itself into the side of a mountain, for example.
        // We'll therefore restrict all gem placements within a boundary and steepness.
        
        // Firstly, the major gem. We'll place this at one of the core cluster's points.
        DBScanCluster coreCluster = region.coreCluster;
        List<int> unvisitedPoints = new List<int>(region.points);


        int majorGemLocationIndex = Voronoi.current.QueryClosestPointIndex(coreCluster.centroid);
        Vector3 point = Voronoi.current.centroids[majorGemLocationIndex];
        TerrainManager.current.TryGetPointOnTerrain(point.x * m_width, point.z * m_height, out Vector3 majorGemLocation, out Vector3 majorNormal, out float majorSteepness);
        m_majorGemLocations.Add(majorGemLocation);
        unvisitedPoints.Remove(majorGemLocationIndex);

        // For each cluster, we grab a random point
        foreach(int iloc in unvisitedPoints) {
            Vector3 gemPoint = Voronoi.current.centroids[iloc];
            if (gemPoint.x < m_widthBorderRange || gemPoint.x > 1f-m_widthBorderRange || gemPoint.z < m_heightBorderRange || gemPoint.y > 1f-m_heightBorderRange) continue;
            if (TerrainManager.current.TryGetPointOnTerrain(gemPoint.x*m_width, gemPoint.z*m_height, out Vector3 gemLocation, out Vector3 gemNormal, out float gemSteepness)) {
                if (gemSteepness <= m_maxSteepnessThreshold) {
                    m_minorGemLocations.Add(gemLocation);
                }
            }
        }

    }
}
