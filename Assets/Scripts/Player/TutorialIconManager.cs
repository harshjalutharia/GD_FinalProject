using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class TutorialIconManager : MonoBehaviour
{
    public static TutorialIconManager current;
    public enum Device { Keyboard, XBox, Playstation, Unknown }

    public GameObject iconCanvas; // Reference to the Canvas object
    public Image iconImage;       // Reference to the Image component

    [Header("=== Input Actions ===")]
    [SerializeField, Tooltip("Skip Cutscene Action")]   private InputActionReference m_skipCutsceneAction;
    [SerializeField, Tooltip("Movement Action")]    private InputActionReference m_movementAction;
    [SerializeField, Tooltip("Jump Action")]        private InputActionReference m_jumpAction;
    [SerializeField, Tooltip("Sprint Action")]      private InputActionReference m_sprintAction;
    [SerializeField, Tooltip("Ring Bell Action")]   private InputActionReference m_ringBellAction;
    [SerializeField, Tooltip("Boost Action")]       private InputActionReference m_boostAction;

    [Header("=== Outputs - READ ONLY ===")]
    [SerializeField, Tooltip("Which device are we currently using?")]   private Device m_currentDevice = Device.Unknown;
    [SerializeField, Tooltip("Ring tutorial needs to be completed before player can move")] private bool m_ringTutorialCompleted = false;
    public bool ringTutorialCompleted => m_ringTutorialCompleted;
    [SerializeField, Tooltip("Boost tutorial needs to be completed")] private bool m_boostTutorialCompleted = false;
    public bool boostTutorialCompleted => m_boostTutorialCompleted;
    
    // All the icons we might show
    public Sprite movekbIcon, movexbIcon, movepsIcon;
    public Sprite jumpkbIcon, jumpxbIcon, jumppsIcon;
    public Sprite ringkbIcon, ringxbIcon, ringpsIcon;
    public Sprite boostkbIcon, boostxbIcon, boostpsIcon;

    private void Awake() {
        current = this;
    }

    private void OnEnable() {
        // Upon being enabled, we tie the Get Device function to all inputs.
        m_skipCutsceneAction.action.performed += GetDeviceAction;
        m_movementAction.action.performed += GetDeviceAction;
        m_jumpAction.action.performed += GetDeviceAction;
        m_sprintAction.action.performed += GetDeviceAction;
        m_ringBellAction.action.performed += GetDeviceAction;
        m_boostAction.action.performed += GetDeviceAction;
    }

    private void OnDisable() {
        // Upon being disabled, we remove all device detection actions from input actions
        m_skipCutsceneAction.action.performed -= GetDeviceAction;
        m_movementAction.action.performed -= GetDeviceAction;
        m_jumpAction.action.performed -= GetDeviceAction;
        m_sprintAction.action.performed -= GetDeviceAction;
        m_ringBellAction.action.performed -= GetDeviceAction;
        m_ringBellAction.action.performed -= OnRingCompleted;
        m_boostAction.action.performed -= GetDeviceAction;
        m_boostAction.action.performed -= OnBoostCompleted;
    }

    public void InitializeRingTutorial() {
        // This tutorial is comprised of two separate subtutorials: one to ring the bell, and one for movement
        // For starters, let's make the ring bell icons appear first
        ShowRingIcon();
        // Then, we attach a listener function `OnRingCompleted` so that we can then do the next thing afterwards
        m_ringBellAction.action.performed += OnRingCompleted;
        
    }
    // Part of ring bell tutorial.
    public void OnRingCompleted(InputAction.CallbackContext ctx) {
        // Remove this as a listener
        m_ringBellAction.action.performed -= OnRingCompleted;
        // Show the movement icon
        ShowMoveIcon();
        // Let Player Movement know they can move
        if (PlayerMovement.current != null) {
            PlayerMovement.current.canMove = true;
            PlayerMovement.current.canJump = true;
            PlayerMovement.current.canSprint = true;
        }
        // Boolean check
        m_ringTutorialCompleted = true;
        SpeechBubbleController.current.ShowText(SpeechBubbleController.current.textTemplates[8].text);
    }

    public void InitializeBoostTutorial() {
        // This tutorial is comprised of a single subtutorial: boosting along the ground
        // Let's make the boost icon appear
        ShowBoostIcon();
        // Then, we attach a listener function `OnBoostCompleted` so that we can then do the next thing afterwards
        m_boostAction.action.performed += OnBoostCompleted;
    }
    // Part of the boost tutorial
    public void OnBoostCompleted(InputAction.CallbackContext ctx) {
        // remove this as a listener
        m_boostAction.action.performed -= OnBoostCompleted;
        // Boolean check
        m_boostTutorialCompleted = true;
    }

    
    /*
    private void Start() {
        iconCanvas.SetActive(false); // Hide the icon at the start
    }
    */

    private void GetDeviceAction(InputAction.CallbackContext ctx) {
        string deviceName = ctx.action.activeControl.device.name;
        Device device;
        if (deviceName.Contains("DualShock") || deviceName.Contains("DualSense") || deviceName.Contains("Wireless Controller")) {
            device = Device.Playstation;
            if (device != m_currentDevice && CanvasController.current != null) CanvasController.current.TogglePlaystation();
        }
        else if (deviceName.Contains("XInput")) {
            device = Device.XBox;
            if (device != m_currentDevice && CanvasController.current != null) CanvasController.current.ToggleXBox();
        } else {
            // Default - Keyboard and Mouse
            device = Device.Keyboard;
            if (device != m_currentDevice && CanvasController.current != null) CanvasController.current.ToggleKeyboard();
        }
        // Finally, set the device to be the current device
        m_currentDevice = device;
    }

    /*
    private void DoTestAction(InputAction.CallbackContext ctx) {
        string deviceName = ctx.action.activeControl.device.name;
        lastDeviceUsed = deviceName;
    }

    public string checkInput() {
        if (lastDeviceUsed.Contains("Keyboard") || lastDeviceUsed.Contains("Mouse")) {
            return "Keyboard";
        }
        else if (lastDeviceUsed.Contains("XInput")) {
            return "Xbox";
        }
        else if (lastDeviceUsed.Contains("DualShock") || lastDeviceUsed.Contains("DualSense") || lastDeviceUsed.Contains("Wireless Controller")) {
            return "PlayStation";
        }
        return "Unknown";
    }
    */

    // ====== Existing Show/Hide Methods ======
    public void ShowRingIcon() {
        /*
        SelectRingSprite();
        iconCanvas.SetActive(true);
        tutorialActive = true;
        */
        if (CanvasController.current != null) CanvasController.current.ToggleRingBellIcons(true);
    }
    public void ShowMoveIcon() {
        //SelectMoveSprite();
        //Debug.Log("showing move icon");
        //iconCanvas.SetActive(true);
        //tutorialActive = true;
        if (CanvasController.current != null) CanvasController.current.ToggleMovementIcons(true);
    }
    public void ShowBoostIcon() {
        //SelectBoostSprite();
        //iconCanvas.SetActive(true);
        //tutorialActive = true;
        if (CanvasController.current != null) CanvasController.current.ToggleBoostIcons(true);
    }
    public void ShowJumpIcon() {
        //SelectJumpSprite();
        //iconCanvas.SetActive(true);
        //tutorialActive = true;
        if (CanvasController.current != null) CanvasController.current.ToggleBoostIcons(true);
    }
    public void HideIcon() {
        //iconCanvas.SetActive(false);
        //tutorialActive = false;
    }

    // ====== Example Coroutines ======
    // A generic coroutine that: show icon, wait until tutorialComplete is true, hide icon
    public IEnumerator ShowIconUntilCondition(System.Action showMethod, System.Func<bool> condition) {
        // 1. Show the icon
        showMethod.Invoke();
        // 2. Wait until the condition is met
        while (!condition())
        {
            yield return null;
        }
        // 3. Hide the icon
        HideIcon();
    }

    // Similarly for Move, Jump, Boost, etc., but reusing ShowIconUntilCondition is simpler.

    // ====== Helper Methods to pick the right sprite based on input ======
    /*
    private void SelectRingSprite()
    {
        if (currentInput == "Xbox") iconImage.sprite = ringxbIcon;
        else if (currentInput == "PlayStation") iconImage.sprite = ringpsIcon;
        else iconImage.sprite = ringkbIcon;
    }
    private void SelectMoveSprite()
    {
        if (currentInput == "Xbox") iconImage.sprite = movexbIcon;
        else if (currentInput == "PlayStation") iconImage.sprite = movepsIcon;
        else iconImage.sprite = movekbIcon;
    }
    private void SelectBoostSprite()
    {
        if (currentInput == "Xbox") iconImage.sprite = boostxbIcon;
        else if (currentInput == "PlayStation") iconImage.sprite = boostpsIcon;
        else iconImage.sprite = boostkbIcon;
    }
    private void SelectJumpSprite()
    {
        if (currentInput == "Xbox") iconImage.sprite = jumpxbIcon;
        else if (currentInput == "PlayStation") iconImage.sprite = jumppsIcon;
        else iconImage.sprite = jumpkbIcon;
    }

    // Example map icon is commented out, but you can replicate similarly
    public void ShowMapIcon()
    {
        return;
    }
    */

    /*
    // ====== Existing Update method ======
    private void Update()
    {
        if (tutorialActive) {
            //currentInput = checkInput();

            // Make the canvas always face the camera
            iconCanvas.transform.LookAt(Camera.main.transform);
            iconCanvas.transform.Rotate(0, 180, 0);
        }
    }
    */
}