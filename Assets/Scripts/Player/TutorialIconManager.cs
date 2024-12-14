using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class TutorialIconManager : MonoBehaviour
{
    public static TutorialIconManager current;

    public GameObject iconCanvas; // Reference to the Canvas object
    public Image iconImage;       // Reference to the Image component

    // All the icons we might show
    public Sprite movekbIcon, movexbIcon, movepsIcon;
    public Sprite jumpkbIcon, jumpxbIcon, jumppsIcon;
    public Sprite ringkbIcon, ringxbIcon, ringpsIcon;
    public Sprite boostkbIcon, boostxbIcon, boostpsIcon;
    public Sprite mapkbIcon;

    private string currentInput = "Keyboard";
    private string lastDeviceUsed = "Keyboard";
    [SerializeField] private InputActionAsset inputActionAsset;

    private bool tutorialActive = false;

    private void Awake() {
        current = this;
    }

    private void Start() {
        iconCanvas.SetActive(false); // Hide the icon at the start
    }

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

    // ====== Existing Show/Hide Methods ======
    public void ShowRingIcon() {
        SelectRingSprite();
        iconCanvas.SetActive(true);
        tutorialActive = true;
    }
    public void ShowMoveIcon() {
        SelectMoveSprite();
        Debug.Log("showing move icon");
        iconCanvas.SetActive(true);
        tutorialActive = true;
    }
    public void ShowBoostIcon() {
        SelectBoostSprite();
        iconCanvas.SetActive(true);
        tutorialActive = true;
    }
    public void ShowJumpIcon() {
        SelectJumpSprite();
        iconCanvas.SetActive(true);
        tutorialActive = true;
    }
    public void HideIcon() {
        iconCanvas.SetActive(false);
        tutorialActive = false;
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

    // ====== Existing Update method ======
    private void Update()
    {
        if (tutorialActive)
        {
            // inputActionAsset.Enable();
            inputActionAsset.FindAction("Move").performed += DoTestAction;
            currentInput = checkInput();

            // Make the canvas always face the camera
            iconCanvas.transform.LookAt(Camera.main.transform);
            iconCanvas.transform.Rotate(0, 180, 0);
        }
    }
}
