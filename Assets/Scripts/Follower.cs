using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Follower : MonoBehaviour
{

    // When attached to an object, it makes this object attempt to follow the orientation and position of other objects
    [SerializeField] private Transform m_positionTarget;
    [SerializeField] private Transform m_orientationTarget;

    [SerializeField] private float m_positionSmoothTime = 0.3f;
    [SerializeField] private float m_orientationSmoothTime = 0.3f;
    
    private Vector3 m_positionVelocity = Vector3.zero;
    private Vector3 m_positionOffset = Vector3.zero;

    private Vector3 m_orientationVelocity = Vector3.zero;
    private Vector3 m_orientationOffset = Vector3.zero;
    
    private void Start() {
        m_positionOffset = m_positionTarget.position - transform.position;
        m_orientationOffset = m_orientationTarget.forward - transform.forward;
    }

    private void Update() {
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
