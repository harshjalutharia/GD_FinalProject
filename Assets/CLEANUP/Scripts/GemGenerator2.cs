using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using DataStructures.ViliWonka.KDTree;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class GemGenerator2 : MonoBehaviour
{
    public static GemGenerator2 current;

    public class GemSpawn : IComparable<GemSpawn> {
        public Vector3 point;
        public int fitness;
        public int CompareTo(GemSpawn other) {
            if (other == null) return 1;
            return other.fitness.CompareTo(this.fitness); 
        }
    }

    [Header("=== Generator Settings ===")]
    [SerializeField, Tooltip("The seed int. used to set the pseudo-rng system")]    private int m_seed;
    public int seed => m_seed;
    [SerializeField, Tooltip("The map width")]  private float m_width = 100f;
    [SerializeField, Tooltip("The map height")] private float m_height = 100f;
    public float width => m_width;
    public float height => m_height;
    [SerializeField, Tooltip("The parent transform where all gems are contained.")] private Transform m_gemParent;
    
    [Header("=== Gem Spawn Restrictions ===")]
    [SerializeField, Tooltip("Buffer area along the width of the map we dont' want to spawn gems in"), Range(0f,0.5f)]  private float m_widthBorderRange = 0.1f;
    [SerializeField, Tooltip("Buffer area along the height of the map we dont' want to spawn gems in"), Range(0f,0.5f)] private float m_heightBorderRange = 0.1f;
    private float widthBorderRange => m_widthBorderRange * m_width;
    private float heightBorderRange => m_heightBorderRange * m_height;
    [SerializeField, Tooltip("Steepness threshold - if a location is too steep, do not place here"), Range(0f,90f)]     private float m_maxSteepnessThreshold = 45f; 
    [SerializeField, Tooltip("Distance threshold - if a location is too close, we cannot place there")] private float m_minDistanceThreshold = 20f;
    [SerializeField, Tooltip("The max number of gem locations per region")] private int m_maxGemCountPerRegion = 15;

    [Header("=== References ===")]
    [SerializeField, Tooltip("The prefab used for small gems")]             private Gem m_smallGemPrefab;
    [SerializeField, Tooltip("The prefab used for the destination gem")]    private Gem m_destinationGemPrefab;

    [Header("=== Outputs - READ ONLY ===")]
    [SerializeField] private List<Gem> m_destinationGems = new List<Gem>();
    public List<Gem> destinationGems => m_destinationGems;
    [SerializeField] private List<Gem> m_smallGems = new List<Gem>();
    public List<Gem> smallGems => m_smallGems;

    private System.Random m_prng;
    private KDQuery m_gemQuery;
    private KDTree m_gemTree;

    [Header("=== Post-Generation ===")]
    private bool m_generated = false;
    public bool generated => m_generated;
    public UnityEvent onGenerationEnd;

    #if UNITY_EDITOR
    [SerializeField] private bool m_drawGizmos = false;
    void OnDrawGizmos() {
        if (!m_drawGizmos) return;
        if (m_destinationGems.Count > 0) {
            Gizmos.color = Color.blue;
            foreach(Gem g in m_destinationGems) Gizmos.DrawSphere(g.transform.position, 10f);
        }
        if (m_smallGems.Count > 0) {
            Gizmos.color = Color.yellow;
            foreach(Gem g in m_smallGems) Gizmos.DrawSphere(g.transform.position, 10f);
        }
    }
    #endif

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

    public void SetDimensions(float w, float h) {
        m_width = w;
        m_height = h;
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

        // Initialize KDQuery 
        m_gemQuery = new KDQuery();

        // Need to generate for reach region
        foreach(Region region in Voronoi.current.regions) {
            GenerateRegion(region);
        }

        m_generated = true;
        onGenerationEnd?.Invoke();
    }

    private void GenerateRegion(Region region) {
        // We must place a major gem and other smaller gems around where the region's clusters' points are positioned. We will place the major gem in one of the core cluster's points, and all points in either the core or sub clusters are fair game.
        // Not all placements are suitable. For example, it'll be hard to find a gem that's lodged itself into the side of a mountain, for example.
        // We'll therefore restrict all gem placements within a boundary and steepness.
        
        // Firstly, the major gem. We'll place this at one of the core cluster's points.
        DBScanCluster coreCluster = region.coreCluster;
        List<Centroid> unvisitedCentroids = new List<Centroid>(region.centroids);

        // Search for the closest centroid. Should still be within this region. 
        Centroid closestCentroid = region.QueryClosestCentroid(coreCluster.centroid, true);
        TerrainManager.current.TryGetPointOnTerrain(closestCentroid.position.x, closestCentroid.position.z, out Vector3 majorGemLocation, out Vector3 majorNormal, out float majorSteepness);
        unvisitedCentroids.Remove(closestCentroid);

        // Instantiate the major gem. Make sure to delete any trees in the region
        majorGemLocation += majorNormal * 0.25f;
        if (VegetationGenerator2.current != null) VegetationGenerator2.current.DeactivateTreesInRadius(majorGemLocation, 10f);
        Gem destinationGem = Instantiate(m_destinationGemPrefab, majorGemLocation, Quaternion.identity, m_gemParent) as Gem;  
        destinationGem.gameObject.name = $"Destination Gem - {region.attributes.name}";
        destinationGem.gemType = Gem.GemType.Destination;
        destinationGem.regionIndex = region.id;
        destinationGem.SetColor(region.attributes.color);
        m_destinationGems.Add(destinationGem);
        region.destinationGem = destinationGem;
        
        // Given the major gem location, we place gems within reasonable distance between each other.
        // We us the GemSpawn comparable to check different points. Basically, we branch out and check for the fitness of this point as a place to spawn a gem.
        // Fitness is dependent on steepness and proximity to other gems (further away from other gems is better). Automatically disregard points that are close to the edge
        List<GemSpawn> potentialSmallGemSpawns = new List<GemSpawn>();
        HashSet<Centroid> visitedPotential = new HashSet<Centroid>();
        visitedPotential.Add(closestCentroid);
        QuerySmallGemCandidate(closestCentroid.position, ref region, ref potentialSmallGemSpawns, ref visitedPotential);

        // For all spawns in potential spawns, add them to minor gem locations
        region.smallGems = new List<Gem>();
        region.collectedGems = new List<Gem>();
        foreach(GemSpawn spawn in potentialSmallGemSpawns) {
            Vector3 smallGemLocation = spawn.point;
            if (VegetationGenerator2.current != null) VegetationGenerator2.current.DeactivateTreesInRadius(smallGemLocation, 5f);
            Gem smallGem = Instantiate(m_smallGemPrefab, smallGemLocation, Quaternion.identity, m_gemParent) as Gem;
            smallGem.gameObject.name = $"Small Gem - {region.attributes.name}";
            smallGem.gemType = Gem.GemType.Small;
            smallGem.regionIndex = region.id;
            smallGem.SetColor(region.attributes.color);
            m_smallGems.Add(smallGem);
            region.smallGems.Add(smallGem);
        }
    }

    //  start` must be normalized between 0 and 1.
    public void QuerySmallGemCandidate(Vector3 point, ref Region region, ref List<GemSpawn> currentLocations, ref HashSet<Centroid> checkedLocations) {
        // double-check that we haven't already gone over the limit
        if (currentLocations.Count >= m_maxGemCountPerRegion) return;
        
        // Query the centroids around this location. Use KNearest
        List<Centroid> potentials = region.QueryKNearestCentroids(point, 10, true);
        
        // For each point, we check its fitness.
        List<GemSpawn> potentialSpawns = new List<GemSpawn>();
        foreach(Centroid c in potentials) {
            // If `i` is already in known current locations, then continue
            if (checkedLocations.Contains(c)) continue;
            // Otherwise, get the point
            Vector3 gemPoint = c.position;
            // Ignore if too close to edge or, if sme reason, the terrain manager can't seem to get the terrain point.
            if (gemPoint.x/m_width < m_widthBorderRange || gemPoint.x/m_width > 1f-m_widthBorderRange || gemPoint.z/m_height < m_heightBorderRange || gemPoint.y/m_height > 1f-m_heightBorderRange) continue;
            if (!TerrainManager.current.TryGetPointOnTerrain(gemPoint.x, gemPoint.z, out Vector3 gemLocation, out Vector3 gemNormal, out float gemSteepness)) continue;
            if (gemSteepness > m_maxSteepnessThreshold) continue;
            gemLocation += gemNormal * 0.5f;
            // Evaluate fitness of this point. Determined by steepness and distance to the current gem point
            int fitness = 0;
            if (currentLocations.Count == 0) fitness = Mathf.RoundToInt(Vector3.Distance(point, gemLocation));
            else {
                foreach(GemSpawn spawn in currentLocations) fitness += Mathf.RoundToInt(Vector3.Distance(spawn.point, gemLocation));
            }
            GemSpawn gemSpawn = new GemSpawn { point=gemLocation, fitness=fitness };
            potentialSpawns.Add(gemSpawn);
            checkedLocations.Add(c);
        }
        // If the list of potential spawns is empty, then don't continue
        if (potentialSpawns.Count == 0) return; 
        // Sort potential spawns
        potentialSpawns.Sort();
        // The topmost result is optimal
        currentLocations.Add(potentialSpawns[0]);

        // If we haven't reached the total number of gems for this region, then we continue searching
        if (currentLocations.Count < m_maxGemCountPerRegion) {
            foreach(GemSpawn spawn in potentialSpawns) QuerySmallGemCandidate(spawn.point, ref region, ref currentLocations, ref checkedLocations);
        }
    }
}
