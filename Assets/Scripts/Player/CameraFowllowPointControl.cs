using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;

// public class CameraFowllowPointControl : MonoBehaviour
// {
//     [Tooltip("Player Object")]
//     public Transform player;
//
//     private Rigidbody playerRb;
//     private PlayerMovement playerMovement;
//
//     [Tooltip("Maximum ground distance tolerated")]
//     public float maxGroundDistance;
//
//     [Tooltip("layers of ground")] 
//     public LayerMask groundMask;
//
//     public float followSpeed;
//
//     [SerializeField, Tooltip("if camera track point overlap with player")]
//     private bool track;
//     
//
//     private void Awake()
//     {
//         playerMovement = player.GetComponent<PlayerMovement>();
//     }
//
//     void Start()
//     {
//         playerRb = player.GetComponent<Rigidbody>();
//         track = false;
//     }
//
//     
//     
//     
//     void Update()
//     {
//         RaycastHit hit = new RaycastHit();
//         
//         
//         if (Physics.Raycast(player.position, Vector3.down, out hit, maxGroundDistance, groundMask))
//         {
//             Vector3 groundPos = player.position + hit.distance * Vector3.down;
//             if (track)
//             {
//                 float supposedY = Mathf.Lerp(transform.position.y, player.position.y, Time.deltaTime * followSpeed);
//                 transform.position = new Vector3(player.position.x, Mathf.Max(groundPos.y, supposedY), player.position.z);
//                 // transform.position = Vector3.Lerp(transform.position, groundPos, Time.deltaTime * followSpeed);
//             }
//             else
//             {
//                 transform.position = groundPos;
//             }
//         }
//         else
//         {
//             transform.position = Vector3.Lerp(transform.position, player.position, Time.deltaTime * followSpeed);
//             track = true;
//         }
//     }
//     
//     private void OnEnable()
//     {
//         Debug.Log(playerMovement.maxFlightStamina);
//         playerMovement.OnLanding += Landing;
//     }
//
//     private void OnDisable()
//     {
//         playerMovement.OnLanding -= Landing;
//     }
//
//     private void Landing()
//     {
//         track = false;
//     }
// }


public class CameraFowllowPointControl : MonoBehaviour
{
    [System.Serializable]
    public class VirtualCameraSettings {
        public CinemachineVirtualCameraBase virtualCamera;
        public bool showPlayerModel;
    }

    [Header("=== Virtual Cameras ===")]
    [SerializeField, Tooltip("The third-person camera settings")]   private VirtualCameraSettings m_thirdPersonSettings;
    [SerializeField, Tooltip("The first-person camera settings")]   private VirtualCameraSettings m_firstPersonSettings;
    [SerializeField, Tooltip("The transform reference that the cinemachine cameras focus on")]              private Transform m_cameraFocusRef;
    [SerializeField, Tooltip("The transform reference that the camera focus tries to smooth damp towards")] private Transform m_cameraFocusDestinationRef;
    [SerializeField, Tooltip("The transform reference that the camera focus tries to follow the y-axis rotation of")]   private Transform m_cameraFocusOrientationRef;
    private VirtualCameraSettings m_activeCameraSettings = null;
    [SerializeField, Tooltip("Renderer for the player avatar")]     private Renderer[] m_playerRenderers;

    [Header("=== Camera Damping Settings ===")]
    [SerializeField, Tooltip("Decouple from parent upon start")]                                    private bool m_decoupleFromParentAtStart = true;
    [SerializeField, Tooltip("Smooth damp along X axis")]                                           private bool m_smoothX = true;
    [SerializeField, Tooltip("Smooth damp along Y axis")]                                           private bool m_smoothY = true;
    [SerializeField, Tooltip("Smooth damp along Z axis")]                                           private bool m_smoothZ = true;
    [SerializeField, Tooltip("Approximately the time the camera will take to reach the player")]    private float m_smoothTime;
    Vector3 currentVelocity = Vector3.zero;

    private PlayerControls m_controls;
    private InputAction m_mapInput;
    
    private void Start() {
        if (m_decoupleFromParentAtStart) m_cameraFocusRef.SetParent(null);
        m_controls = InputManager.Instance.controls;
        m_mapInput = m_controls.Player.Map;
        SetCameraSettings(m_thirdPersonSettings);
    }

    private void Update() {
        float myTargetRotationX = m_cameraFocusOrientationRef.rotation.x; //get the X rotation from anotherObject
        float myTargetRotationY = m_cameraFocusRef.rotation.y; //get the Y rotation from this object
        float myTargetRotationZ = m_cameraFocusDestinationRef.rotation.z; //get the Z rotation from this object
        Vector3 myEulerAngleRotation = new Vector3(myTargetRotationX, myTargetRotationY, myTargetRotationZ);
        m_cameraFocusRef.rotation = Quaternion.Euler(myEulerAngleRotation);

        bool mapInputActive = m_mapInput.IsPressed();
        if (mapInputActive && m_activeCameraSettings != m_firstPersonSettings) SetCameraSettings(m_firstPersonSettings);
        else if (!mapInputActive && m_activeCameraSettings != m_thirdPersonSettings) SetCameraSettings(m_thirdPersonSettings);

    }

    private void FixedUpdate() {
        Vector3 targetPos = m_cameraFocusDestinationRef.position;
        float x = (m_smoothX) ? m_cameraFocusRef.position.x : targetPos.x;
        float y = (m_smoothY) ? m_cameraFocusRef.position.y : targetPos.y;
        float z = (m_smoothZ) ? m_cameraFocusRef.position.z : targetPos.z;
        m_cameraFocusRef.position = new Vector3(x,y,z);
        m_cameraFocusRef.position = Vector3.SmoothDamp(m_cameraFocusRef.position, targetPos, ref currentVelocity, m_smoothTime);
    }

    public void SetCameraSettings(VirtualCameraSettings settings) {
        if (m_activeCameraSettings != null && m_activeCameraSettings != settings) {
            m_activeCameraSettings.virtualCamera.Priority = 0;
        }
        settings.virtualCamera.Priority = 1;
        foreach(Renderer r in m_playerRenderers) r.enabled = settings.showPlayerModel;
        m_activeCameraSettings = settings;
    }
}




