using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Landmark : MonoBehaviour
{
    [SerializeField, Tooltip("Reference to this object's renderer")]
    private Renderer m_renderer;
    public Renderer _renderer => m_renderer;

    [SerializeField, Tooltip("Reference to this object's collider")]
    private Collider m_collider;
    public Collider _collider => m_collider;
}
