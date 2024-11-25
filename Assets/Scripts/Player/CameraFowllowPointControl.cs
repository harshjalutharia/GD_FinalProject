using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;
using Unity.VisualScripting;


public class CameraFowllowPointControl : MonoBehaviour
{

    [Header("=== Virtual Cameras ===")]
    [SerializeField, Tooltip("The transform reference that the cinemachine cameras focus on")]              private Transform m_cameraFocusRef;
    [SerializeField, Tooltip("The transform reference that the camera focus tries to smooth damp towards")] private Transform m_cameraFocusDestinationRef;
    [SerializeField, Tooltip("The transform reference that the camera focus tries to follow the y-axis rotation of")]   private Transform m_cameraFocusOrientationRef;
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
    [SerializeField, Tooltip("if the first person camera is active")]                               public bool  m_firstPersonCameraActive;
    public bool firstPersonCameraActive => m_firstPersonCameraActive;
    private Vector3 fpCameraVelocity = Vector3.zero;

    private PlayerControls m_controls;
    //private InputAction m_mapInput;
    private InputAction m_viewInput;
    private PlayerMovement playerMovement;
    
    private void Start() {
        if (m_decoupleFromParentAtStart) m_cameraFocusRef.SetParent(null);
        firstPersonCamera.transform.SetParent(null);
        m_controls = InputManager.Instance.controls;
        //m_mapInput = m_controls.Player.Map;
        m_viewInput = m_controls.Player.View;

        playerMovement = gameObject.GetComponent<PlayerMovement>();
        firstPersonCamera.targetDisplay = 1;
        thirdPersonCamera.targetDisplay = 0;
         m_firstPersonCameraActive = false;
    }

    private void Update() {
        // float myTargetRotationX = m_cameraFocusOrientationRef.rotation.x; //get the X rotation from anotherObject
        // float myTargetRotationY = m_cameraFocusRef.rotation.y; //get the Y rotation from this object
        // float myTargetRotationZ = m_cameraFocusDestinationRef.rotation.z; //get the Z rotation from this object
        // Vector3 myEulerAngleRotation = new Vector3(myTargetRotationX, myTargetRotationY, myTargetRotationZ);
        // m_cameraFocusRef.rotation = Quaternion.Euler(myEulerAngleRotation);

        // Update our knowledge on whether the map is active
        mapInputActive = playerMovement.GetHoldingMap();

        // We actually don't translate the 1st person camera stuff here (that's in fixed update)
        // Instead, we let the fixed update translate the cameras, and we have hooks to detect when to activate the 1st or 3rd person camera.

        // If ` m_firstPersonCameraActive` is set, then the fixed update has started moving the 1st person camera at least.
        if ( m_firstPersonCameraActive) 
        {
            // Switch the output display. The 3rd person display becomes subservient to the 1st person camera, which becomes the main display.
            thirdPersonCamera.targetDisplay = 1;
            firstPersonCamera.targetDisplay = 0;

            // Set the main terrain fustrum to the first person camera
            FustrumManager.current.SetMainFustrumCamera(firstPersonCamera);

            
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
        // In this case, we ONLY activate the 3rd person camera if the fixed upddate moves the 1st-person camera safely back into position. 
        // This is caught only at the tail end of the smooth damp of the 1st-person cam back into the 3rd-person cam position.
        else
        {
            thirdPersonCamera.targetDisplay = 0;
            firstPersonCamera.targetDisplay = 1;
            FustrumManager.current.SetMainFustrumCamera(thirdPersonCamera);
        }
    }

    private void FixedUpdate() {
        // `m_cameraFocusRef` is what the cinemachine camera tries to follow. We need to damp its movement, as to prevent the viewport sickness issue
        // `m_cameraFocusRef` follows `m_cameraFocusDestinationRef`, which is a static object attached to the player. We smoothdamp `m_cameraFocusRef` to follow it.
        Vector3 targetPos = m_cameraFocusDestinationRef.position;
        float x = (m_smoothX) ? m_cameraFocusRef.position.x : targetPos.x;
        float y = (m_smoothY) ? m_cameraFocusRef.position.y : targetPos.y;
        float z = (m_smoothZ) ? m_cameraFocusRef.position.z : targetPos.z;
        m_cameraFocusRef.position = new Vector3(x,y,z);
        m_cameraFocusRef.position = Vector3.SmoothDamp(m_cameraFocusRef.position, targetPos, ref currentVelocity, m_smoothTime);

        // There are technically two cameras: the cinemachine freelook 3rd person camera, and the map input 1st person camera. We need to handle what happens when we switch between the two cameras.
        // When the map input is activated, we need to smooth damp it to the proper position - in this case, the `firstPersonCameraDestination`. So, very similar to the cinemachine 3rd person cam.
        // When the map input is deactivated, we need to smoothdamp it to the third person camera again
        
        // Map input detected (via Update). Smoothdamp the 1st person cam to the 3rd person cam.
        if (mapInputActive)
        {
             m_firstPersonCameraActive = true;
            
            // Purely for translating the 1st-person camera to the proper 1st-person position.
            firstPersonCamera.transform.position = Vector3.SmoothDamp(firstPersonCamera.transform.position, firstPersonCameraDestination.position,
                ref fpCameraVelocity, firstPersonSoothTime);
            // We don't rotate the camera until we get close enough. This creates the "zoom-in" effect first when the camera starts from the 3rd-person view.
            if (Vector3.Distance(firstPersonCamera.transform.position, firstPersonCameraDestination.position) < rotationStartDistance)
            {
                firstPersonCamera.transform.rotation = Quaternion.RotateTowards(firstPersonCamera.transform.rotation,
                    firstPersonCameraDestination.rotation, firstPersonCameraRotationSpeed * Time.fixedDeltaTime);
            }
        }

        // Map input not detected. Smoothdamp the 1st person cam back to the 3rd person position
        else
        {
            if ( m_firstPersonCameraActive)
            {
                // We smoothdamp and rotate to match the 3rd person camera settings.
                firstPersonCamera.transform.position = Vector3.SmoothDamp(firstPersonCamera.transform.position, thirdPersonCamera.transform.position,
                    ref fpCameraVelocity, firstPersonSoothTime);
                firstPersonCamera.transform.rotation = Quaternion.RotateTowards(firstPersonCamera.transform.rotation,
                    thirdPersonCamera.transform.rotation, firstPersonCameraRotationSpeed * Time.fixedDeltaTime);
                // If the distance is close enough, we toggle that the first person is no longer active. This gets noticed in the `Update` loop and switches the displays properly.
                if (Vector3.Distance(firstPersonCamera.transform.position, thirdPersonCamera.transform.position) <
                    0.05f)
                {
                     m_firstPersonCameraActive = false;
                }
            }
            else
            {
                firstPersonCamera.transform.position = thirdPersonCamera.transform.position;
                firstPersonCamera.transform.rotation = thirdPersonCamera.transform.rotation;
            }
            
        }
        
    }
    
}




