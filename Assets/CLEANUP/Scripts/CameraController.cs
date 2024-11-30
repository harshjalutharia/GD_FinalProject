using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    public static CameraController current;

    [Header("=== Core References ===")]
    [SerializeField, Tooltip("The animator controlling the animation to show the map")]             private Animator m_playerAnimator;
    [SerializeField, Tooltip("The action input that activates/deactivates the held map toggle.")]   private InputActionReference m_actionReference;
    [SerializeField, Tooltip("The action input that corresponds to the... view?")]                  private InputActionReference m_viewReference;
    [Space]
    [SerializeField, Tooltip("The loading camera reference.")]      private Camera m_loadingCamera;
    [SerializeField, Tooltip("The 3rd-person camera reference.")]   private Camera m_thirdPersonCamera;
    [SerializeField, Tooltip("The 1st-person camera reference")]    private Camera m_firstPersonCamera;
    [Space]
    [SerializeField, Tooltip("The hand-held map that must be shown when in 1st-person")]    private GameObject m_heldMap;
    [SerializeField, Tooltip("The compass that must be shown when in 1st-person")]          private GameObject m_compass;
    
    [Header("=== First/Third Person Transition Settings ===")]
    [SerializeField, Tooltip("Do we initialize the listeners on enable by default?")]                   private bool m_enableOnStart = true;
    [SerializeField, Tooltip("The transform ref that the first person camera tries to smooth damp to")] private Transform m_firstPersonDestination;
    [SerializeField, Tooltip("How long do we want the first person smooth damp to occur?")]             private float m_firstPersonSmoothingTime = 0.15f;
    [SerializeField, Tooltip("At what distance away do we start to rotate the first person camera when entering 1st-person mode?")] private float m_firstPersonRotationStartDistance = 0.5f;
    [SerializeField, Tooltip("the speed of first person camera rotate in transit")]                     private float m_firstPersonRotationSpeed = 180f;
    [Space]
    [SerializeField, Tooltip("... Okay I dunno what this does...")]                                     private float m_verticalSensitivity = 1f;
    [SerializeField, Tooltip("Change in maximum pitch angle relative to the horizontal plane")]         private float m_firstPersonMaxPitchAngle = 30f;

    [Space]
    [Header("=== Outputs - READ ONLY ===")]
    [SerializeField, Tooltip("Is the held map input active?")]              private bool m_mapInputActive = false;
    public bool mapInputActive => m_mapInputActive;
    [SerializeField, Tooltip("Is the player holding the map currently?")]   private bool m_firstPersonCameraActive = false;
    public bool firstPersonCameraActive => m_firstPersonCameraActive;

    // Private variables - truly unrelated to othe components
    private Vector3 m_firstPersonCameraVelocity = Vector3.zero;
    private bool m_movingFirstPersonCamera = false;

    // This gets called regardless if this component itself is enabled or not. Only doesn't run if..
    // ,, the game object that this is attached to is not active.
    private void Awake() {
        current = this;

        // Make sure the held map and compass are both inactive on start
        m_heldMap.SetActive(false);
        m_compass.SetActive(false);
    }

    private void OnEnable() {
        // Upon enabling of this game object, we intentionally turn off loading camera and set the first and third player cameras to be active.
        // However, we make the first person camera disabled, though we still turn it on
        ToggleThirdPersonCamera(true, true);
        ToggleFirstPersonCamera(true, false);
        ToggleLoadingCamera(false, false);
        // Set the references. We attach listeners to each action reference
        m_mapInputActive = false;
        m_firstPersonCameraActive = false;
        m_actionReference.action.started += OpenMap;
        m_actionReference.action.canceled += CloseMap;
        m_actionReference.action.Enable();
    }

    public void OpenMap(InputAction.CallbackContext ctx) {
        // We at least indicate that the map input is... active
        m_mapInputActive = true;

        // We escape early if 1) we're not grounded, or 2) we're moving too fast (i.e. sliding)
        if (!PlayerMovement.current.GetGrounded() || PlayerMovement.current.GetSliding() || PlayerMovement.current.GetVelocity().magnitude >= 2f) return;

        // Assuming the player movement check is satisfied, we can safely proceed
        m_firstPersonCameraActive = true;

        // Set the animator to show the map
        m_playerAnimator.SetBool("holdingMap", true);
        m_heldMap.SetActive(true);
        m_compass.SetActive(true);

        // Switch the output display.
        ToggleFirstPersonCamera(true, true);
        ToggleThirdPersonCamera(true, false);
        
        // Set the main terrain fustrum to the first person camera
        FustrumManager.current.SetMainFustrumCamera(m_firstPersonCamera);
    }

    public void CloseMap(InputAction.CallbackContext ctx) {
        // Indicate that we're not holding the map
        m_mapInputActive = false;
        
        // Set the animator to hide the map
        m_playerAnimator.SetBool("holdingMap", false);

        // We don't do ANYTHING else - the FixedUpdate loop will handle this.
    }

    private void Update() {
        if (m_mapInputActive) {
            // Set vertical direction of the first person camera
            float viewInputY = m_viewReference.action.ReadValue<Vector2>().y;
            float currentPitch = m_firstPersonDestination.localEulerAngles.x - viewInputY * m_verticalSensitivity;
            currentPitch = (currentPitch < 180) 
                ? Mathf.Clamp(currentPitch, -100, m_firstPersonMaxPitchAngle)
                : Mathf.Clamp(currentPitch, 360 - m_firstPersonMaxPitchAngle, 460);
            m_firstPersonDestination.localEulerAngles = new Vector3(currentPitch, m_firstPersonDestination.localEulerAngles.y, m_firstPersonDestination.localEulerAngles.z);
        }
    }

    private void FixedUpdate() {
        // If we switched to holding the map, then we need to smooth damp the first person camear to its destination reference
        if (m_mapInputActive && m_firstPersonCameraActive) {

            // Purely for translating the 1st-person camera to the proper 1st-person position.
            m_firstPersonCamera.transform.position = Vector3.SmoothDamp(m_firstPersonCamera.transform.position, m_firstPersonDestination.position, ref m_firstPersonCameraVelocity, m_firstPersonSmoothingTime);
            
            // Calculate the distance remaining between its intended destination and its current position
            float firstPersonDistance = Vector3.Distance(m_firstPersonCamera.transform.position, m_firstPersonDestination.position); 
            
            // We don't rotate the camera until we get close enough. This creates the "zoom-in" effect first when the camera starts from the 3rd-person view.
            if (firstPersonDistance < m_firstPersonRotationStartDistance) {
                m_firstPersonCamera.transform.rotation = Quaternion.RotateTowards(m_firstPersonCamera.transform.rotation, m_firstPersonDestination.rotation, m_firstPersonRotationSpeed * Time.fixedDeltaTime);
            }

        }

        // If we switched out of holding the map, then we need to move the first person camera back to the third person camera
        // The behavior will change depending on if we completed the animation and smooth damp or not (aka m_firstPersonCameraActive)
        else if (m_firstPersonCameraActive) {
            
            // We smoothdamp and rotate to match the 3rd person camera settings.
            m_firstPersonCamera.transform.position = Vector3.SmoothDamp(m_firstPersonCamera.transform.position, m_thirdPersonCamera.transform.position, ref m_firstPersonCameraVelocity, m_firstPersonSmoothingTime);
            m_firstPersonCamera.transform.rotation = Quaternion.RotateTowards(m_firstPersonCamera.transform.rotation, m_thirdPersonCamera.transform.rotation, m_firstPersonRotationSpeed * Time.fixedDeltaTime);
            
            // If the distance is close enough, we toggle that the first person is no longer active.
            if (Vector3.Distance(m_firstPersonCamera.transform.position, m_thirdPersonCamera.transform.position) < 0.05f) {
                // Let the system know we're no longer in first person
                m_firstPersonCameraActive = false;

                // This is expected to execute once, so it's safe to do it here.
                m_heldMap.SetActive(false);
                m_compass.SetActive(false);
                ToggleThirdPersonCamera(true, true);
                ToggleFirstPersonCamera(true, false);
                FustrumManager.current.SetMainFustrumCamera(m_thirdPersonCamera);
            }
        }

        // At this point, we're not actively holding the map button, and we've completed our return from first person to third person.
        // The only thing left to do is make sure the first person caomera's position and rotation matches the third person camera exactly.
        else {
            m_firstPersonCamera.transform.position = m_thirdPersonCamera.transform.position;
            m_firstPersonCamera.transform.rotation = m_thirdPersonCamera.transform.rotation;
        }
    }

    public void ToggleLoadingCamera(bool active, bool enabled) {
        m_loadingCamera.gameObject.SetActive(active);
        m_loadingCamera.enabled = enabled;
    }
    public void ToggleThirdPersonCamera(bool active, bool enabled) {
        m_thirdPersonCamera.gameObject.SetActive(active);
        m_thirdPersonCamera.enabled = enabled;
    }
    public void ToggleFirstPersonCamera(bool active, bool enabled) {
        m_firstPersonCamera.gameObject.SetActive(active);
        m_firstPersonCamera.enabled = enabled;
    }

    private void OnDisable() {
        m_actionReference.action.started -= OpenMap;
        m_actionReference.action.canceled -= CloseMap;
        m_firstPersonCameraActive = false;
        m_mapInputActive = false;
    }
}
