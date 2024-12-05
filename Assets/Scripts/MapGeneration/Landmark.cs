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
    [SerializeField, Tooltip("The color we want to draw for this landmark on the held map")]  private Color m_mapColor = Color.white;

    [Header("=== Outputs - READ ONLY ===")]
    [SerializeField, Tooltip("The bounds of this landmark")]    private Bounds m_bounds;
    public Bounds bounds => m_bounds;
    [SerializeField, Tooltip("Am I in the camera fustrum?")]    private bool m_inFustrum = false;
    public bool inFustrum => m_inFustrum;
    [SerializeField, Tooltip("Have I been drawn on the map?")]  private bool m_drawnOnMap = false;
    public bool drawnOnMap => m_drawnOnMap;
    [SerializeField, Tooltip("The viewport position that the closest bound point is to the first person camera")]   private Vector2 m_viewportPoint = Vector2.zero;
    public Vector2 viewportPoint => m_viewportPoint;
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
        OffsetLandmarkInYaxis();
    }

    private void LateUpdate() {
        if (!CameraController.current.enabled) return;

        // Don't do a fustrum check if we aren't evne pressed down on the map input
        bool mapInputActive = CameraController.current.firstPersonCameraActive;
        if (!mapInputActive) {
            m_inFustrum = false;
            m_distToCamCenter = -1f;
            m_viewportPoint = Vector2.zero;
            return;
        }

        // Rather than chekc the fustrum, we will be doing a different test today. In this case, we will be attempting to check 
        // if this landmark's bounds are within the right side of the camera.
        // To do this, we get the min and max points of the bounds and do a comparison check.
        // The bounds is considered inside if either the min is greater than (0.5,0) and smaller than (1,1), or similar for max.
        Vector3 min = m_bounds.min;
        Vector3 max = m_bounds.max;

        // Get the screen points of each
        Vector3 minScreen = CameraController.current.firstPersonCamera.WorldToViewportPoint(min);
        Vector3 maxScreen = CameraController.current.firstPersonCamera.WorldToViewportPoint(max);
        
        // We need to convert the x-coords for both the min and max points to fit the range between (0.5f,1f), to check for right-sidedness
        // Note that for viewport, the bottom-left is (0,0) and the top-right is (1,1)
        Vector2 minScreenNormalized = new Vector2((minScreen.x - 0.5f)/0.5f, minScreen.y);
        Vector2 maxScreenNormalized = new Vector2((maxScreen.x - 0.5f)/0.5f, maxScreen.y);

        // At this point: 1) if any values are < 0, then they're outside on the lower bound. Likewise, if values are > 1, then they are outside on the upper bound
        if ( !(minScreenNormalized.x > 0f && minScreenNormalized.x < 1f && minScreenNormalized.y > 0f && maxScreenNormalized.y < 1f) && !(maxScreenNormalized.x > 0f && maxScreenNormalized.x < 1f && maxScreenNormalized.y > 0f && maxScreenNormalized.y < 1f) ) {
            // We are not in the view fustrum. Cancel out early
            m_inFustrum = false;
            m_viewportPoint = Vector2.zero;
            m_distToCamCenter = -1f;
            return;
        }

        m_inFustrum = true;

        // If we haven't been drawn yet, we gotta
        if (!m_drawnOnMap && m_mapColor.a > 0f) {
            SessionManager.current.terrainGenerator.DrawBoxOnHeldMap(m_bounds.center.x, m_bounds.center.z, m_bounds.min.x, m_bounds.min.z, m_bounds.max.x, m_bounds.max.z, Color.black);
            SessionManager.current.terrainGenerator.DrawBoxOnHeldMap(m_bounds.center.x, m_bounds.center.z, m_bounds.min.x+1, m_bounds.min.z+1, m_bounds.max.x-1, m_bounds.max.z-1, m_mapColor);
            m_drawnOnMap = true;
        }

        // What's the closest point on bounds to the first-person camera and its distance?
        Vector3 closestPointOnBounds = m_bounds.ClosestPoint(CameraController.current.firstPersonCamera.transform.position);
        Vector3 closestViewportPoint = CameraController.current.firstPersonCamera.WorldToViewportPoint(closestPointOnBounds);
        m_viewportPoint = new Vector3(Mathf.Clamp(closestViewportPoint.x, 0f, 1f), Mathf.Clamp(closestViewportPoint.y, 0f, 1f));
        m_distToCamCenter = Mathf.Abs(0.65f - viewportPoint.x);
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
        if (SessionManager.current == null || SessionManager.current.terrainGenerator == null) return;
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
            bool moveDown = false;
            for (int i = 0; i < bottomPoints.Length; i++) {
                // Get the terrain height at the bound point
                float terrainHeight = SessionManager.current.terrainGenerator.QueryHeightAtWorldPos(bottomPoints[i].x, bottomPoints[i].z, out var x, out var y);
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


