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
    [SerializeField, Tooltip("Reference to the object's audio source")] private AudioSource m_bellRing;
    [SerializeField, Tooltip("Reference to the particle system")]       private ParticleSystem m_particleSystem;

    [Header("=== Gem Properties ===")]
    public GemType gemType;
    public int regionIndex;

    private void Awake() {
        m_collider = GetComponent<Collider>();
        m_renderer = GetComponent<Renderer>();
        if (m_collider != null) m_collider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other) {
        if (other.gameObject.tag != "Player") return;
        Debug.Log("Gem Reached!");
        if (SessionManager2.current != null) SessionManager2.current.CollectGem(this);
    }

    public void RingGem() {
        if (m_bellRing != null) m_bellRing.Play();
        if (m_particleSystem != null) m_particleSystem.Play();
    }

    
}
