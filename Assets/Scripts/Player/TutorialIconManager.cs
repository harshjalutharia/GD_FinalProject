using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class TutorialIconManager : MonoBehaviour
{
    public GameObject iconCanvas; // Reference to the Canvas object
    public Image iconImage; // Reference to the Image component
    public Sprite movekbIcon; // Icon for movement tutorial
    public Sprite movexbIcon;
    public Sprite movepsIcon;
    public Sprite jumpkbIcon; // Icon for jump tutorial
    public Sprite jumpxbIcon;
    public Sprite jumppsIcon;
    public Sprite sprintkbIcon;
    public Sprite sprintxbIcon;
    public Sprite sprintpsIcon;
    public Sprite mapkbIcon;

    private string currentInput = "Keyboard";
    private string lastDeviceUsed = "Keyboard"; // Default to keyboard at start, or leave blank
    [SerializeField]
    private InputActionAsset inputActionAsset;

    private bool tutorialActive = false;

    private void Start()
    {
        iconCanvas.SetActive(false); // Hide the icon at the start
    }


    private void DoTestAction(InputAction.CallbackContext ctx)
    {
        string deviceName = ctx.action.activeControl.device.name;
        //Debug.Log("Device used: " + deviceName);
        lastDeviceUsed = deviceName;
    }

    public string checkInput()
    {
        // Check the last device that triggered an input event
        // We�ll simplify and look for keywords that identify the device type.
        if (lastDeviceUsed.Contains("Keyboard") || lastDeviceUsed.Contains("Mouse"))
        {
            return "Keyboard";
        }
        else if (lastDeviceUsed.Contains("XInput") || lastDeviceUsed.Contains("XInputControllerWindows"))
        {
            return "Xbox";
        }
        else if (lastDeviceUsed.Contains("DualShock") || lastDeviceUsed.Contains("DualSense") || lastDeviceUsed.Contains("Wireless Controller"))
        {
            return "PlayStation";
        }

        // If none of the above, you can return a generic "Unknown"
        return "Unknown";
    }

    public void ShowMoveIcon()
    {

        if (currentInput == "Xbox")
        {
            iconImage.sprite = movexbIcon;
        }
        else if (currentInput == "PlayStation")
        {
            iconImage.sprite = movepsIcon;
        }
        else
        {
            iconImage.sprite = movekbIcon;
        }
        
        iconCanvas.SetActive(true);
        tutorialActive = true;
    }

    public void ShowSprintIcon()
    {
        if (currentInput == "Xbox")
        {
            iconImage.sprite = sprintxbIcon;
        }
        else if (currentInput == "PlayStation")
        {
            iconImage.sprite = sprintpsIcon;
        }
        else
        {
            iconImage.sprite = sprintkbIcon;
        }
        iconCanvas.SetActive(true);
        tutorialActive = true;
    }

    public void ShowJumpIcon()
    {
        if (currentInput == "Xbox")
        {
            iconImage.sprite = jumpxbIcon;
        }
        else if (currentInput == "PlayStation")
        {
            iconImage.sprite = jumppsIcon;
        }
        else
        {
            iconImage.sprite = jumpkbIcon;
        }
        iconCanvas.SetActive(true);
        tutorialActive = true;
    }

    public void ShowMapIcon()
    {
        return;
        /*
        if (currentInput == "Keyboard")
        {
            iconImage.sprite = mapkbIcon;
        }
        else if (currentInput == "Xbox")
        {
            // Show Xbox-related icons or hints
        }
        else if (currentInput == "PlayStation")
        {
            // Show PlayStation-related icons or hints
        }
        
        iconCanvas.SetActive(true);
        tutorialActive = true;
        */
    }

    public void HideIcon()
    {
        iconCanvas.SetActive(false);
        tutorialActive = false;
    }

    private void Update()
    {
        if (tutorialActive)
        {
            inputActionAsset.Enable();
            inputActionAsset.FindAction("Move").performed += DoTestAction;
            currentInput = checkInput();
            // Make the canvas always face the camera
            iconCanvas.transform.LookAt(Camera.main.transform);
            iconCanvas.transform.Rotate(0, 180, 0); // Adjust orientation if needed
        }
    }
}

