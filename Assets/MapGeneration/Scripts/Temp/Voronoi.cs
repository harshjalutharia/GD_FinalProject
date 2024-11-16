using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
    [SerializeField, Tooltip("Noise maps to place the tessellation on top of")] protected NoiseMap[] m_noiseMaps;
    private Dictionary<NoiseMap, int[]> m_noiseMapDict = new Dictionary<NoiseMap, int[]>(); 

    [Header("=== Inspector tools ===")]
    [SerializeField, Tooltip("Should we auto-updated per change?")] protected bool m_autoUpdate = true;
    public bool autoUpdate => m_autoUpdate;

    [Header("=== Outputs - READ ONLY ===")]
    [SerializeField, Tooltip("The voronoi segment centroids")]  protected Vector2[] m_centroids;
    private System.Random m_prng;

    private int[] m_pointClass;
    [SerializeField] private List<DBScanCluster> m_clusters;
    private int[] m_centroidToClusterMap;

    #if UNITY_EDITOR
    [Header("=== Debug Settings ===")]
    [SerializeField, Tooltip("Print debug?")] protected bool m_showDebug = false;
    [SerializeField, Tooltip("The gizmos width and height")] protected Vector2 m_gizmosDimensions = new Vector2(100f,100f);
    
    protected virtual void OnDrawGizmos() {
        if (!m_showDebug) return;

        Gizmos.color = Color.white;
        Vector3 worldCenter = transform.position + new Vector3(m_gizmosDimensions.x/2f,0f,m_gizmosDimensions.y/2f);
        Gizmos.DrawWireCube(worldCenter, new Vector3(m_gizmosDimensions.x, 0f, m_gizmosDimensions.y));
        
        if (m_centroids == null || m_centroids.Length == 0 || m_clusters == null || m_clusters.Count == 0 || m_centroidToClusterMap == null || m_centroidToClusterMap.Length == 0) return;
        for(int i = 0; i < m_centroids.Length; i++) {
            Gizmos.color = m_clusters[m_centroidToClusterMap[i]].color;
            Vector3 p = transform.position + new Vector3(m_centroids[i].x * m_gizmosDimensions.x, 0f, m_centroids[i].y * m_gizmosDimensions.y);
            Gizmos.DrawSphere(p, 1f);
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

    public virtual void GenerateTessellation() {
        m_prng = new System.Random(m_seed);
        m_centroids = GenerateCentroids(m_numSegments);
        if (m_numRelaxations > 0) m_centroids = LloydRelaxation(m_centroids, m_numRelaxations);
        
        // Clustering
        DBScanClusterRegions(m_centroids, m_dbscanEpsilon, m_dbscanMinPoints);
    }

    public virtual Vector2[] GenerateCentroids(int n) {
        List<Vector2> centers = new List<Vector2>();
        int horBound = (int)(100f*m_horizontalBuffer);
        int verBound = (int)(100f*m_verticalBuffer);
        while(centers.Count < n) {
            float x = (float)m_prng.Next(horBound,101-horBound)/100f;
            float y = (float)m_prng.Next(verBound,101-verBound)/100f;
            Vector2 c = new Vector2(x,y);
            if (!centers.Contains(c)) centers.Add(c);
        }
        return centers.ToArray();
    }

    public virtual Vector2[] LloydRelaxation(Vector2[] centroids, int n, int fidelity = 100) {
        Vector2[] relaxedCentroids = centroids;
        for(int iter = 0; iter < n; iter++) {
            Vector2[] centroidTotals = new Vector2[centroids.Length];
            int[] centroidCounts = new int[centroids.Length];
            for(int y = 0; y < fidelity; y++) {
                for(int x = 0; x < fidelity; x++) {
                    Vector2 p = new Vector2(x,y) / (float)fidelity;
                    float smallestDistance = float.MaxValue;
                    int smallestIndex = 0;
                    for(int i = 0; i < centroids.Length; i++) {
                        Vector2 c = relaxedCentroids[i];
                        float distance = Vector2.Distance(c,p);
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

    public virtual void DBScanClusterRegions(Vector2[] centroids, float eps, int minPoints) {
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
            Vector2 p = centroids[i];
            pointNeighbors[i] = new List<int>();
            // Iterate through other centroids
            for(int j = 0; j < centroids.Length; j++) {
                // Ignore self
                if (i == j) continue;
                // Check the distance between p and this centroid
                float d = Vector2.Distance(p, centroids[j]);
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
        clusters.Add(new DBScanCluster());
        int[] centroidToClusterMap = new int[centroids.Length];
        for(int i = 0; i < centroids.Length; i++) {
            // Ignore any that are not core points
            if (pointClassification[i] != 2) continue;
            // Ignore if this point already is in a cluster
            if (centroidToClusterMap[i] != 0) continue;
            // Create its own cluster
            DBScanCluster c = new DBScanCluster();
            clusters.Add(c);
            c.id = clusters.Count-1;
            c.points.Add(i);
            c.color = new Color(Random.Range(0f,1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
            // Add ref to this cluster via mapper
            centroidToClusterMap[i] = c.id;
            // Among all other points, check if they're a core point and can be density-connected to current point
            for(int j = 0; j < centroids.Length; j++) {
                if (i == j) continue;
                if (pointClassification[j] != 2) continue;  // Ignore if not core poijnt
                if (centroidToClusterMap[j] != 0) continue; // ignore if this is already classified
                HashSet<int> visitedPoints = new HashSet<int>();
                foreach(int nI in pointNeighbors[j]) {
                    if (pointClassification[nI] != 2) continue;
                    visitedPoints.Add(nI);
                }
                if (visitedPoints.Contains(i)) {
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
                float distance = Vector2.Distance(centroids[i], centroids[j]);
                if (distance < smallestDistance) {
                    smallestDistance = distance;
                    smallestIndex = j;
                }
            }
            clusters[centroidToClusterMap[smallestIndex]].points.Add(i);
            centroidToClusterMap[i] = clusters[centroidToClusterMap[smallestIndex]].id;
        }

        // DEBUG
        m_clusters = clusters;
        m_centroidToClusterMap = centroidToClusterMap;

    }

    [System.Serializable]
    public class DBScanCluster {
        public int id = 0;
        public Color color = Color.black;
        public List<int> points = new List<int>();
    }
}
