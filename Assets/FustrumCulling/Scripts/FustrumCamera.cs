using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class FustrumCamera : MonoBehaviour
{    
    [SerializeField, Tooltip("The camera to calculate the fustrum planes")] private Camera m_camera;
    public Camera camera => m_camera;
    [SerializeField, Tooltip("The fustrum planes generated from the camera")] private Plane[] m_planes;
    public Plane[] planes => m_planes;
    private Vector3 m_prevPosition = Vector3.zero;
    private Vector3 m_velocity = Vector3.zero;
    public Vector3 velocity => m_velocity;
    public float speed => m_velocity.magnitude;

    [Header("=== Debug Settings ===")]
    [SerializeField] private bool m_debugMovement = false;
    [SerializeField] private float m_debugMovementSpeed = 10f;

    private void Awake() {
        m_prevPosition = transform.position;
    }

    private void Update() {
        // Initialize fustrum planes
        m_planes = GeometryUtility.CalculateFrustumPlanes(m_camera);
        // If debug movement toggled, then toggle
        if (m_debugMovement) {
            Vector3 moveVec = new Vector3( Input.GetAxis("Horizontal") , 0 , Input.GetAxis("Vertical") );
            transform.position += moveVec * m_debugMovementSpeed * Time.deltaTime;
        }
        // Update speed
        m_velocity = (transform.position - m_prevPosition) / Time.deltaTime;
        m_prevPosition = transform.position;
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
        if (planes == null) return false;
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
