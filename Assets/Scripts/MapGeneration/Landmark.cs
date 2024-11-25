using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Landmark : MonoBehaviour
{
    [Header("=== References ===")]
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

    private void OnEnable() {
        CalculateBounds();
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

        Vector3[] bottomPoints = { m_bounds.center, m_bounds.center, m_bounds.center, m_bounds.center };
        bottomPoints[0] -= m_bounds.extents;
        bottomPoints[1] += new Vector3(m_bounds.extents.x, -m_bounds.extents.y, m_bounds.extents.z);
        bottomPoints[2] += new Vector3(-m_bounds.extents.x, -m_bounds.extents.y, m_bounds.extents.z);
        bottomPoints[3] += new Vector3(m_bounds.extents.x, -m_bounds.extents.y, -m_bounds.extents.z);

        float offset = 0f;

        for (int i = 0; i < bottomPoints.Length; i++)
        {
            float terrainHeight = terrainMap.QueryHeightAtWorldPos(bottomPoints[i].x, bottomPoints[i].z, out var x, out var y);
            if (terrainHeight < bottomPoints[i].y && bottomPoints[i].y - terrainHeight > offset)
                offset = bottomPoints[i].y - terrainHeight;
        }

        if (offset != 0f)
        {
            transform.position += Vector3.down * offset;
            CalculateBounds();
        }
    }
}


