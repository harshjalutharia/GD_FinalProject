using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// When attached to an object, it makes this object attempt to follow the postion and/or rotation of other objcts
public class Follower : MonoBehaviour
{
    public enum UpdateType { Update, LateUpdate, FixedUpdate }
    [SerializeField] private UpdateType m_updateType = UpdateType.Update;

    [Header("=== Position Following ===")]
    [SerializeField] private Transform m_positionTarget;
    [SerializeField] private bool m_smoothX = true;
    [SerializeField] private bool m_smoothY = true;
    [SerializeField] private bool m_smoothZ = true;
    private bool m_smoothTranslation => m_smoothX || m_smoothY || m_smoothZ;
    [SerializeField] private float m_smoothTime = 1f;
    private Vector3 m_positionVelocity = Vector3.zero;
    
    [Header("=== Rotation Following ===")]
    [SerializeField] private Transform m_orientationTarget;

    private void Update() {
        if (m_updateType == UpdateType.Update) Follow(Time.deltaTime);
    }

    private void LateUpdate() {
        if (m_updateType == UpdateType.LateUpdate) Follow(Time.deltaTime);
    }

    private void FixedUpdate() {
        if (m_updateType == UpdateType.FixedUpdate) Follow(Time.fixedDeltaTime);
    }

    public void Follow(float deltaTime) {
        // Position
        if (m_positionTarget != null) {
            Vector3 targetPos = m_positionTarget.position;
            float x = (m_smoothX) ? transform.position.x : targetPos.x;
            float y = (m_smoothY) ? transform.position.y : targetPos.y;
            float z = (m_smoothZ) ? transform.position.z : targetPos.z;
            transform.position = new Vector3(x,y,z);
            if (m_smoothTranslation) transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref m_positionVelocity, m_smoothTime);
        }

        // Rotation
        if (m_orientationTarget != null) transform.rotation = m_orientationTarget.rotation;

        //Vector3 targetForward = m_positionTarget.forward;
        //+ m_orientationOffset;
        //Vector3 currentForward = Vector3.SmoothDamp(transform.forward, targetForward, ref m_orientationVelocity, m_orientationSmoothTime);
        //transform.LookAt(m_positionTarget.forward);
    }
}
