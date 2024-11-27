using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PseudoFustrumObject : MonoBehaviour
{
    [SerializeField] private PseudoFustrumCamera m_camera;
    [SerializeField] private HashSet<Renderer> m_renderers;
    [SerializeField] private Bounds m_bounds;
    public Bounds bounds => m_bounds;

    [SerializeField] private bool m_updateBoundsContinuously = false;

    private void OnEnable() {
        CalculateBounds();
    }

    private void Update() {
        if (m_updateBoundsContinuously) CalculateBounds();
    }

    private void LateUpdate() {
        CheckInFustrum();
    }

    public void CalculateBounds() {
        // Create new bounds as baseline
        m_bounds = new Bounds(transform.position, Vector3.one);

        // Create hashset for renderers, iteratively add these renderers.
        m_renderers = new HashSet<Renderer>(GetComponentsInChildren<Renderer>());
        foreach(Renderer r in m_renderers) m_bounds.Encapsulate(r.bounds);
    }

    public void AddRenderer(Renderer r) {
        if (!m_renderers.Contains(r)) {
            m_renderers.Add(r);
            m_bounds.Encapsulate(r.bounds);
        }
    }

    public void CheckInFustrum() {
        if (m_camera == null) return;
        if (m_camera.CheckInHorizontalFustrum(transform.position)) Debug.Log("In fustrum");
    }
}