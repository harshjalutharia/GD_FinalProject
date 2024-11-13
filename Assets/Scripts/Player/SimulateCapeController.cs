using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulateCapeController : MonoBehaviour
{
    private SimulateCloth simulateCloth;

    [Tooltip("Material of the Cape")]
    private Material capeMaterial;
    
    [Tooltip("Control how much the cape swings when the character moves")]
    public float oscillationLevel;
    
    [Tooltip("Component of PlayerMovement")]
    public PlayerMovement playerMovement;
    private Rigidbody playerRb;

    public Vector3 fixedForce = new Vector3(0, 2, -2);
    
    void Start()
    {
        playerRb = playerMovement.gameObject.GetComponent<Rigidbody>();
        simulateCloth = GetComponent<SimulateCloth>();
        capeMaterial = GetComponent<Renderer>().material;
    }

    
    void Update()
    {
        float offsetY = Mathf.Lerp(0.5f, 0, playerMovement.GetFlightStamina() / playerMovement.maxFlightStamina);
        Vector2 offset = new Vector2(0, offsetY);
        capeMaterial.SetTextureOffset("_MainTex", offset);
    }

    private void FixedUpdate()
    {
        // Let the cape float when moving
        simulateCloth.AddForce(-1 * oscillationLevel * playerRb.velocity + transform.TransformDirection(fixedForce));
    }
}
