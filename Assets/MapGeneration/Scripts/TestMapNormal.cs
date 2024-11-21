using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class TestMapNormal : MonoBehaviour
{
    [SerializeField] private NoiseMap m_noiseMap;
    [SerializeField] private LayerMask m_queryMask;
    [SerializeField] private Vector3 m_currentNormal;
    [SerializeField] private Vector3 m_rayHitPos;

    #if UNITY_EDITOR
    private void OnDrawGizmos() {
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, m_rayHitPos);
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(m_rayHitPos, m_currentNormal);
    }
    #endif

    // Update is called once per frame
    void Update() {
        if (m_noiseMap == null) return;

        m_currentNormal = m_noiseMap.QueryMapNormalAtWorldPos(transform.position.x, transform.position.z, m_queryMask, out int x, out int y, out float worldY);
        m_rayHitPos = new Vector3(transform.position.x, worldY, transform.position.z);
    }
}
