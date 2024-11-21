using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FustrumCull : MonoBehaviour
{
    [SerializeField, Tooltip("Reference to the collider used to check for fustrum culling.")] private Collider m_collider;
    [SerializeField, Tooltip("List of renderers to activate/deactivate")] private Renderer[] m_renderers;

    private void LateUpdate() {
        if (m_collider == null) return;
        bool visible = FustrumCamera.current.CheckInFustrum(m_collider);
        foreach(Renderer r in m_renderers) r.enabled = visible;
    }
}
