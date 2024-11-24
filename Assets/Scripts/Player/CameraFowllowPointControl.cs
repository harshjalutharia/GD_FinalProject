using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;
using Unity.VisualScripting;


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

    [Header("=== Third Person Camera Damping Settings ===")]
    [SerializeField, Tooltip("Component of the third person camera")]                               private Camera thirdPersonCamera;
    [SerializeField, Tooltip("Decouple from parent upon start")]                                    private bool m_decoupleFromParentAtStart = true;
    [SerializeField, Tooltip("Smooth damp along X axis")]                                           private bool m_smoothX = true;
    [SerializeField, Tooltip("Smooth damp along Y axis")]                                           private bool m_smoothY = true;
    [SerializeField, Tooltip("Smooth damp along Z axis")]                                           private bool m_smoothZ = true;
    [SerializeField, Tooltip("Approximately the time the camera will take to reach the player")]    private float m_smoothTime;
    Vector3 currentVelocity = Vector3.zero;


    [Header("=== First Person Camera Settings")]
    [SerializeField, Tooltip("Component of the first person camera")]                               private Camera firstPersonCamera;
    [SerializeField, Tooltip("time camera trans from third person to first person")]                private float firstPersonSoothTime;
    [SerializeField, Tooltip("the speed of first person camera rotate in transit")]                 private float firstPersonCameraRotationSpeed;
    [SerializeField, Tooltip("decides when the camera rotate")]                                     private float rotationStartDistance;
    [SerializeField, Tooltip("the final position of the first person camera")]                      private Transform firstPersonCameraDestination;
    [SerializeField, Tooltip("Change in maximum pitch angle relative to the horizontal plane")]     private float maxPitchAngle;
    [SerializeField, Tooltip("camera vertical sensitivity")]                                        private float verticalSensitivity;
    public float pitch;
    [Tooltip("if player is holding a map")]                                                         public bool mapInputActive;
    [SerializeField, Tooltip("if the first person camera is active")]                               private bool firstPersonCameraActive;
    private Vector3 fpCameraVelocity = Vector3.zero;

    private PlayerControls m_controls;
    private InputAction m_mapInput;
    private InputAction m_viewInput;
    private PlayerMovement playerMovement;
    
    private void Start() {
        if (m_decoupleFromParentAtStart) m_cameraFocusRef.SetParent(null);
        firstPersonCamera.transform.SetParent(null);
        m_controls = InputManager.Instance.controls;
        m_mapInput = m_controls.Player.Map;
        m_viewInput = m_controls.Player.View;
        SetCameraSettings(m_thirdPersonSettings);


        playerMovement = gameObject.GetComponent<PlayerMovement>();
        firstPersonCamera.targetDisplay = 1;
        thirdPersonCamera.targetDisplay = 0;
        firstPersonCameraActive = false;
    }

    private void Update() {
        // float myTargetRotationX = m_cameraFocusOrientationRef.rotation.x; //get the X rotation from anotherObject
        // float myTargetRotationY = m_cameraFocusRef.rotation.y; //get the Y rotation from this object
        // float myTargetRotationZ = m_cameraFocusDestinationRef.rotation.z; //get the Z rotation from this object
        // Vector3 myEulerAngleRotation = new Vector3(myTargetRotationX, myTargetRotationY, myTargetRotationZ);
        // m_cameraFocusRef.rotation = Quaternion.Euler(myEulerAngleRotation);

        // setting the display target
        //mapInputActive = m_mapInput.IsPressed();
        mapInputActive = playerMovement.GetHoldingMap();
        if (firstPersonCameraActive) 
        {
            thirdPersonCamera.targetDisplay = 1;
            firstPersonCamera.targetDisplay = 0;
            
            // set vertical direction of the first person camera
            float viewInputY = m_viewInput.ReadValue<Vector2>().y;
            float currentPitch = firstPersonCameraDestination.localEulerAngles.x - viewInputY * verticalSensitivity;
            pitch = currentPitch;
            if (currentPitch < 180)
            {
                currentPitch = Mathf.Clamp(currentPitch, -100, maxPitchAngle);
            }
            else
            {
                currentPitch = Mathf.Clamp(currentPitch, 360 - maxPitchAngle, 460);
            }
            firstPersonCameraDestination.localEulerAngles = new Vector3(currentPitch, firstPersonCameraDestination.localEulerAngles.y, firstPersonCameraDestination.localEulerAngles.z);

        }
        else
        {
            thirdPersonCamera.targetDisplay = 0;
            firstPersonCamera.targetDisplay = 1;
        }
        // if (mapInputActive && m_activeCameraSettings != m_firstPersonSettings) SetCameraSettings(m_firstPersonSettings);
        // else if (!mapInputActive && m_activeCameraSettings != m_thirdPersonSettings) SetCameraSettings(m_thirdPersonSettings);

    }

    private void FixedUpdate() {
        Vector3 targetPos = m_cameraFocusDestinationRef.position;
        float x = (m_smoothX) ? m_cameraFocusRef.position.x : targetPos.x;
        float y = (m_smoothY) ? m_cameraFocusRef.position.y : targetPos.y;
        float z = (m_smoothZ) ? m_cameraFocusRef.position.z : targetPos.z;
        m_cameraFocusRef.position = new Vector3(x,y,z);
        m_cameraFocusRef.position = Vector3.SmoothDamp(m_cameraFocusRef.position, targetPos, ref currentVelocity, m_smoothTime);
        
        //set position of first person camera
        if (mapInputActive)
        {
            firstPersonCameraActive = true;
            firstPersonCamera.transform.position = Vector3.SmoothDamp(firstPersonCamera.transform.position, firstPersonCameraDestination.position,
                ref fpCameraVelocity, firstPersonSoothTime);
            if (Vector3.Distance(firstPersonCamera.transform.position, firstPersonCameraDestination.position) < rotationStartDistance)
            {
                firstPersonCamera.transform.rotation = Quaternion.RotateTowards(firstPersonCamera.transform.rotation,
                    firstPersonCameraDestination.rotation, firstPersonCameraRotationSpeed * Time.fixedDeltaTime);
            }
        }
        else
        {
            if (firstPersonCameraActive)
            {
                firstPersonCamera.transform.position = Vector3.SmoothDamp(firstPersonCamera.transform.position, thirdPersonCamera.transform.position,
                    ref fpCameraVelocity, firstPersonSoothTime);
                firstPersonCamera.transform.rotation = Quaternion.RotateTowards(firstPersonCamera.transform.rotation,
                    thirdPersonCamera.transform.rotation, firstPersonCameraRotationSpeed * Time.fixedDeltaTime);
                if (Vector3.Distance(firstPersonCamera.transform.position, thirdPersonCamera.transform.position) <
                    0.05f)
                {
                    firstPersonCameraActive = false;
                }
            }
            else
            {
                firstPersonCamera.transform.position = thirdPersonCamera.transform.position;
                firstPersonCamera.transform.rotation = thirdPersonCamera.transform.rotation;
            }
            
        }
        
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




