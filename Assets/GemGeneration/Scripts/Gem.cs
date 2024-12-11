using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gem : MonoBehaviour
{
    public enum GemType { Destination, Small }

    [Header("=== References ===")]
    [SerializeField, Tooltip("Reference to this object's collider. if unset, will attempt to set by itself")] private Collider m_collider = null;
    public Collider _collider => m_collider;
    private Renderer m_renderer = null;

    [Header("=== Gem Properties ===")]
    public GemType gemType;
    public int regionIndex;

    private void Awake() {
        m_collider = GetComponent<Collider>();
        m_renderer = GetComponent<Renderer>();
        if (m_collider != null) m_collider.isTrigger = true;
    }

    public void SetColor(Color color) {
        if (m_renderer != null) m_renderer.material.SetColor("_Color", color); 
    }

    private void OnTriggerEnter(Collider other) {
        if (other.gameObject.tag != "Player") return;
        Debug.Log("Gem Reached!");
        if (SessionManager2.current != null) SessionManager2.current.CollectGem(this);
    }

    
}
