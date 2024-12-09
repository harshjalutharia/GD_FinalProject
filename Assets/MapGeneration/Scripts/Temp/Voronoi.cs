using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DataStructures.ViliWonka.KDTree;
using UnityEngine.Events;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class Voronoi : MonoBehaviour
{
    public static Voronoi current;

    [Header("=== Voronoi Tessellation Settings ===")]
    [SerializeField, Tooltip("The seed used for pseudo random generation")] protected int m_seed;
    public int seed => m_seed;
    [SerializeField, Tooltip("How many segments do we want?")]      protected int m_numSegments = 50;
    [SerializeField, Tooltip("Buffer ratio from horizontal edges"), Range(0f,0.5f)] protected float m_horizontalBuffer = 0.1f;
    [SerializeField, Tooltip("Buffer ratio from vertical edges"), Range(0f, 0.5f)]  protected float m_verticalBuffer = 0.1f;
    
    [Header("=== Lloyd Relaxation Settings ===")]
    [SerializeField, Tooltip("How many iterations of Lloyd Relaxation should we perform?")] protected int m_numRelaxations = 2;

    [Header("=== DBScan Clustering Settings ===")]
    [SerializeField, Tooltip("The epsilon distance required for DBScan"), Range(0f,1f)] protected float m_dbscanEpsilon = 0.1f;
    [SerializeField, Tooltip("The minimum number of points for core point classification"), Range(0,20)] protected int m_dbscanMinPoints = 10;

    [Header("=== Region Determination ===")]
    [SerializeField] private RegionAttributes m_grasslandsAttributes;
    [SerializeField] private RegionAttributes m_oakAttributes;
    [SerializeField] private RegionAttributes m_birchAttributes;
    [SerializeField] private RegionAttributes m_spruceAttributes;

    [Header("=== Outputs - READ ONLY ===")]
    [SerializeField, Tooltip("The voronoi segment centroids")]  protected Vector3[] m_centroids;
    [SerializeField] private List<DBScanCluster> m_clusters;
    public List<DBScanCluster> clusters => m_clusters;
    [SerializeField] private List<Region> m_regions;
    public List<Region> regions => m_regions;
    [Space]
    [SerializeField] private Region m_grasslandsRegion;
    [SerializeField] private Region m_oakRegion;
    [SerializeField] private Region m_birchRegion;
    [SerializeField] private Region m_spruceRegion;
    
    [Header("=== Noise Map Interactions ===")]
    [SerializeField, Tooltip("The world width of the resulting voronoi map. Used in gizmos rendering + for cluster querying when provided world positions")]    protected float m_worldWidth = 1f;
    [SerializeField, Tooltip("The world height of the resulting voronoi map. Used in gizmos rendering + for cluster querying when provided world positions")]   protected float m_worldHeight = 1f;
    [SerializeField, Tooltip("The height curve when converting from noise height to world height")] protected AnimationCurve m_heightCurve;
    [SerializeField, Tooltip("The height multiplier when converting from noise height to world height")]    protected float m_heightMultiplier = 1f;

    [Header("=== Inspector tools ===")]
    [SerializeField, Tooltip("Do we use a coroutine?")] private bool m_useCoroutine = false;
    [SerializeField, Tooltip("If using a coroutine, then how many for-loop iterations do we wait for each frame?")] private int m_coroutineNumThreshold = 20;
    [SerializeField, Tooltip("Should we generate on start?")]   private bool m_generateOnStart = true;
    [SerializeField, Tooltip("Should we auto-updated per change?")] protected bool m_autoUpdate = true;
    public bool autoUpdate => m_autoUpdate;

    [Header("=== On Completion ===")]
    private bool m_generated = false;
    public bool generated => m_generated;
    public UnityEvent onCompletion;

    private KDTree m_centroidTree;
    private int[] m_centroidToClusterMap;
    private Vector3[] m_clusterCentroids;
    private KDTree m_clusterCentroidTree;
    private Vector3[] m_clusterWorldCentroids;
    private KDTree m_clusterWorldCentroidTree;
    private Vector3[] m_regionCentroids;
    private KDTree m_regionCentroidsTree;

    private KDQuery m_clusterCentroidQuery;
    private System.Random m_prng;

    #if UNITY_EDITOR
    [Header("=== Debug Settings ===")]
    [SerializeField, Tooltip("Print debug?")] protected bool m_showDebug = false;
    [SerializeField, Tooltip("The sphere size for region centroids")] protected float m_gizmosCentroidSize = 2f;
    [SerializeField, Tooltip("The sphere size for region centroids")] protected float m_gizmosPointSize = 1f;
    protected virtual void OnDrawGizmos() {
        if (!m_showDebug) return;

        Gizmos.color = Color.black;
        Vector3 worldCenter = transform.position + new Vector3(m_worldWidth/2f,0f,m_worldHeight/2f);
        Vector3 world1 = transform.rotation * transform.position;
        Vector3 world2 = transform.rotation * (transform.position + new Vector3(m_worldWidth, 0f, 0f));
        Vector3 world3 = transform.rotation * (transform.position + new Vector3(0f, 0f, m_worldHeight));
        Vector3 world4 = transform.rotation * (transform.position + new Vector3(m_worldWidth, 0f, m_worldHeight));
        Gizmos.DrawLine(world1, world2);
        Gizmos.DrawLine(world1, world3);
        Gizmos.DrawLine(world2, world4);
        Gizmos.DrawLine(world3, world4);
        
        if (m_centroids == null || m_centroids.Length == 0 || m_clusters == null || m_clusters.Count == 0 || m_centroidToClusterMap == null || m_centroidToClusterMap.Length == 0) return;
        for(int i = 0; i < m_regions.Count; i++) {
            Region region = m_regions[i];
            Gizmos.color = region.attributes.color;            
            Gizmos.DrawSphere(transform.rotation * (transform.position + region.coreCluster.worldCentroid), m_gizmosCentroidSize);
            foreach(int pi in region.coreCluster.points) {
                Vector3 p = transform.rotation * (transform.position + new Vector3(m_centroids[pi].x * m_worldWidth, region.coreCluster.worldCentroid.y, m_centroids[pi].z * m_worldHeight));
                Gizmos.DrawSphere(p, m_gizmosPointSize);
            }
            foreach(DBScanCluster cluster in region.subClusters) {
                foreach(int pi in cluster.points) {
                    Vector3 p = transform.rotation * (transform.position + new Vector3(m_centroids[pi].x * m_worldWidth, cluster.worldCentroid.y, m_centroids[pi].z * m_worldHeight));
                    Gizmos.DrawSphere(p, m_gizmosPointSize);
                }
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

    public void SetScale(float width, float height) {
        m_worldWidth = width;
        m_worldHeight = height;
    }

    private void Awake() {
        current = this;
    }
    private void Start() {
        // If there is a terrain manager that is active in the world, then get its elements
        if (TerrainManager.current != null) {
            SetSeed(TerrainManager.current.seed);
            SetScale(TerrainManager.current.width, TerrainManager.current.height);
        }

        if (m_generateOnStart) {
            if (m_useCoroutine) StartCoroutine(GenerateCoroutine());
            else Generate();
        }
    }

    public void Generate() {
        m_prng = new System.Random(m_seed);         // Initialize the randomization
        m_clusterCentroidQuery = new KDQuery();     // This is a query class that interacts with any KDTrees we formulate.
        GenerateCentroids(m_numSegments);                   // Generate the initial list of centroids.
        if (m_numRelaxations > 0) {
            LloydRelaxation(m_centroids, m_numRelaxations); // We modify the centroid positions via Lloyd Relaxation.
        }
        m_centroidTree = new KDTree(m_centroids, 32);       // Generate a KD tree with all the centroids. At this point, the centroids will no longer be modified
        DBScanClusterRegions(m_centroids, m_dbscanEpsilon, m_dbscanMinPoints);  // Use DBScan to generate clusters, which can be interpreted as regions
        DetermineRegions(m_clusters);
        m_generated = true;
        onCompletion?.Invoke();
    }

    public IEnumerator GenerateCoroutine() {
        m_prng = new System.Random(m_seed);             // Initialize the randomization
        m_clusterCentroidQuery = new KDQuery();         // This is a query class that interacts with any KDTrees we formulate.
        yield return GenerateCentroidsCoroutine(m_numSegments);             // Generate the initial list of centroids.
        if (m_numRelaxations > 0) {
            yield return LloydRelaxationCoroutine(m_centroids, m_numRelaxations);    // We modify the centroid positions via Lloyd Relaxation.
        }
        m_centroidTree = new KDTree(m_centroids, 32);                       // Generate a KD tree with all the centroids. At this point, the centroids will no longer be modified
        DBScanClusterRegions(m_centroids, m_dbscanEpsilon, m_dbscanMinPoints);  // Use DBScan to generate clusters, which can be interpreted as regions
        yield return DetermineRegionsCoroutine(m_clusters);
        m_generated = true;
        onCompletion?.Invoke();
    }

    public void GenerateCentroids(int n) {
        List<Vector3> centers = new List<Vector3>();
        int horBound = (int)(100f*m_horizontalBuffer);
        int verBound = (int)(100f*m_verticalBuffer);
        while(centers.Count < n) {
            float x = (float)m_prng.Next(horBound,101-horBound)/100f;
            float z = (float)m_prng.Next(verBound,101-verBound)/100f;
            Vector3 c = new Vector3(x,0f,z);
            if (!centers.Contains(c)) centers.Add(c);
        }
        m_centroids = centers.ToArray();
    }
    public IEnumerator GenerateCentroidsCoroutine(int n) {
        List<Vector3> centers = new List<Vector3>();
        int horBound = (int)(100f*m_horizontalBuffer);
        int verBound = (int)(100f*m_verticalBuffer);
        int counter = 0;
        while(centers.Count < n) {
            float x = (float)m_prng.Next(horBound,101-horBound)/100f;
            float z = (float)m_prng.Next(verBound,101-verBound)/100f;
            Vector3 c = new Vector3(x,0f,z);
            if (!centers.Contains(c)) centers.Add(c);
            counter++;
            if ((float)counter % m_coroutineNumThreshold == 0) yield return null;
        }
        m_centroids = centers.ToArray();
        yield return null;
    }

    public void LloydRelaxation(Vector3[] centroids, int n, int fidelity = 100) {
        Vector3[] relaxedCentroids = centroids;
        for(int iter = 0; iter < n; iter++) {
            Vector3[] centroidTotals = new Vector3[centroids.Length];
            int[] centroidCounts = new int[centroids.Length];
            for(int y = 0; y < fidelity; y++) {
                for(int x = 0; x < fidelity; x++) {
                    Vector3 p = new Vector3(x,0f,y) / (float)fidelity;
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
                    centroidTotals[smallestIndex] += p;
                    centroidCounts[smallestIndex] += 1;
                }
            }
            for(int i = 0; i < centroids.Length; i++) centroidTotals[i] /= (float)centroidCounts[i];
            relaxedCentroids = centroidTotals;
        }
        m_centroids = relaxedCentroids;
    }
    public IEnumerator LloydRelaxationCoroutine(Vector3[] centroids, int n, int fidelity = 100) {
        Vector3[] relaxedCentroids = centroids;
        int counter = 0;
        for(int iter = 0; iter < n; iter++) {
            Vector3[] centroidTotals = new Vector3[centroids.Length];
            int[] centroidCounts = new int[centroids.Length];
            for(int y = 0; y < fidelity; y++) {
                for(int x = 0; x < fidelity; x++) {
                    Vector3 p = new Vector3(x,0f,y) / (float)fidelity;
                    float smallestDistance = float.MaxValue;
                    int smallestIndex = 0;
                    for(int i = 0; i < centroids.Length; i++) {
                        Vector3 c = relaxedCentroids[i];
                        float distance = Vector3.Distance(c,p);
                        if (distance < smallestDistance) {
                            smallestDistance = distance;
                            smallestIndex = i;
                        }
                        counter++;
                        if ((float)counter % m_coroutineNumThreshold == 0) yield return null;
                    }
                    centroidTotals[smallestIndex] += p;
                    centroidCounts[smallestIndex] += 1;
                }
            }
            for(int i = 0; i < centroids.Length; i++) centroidTotals[i] /= (float)centroidCounts[i];
            relaxedCentroids = centroidTotals;
        }
        m_centroids = relaxedCentroids;
        yield return null;
    }

    public void DBScanClusterRegions(Vector3[] centroids, float eps, int minPoints) {
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
        int[] pointClassification = new int[centroids.Length];
        List<int>[] pointNeighbors = new List<int>[centroids.Length];
        for(int i = 0; i < centroids.Length; i++) {
            // Get current point
            Vector3 p = centroids[i];
            pointNeighbors[i] = new List<int>();
            // Query all neighbors using KDTree
            m_clusterCentroidQuery.Radius(m_centroidTree, p, eps, pointNeighbors[i]);
            // Check if nNeighbors >= minPoints. If so, this is a core point.
            if (pointNeighbors[i].Count >= minPoints) {
                pointClassification[i] = 2;
                // For any neighbors, at least classify them as border point if they're still unclassified.
                foreach(int nI in pointNeighbors[i]) if (pointClassification[nI] == 0) pointClassification[nI] = 1;
            }
            else if (pointClassification[i] == 0) {
                // We can still classifify this point as a border point as long as this point is still unclassified (maybe it was classified as a neighbor point at some point in the past)
                foreach(int nI in pointNeighbors[i]) {
                    if (pointClassification[nI] == 2) {
                        pointClassification[i] = 1;
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
        int[] centroidToClusterMap = new int[centroids.Length];
        for(int i = 0; i < centroids.Length; i++) {
            if (pointClassification[i] != 2)    continue;   // Ignore any that are not core points
            if (centroidToClusterMap[i] != 0)   continue;   // Ignore if this point already is in a cluster
            
            // Create its own cluster
            DBScanCluster c = new DBScanCluster();
            clusters.Add(c);
            c.id = clusters.Count-1;
            c.color = new Color(UnityEngine.Random.Range(0f,1f), UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f));
            c.points.Add(i);
            centroidToClusterMap[i] = c.id;

            // Among all other points, check if they're a core point and can be density-connected to current point
            for(int j = i+1; j < centroids.Length; j++) {
                if (i == j) continue;
                if (pointClassification[j] != 2)    continue;   // Ignore if not core poijnt
                if (centroidToClusterMap[j] != 0)   continue;   // ignore if this is already classified
                HashSet<int> visitedPoints = new HashSet<int>();
                foreach(int nI in pointNeighbors[j]) {
                    if (pointClassification[nI] != 2) continue;
                    visitedPoints.Add(nI);
                }
                if (visitedPoints.Contains(i) && !c.points.Contains(j)) {
                    c.points.Add(j);
                    centroidToClusterMap[j] = c.id;
                }
            }
        }

        // Step 3: For each unclustered border point, assign it the cluster of the nearest core point
        for(int i = 0; i < centroids.Length; i++) {
            if (pointClassification[i] != 1) continue;  // Ignore if not border point
            if (centroidToClusterMap[i] != 0) continue; // Ignore if clustered already
            float smallestDistance = float.MaxValue;
            int smallestIndex = 0;
            for(int j = 0; j < centroids.Length; j++) {
                if (i == j) continue;
                if (pointClassification[i] != 2) continue;
                float distance = Vector3.Distance(centroids[i], centroids[j]);
                if (distance < smallestDistance) {
                    smallestDistance = distance;
                    smallestIndex = j;
                }
            }
            clusters[centroidToClusterMap[smallestIndex]].points.Add(i);
            centroidToClusterMap[i] = clusters[centroidToClusterMap[smallestIndex]].id;
        }

        // Step 4: sort the clusters by size.
        clusters.Sort();
        m_clusters = clusters;
       
        // Step 5: for each cluster, map their points to their cluster in `m_centroidToClusterMap`
        // We cannot use already-generated `centroidToClusterMap` due to sorting, which has messed up the mapping already
        // Simultaneously, we use this opportunity to estimate the centroid of the cluster itself.
        m_centroidToClusterMap = new int[centroids.Length];
        m_clusterCentroids = new Vector3[m_clusters.Count];
        m_clusterWorldCentroids = new Vector3[m_clusters.Count];
        for(int ci = 0; ci < m_clusters.Count; ci++) {
            DBScanCluster cluster = m_clusters[ci];
            cluster.noiseHeight = (float)ci/m_clusters.Count;
            cluster.centroid = Vector3.zero;
            foreach(int i in cluster.points) {
                m_centroidToClusterMap[i] = ci;
                cluster.centroid += new Vector3(centroids[i].x, 0f, centroids[i].z);
            }
            cluster.centroid /= cluster.points.Count;
            cluster.centroid.y = cluster.noiseHeight;
            cluster.worldCentroid = transform.position + new Vector3(cluster.centroid.x * m_worldWidth, m_heightCurve.Evaluate(cluster.noiseHeight) * m_heightMultiplier, cluster.centroid.z * m_worldHeight);
            m_clusterCentroids[ci] = cluster.centroid;
            m_clusterWorldCentroids[ci] = cluster.worldCentroid;
        }
        m_clusterCentroidTree = new KDTree(m_clusterCentroids, 32);
        m_clusterWorldCentroidTree = new KDTree(m_clusterWorldCentroids, 16);
    }

    public void DetermineRegions(List<DBScanCluster> clusters, int numMajorRegions=4, float edgeRatio=0.25f) {
        Vector2 normalizedCenter = Vector2.one * 0.5f;
        float maxDistance = 0.5f * Mathf.Sqrt(2f);
        List<Region> regions = new List<Region>();
        TerrainChunk.MinMax edgeRange = new TerrainChunk.MinMax { min = edgeRatio, max = 1f-edgeRatio };
        
        for(int i = 0; i < clusters.Count; i++) {
            DBScanCluster cluster = clusters[i];
            Vector3 clusterCentroid = cluster.worldCentroid;
            float normalizedX = Mathf.Clamp(clusterCentroid.x / m_worldWidth, 0f, 1f);
            float normalizedY = Mathf.Clamp(clusterCentroid.z / m_worldHeight, 0f, 1f);
            int distanceToCenter = Mathf.RoundToInt(Mathf.InverseLerp(maxDistance, 0f, Vector2.Distance(new Vector2(normalizedX, normalizedY), normalizedCenter)) * 10f);
            int size = Mathf.RoundToInt((float)cluster.points.Count / m_numSegments * 10f);
            Region region = new Region { coreCluster=cluster, majorRegionWeight=distanceToCenter*size };
            regions.Add(region);
        }
        regions.Sort();
        
        m_regionCentroids = new Vector3[4];
        // First region: grassland
        m_grasslandsRegion = regions[0];
        m_grasslandsRegion.attributes = m_grasslandsAttributes;
        m_grasslandsRegion.coreCluster.regionIndex = 0;
        m_regionCentroids[0] = m_grasslandsRegion.coreCluster.worldCentroid;
        // Second region: oak
        m_oakRegion = regions[1];
        m_oakRegion.attributes = m_oakAttributes;
        m_oakRegion.coreCluster.regionIndex = 1;
        m_regionCentroids[1] = m_oakRegion.coreCluster.worldCentroid;
        // Third region: birch
        m_birchRegion = regions[2];
        m_birchRegion.attributes = m_birchAttributes;
        m_birchRegion.coreCluster.regionIndex = 2;
        m_regionCentroids[2] = m_birchRegion.coreCluster.worldCentroid;
        // Fourth region: spruce
        m_spruceRegion = regions[3];
        m_spruceRegion.attributes = m_spruceAttributes;
        m_spruceRegion.coreCluster.regionIndex = 3;
        m_regionCentroids[3] = m_spruceRegion.coreCluster.worldCentroid;
        // Set `m_regions`
        m_regions = new List<Region>() { m_grasslandsRegion, m_oakRegion, m_birchRegion, m_spruceRegion };
        m_regionCentroidsTree = new KDTree(m_regionCentroids, 4);

        // Iterate through remaining regions. Associate non-major regions with major regions via KDTree KNearest
        for(int i = 4; i < regions.Count; i++) {
            // Cluster
            DBScanCluster cluster = regions[i].coreCluster;
            // Get nearest regions
            List<int> results = new List<int>();
            m_clusterCentroidQuery.ClosestPoint(m_regionCentroidsTree, cluster.worldCentroid, results);
            // First item is considered the closest
            cluster.regionIndex = results[0];
            m_regions[results[0]].subClusters.Add(cluster); 
        } 
    }
    public IEnumerator DetermineRegionsCoroutine(List<DBScanCluster> clusters, int numMajorRegions=4, float edgeRatio=0.25f) {
        Vector2 normalizedCenter = Vector2.one * 0.5f;
        float maxDistance = 0.5f * Mathf.Sqrt(2f);
        List<Region> regions = new List<Region>();
        TerrainChunk.MinMax edgeRange = new TerrainChunk.MinMax { min = edgeRatio, max = 1f-edgeRatio };
        int counter = 0;

        for(int i = 0; i < clusters.Count; i++) {
            DBScanCluster cluster = clusters[i];
            Vector3 clusterCentroid = cluster.worldCentroid;
            float normalizedX = Mathf.Clamp(clusterCentroid.x / m_worldWidth, 0f, 1f);
            float normalizedY = Mathf.Clamp(clusterCentroid.z / m_worldHeight, 0f, 1f);
            int distanceToCenter = Mathf.RoundToInt(Mathf.InverseLerp(maxDistance, 0f, Vector2.Distance(new Vector2(normalizedX, normalizedY), normalizedCenter)) * 10f);
            int size = Mathf.RoundToInt((float)cluster.points.Count / m_numSegments * 10f);
            Region region = new Region { coreCluster=cluster, majorRegionWeight=distanceToCenter*size };
            regions.Add(region);
            counter++;
            if ((float)counter % m_coroutineNumThreshold == 0) yield return null;
        }
        regions.Sort();
        yield return null;

        m_regionCentroids = new Vector3[4];
        // First region: grassland
        m_grasslandsRegion = regions[0];
        m_grasslandsRegion.attributes = m_grasslandsAttributes;
        m_grasslandsRegion.coreCluster.regionIndex = 0;
        m_regionCentroids[0] = m_grasslandsRegion.coreCluster.worldCentroid;
        // Second region: oak
        m_oakRegion = regions[1];
        m_oakRegion.attributes = m_oakAttributes;
        m_oakRegion.coreCluster.regionIndex = 1;
        m_regionCentroids[1] = m_oakRegion.coreCluster.worldCentroid;
        // Third region: birch
        m_birchRegion = regions[2];
        m_birchRegion.attributes = m_birchAttributes;
        m_birchRegion.coreCluster.regionIndex = 2;
        m_regionCentroids[2] = m_birchRegion.coreCluster.worldCentroid;
        // Fourth region: spruce
        m_spruceRegion = regions[3];
        m_spruceRegion.attributes = m_spruceAttributes;
        m_spruceRegion.coreCluster.regionIndex = 3;
        m_regionCentroids[3] = m_spruceRegion.coreCluster.worldCentroid;

        // Set `m_regions`
        m_regions = new List<Region>() { m_grasslandsRegion, m_oakRegion, m_birchRegion, m_spruceRegion };
        m_regionCentroidsTree = new KDTree(m_regionCentroids, 2);

        // Iterate through remaining regions. Associate non-major regions with major regions via KDTree KNearest
        counter = 0;
        List<int> results = new List<int>();
        for(int i = 4; i < regions.Count; i++) {
            // Cluster
            DBScanCluster cluster = regions[i].coreCluster;
            // Get nearest regions
            m_clusterCentroidQuery.ClosestPoint(m_regionCentroidsTree, cluster.worldCentroid, results);
            // First item is considered the closest
            cluster.regionIndex = results[0];
            m_regions[results[0]].subClusters.Add(cluster);
            // Coroutine logic
            counter++;
            if ((float)counter % m_coroutineNumThreshold == 0) yield return null;   
        } 

        // Yield return null
        yield return null;
    }

    public DBScanCluster QueryCluster(Vector3 query) {
        Vector3 flattened = new Vector3(query.x, 0f, query.z);
        List<int> results = new List<int>();
        m_clusterCentroidQuery.ClosestPoint(m_clusterWorldCentroidTree, flattened, results);
        return m_clusters[results[0]];
    }

    public Region QueryRegion(Vector3 query) {
        DBScanCluster cluster = QueryCluster(query);
        return m_regions[cluster.regionIndex];
    }

    public List<int> QueryKNearestClusters(Vector3 query, int k) {
        Vector3 flattened = new Vector3(query.x, 0f, query.z);
        List<int> results = new List<int>();
        m_clusterCentroidQuery.KNearest(m_clusterWorldCentroidTree, flattened, k, results);
        return results;
    }

}

[System.Serializable]
public class DBScanCluster : IComparable<DBScanCluster> {
    public int id = 0;
    public Color color = Color.black;
    public List<int> points = new List<int>();
    public List<int> gemPoints = new List<int>();
    public float noiseHeight;
    public Vector3 centroid;
    public Vector3 worldCentroid;
    public int regionIndex;
    public int CompareTo(DBScanCluster other) {		
        // A null value means that this object is greater. 
	    if (other == null) return 1;	
		return other.points.Count.CompareTo(this.points.Count);
	}
}

[System.Serializable]
public class Region : IComparable<Region> {
    public RegionAttributes attributes;
    public DBScanCluster coreCluster;
    public List<DBScanCluster> subClusters = new List<DBScanCluster>();
    [HideInInspector] public int majorRegionWeight;
    
    public int CompareTo(Region other) {		
        // A null value means that this object is greater. 
	    if (other == null) return 1;
		return other.majorRegionWeight.CompareTo(this.majorRegionWeight);
	}
}
