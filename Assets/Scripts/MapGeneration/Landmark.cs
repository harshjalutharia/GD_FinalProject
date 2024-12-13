using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class Landmark : MonoBehaviour
{
    [Header("=== References ===")]
    [SerializeField, Tooltip("Reference to this object's renderer")]    private Renderer[] m_renderers;
    public Renderer[] _renderers => m_renderers;
    [SerializeField, Tooltip("Reference to an audio source, if present.")]  private AudioSource m_audioSource;
    [SerializeField, Tooltip("Should camera parent ref")]   private Transform m_shoulderCameraParent;
    [SerializeField, Tooltip("Reference to the Cinemachine camera attached to this landmark's shoulder, if present")]  private CinemachineVirtualCamera m_shoulderCamera;
    [SerializeField, Tooltip("Destination of the destination gem trail")]   private Transform m_pathingDestination;
    public Transform pathingDestination => m_pathingDestination;
    [SerializeField, Tooltip("Forward point of the destination gem trail")] private Transform m_pathingForward;
    public Transform pathingForward => m_pathingForward;
    public int regionIndex = 0;
    public List<LandmarkGenerator.PathBetweenLandmarks> m_pathsToOtherLandmarks = new List<LandmarkGenerator.PathBetweenLandmarks>();

    [Header("=== Outputs - READ ONLY ===")]
    [SerializeField, Tooltip("The bounds of this landmark")]    private Bounds m_bounds;
    public Bounds bounds => m_bounds;

    private List<Vector3> m_toDrawGizmos = new List<Vector3>();

    #if UNITY_EDITOR
    void OnDrawGizmos() {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(m_bounds.center, m_bounds.extents*2f);

        if (m_renderers == null || m_renderers.Length == 0) return;
        Gizmos.color = Color.blue;
        foreach(Renderer r in m_renderers) {
            Gizmos.DrawWireCube(r.bounds.center, r.bounds.extents*2f);
        }
        Gizmos.color = Color.yellow;
        foreach(Vector3 p in m_toDrawGizmos) {
            Gizmos.DrawSphere(p, 0.25f);
        }
    }
    #endif

    private void Start() {
        // Find all renderers associated with this object
        m_renderers = GetComponentsInChildren<Renderer>();
        // Offset Landmark
        OffsetLandmarkInYaxis();
    }

    public void CalculateBounds() {
        // Find all renderers associated with this object
        m_renderers = GetComponentsInChildren<Renderer>();
        // Initialize a new bounds that'll act as the composite bounds of this object
        m_bounds = new Bounds (transform.position, Vector3.one);
	    foreach (Renderer renderer in m_renderers) m_bounds.Encapsulate (renderer.bounds);
    }

    // offsets the landmark's position in the y-axis to fix it floating above ground
    public void OffsetLandmarkInYaxis() {
        if (TerrainManager.current == null) return;
        //if (SessionManager.current == null || SessionManager.current.terrainGenerator == null) return;
        if (m_renderers == null || m_renderers.Length == 0) return;

        foreach(Renderer r in m_renderers) {
            Vector3[] bottomPoints = new Vector3[4];

            // We need to translate from local space bounds to world space bounds.
            // This is to handle the situation of rotations and the like.
            // If this renderer has a custom bounds component attached, just get it from here
            CustomBounds cb = r.gameObject.GetComponent<CustomBounds>();
            if (cb == null) continue;
            
            bottomPoints[0] = transform.TransformPoint(cb.boundPoints[0]);
            bottomPoints[1] = transform.TransformPoint(cb.boundPoints[1]);
            bottomPoints[2] = transform.TransformPoint(cb.boundPoints[2]);
            bottomPoints[3] = transform.TransformPoint(cb.boundPoints[3]);
            m_toDrawGizmos.AddRange(bottomPoints);

            float finalOffset = float.MaxValue;
            for (int i = 0; i < bottomPoints.Length; i++) {
                // Get the terrain height at the bound point
                TerrainManager.current.TryGetPointOnTerrain(bottomPoints[i], out var point, out var normal,
                    out var steepness);
                float terrainHeight = point.y;//SessionManager.current.terrainGenerator.QueryHeightAtWorldPos(bottomPoints[i].x, bottomPoints[i].z, out var x, out var y);
                // We need to check if we want to move downward or upward. This is because
                // sometimes, a bound can be either completely hanging above the terrain...
                // ... maybe in between the terrain...
                // ... or maybe underneath the terrain
                // We confirm if we want to move down if ANY of the bound points are above the terrain.
                float offset = terrainHeight - bottomPoints[i].y;
                if (offset < finalOffset) finalOffset = offset;
            }
            r.transform.position += Vector3.up * finalOffset;

        }
        CalculateBounds();
    }

    public Vector3[] CalculateLowestBoundPoints(Renderer r) {
        Bounds localBounds = r.localBounds;
        Vector3[] bottomPoints = new Vector3[4];

        // We have to first 
        return bottomPoints;
    }

    public void PlayAudioSource() {
        if (m_audioSource == null) return;
        m_audioSource.Play();
    }

    public void ToggleShoulderCamera(bool setTo) {
        if (m_shoulderCamera == null) return;
        // Firstly, detect which is the optimal direction to rotate to. Only possible of Voronoi is not null
        //if (Voronoi.current != null) {
        if (TerrainManager.current != null) {
            Region region = Voronoi.current.regions[this.regionIndex];
            Vector3 direction = Vector3.zero;
            foreach(Gem gem in region.smallGems) {
                direction += (gem.transform.position - this.transform.position).normalized;
            }
            Vector3 averageDir = direction / region.smallGems.Count;
            //Vector3 averageDir = (TerrainManager.current.worldCenter - transform.position).normalized;
            Vector3 targetPostition = m_shoulderCameraParent.position + new Vector3(averageDir.x, 0f, averageDir.z);
            m_shoulderCameraParent.LookAt(targetPostition);
        }
        m_shoulderCamera.m_Priority = (setTo) ? 10 : 0;
    }
}


