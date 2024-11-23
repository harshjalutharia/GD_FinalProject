using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gem : MonoBehaviour
{
    [SerializeField, Tooltip("Reference to this object's collider. if unset, will attempt to set by itself")]
    private Collider m_collider = null;
    public Collider _collider => m_collider;
    private Renderer m_renderer = null;

    private void Awake() {
        m_collider = GetComponent<Collider>();
        m_renderer = GetComponent<Renderer>();
        if (m_collider != null) m_collider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other) {
        if (other.gameObject.tag != "Player") return;
        Debug.Log("Gem Reached!");
        GemGenerator.current.CollectGem(this);
    }

    
}
