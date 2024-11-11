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
    
    [Header("=== Noise Map Interactions ===")]
    [SerializeField, Tooltip("Noise maps to place the tessellation on top of")] protected NoiseMap[] m_noiseMaps;
    private Dictionary<NoiseMap, int[]> m_noiseMapDict = new Dictionary<NoiseMap, int[]>(); 

    [Header("=== Inspector tools ===")]
    [SerializeField, Tooltip("Should we auto-updated per change?")] protected bool m_autoUpdate = true;
    public bool autoUpdate => m_autoUpdate;

    [Header("=== Outputs - READ ONLY ===")]
    [SerializeField, Tooltip("The voronoi segment centroids")]  protected Vector2[] m_centroids;
    private System.Random m_prng;

    #if UNITY_EDITOR
    [Header("=== Debug Settings ===")]
    [SerializeField, Tooltip("Print debug?")] protected bool m_showDebug = false;
    [SerializeField, Tooltip("The gizmos width and height")] protected Vector2 m_gizmosDimensions = new Vector2(100f,100f);
    
    protected virtual void OnDrawGizmos() {
        if (!m_showDebug) return;

        Gizmos.color = Color.white;
        Vector3 worldCenter = transform.position + new Vector3(m_gizmosDimensions.x/2f,0f,m_gizmosDimensions.y/2f);
        Gizmos.DrawWireCube(worldCenter, new Vector3(m_gizmosDimensions.x, 0f, m_gizmosDimensions.y));
        
        if (m_centroids == null || m_centroids.Length == 0) return;
        Gizmos.color = Color.blue;
        for(int i = 0; i < m_centroids.Length; i++) {
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
}
