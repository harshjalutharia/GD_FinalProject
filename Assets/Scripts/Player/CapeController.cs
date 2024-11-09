using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CapeController : MonoBehaviour
{
    [Tooltip("Cape Object")] 
    public GameObject cape;
    
    [Tooltip("Component of Cloth")]
    private Cloth capeCloth; // Cloth Component 

    [Tooltip("Material of the Cape")]
    private Material capeMaterial;
    
    [Tooltip("Control how much the cape swings when the character moves")]
    public float oscillationLevel;

    [Tooltip("Component of PlayerMovement")]
    private PlayerMovement playerMovement;
    private Rigidbody rb;

    private static Vector3 fixedForce = new Vector3(0, 2, -2);
    
    void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();
        rb = GetComponent<Rigidbody>();
        capeCloth = cape.GetComponent<Cloth>();
        capeMaterial = cape.GetComponent<Renderer>().material;
        capeCloth.enabled = false;
        StartCoroutine(DelayEnableCloth());
    }

    
    void Update()
    {
        // Let the cape float when moving
        capeCloth.externalAcceleration = -1 * oscillationLevel * rb.velocity + cape.transform.TransformDirection(fixedForce);;

        float offsetY = Mathf.Lerp(0.5f, 0, playerMovement.GetFlightStamina() / playerMovement.maxFlightStamina);
        Vector2 offset = new Vector2(0, offsetY);
        capeMaterial.SetTextureOffset("_MainTex", offset);
        
    }

    IEnumerator DelayEnableCloth()
    {
        yield return new WaitForSeconds(1f);
        capeCloth.enabled = true;
    }
}
