using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StaminaScanner : MonoBehaviour
{
    private PlayerMovement playerMovement;  // reference for player
    private UIBarScript uiBarScript;   // component of the UIBar script
    
    void Start()
    {
        playerMovement = PlayerMovement.current;
        uiBarScript = GetComponent<UIBarScript>();
    }
    
    
    void Update()
    {
        uiBarScript.UpdateValue(playerMovement.GetFlightStamina() / playerMovement.GetMaxAccessibleFlightStamina(),
            playerMovement.GetMaxFlightStamina() / playerMovement.GetMaxAccessibleFlightStamina());
    }
}
