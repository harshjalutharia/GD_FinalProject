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
        // change texture offset according to player's stamina
        // float offsetY = Mathf.Lerp(0.5f, 0, playerMovement.GetFlightStamina() / playerMovement.maxAccessibleStamina);
        float offsetY = Mathf.Lerp(0.5f, 0, Mathf.Log(playerMovement.GetFlightStamina(),playerMovement.maxAccessibleStamina));
        Vector2 offset = new Vector2(0, offsetY);
        capeMaterial.SetTextureOffset("_MainTex", offset);
    }

    private void FixedUpdate()
    {
        // Let the cape float when moving
        simulateCloth.AddForce(-1 * oscillationLevel * playerRb.velocity + transform.TransformDirection(fixedForce));
    }

    public void CapePowerUp(float totalDuration)
    {
        StartCoroutine(ChangeEmissionColor(totalDuration/2));
    }
    
    
    private IEnumerator ChangeEmissionColor(float duration)
    {
        Color startColor = Color.black;
        Color endColor = Color.white;
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            capeMaterial.SetColor("_EmissionColor", Color.Lerp(startColor, endColor, elapsedTime / duration));
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        capeMaterial.SetColor("_EmissionColor", endColor);

        elapsedTime = 0f;
        yield return new WaitForSeconds(duration);
        
        while (elapsedTime < duration)
        {
            capeMaterial.SetColor("_EmissionColor", Color.Lerp(endColor, startColor, elapsedTime / duration));
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        capeMaterial.SetColor("_EmissionColor", startColor);
    }
}
