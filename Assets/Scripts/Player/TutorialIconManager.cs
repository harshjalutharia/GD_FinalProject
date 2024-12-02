using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TutorialIconManager : MonoBehaviour
{
    public GameObject iconCanvas; // Reference to the Canvas object
    public Image iconImage; // Reference to the Image component
    public Sprite moveIcon; // Icon for movement tutorial
    public Sprite jumpIcon; // Icon for jump tutorial
    public Sprite mapIcon; // Icon for map tutorial

    private bool tutorialActive = false;

    private void Start()
    {
        iconCanvas.SetActive(false); // Hide the icon at the start
    }

    public void ShowMoveIcon()
    {
        iconImage.sprite = moveIcon;
        iconCanvas.SetActive(true);
        tutorialActive = true;
    }

    public void ShowJumpIcon()
    {
        iconImage.sprite = jumpIcon;
        iconCanvas.SetActive(true);
        tutorialActive = true;
    }

    public void ShowMapIcon()
    {
        iconImage.sprite = mapIcon;
        iconCanvas.SetActive(true);
        tutorialActive = true;
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
            // Make the canvas always face the camera
            iconCanvas.transform.LookAt(Camera.main.transform);
            iconCanvas.transform.Rotate(0, 180, 0); // Adjust orientation if needed
        }
    }
}

