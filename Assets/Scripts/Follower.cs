using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Follower : MonoBehaviour
{
    public enum UpdateType { Update, LateUpdate }

    // When attached to an object, it makes this object attempt to follow the orientation and position of other objects
    [SerializeField] private Transform m_positionTarget;
    [SerializeField] private Transform m_orientationTarget;
    [SerializeField] private UpdateType m_updateType = UpdateType.Update;

    private void Update() {
        if (m_updateType == UpdateType.Update) Follow();
    }

    private void LateUpdate() {
        if (m_updateType == UpdateType.LateUpdate) Follow();
    }

    public void Follow() {
        transform.position = m_positionTarget.position;
        transform.rotation = m_orientationTarget.rotation;
        // Update position
        //Vector3 targetPos = m_positionTarget.position;
        // + m_positionOffset;
        //transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref m_positionVelocity, m_positionSmoothTime);

        // For orientation, we instead smoothdamp between the forwards directions
        //Vector3 targetForward = m_positionTarget.forward;
        //+ m_orientationOffset;
        //Vector3 currentForward = Vector3.SmoothDamp(transform.forward, targetForward, ref m_orientationVelocity, m_orientationSmoothTime);
        //transform.LookAt(m_positionTarget.forward);
    }
}
