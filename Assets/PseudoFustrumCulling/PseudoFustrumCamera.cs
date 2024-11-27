using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class PseudoFustrumCamera : MonoBehaviour
{
    [SerializeField, Range(-180f,180f), Tooltip("Left fustrum plane angle range.")]    private float m_leftAngleRange;
    [SerializeField, Range(-180f,180f), Tooltip("Right fustrum plane angle range.")]   private float m_rightAngleRange; 
    [SerializeField, Range(-180f,180f), Tooltip("Top fustrum plane angle range.")]     private float m_topAngleRange;
    [SerializeField, Range(-180f,180f), Tooltip("Bottom fustrum plane angle range.")]  private float m_bottomAngleRange;
    [SerializeField, Tooltip("Distance range for checking fustrum")]                private float m_distanceRange;
    private float m_horAngleRange => m_rightAngleRange - m_leftAngleRange;

    [SerializeField] private Transform m_debugTestRef;


    #if UNITY_EDITOR
    [SerializeField] private bool m_showFustrum = true;
    private void OnDrawGizmos() {
       Vector3 topLeftVector = transform.rotation * CalculateVector3FromTwoAngles(m_leftAngleRange, m_topAngleRange, m_distanceRange);
       Vector3 topRightVector = transform.rotation * CalculateVector3FromTwoAngles(m_rightAngleRange, m_topAngleRange, m_distanceRange);
       Vector3 bottomLeftVector = transform.rotation * CalculateVector3FromTwoAngles(m_leftAngleRange, m_bottomAngleRange, m_distanceRange);
       Vector3 bottomRightVector = transform.rotation * CalculateVector3FromTwoAngles(m_rightAngleRange, m_bottomAngleRange, m_distanceRange);

       Gizmos.color = Color.white;
       Gizmos.DrawRay(transform.position, topLeftVector);
       Gizmos.DrawRay(transform.position, topRightVector);
       Gizmos.DrawRay(transform.position, bottomLeftVector);
       Gizmos.DrawRay(transform.position, bottomRightVector);
       Gizmos.DrawLine(transform.position + topLeftVector, transform.position + topRightVector);
       Gizmos.DrawLine(transform.position + topLeftVector, transform.position + bottomLeftVector);
       Gizmos.DrawLine(transform.position + bottomLeftVector, transform.position + bottomRightVector);
       Gizmos.DrawLine(transform.position + topRightVector, transform.position + bottomRightVector);
       
    }
    #endif

    private void Update() {
        if (m_debugTestRef != null && IsPositionInFrustum(m_debugTestRef.position)) Debug.Log("In fustrum!");
    }

    private Vector3 CalculateVector3FromTwoAngles(float horizontalAngle, float verticalAngle, float distance) {
        // Convert angles to radians (Unity uses radians for trigonometric functions)
        float horizontalRad = horizontalAngle * Mathf.Deg2Rad;
        float verticalRad = verticalAngle * Mathf.Deg2Rad;

        // Calculate the direction based on the horizontal and vertical angles
        // Horizontal rotation (around Y-axis) affects the XZ-plane
        float x = Mathf.Cos(verticalRad) * Mathf.Sin(horizontalRad);
        float y = Mathf.Sin(verticalRad); // Vertical angle affects the Y direction
        float z = Mathf.Cos(verticalRad) * Mathf.Cos(horizontalRad);

        // Create the direction vector
        Vector3 direction = new Vector3(x, y, z);

        // Normalize the direction (make sure it's a unit vector)
        direction.Normalize();
        float scalingFactor = distance / Mathf.Abs(direction.z);  // Scale based on Z-component

        return direction * scalingFactor;
    }

    public bool CheckInHorizontalFustrum(Vector3 query) {
        float signedAngle = Vector3.SignedAngle(transform.forward, query-transform.position, Vector3.up);
        return ( (signedAngle >= 0 && signedAngle <= m_rightAngleRange) || (signedAngle < 0 && signedAngle >= m_leftAngleRange) );
    }

    public bool IsPositionInFrustum(Vector3 targetPosition) {
        // Get the camera's position and direction (forward)
        Vector3 cameraPosition = transform.position;
        Vector3 cameraForward = transform.forward;
        Vector3 cameraRight = transform.right;
        Vector3 cameraUp = transform.up;

        // Calculate the left, right, top, and bottom planes of the frustum based on the angles
        Plane leftPlane = new Plane(cameraRight, cameraPosition);
        Plane rightPlane = new Plane(-cameraRight, cameraPosition);
        Plane topPlane = new Plane(cameraUp, cameraPosition);
        Plane bottomPlane = new Plane(-cameraUp, cameraPosition);

        // Check if the target position is in front of each frustum plane
        Vector3 displacement = targetPosition - cameraPosition;
        if (Vector3.Dot(displacement, cameraRight) < Mathf.Tan(m_leftAngleRange * Mathf.Deg2Rad) ||
            Vector3.Dot(displacement, -cameraRight) < Mathf.Tan(m_rightAngleRange * Mathf.Deg2Rad) ||
            Vector3.Dot(displacement, cameraUp) < Mathf.Tan(m_topAngleRange * Mathf.Deg2Rad) ||
            Vector3.Dot(displacement, -cameraUp) < Mathf.Tan(m_bottomAngleRange * Mathf.Deg2Rad) ||
            Vector3.Dot(displacement, -cameraForward) > m_distanceRange) {
            return false; // The position is outside the frustum
        }

        return true; // The position is inside the frustum
    }

    private void OnValidate() {
        if (m_distanceRange <= 0f) m_distanceRange = 1f;
    }
}
