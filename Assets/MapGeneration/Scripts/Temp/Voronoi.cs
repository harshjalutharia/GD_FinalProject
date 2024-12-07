using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DataStructures.ViliWonka.KDTree;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class Voronoi : MonoBehaviour
{

    [Header("=== Voronoi Tessellation Settings ===")]
    [SerializeField, Tooltip("The seed used for pseudo random generation")] protected int m_seed;
    [SerializeField, Tooltip("How many segments do we want?")]      protected int m_numSegments = 50;
    [SerializeField, Tooltip("Buffer ratio from horizontal edges"), Range(0f,0.5f)] protected float m_horizontalBuffer = 0.1f;
    [SerializeField, Tooltip("Buffer ratio from vertical edges"), Range(0f, 0.5f)]  protected float m_verticalBuffer = 0.1f;
    
    [Header("=== Lloyd Relaxation Settings ===")]
    [SerializeField, Tooltip("How many iterations of Lloyd Relaxation should we perform?")] protected int m_numRelaxations = 2;

    [Header("=== DBScan Clustering Settings ===")]
    [SerializeField, Tooltip("The epsilon distance required for DBScan"), Range(0f,1f)] protected float m_dbscanEpsilon = 0.1f;
    [SerializeField, Tooltip("The minimum number of points for core point classification"), Range(0,20)] protected int m_dbscanMinPoints = 10;
    
    [Header("=== Noise Map Interactions ===")]
    [SerializeField, Tooltip("The world width of the resulting voronoi map. Used in gizmos rendering + for cluster querying when provided world positions")]    protected float m_worldWidth = 1f;
    [SerializeField, Tooltip("The world height of the resulting voronoi map. Used in gizmos rendering + for cluster querying when provided world positions")]   protected float m_worldHeight = 1f;
    [SerializeField, Tooltip("The height curve when converting from noise height to world height")] protected AnimationCurve m_heightCurve;
    [SerializeField, Tooltip("The height multiplier when converting from noise height to world height")]    protected float m_heightMultiplier = 1f;

    [Header("=== Inspector tools ===")]
    [SerializeField, Tooltip("Should we auto-updated per change?")] protected bool m_autoUpdate = true;
    public bool autoUpdate => m_autoUpdate;

    [Header("=== Outputs - READ ONLY ===")]
    [SerializeField, Tooltip("The voronoi segment centroids")]  protected Vector3[] m_centroids;
    [SerializeField] private List<DBScanCluster> m_clusters;
    
    private int[] m_pointClass;
    private int[] m_centroidToClusterMap;
    private KDTree m_centroidTree;
    private Vector3[] m_clusterCentroids;
    private KDTree m_clusterCentroidTree;
    private Vector3[] m_clusterWorldCentroids;
    private KDTree m_clusterWorldCentroidTree;

    private KDQuery m_clusterCentroidQuery;
    private System.Random m_prng;

    #if UNITY_EDITOR
    [Header("=== Debug Settings ===")]
    [SerializeField, Tooltip("Print debug?")] protected bool m_showDebug = false;
    [SerializeField, Tooltip("The sphere size for region centroids")] protected float m_gizmosCentroidSize = 2f;
    [SerializeField, Tooltip("The sphere size for region centroids")] protected float m_gizmosPointSize = 1f;
    protected virtual void OnDrawGizmos() {
        if (!m_showDebug) return;

        Gizmos.color = Color.white;
        Vector3 worldCenter = transform.position + new Vector3(m_worldWidth/2f,0f,m_worldHeight/2f);
        Gizmos.DrawWireCube(worldCenter, new Vector3(m_worldWidth, 0f, m_worldHeight));
        
        if (m_centroids == null || m_centroids.Length == 0 || m_clusters == null || m_clusters.Count == 0 || m_centroidToClusterMap == null || m_centroidToClusterMap.Length == 0) return;
        for(int i = 0; i < m_clusters.Count; i++) {
            DBScanCluster cluster = m_clusters[i];
            Gizmos.color = cluster.color;
            Gizmos.DrawSphere(cluster.worldCentroid, m_gizmosCentroidSize);

            foreach(int pi in cluster.points) {
                Vector3 p = transform.position + new Vector3(m_centroids[pi].x * m_worldWidth, cluster.worldCentroid.y, m_centroids[pi].z * m_worldHeight);
                Gizmos.DrawSphere(p, m_gizmosPointSize);
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

    public virtual void GenerateTessellation() {
        m_prng = new System.Random(m_seed);
        m_clusterCentroidQuery = new KDQuery();

        m_centroids = GenerateCentroids(m_numSegments);
        if (m_numRelaxations > 0) m_centroids = LloydRelaxation(m_centroids, m_numRelaxations);
        m_centroidTree = new KDTree(m_centroids, 32);

        DBScanClusterRegions(m_centroids, m_dbscanEpsilon, m_dbscanMinPoints);
    }

    public virtual Vector3[] GenerateCentroids(int n) {
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

    public virtual Vector3[] LloydRelaxation(Vector3[] centroids, int n, int fidelity = 100) {
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
        return relaxedCentroids;
    }

    public virtual void DBScanClusterRegions(Vector3[] centroids, float eps, int minPoints) {
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
            // Iterate through other centroids
            for(int j = 0; j < centroids.Length; j++) {
                // Ignore self
                if (i == j) continue;
                // Check the distance between p and this centroid
                float d = Vector3.Distance(p, centroids[j]);
                // If the distance is no more than epsilon, we consider this a neighbor
                if (d <= eps) pointNeighbors[i].Add(j);
            }
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
        // DEBUG
        m_pointClass = pointClassification;

        // Step 2: Among all unclustered core points, 1) create a new cluster, and 2) add all points that are unclustered and density-connected to the current point
        List<DBScanCluster> clusters = new List<DBScanCluster>();
        // First cluster contains all noise points
        DBScanCluster initialCluster = new DBScanCluster();
        initialCluster.id = 0;
        clusters.Add(new DBScanCluster());
        int[] centroidToClusterMap = new int[centroids.Length];
        for(int i = 0; i < centroids.Length; i++) {
            // Ignore any that are not core points
            if (pointClassification[i] != 2)    continue;
            // Ignore if this point already is in a cluster
            if (centroidToClusterMap[i] != 0)   continue;
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
                if (pointClassification[j] != 2)    continue;  // Ignore if not core poijnt
                if (centroidToClusterMap[j] != 0)   continue; // ignore if this is already classified
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
        float maxPointCountRatio = (float)clusters[0].points.Count/centroids.Length;
        float minPointCountRatio = (float)clusters[clusters.Count-1].points.Count/centroids.Length;
        for(int ci = 0; ci < m_clusters.Count; ci++) {
            DBScanCluster cluster = m_clusters[ci];
            //cluster.noiseHeight = 1f-Mathf.InverseLerp(minPointCountRatio, maxPointCountRatio, (float)cluster.points.Count/centroids.Length);
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

    public DBScanCluster QueryCluster(Vector3 query) {
        Vector3 flattened = new Vector3(query.x, 0f, query.z);
        List<int> results = new List<int>();
        m_clusterCentroidQuery.ClosestPoint(m_clusterWorldCentroidTree, flattened, results);
        return m_clusters[results[0]];
    }
    public float QueryHeightFromCluster(Vector3 query) {
        Vector3 normalized = new Vector3(Mathf.Clamp(query.x/m_worldWidth, 0f, 1f), 0f, Mathf.Clamp(query.z/m_worldHeight, 0f, 1f));
        List<int> results = new List<int>();
        m_clusterCentroidQuery.ClosestPoint(m_clusterCentroidTree, normalized, results);
        return  m_clusters[results[0]].worldCentroid.y;
    }
    public float QueryHeightFromKNearest(Vector3 query, int k=10) {
        Vector3 normalized = new Vector3(Mathf.Clamp(query.x/m_worldWidth, 0f, 1f), 0f, Mathf.Clamp(query.z/m_worldHeight, 0f, 1f));
        List<int> results = new List<int>();
        m_clusterCentroidQuery.KNearest(m_centroidTree, normalized, k, results);

        // Get the height based on the distribution of clusters derived fron the centroids
        Dictionary<DBScanCluster, int> detectedClusters = new Dictionary<DBScanCluster, int>();
        foreach(int i in results) {
            DBScanCluster cluster = m_clusters[m_centroidToClusterMap[i]];
            if (!detectedClusters.ContainsKey(cluster)) detectedClusters.Add(cluster, 0);
            detectedClusters[cluster] += 1; 
        }
        float height = 0f;
        foreach(KeyValuePair<DBScanCluster, int> kvp in detectedClusters) height += kvp.Key.worldCentroid.y * (float)kvp.Value/k;
        return height;
    }
    public float QueryHeightFromRadius(Vector3 query, float radius=10f) {
        Vector3 normalized = new Vector3(Mathf.Clamp(query.x/m_worldWidth, 0f, 1f), 0f, Mathf.Clamp(query.z/m_worldHeight, 0f, 1f));
        List<int> results = new List<int>();
        m_clusterCentroidQuery.Radius(m_centroidTree, normalized, radius, results);

        // Get the height based on the distribution of clusters derived fron the centroids
        Dictionary<DBScanCluster, int> detectedClusters = new Dictionary<DBScanCluster, int>();
        int totalPoints = 0;
        foreach(int i in results) {
            DBScanCluster cluster = m_clusters[m_centroidToClusterMap[i]];
            if (!detectedClusters.ContainsKey(cluster)) detectedClusters.Add(cluster, 0);
            detectedClusters[cluster] += 1; 
            totalPoints += 1;
        }
        float height = 0f;
        if (totalPoints == 0) totalPoints = 100;
        foreach(KeyValuePair<DBScanCluster, int> kvp in detectedClusters) height += kvp.Key.worldCentroid.y * (float)kvp.Value/totalPoints;
        return height;
    }

    [System.Serializable]
    public class DBScanCluster : IComparable<DBScanCluster> {
        public int id = 0;
        public Color color = Color.black;
        public List<int> points = new List<int>();
        public float noiseHeight;
        public Vector3 centroid;
        public Vector3 worldCentroid;
        public int CompareTo(DBScanCluster other) {		
            // A null value means that this object is greater. 
		    if (other == null) return 1;	
			return other.points.Count.CompareTo(this.points.Count);
	    }
    }
}
