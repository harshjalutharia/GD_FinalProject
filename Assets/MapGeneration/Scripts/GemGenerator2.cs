using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

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

    [Header("=== Post-Generation ===")]
    [SerializeField, Tooltip("Event to call when small gems are generated")]            private UnityEvent m_onSmallGemGenerationEnd;
    [SerializeField, Tooltip("Event to call when the destination gem is generated")]    private UnityEvent m_onDestinationGemGenerationEnd;

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

        // Initialize the prng system
        m_prng = new System.Random(m_seed);
    }

    private void GenerateRegion(Region region) {
        // We must place a major gem and other smaller gems around where the region's clusters' points are positioned. We will place the major gem in one of the core cluster's points, and all points in either the core or sub clusters are fair game.
        // Not all placements are suitable. For example, it'll be hard to find a gem that's lodged itself into the side of a mountain, for example.
        // We'll therefore restrict all gem placements within a boundary and steepness.
        
        // Store a list of the clusters that actually were either used or considered. We do not want to repeat clusters.
        Dictionary<DBScanCluster, bool> visitedClusters = new Dictionary<DBScanCluster, bool>();
        List<int> visitedPoints = new List<int>(region.points);

        // Firstly, the major gem. We'll place this at one of the core cluster's points.
        DBScanCluster coreCluster = region.coreCluster;
        Vector2 coreCentroid2D = new Vector2(coreCluster.worldCentroid.x, coreCluster.worldCentroid.z);
        bool majorGemLocationFound = false;
        Vector3 majorGemLocation = Vector3.zero;
        do {
            // Get the centroid from the list of visited gems
            int centroidIndex = visitedPoints[m_prng.Next(0, visitedPoints.Count-1)];
            visitedPoints.Remove(centroidIndex);

            // Get the point that corresponds to this centroid
            Vector3 point = Voronoi.current.centroids[centroidIndex];
            Vector2 point2D = new Vector2(point.x, point.z);

            // Check: is this too close to the bell tower (aka this region's core cluster's centroid?)
            float distanceToCore = Vector2.Distance(coreCentroid2D, point2D);
            if (distanceToCore > m_maxRadiusFromGems) continue;

            // Assuming we are here, then the major gem location is found.
            majorGemLocationFound = TerrainManager.current.TryGetPointOnTerrain(point.x, point.z, out majorGemLocation, out Vector3 majorNormal, out float majorSteepness);

            // This COULD lead to an infinite loop - what if, by pure, unadultarated chance, there are NO good positions?
            // Though granted, very unlikely.
        } while(!majorGemLocationFound);

        

    }
}
