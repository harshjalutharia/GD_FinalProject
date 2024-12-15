using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class TestMapNormal : MonoBehaviour
{
    public enum QueryType { Coords, Noise, Height, Normal }
    [SerializeField] private NoiseMap m_noiseMap;
    [SerializeField] private LayerMask m_queryMask;
    [SerializeField] private QueryType m_queryType;

    [Header("=== OUTPUTS ===")]
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

        // First, do a raycast downward to confirm
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 100f, m_queryMask)) {
            m_rayHitPos = hit.point;
            m_currentNormal = hit.normal;
            switch(m_queryType) {
                case QueryType.Height:
                    float queryY = m_noiseMap.QueryHeightAtWorldPos(transform.position.x, transform.position.z, out int hx, out int hy);
                    Debug.Log($"Raycast Y: {m_rayHitPos.y} | Query Y: {queryY}");
                    break;
                case QueryType.Normal:
                    Vector3 queryNormal = m_noiseMap.QueryMapNormalAtWorldPos(transform.position.x, transform.position.z, m_queryMask, out int nx, out int ny, out float fy);
                    Debug.Log($"Raycast Normal: {m_currentNormal.ToString()} | Query Normal: {queryNormal.ToString()}");
                    break;
                case QueryType.Coords:
                    Vector2Int queryCoords = m_noiseMap.QueryCoordsAtWorldPos(transform.position.x, transform.position.z);
                    Debug.Log($"Query Coords: {queryCoords.ToString()}");
                    break;
                default:
                    float queryNoise = m_noiseMap.QueryNoiseAtWorldPos(transform.position.x, transform.position.z, out int x, out int y);
                    Debug.Log($"Query Noise: {queryNoise}");
                    break;
            }
            
        }
        /*
        m_currentNormal = m_noiseMap.QueryMapNormalAtWorldPos(transform.position.x, transform.position.z, m_queryMask, out int x, out int y, out float worldY);
        m_rayHitPos = new Vector3(transform.position.x, worldY, transform.position.z);
        */
    }
}
