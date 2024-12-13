using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StaminaScanner : MonoBehaviour
{
    private PlayerMovement playerMovement;  // reference for player
    private UIBarScript uiBarScript;   // component of the UIBar script
    
    public Image staminaBoostIcon;  // the image for the buff icon
    private Color iconColor; // color for the buff icon
    
    void Start()
    {
        playerMovement = PlayerMovement.current;
        uiBarScript = GetComponent<UIBarScript>();
        iconColor = staminaBoostIcon.color;
        iconColor.a = 0;
        staminaBoostIcon.color = iconColor;
    }
    
    
    void Update()
    {
        uiBarScript.UpdateValue(playerMovement.GetFlightStamina() / playerMovement.GetMaxAccessibleFlightStamina(),
            playerMovement.GetMaxFlightStamina() / playerMovement.GetMaxAccessibleFlightStamina());
        
        // set stamina icon alpha
        iconColor.a = playerMovement.staminaBoosting ? 1 : 0;
        staminaBoostIcon.color = iconColor;
    }
    
    
}
