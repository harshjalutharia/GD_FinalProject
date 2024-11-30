using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class Landmark : MonoBehaviour
{
    [Header("=== References ===")]
    [SerializeField, Tooltip("The camera controller that manages first-person camera-ing")] private CameraController m_cameraController;
    [SerializeField, Tooltip("Reference to this object's renderer")]    private Renderer[] m_renderers;
    public Renderer[] _renderers => m_renderers;

    [Header("=== Map Settings ===")]
    [SerializeField, Tooltip("The total amount of time required for this to track as 'detected'")]  private float m_totalTimeToDetect = 5f;

    [Header("=== Outputs - READ ONLY ===")]
    [SerializeField, Tooltip("The bounds of this landmark")]    private Bounds m_bounds;
    public Bounds bounds => m_bounds;
    [SerializeField, Tooltip("Am I in the camera fustrum?")]    private bool m_inFustrum = false;
    public bool inFustrum => m_inFustrum;
    [SerializeField, Tooltip("The distance between the landmark fustrum camera's center and the viewport projection of the bounds center onto this camera")]    private float m_distToCamCenter;
    public float distToCamCenter => m_distToCamCenter;

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

    private void OnEnable() {
        CalculateBounds();
    }

    private void LateUpdate() {
        // Don't do a fustrum check if we aren't evne pressed down on the map input
        bool mapInputActive = CameraController.current.firstPersonCameraActive;
        if (!mapInputActive) {
            m_inFustrum = false;
            m_distToCamCenter = -1f;
            return;
        }

        // Check if the renderer bounds is visible in the view fustrum
        bool visible = FustrumManager.current.landmarkFustrumCamera.CheckInFustrum(m_bounds);
        if (!visible) {
            m_inFustrum = false;
            m_distToCamCenter = -1f;
            return;
        }

        // What's the closest point on bounds to this fustrum camera?
        Vector3 closestPointOnBounds = m_bounds.ClosestPoint(FustrumManager.current.landmarkFustrumCamera.transform.position);

        // Calculate the distance from the camera's center to the closets point on the bounds
        Vector3 viewportPoint = FustrumManager.current.landmarkFustrumCamera.camera.WorldToViewportPoint(closestPointOnBounds);
        m_distToCamCenter = Vector3.Distance(new Vector3(0.5f, 0.5f, 0f), new Vector3(viewportPoint.x, viewportPoint.y, 0f));

        // Let us record that we're in the fustrum
        m_inFustrum = true;

        Debug.Log($"{gameObject.name}: Inside Range | {m_distToCamCenter}");
    }

    public void CalculateBounds() {
        // Find all renderers associated with this object
        m_renderers = GetComponentsInChildren<Renderer>();

        // Initialize a new bounds that'll act as the composite bounds of this object
        m_bounds = new Bounds (transform.position, Vector3.one);
	    foreach (Renderer renderer in m_renderers) m_bounds.Encapsulate (renderer.bounds);
    }

    // offsets the landmark's position in the y-axis to fix it floating above ground
    public void OffsetLandmarkInYaxis(NoiseMap terrainMap)
    {   
        if (terrainMap == null) return;
        if (m_renderers == null || m_renderers.Length == 0) return;

        foreach(Renderer r in m_renderers) {
            Vector3[] bottomPoints = new Vector3[4];

            // We need to translate from local space bounds to world space bounds.
            // This is to handle the situation of rotations and the like.
            // If this renderer has a custom bounds component attached, just get it from here
            CustomBounds cb = r.gameObject.GetComponent<CustomBounds>();
            if (cb != null) {
                Debug.Log($"{r.gameObject.name} using CustomBounds");
                bottomPoints[0] = transform.TransformPoint(cb.boundPoints[0]);
                bottomPoints[1] = transform.TransformPoint(cb.boundPoints[1]);
                bottomPoints[2] = transform.TransformPoint(cb.boundPoints[2]);
                bottomPoints[3] = transform.TransformPoint(cb.boundPoints[3]);
            } else {
                Bounds localBounds = r.localBounds;
                // Because some local bounds may actually be misaligned due to the fact that the models themselves are sideways...
                // .. we have to calculate the min/max x, min/max z, and min y
                
                //{ r.bounds.center, r.bounds.center, r.bounds.center, r.bounds.center };
                bottomPoints[0] = r.transform.TransformPoint(-localBounds.extents);
                bottomPoints[1] = r.transform.TransformPoint(new Vector3(localBounds.extents.x, -localBounds.extents.y, localBounds.extents.z));
                bottomPoints[2] = r.transform.TransformPoint(new Vector3(-localBounds.extents.x, -localBounds.extents.y, localBounds.extents.z));
                bottomPoints[3] = r.transform.TransformPoint(new Vector3(localBounds.extents.x, -localBounds.extents.y, -localBounds.extents.z));
            }
            m_toDrawGizmos.AddRange(bottomPoints);

            float finalOffset = float.MaxValue;
            bool moveDown = false;
            for (int i = 0; i < bottomPoints.Length; i++) {
                // Get the terrain height at the bound point
                float terrainHeight = terrainMap.QueryHeightAtWorldPos(bottomPoints[i].x, bottomPoints[i].z, out var x, out var y);
                // We need to check if we want to move downward or upward. This is because
                // sometimes, a bound can be either completely hanging above the terrain...
                // ... maybe in between the terrain...
                // ... or maybe underneath the terrain
                // We confirm if we want to move down if ANY of the bound points are above the terrain.
                if (terrainHeight <= bottomPoints[i].y) moveDown = true;
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
}


