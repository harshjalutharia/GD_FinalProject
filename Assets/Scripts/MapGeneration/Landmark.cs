using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Landmark : MonoBehaviour
{
    [Header("=== References ===")]
    [SerializeField, Tooltip("Reference to this object's renderer")]    private Renderer m_renderer;
    public Renderer _renderer => m_renderer;
    [SerializeField, Tooltip("Reference to this object's collider")]    private Collider m_collider;
    public Collider _collider => m_collider;

    [Header("=== Map Settings ===")]
    [SerializeField, Tooltip("The bounds of this landmark")]    private Bounds m_bounds;
    [SerializeField, Tooltip("The min horizontal angle range for 'detection'"), Range(-180f,180f)]  private float m_minHorAngle = 5f;
    [SerializeField, Tooltip("The max horizontal angle range for 'detection'"), Range(-180f,180f)]  private float m_maxHorAngle = 30f;
    [SerializeField, Tooltip("The min vertical angle range for 'detection'"), Range(-180f,180f)]  private float m_minVerAngle = -30f;
    [SerializeField, Tooltip("The max vertical angle range for 'detection'"), Range(-180f,180f)]  private float m_maxVerAngle = 30f;
    [SerializeField, Tooltip("The total amount of time required for this to track as 'detected'")]  private float m_totalTimeToDetect = 5f;

    [Header("=== Outputs - READ ONLY ===")]
    [SerializeField, Tooltip("Am I in the camera fustrum?")]    private bool m_inFustrum = false;
    public bool inFustrum => m_inFustrum;
    [SerializeField, Tooltip("The distance between the landmark fustrum camera's center and the viewport projection of the bounds center onto this camera")]    private float m_distToCamCenter;
    public float distToCamCenter => m_distToCamCenter;

    private void OnEnable() {
        // Find all renderers associated with this object
        Renderer[] renderers = GetComponentsInChildren<Renderer>();

        // Initialize a new bounds that'll act as the composite bounds of this object
        m_bounds = new Bounds (transform.position, Vector3.one);
	    foreach (Renderer renderer in renderers) m_bounds.Encapsulate (renderer.bounds);
    }

    private void LateUpdate() {
        // Don't do a fustrum check if we aren't evne pressed down on the map input
        bool mapInputActive = PlayerMovement.current.GetHoldingMap();
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
        /*
        // One additional check is to see if the bound's center is contained within the right half of the view camera
        // We do this by calculating the signed angle between the camera's forward angle and the vector between the player and landmark position. The up vector is the world up vector.
        // + = right, - = left
        Vector3 from = FustrumManager.current.landmarkFustrumCamera.transform.forward;
        Vector3 to = transform.position - FustrumManager.current.landmarkFustrumCamera.transform.position;
        float horAngle = Vector3.SignedAngle(from, to, Vector3.up);
        float verAngle = Vector3.SignedAngle(from, to, Vector3.right);
        if (horAngle >= m_minHorAngle && horAngle <= m_maxHorAngle && verAngle >= m_minVerAngle && verAngle <= m_maxVerAngle) {
            Debug.Log($"{gameObject.name}: Inside Range");
        }\
        */
    }
}
