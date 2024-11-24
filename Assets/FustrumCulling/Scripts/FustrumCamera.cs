using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class FustrumCamera : MonoBehaviour
{    
    [SerializeField, Tooltip("The camera to calculate the fustrum planes")] private Camera m_camera;
    [SerializeField, Tooltip("The fustrum planes generated from the camera")] private Plane[] m_planes;
    public Plane[] planes => m_planes;

    private void Update() {
        // Initialize fustrum planes
        m_planes = GeometryUtility.CalculateFrustumPlanes(m_camera);
    }

    public bool CheckInFustrum(Collider col) {
        return TestPlanesAABB(m_planes, col.bounds);
    }
    public bool CheckInFustrum(Renderer renderer) {
        return TestPlanesAABB(m_planes, renderer.bounds);
    }
    public bool CheckInFustrum(Bounds bounds) {
        return TestPlanesAABB(m_planes, bounds);
    }

    public static bool TestPlanesAABB(Plane[] planes, Bounds bounds) {
        for (int i = 0; i < planes.Length; i++) {
            Plane plane = planes[i];
            float3 normal_sign = math.sign(plane.normal);
            float3 test_point = (float3)bounds.center + (((float3)bounds.extents)*normal_sign);
            float dot = math.dot(test_point, plane.normal);
            if (dot + plane.distance < 0) return false;
        }
        return true;
    }
}
