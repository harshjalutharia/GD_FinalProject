using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DataStructures.ViliWonka.KDTree;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class Voronoi : MonoBehaviour
{
    public static Voronoi current;

    [Header("=== Generator Settings ===")]
    [SerializeField, Tooltip("The seed used for pseudo random generation")] protected int m_seed;
    [SerializeField, Tooltip("The world width of the resulting voronoi map. Used in gizmos rendering + for cluster querying when provided world positions")]    protected float m_width = 1f;
    [SerializeField, Tooltip("The world height of the resulting voronoi map. Used in gizmos rendering + for cluster querying when provided world positions")]   protected float m_height = 1f;
    [SerializeField, Tooltip("Should we generate on start?")]   private bool m_generateOnStart = true;
    [SerializeField, Tooltip("Should we auto-updated per change?")] protected bool m_autoUpdate = true;

    [Header("=== Voronoi Tessellation Settings ===")]
    [SerializeField, Tooltip("How many segments do we want?")]      protected int m_numSegments = 50;
    [SerializeField, Tooltip("Buffer ratio from horizontal edges"), Range(0f,0.5f)] protected float m_horizontalBuffer = 0.1f;
    [SerializeField, Tooltip("Buffer ratio from vertical edges"), Range(0f, 0.5f)]  protected float m_verticalBuffer = 0.1f;
    [SerializeField, Tooltip("How many iterations of Lloyd Relaxation should we perform?")] protected int m_numRelaxations = 2;

    [Header("=== DBScan Clustering, Regions ===")]
    [SerializeField, Tooltip("The epsilon distance required for DBScan"), Range(0f,1f)] protected float m_dbscanEpsilon = 0.1f;
    [SerializeField, Tooltip("The minimum number of points for core point classification"), Range(0,20)] protected int m_dbscanMinPoints = 10;
    [SerializeField] private RegionAttributes m_grasslandsAttributes;
    [SerializeField] private RegionAttributes m_oakAttributes;
    [SerializeField] private RegionAttributes m_birchAttributes;
    [SerializeField] private RegionAttributes m_spruceAttributes;

    [Header("=== Gameplay ===")]
    [SerializeField, Tooltip("Reference to the player transform")]  private Transform m_playerRef;
    [SerializeField, Tooltip("The TextMeshProUGUI textbox for the region's name")]  private TextMeshProUGUI m_regionNameTextbox = null;
    [SerializeField, Tooltip("The TextMeshProUGUI textbox for the total # of gems")]    private TextMeshProUGUI m_regionTotalCountTextbox = null;
    [SerializeField, Tooltip("The TextMeshProUGUI textbox for the current # of gems collected")]    private TextMeshProUGUI m_regionCurrentCountTextbox = null;

    [Header("=== Outputs - READ ONLY ===")]
    [SerializeField, Tooltip("The voronoi segment centroids")] protected Centroid[] m_centroids;
    private KDTree m_centroidsTree;
    [Space]
    [SerializeField] private List<DBScanCluster> m_clusters;
    private KDTree m_clustersTree;
    [Space]
    [SerializeField] private List<Region> m_regions;
    [SerializeField] private Region m_grasslandsRegion;
    [SerializeField] private Region m_oakRegion;
    [SerializeField] private Region m_birchRegion;
    [SerializeField] private Region m_spruceRegion;
    [SerializeField, Tooltip("Which region is the player currently in?")]   private Region m_playerRegion = null;
    // ---
    public KDTree normalizedCentroidsTree;
    public KDTree centroidsTree;

    public int seed => m_seed;
    public bool autoUpdate => m_autoUpdate;
    public float width => m_width;
    public float height => m_height;
    public Centroid[] centroids => m_centroids;
    public List<DBScanCluster> clusters => m_clusters;
    public List<Region> regions => m_regions;
    public Region playerRegion => m_playerRegion;

    [Header("=== On Completion ===")]
    private bool m_generated = false;
    public bool generated => m_generated;
    public UnityEvent onGenerationEnd;

    private int[] m_centroidToClusterMap;
    private Vector3[] m_clusterWorldCentroids;
    private Vector3[] m_regionCentroids;
    private KDTree m_regionTree;

    private KDQuery m_query;
    private System.Random m_prng;

    #if UNITY_EDITOR
    [Header("=== Debug Settings ===")]
    [SerializeField, Tooltip("Print debug?")] protected bool m_showDebug = false;
    [SerializeField, Tooltip("The sphere size for region centroids")] protected float m_gizmosCentroidSize = 2f;
    [SerializeField, Tooltip("The sphere size for region centroids")] protected float m_gizmosPointSize = 1f;
    protected virtual void OnDrawGizmos() {
        if (!m_showDebug) return;

        Gizmos.color = Color.black;
        Vector3 worldCenter = transform.position + new Vector3(m_width/2f,0f,m_height/2f);
        Vector3 world1 = transform.rotation * transform.position;
        Vector3 world2 = transform.rotation * (transform.position + new Vector3(m_width, 0f, 0f));
        Vector3 world3 = transform.rotation * (transform.position + new Vector3(0f, 0f, m_height));
        Vector3 world4 = transform.rotation * (transform.position + new Vector3(m_width, 0f, m_height));
        Gizmos.DrawLine(world1, world2);
        Gizmos.DrawLine(world1, world3);
        Gizmos.DrawLine(world2, world4);
        Gizmos.DrawLine(world3, world4);
        
        if (!m_generated) return;
        for(int i = 0; i < m_regions.Count; i++) {
            Region region = m_regions[i];
            Gizmos.color = region.attributes.color;            
            Gizmos.DrawSphere(transform.rotation * (transform.position + region.coreCluster.centroid), m_gizmosCentroidSize);
            foreach(Centroid centroid in region.centroids) {
                Gizmos.DrawSphere(transform.rotation * (transform.position + centroid.position), m_gizmosPointSize);
            }
        }
    }
    #endif

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

    private void Awake() {
        current = this;
    }
    private void Start() {
        if (m_generateOnStart) Generate();
    }

    public void Generate() {
        m_prng = new System.Random(m_seed);         // Initialize the randomization
        m_query = new KDQuery();                    // This is a query class that interacts with any KDTrees we formulate.
        
        Vector3[] initial = GenerateCentroids(m_numSegments);   // Generate the initial list of centroids.
        Vector3[] normalized = LloydRelaxation(initial, m_numRelaxations); // We modify the centroid positions via Lloyd Relaxation.
        FinalizeCentroids(normalized);
        
        DBScanClusterRegions(m_dbscanEpsilon*Mathf.Max(m_width, m_height), m_dbscanMinPoints);  // Use DBScan to generate clusters, which can be interpreted as regions
        DetermineRegions();
        
        m_generated = true;
        onGenerationEnd?.Invoke();
    }

    public Vector3[] GenerateCentroids(int n) {
        List<Vector3> centers = new List<Vector3>();
        int horBound = (int)(100f*m_horizontalBuffer);
        int verBound = (int)(100f*m_verticalBuffer);
        while(centers.Count < n) {
            float x = (float)m_prng.Next(horBound,101-horBound)/100f;
            float z = (float)m_prng.Next(verBound,101-verBound)/100f;
            Vector3 c = new Vector3(x,0f,z);
            if (!centers.Contains(c)) centers.Add(c);
        }
        return centers.ToArray();
    }

    public Vector3[] LloydRelaxation(Vector3[] initial, int n, int fidelity = 100) {
        Vector3[] relaxedCentroids = initial;
        if (m_numRelaxations == 0) return relaxedCentroids;

        for(int iter = 0; iter < n; iter++) {

            // Generate a KDTree
            KDTree tree = new KDTree(relaxedCentroids, 32);

            // Generate a total and count for each centroid
            Vector3[] centroidTotals = new Vector3[initial.Length];
            int[] centroidCounts = new int[initial.Length];

            // Iterate across based on fidelity
            for(int y = 0; y < fidelity; y++) {
                for(int x = 0; x < fidelity; x++) {
                    
                    // Get the closest centroid
                    Vector3 p = new Vector3(x,0f,y) / (float)fidelity;
                    List<int> closestIndices = new List<int>();
                    m_query.ClosestPoint(tree, p, closestIndices);
                    centroidTotals[closestIndices[0]] += p;
                    centroidCounts[closestIndices[0]] += 1;

                    /*
                    float smallestDistance = float.MaxValue;
                    int smallestIndex = 0;

                    for(int i = 0; i < centroids.Length; i++) {
                        Vector3 c = relaxedCentroids[i];
                        float distance = Vector3.Distance(c,p);
                        if (distance < smallestDistance) {
                            smallestDistance = distance;
                            smallestIndex = i;
                        }
                    }
                    */
                    
                }
            }
            for(int i = 0; i < initial.Length; i++) centroidTotals[i] /= (float)centroidCounts[i];
            relaxedCentroids = centroidTotals;
        }
        return relaxedCentroids;
    }

    public void FinalizeCentroids(Vector3[] normalized) {
        // We have a normalized set, and need to generate a world equivalent.
        // We now need to convert into an array of `Centroid` class

        m_centroids = new Centroid[normalized.Length];
        Vector3[] centroidPositions = new Vector3[normalized.Length];

        for(int i = 0; i < normalized.Length; i++) {
            Vector3 p = normalized[i];
            // We know we can just do X and Z
            float worldX = p.x * m_width;
            float worldZ = p.z * m_height;
            // Generate world position
            Vector3 wp = new Vector3(worldX, 0f, worldZ);
            if (TerrainManager.current != null && !TerrainManager.current.generated && TerrainManager.current.TryGetPointOnTerrain(worldX, worldZ, out Vector3 worldPos, out Vector3 n, out float s)) {
                wp = worldPos;
            }
            // Add to our list of centroids
            m_centroids[i] = new Centroid { 
                index=i, 
                clusterIndex=0, 
                regionIndex=0, 
                classification=0, 
                normalizedPosition=p,
                position=wp 
            };
            centroidPositions[i] = wp;
        }

        // Form the KDTree for centroids
        m_centroidsTree = new KDTree(centroidPositions, 32);
    }

    public void DBScanClusterRegions(float eps, int minPoints) {
        //  - core point = any point who, given a radius of epsilon, has at least minPoints number of neighbors.
        //  - border point = not a core point (aka fewer than minPoints neighbors within epsilon radius) but is a neighbor of a core point (aka is within epsilon distance to a core point)
        //  - noise point = neither a core or a border point

        // Important concepts: 
        //  1. Directly density-reachable: A point P is directly density-reachable from a query point Q if 1) P is in the epsilon neighborhood of Q, and 2) both P and Q are core points
        //  2. Density Connected: a point P is density connected to query point Q if there is a chain of points P1, P2,... Pn, P1=P such that P(i+1) is directly density-reachable from Pi

        // Overall steps:
        //  1. Identify all points as either core point, border point, or noise point.
        //  2. For all unclustered core points:
        //      a. create a new cluster
        //      b. add all points that are unclustered and density-connected to the current point in this cluster.
        //  3. For eahc unclustered border point, assign it the cluster of the nearest core point
        //  4. Ignore all noise points

        // First step is to classify each centroid as either a core point (2), border point (1), or noise point (0).
        for(int i = 0; i < m_centroids.Length; i++) {
            Centroid centroid = m_centroids[i];
            centroid.classification = 0;

            // Query all neighbors using KDTree
            m_query.Radius(m_centroidsTree, centroid.position, eps, centroid.neighbors);

            // Check if nNeighbors >= minPoints. If so, this is a core point.
            if (centroid.neighbors.Count >= minPoints) {
                centroid.classification = 2;
                // For any neighbors, at least classify them as border point if they're still unclassified.
                foreach(int neighborIndex in centroid.neighbors) {
                    if (m_centroids[neighborIndex].classification == 0) m_centroids[neighborIndex].classification = 1;
                }
            }
            else if (centroid.classification == 0) {
                // We can still classifify this point as a border point as long as this point is still unclassified (maybe it was classified as a neighbor point at some point in the past)
                foreach(int nI in centroid.neighbors) {
                    if (m_centroids[nI].classification == 2) {
                        centroid.classification = 1;
                        break;
                    }
                }
            }
        }

        // Step 2: Among all unclustered core points, 1) create a new cluster, and 2) add all points that are unclustered and density-connected to the current point
        List<DBScanCluster> clusters = new List<DBScanCluster>();
        
        // First cluster contains all noise points
        DBScanCluster initialCluster = new DBScanCluster();
        initialCluster.id = 0;
        clusters.Add(new DBScanCluster());

        // We use `centroidToClusterMap` to check if a centroid is already in a cluster
        for(int i = 0; i < m_centroids.Length; i++) {
            Centroid centroid = m_centroids[i];
            if (centroid.classification != 2)   continue;   // Ignore any that are not core points
            if (centroid.clusterIndex != 0)     continue;   // Ignore if this point already is in a cluster
            
            // Create its own cluster
            DBScanCluster c = new DBScanCluster();
            clusters.Add(c);
            c.id = clusters.Count-1;
            c.color = new Color(UnityEngine.Random.Range(0f,1f), UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f));
            c.centroids.Add(centroid);
            centroid.clusterIndex = c.id;

            // Among all other points, check if they're a core point and can be density-connected to current point
            for(int j = i+1; j < m_centroids.Length; j++) {
                if (i == j) continue;
                Centroid other = m_centroids[j];
                if (other.classification != 2)  continue;   // Ignore if not core poijnt
                if (other.clusterIndex != 0)    continue;   // ignore if this is already classified
                HashSet<Centroid> visitedPoints = new HashSet<Centroid>();
                foreach(int nI in m_centroids[j].neighbors) {
                    if (m_centroids[nI].classification != 2) continue;
                    visitedPoints.Add(m_centroids[nI]);
                }
                if (visitedPoints.Contains(centroid) && !c.centroids.Contains(other)) {
                    c.centroids.Add(other);
                    other.clusterIndex = c.id;
                }
            }
        }

        // Step 3: For each unclustered border point, assign it the cluster of the nearest core point
        for(int i = 0; i < m_centroids.Length; i++) {
            Centroid centroid = m_centroids[i];
            if (centroid.classification != 1)   continue;   // Ignore if not border point
            if (centroid.clusterIndex != 0)     continue;   // Ignore if clustered already
            float smallestDistance = float.MaxValue;
            Centroid closest = null;
            for(int j = 0; j < m_centroids.Length; j++) {
                if (i == j) continue;
                if (m_centroids[j].classification != 2) continue;
                float distance = Vector3.Distance(centroid.position, m_centroids[j].position);
                if (distance < smallestDistance) {
                    smallestDistance = distance;
                    closest = m_centroids[j];
                }
            }
            clusters[closest.clusterIndex].centroids.Add(centroid);
            centroid.clusterIndex = clusters[closest.clusterIndex].id;
        }

        // Step 4: sort the clusters by size.
        clusters.Sort();

        // We must now look through all clusters once again, remapping centroids to their new cluster ID
        m_clusters = clusters;
        Vector3[] clusterCentroids = new Vector3[m_clusters.Count];
        for(int ci = 0; ci < m_clusters.Count; ci++) {
            DBScanCluster cluster = m_clusters[ci];
            cluster.centroid = Vector3.zero;    // in world position
            foreach(Centroid c in cluster.centroids) {
                c.clusterIndex = ci;
                cluster.centroid += c.position;
            }
            cluster.centroid /= cluster.centroids.Count;
            clusterCentroids[ci] = cluster.centroid;
            cluster.GenerateTree();
        }
        m_clustersTree = new KDTree(clusterCentroids, 16);

        /*
        // Step 5: for each cluster, map their centroids to their cluster in `m_centroidToClusterMap`
        // We cannot use already-generated `centroidToClusterMap` due to sorting, which has messed up the mapping already
        // Simultaneously, we use this opportunity to estimate the centroid of the cluster itself.
        m_centroidToClusterMap = new int[m_normalizedCentroids.Length];
        m_clusterWorldCentroids = new Vector3[m_clusters.Count];
        // Iterate through allclusters
        for(int ci = 0; ci < m_clusters.Count; ci++) {
            DBScanCluster cluster = m_clusters[ci];
            cluster.centroid = Vector3.zero;
            foreach(int i in cluster.pointIndices) {
                m_centroidToClusterMap[i] = ci;
                cluster.centroid += new Vector3(centroids[i].x, 0f, centroids[i].z);
            }
            cluster.centroid /= cluster.points.Count;
            cluster.worldCentroid = transform.position + new Vector3(cluster.centroid.x * m_width, 0f, cluster.centroid.z * m_height);
            m_clusterCentroids[ci] = cluster.centroid;
            m_clusterWorldCentroids[ci] = cluster.worldCentroid;
        }
        m_clusterWorldCentroidTree = new KDTree(m_clusterWorldCentroids, 16);
        */
    }

    public void DetermineRegions(float edgeRatio=0.25f) {
        Vector2 worldCenter = Vector2.one * 0.5f;
        float maxDistance = 0.5f * Mathf.Sqrt(2f);

        List<Region> regions = new List<Region>();
        TerrainChunk.MinMax edgeRange = new TerrainChunk.MinMax { min = edgeRatio, max = 1f-edgeRatio };
        
        for(int i = 0; i < m_clusters.Count; i++) {
            DBScanCluster cluster = clusters[i];
            Vector3 clusterCentroid = cluster.centroid;
            float normalizedX = Mathf.Clamp(clusterCentroid.x / m_width, 0f, 1f);
            float normalizedY = Mathf.Clamp(clusterCentroid.z / m_height, 0f, 1f);
            int distanceToCenter = Mathf.RoundToInt(Mathf.InverseLerp(maxDistance, 0f, Vector2.Distance(new Vector2(normalizedX, normalizedY), worldCenter)) * 10f);
            int size = Mathf.RoundToInt((float)cluster.centroids.Count / m_numSegments * 10f);
            Region region = new Region { coreCluster=cluster, majorRegionWeight=distanceToCenter*size };
            regions.Add(region);
        }
        regions.Sort();
        
        m_regionCentroids = new Vector3[4];
        // First region: grassland
        m_grasslandsRegion = regions[0];
        m_grasslandsRegion.id = 0;
        m_grasslandsRegion.attributes = m_grasslandsAttributes;
        m_grasslandsRegion.coreCluster.regionIndex = 0;
        m_grasslandsRegion.centroids.AddRange(m_grasslandsRegion.coreCluster.centroids);
        m_regionCentroids[0] = m_grasslandsRegion.coreCluster.centroid;
        // Second region: oak
        m_oakRegion = regions[1];
        m_oakRegion.id = 1;
        m_oakRegion.attributes = m_oakAttributes;
        m_oakRegion.coreCluster.regionIndex = 1;
        m_oakRegion.centroids.AddRange(m_oakRegion.coreCluster.centroids);
        m_regionCentroids[1] = m_oakRegion.coreCluster.centroid;
        // Third region: birch
        m_birchRegion = regions[2];
        m_birchRegion.id = 2;
        m_birchRegion.attributes = m_birchAttributes;
        m_birchRegion.coreCluster.regionIndex = 2;
        m_birchRegion.centroids.AddRange(m_birchRegion.coreCluster.centroids);
        m_regionCentroids[2] = m_birchRegion.coreCluster.centroid;
        // Fourth region: spruce
        m_spruceRegion = regions[3];
        m_spruceRegion.id = 3;
        m_spruceRegion.attributes = m_spruceAttributes;
        m_spruceRegion.coreCluster.regionIndex = 3;
        m_spruceRegion.centroids.AddRange(m_spruceRegion.coreCluster.centroids);
        m_regionCentroids[3] = m_spruceRegion.coreCluster.centroid;
        // Set `m_regions`
        m_regions = new List<Region>() { m_grasslandsRegion, m_oakRegion, m_birchRegion, m_spruceRegion };
        m_regionTree = new KDTree(m_regionCentroids, 4);

        // Iterate through remaining regions. Associate non-major regions with major regions via KDTree KNearest
        for(int i = 4; i < regions.Count; i++) {
            // Cluster
            DBScanCluster cluster = regions[i].coreCluster;
            // Get nearest regions
            List<int> results = new List<int>();
            m_query.ClosestPoint(m_regionTree, cluster.centroid, results);
            // First item is considered the closest
            cluster.regionIndex = results[0];
            m_regions[results[0]].subClusters.Add(cluster); 
            m_regions[results[0]].centroids.AddRange(cluster.centroids);
        } 

        m_grasslandsRegion.GenerateTree();
        m_oakRegion.GenerateTree();
        m_birchRegion.GenerateTree();
        m_spruceRegion.GenerateTree();

    }
    
    // Note: query MUST BE NORMALIZED BETWEEN 0 and 1 for each axis
    public Centroid QueryClosestCentroid(Vector3 query) {
        List<int> results = new List<int>();
        m_query.ClosestPoint(m_centroidsTree, query, results);
        return m_centroids[results[0]];
    }

    // Note: query MUST BE NORMALIZED BETWEEN 0 and 1 for each axis
    public List<Centroid> QueryKNearestCentroids(Vector3 query, int k) {
        List<int> results = new List<int>();
        m_query.KNearest(m_centroidsTree, query, k, results);
        List<Centroid> nearest = new List<Centroid>();
        foreach(int i in results) nearest.Add(m_centroids[i]);
        return nearest;
    }

    public DBScanCluster QueryClosestCluster(Vector3 query) {
        List<int> results = new List<int>();
        m_query.ClosestPoint(m_clustersTree, query, results);
        return m_clusters[results[0]];
    }

    public List<DBScanCluster> QueryKNearestClusters(Vector3 query, int k) {
        List<int> results = new List<int>();
        m_query.KNearest(m_clustersTree, query, k, results);
        List<DBScanCluster> nearest = new List<DBScanCluster>();
        foreach(int i in results) nearest.Add(m_clusters[i]);
        return nearest;
    }

    public Region QueryClosestRegion(Vector3 query) {
        List<int> results = new List<int>();
        m_query.ClosestPoint(m_regionTree, query, results);
        return m_regions[results[0]];
    }

    private void Update() {
        if (SessionManager2.current != null && !SessionManager2.current.gameplayInitialized) return;
        if (m_playerRef == null) return;

        // Track where the player is currently
        m_playerRegion = QueryClosestRegion(m_playerRef.position);
        // Populate the textmeshpros
        if (m_regionNameTextbox != null) m_regionNameTextbox.text = m_playerRegion.attributes.name;
        if (m_regionTotalCountTextbox != null) m_regionTotalCountTextbox.text = m_playerRegion.smallGems.Count.ToString();
        if (m_regionCurrentCountTextbox != null) m_regionCurrentCountTextbox.text = m_playerRegion.collectedGems.Count.ToString();

    }
}

[System.Serializable]
public class Centroid {
    public int index;
    public int clusterIndex = 0;
    public int regionIndex = 0;
    [Space]
    public int classification = 0;
    public Vector3 normalizedPosition;
    public Vector3 position;
    [Space]
    public List<int> neighbors = new List<int>();
}

[System.Serializable]
public class DBScanCluster : IComparable<DBScanCluster> {
    public int id = 0;
    public int regionIndex;
    public Color color = Color.black;
    [Space]
    public Vector3 centroid;
    public List<Centroid> centroids = new List<Centroid>();
    public KDTree centroidTree;
    [Space]
    public List<int> gemPoints = new List<int>();

    public KDQuery query;
    public void GenerateTree() {
        query = new KDQuery();
        Vector3[] points = new Vector3[this.centroids.Count];
        for(int i = 0; i < this.centroids.Count; i++) points[i] = this.centroids[i].position;
        this.centroidTree = new KDTree(points, 32);
    }

    public int CompareTo(DBScanCluster other) {		
        // A null value means that this object is greater. 
	    if (other == null) return 1;	
		return other.centroids.Count.CompareTo(this.centroids.Count);
	}
}

[System.Serializable]
public class Region : IComparable<Region> {
    public int id;
    public RegionAttributes attributes;
    public DBScanCluster coreCluster;
    public List<DBScanCluster> subClusters = new List<DBScanCluster>();
    public List<Centroid> centroids = new List<Centroid>();
    public KDQuery query;
    public KDTree tree;
    [HideInInspector] public int majorRegionWeight;
    [Space]
    public Gem destinationGem;
    public List<Gem> smallGems;
    public List<Gem> collectedGems;
    public bool destinationCollected = false;
    public bool smallGemsCollected => collectedGems.Count == smallGems.Count;
    [Space]
    public Landmark towerLandmark;
    public List<Landmark> minorLandmarks = new List<Landmark>();

    public void GenerateTree() {
        this.query = new KDQuery();
        Vector3[] points = new Vector3[this.centroids.Count];
        for(int i = 0; i < this.centroids.Count; i++) points[i] = this.centroids[i].position;
        this.tree = new KDTree(points, 32);
    }

    public Centroid QueryClosestCentroid(Vector3 queryPos, bool flatten = true) {
        Vector3 q = (flatten) ? new Vector3(queryPos.x, 0f, queryPos.z) : queryPos;
        List<int> results = new List<int>();
        this.query.ClosestPoint(this.tree, q, results);
        return this.centroids[results[0]];
    }

    public List<Centroid> QueryKNearestCentroids(Vector3 queryPos, int k, bool flatten = true) {
        Vector3 q = (flatten) ? new Vector3(queryPos.x, 0f, queryPos.z) : queryPos;
        List<int> results = new List<int>();
        this.query.KNearest(this.tree, q, k, results);
        List<Centroid> toReturn = new List<Centroid>();
        foreach(int i in results) toReturn.Add(this.centroids[i]);
        return toReturn;
    }
    
    public int CompareTo(Region other) {		
        // A null value means that this object is greater. 
	    if (other == null) return 1;
		return other.majorRegionWeight.CompareTo(this.majorRegionWeight);
	}
}
