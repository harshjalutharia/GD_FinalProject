using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class CustomBounds : MonoBehaviour
{
    [Header("=== Custom Bounds ===")]
    public Vector3 center;
    public Vector3 rotation;
    public Vector3 extents = Vector3.one;

    public Vector3[] boundPoints;

    #if UNITY_EDITOR
    public bool drawGizmos = false;
    void OnDrawGizmos() {
        if (!drawGizmos) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(center, 0.25f);

        CalculateBoundPoints();
        Gizmos.color = Color.red;
        for(int i = 0; i < boundPoints.Length; i++) {
            Gizmos.DrawSphere(boundPoints[i], 0.2f);
        }
        Gizmos.DrawLine(boundPoints[0], boundPoints[1]);
        Gizmos.DrawLine(boundPoints[0], boundPoints[2]);
        Gizmos.DrawLine(boundPoints[1], boundPoints[3]);
        Gizmos.DrawLine(boundPoints[2], boundPoints[3]);
        Gizmos.DrawLine(boundPoints[4], boundPoints[5]);
        Gizmos.DrawLine(boundPoints[4], boundPoints[6]);
        Gizmos.DrawLine(boundPoints[5], boundPoints[7]);
        Gizmos.DrawLine(boundPoints[6], boundPoints[7]);

    }
    #endif

    public void Awake() {
        CalculateBoundPoints();
    }

    // First 4 = lower points
    // Last 4 = upper points
    public void CalculateBoundPoints() {
        boundPoints = new Vector3[8];
        Quaternion rotationMatrix = Quaternion.Euler(rotation);
        boundPoints[0] = center + rotationMatrix * -extents;
        boundPoints[1] = center + rotationMatrix * new Vector3(-extents.x, -extents.y, extents.z);
        boundPoints[2] = center + rotationMatrix * new Vector3(extents.x, -extents.y, -extents.z);
        boundPoints[3] = center + rotationMatrix * new Vector3(extents.x, -extents.y, extents.z);
        boundPoints[4] = center + rotationMatrix * extents;
        boundPoints[5] = center + rotationMatrix * new Vector3(-extents.x, extents.y, extents.z);
        boundPoints[6] = center + rotationMatrix * new Vector3(extents.x, extents.y, -extents.z);
        boundPoints[7] = center + rotationMatrix * new Vector3(-extents.x, extents.y, -extents.z);
    }
}
