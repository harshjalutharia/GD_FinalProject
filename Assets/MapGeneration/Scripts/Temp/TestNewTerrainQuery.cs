using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class TestNewTerrainQuery : MonoBehaviour
{

    [SerializeField] private TerrainManager m_terrainManager;
    private bool hit = false;
    private Vector3 hitPoint, hitNormal;
    private float hitSteepness;

    #if UNITY_EDITOR
    [SerializeField] private Color m_debugColor;
    [SerializeField] private float m_debugSize = 1f;
    [SerializeField] private float m_debugNormalDistance = 10f;
    private void OnDrawGizmos() {
        if (!hit) return;
        Gizmos.color = m_debugColor;
        Gizmos.DrawCube(hitPoint, Vector3.one*m_debugSize);
        Gizmos.DrawLine(transform.position, hitPoint);
        Gizmos.DrawRay(hitPoint, hitNormal*m_debugNormalDistance);
    }
    #endif

    private void Update() {
        if (m_terrainManager == null) {
            hit=false; 
            return;
        }
        hit = m_terrainManager.TryGetPointOnTerrain(transform.position, out hitPoint, out hitNormal, out hitSteepness);
        if (hit)    Debug.Log($"Point: {hitPoint.ToString()} | Normal: {hitNormal.ToString()} | Steepness: {hitSteepness.ToString()}");
        else        Debug.Log("No hit with terrain mesh");
    }
}
