using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FustrumGroup : MonoBehaviour
{
    [Header("=== References ===")]
    [SerializeField, Tooltip("The fustrum camera used for fustrum culling this object")]        private FustrumCamera m_fustrumCamera;
    [SerializeField, Tooltip("Reference to the collider used to check for fustrum culling.")]   private Collider m_collider;

    [Header("=== Parent-Child References ===")]
    [SerializeField, Tooltip("Do we have another FustrumGroup as a parent?")]                   private FustrumGroup m_fustrumParent = null;
    [SerializeField, Tooltip("List of renderers to activate/deactivate")]                       private List<Renderer> m_renderers = new List<Renderer>();
    [SerializeField, Tooltip("List of gameobjects to activate/deactivate")]                     private List<GameObject> m_gameObjects = new List<GameObject>();
    [SerializeField, Tooltip("Do we have any fustrum group children?")] private List<FustrumGroup> m_fustrumChildren = new List<FustrumGroup>();

    [Header("=== Outputs - READ ONLY ===")]
    [SerializeField, Tooltip("Am I currently visible to the camera?")]  private bool m_visible = false;
    public bool visible => m_visible;
    
    private bool m_previousVisibility = true;

    private void LateUpdate() {
        // Can't do anything if we're missing a fustrum camera references
        if (FustrumManager.current.mainFustrumCamera == null) return;

        // Can't do anything if we don't even have a collider
        if (m_collider == null) return;

        // Don't do anything if we don't even have any renderers or children to consider
        if (m_renderers.Count == 0 && m_gameObjects.Count == 0 && m_fustrumChildren.Count == 0) return;

        // Check our visibility
        m_visible = FustrumManager.current.mainFustrumCamera.CheckInFustrum(m_collider);
        
        if (m_visible != m_previousVisibility) {
            // If any renderers attached, enable/disable them
            if (m_renderers.Count > 0) foreach(Renderer r in m_renderers) r.enabled = m_visible;
            // If any gameobjects attached, enable/disable them
            if (m_gameObjects.Count > 0) foreach(GameObject go in m_gameObjects) go.SetActive(m_visible);

            // If any FustrumGroup children, then toggle them too
            if (m_fustrumChildren.Count > 0) foreach(FustrumGroup child in m_fustrumChildren) child.enabled = m_visible;

            m_previousVisibility = m_visible;
        }
    }

    public void QueryGridParent() {
        // Do we have a parent om a Fustrum Manager grid? Check by querying the FustrumManager to see
        if (FustrumManager.current == null) return;
        if (!FustrumManager.current.initialized) return;

        Vector2Int coords = FustrumManager.current.GetCoordsFromWorldPosition(transform.position);
        if (FustrumManager.current.coordToChunkMap.ContainsKey(coords)) {
            FustrumGroup parent = FustrumManager.current.coordToChunkMap[coords];
            SetParent(parent);
        }
    }

    public void SetParent(FustrumGroup newParent = null) {
        if (m_fustrumParent != null) m_fustrumParent.RemoveChild(this);
        m_fustrumParent = newParent;
        m_fustrumParent.AddChild(this);
    }

    public void AddChild(FustrumGroup child) {
        m_fustrumChildren.Add(child);
    }

    public void RemoveChild(FustrumGroup child) {
        m_fustrumChildren.Remove(child);
    }

    public void AddRenderers(Renderer[] toAdd) {
        m_renderers.AddRange(toAdd);
        foreach(Renderer r in toAdd) {
            r.enabled = m_visible;
        }
    }

    public void AddGameObject(GameObject go) {
        m_gameObjects.Add(go);
        go.SetActive(m_visible);
    }

    private void OnDisable() {
        m_visible = false;
        if (m_renderers.Count > 0) foreach(Renderer r in m_renderers) r.enabled = false;
        if (m_fustrumChildren.Count > 0) foreach(FustrumGroup child in m_fustrumChildren) child.enabled = false;
    }
}
