using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightningFlash : MonoBehaviour
{
    private Light m_Light; // Reference to the Light component

    public float minInterval = 2.0f; // Minimum time between flashes (adjust for less frequent flashes)
    public float maxInterval = 8.0f; // Maximum time between flashes (adjust for less frequent flashes)
    public float flashDuration = 0.2f; // Duration of each flash

    // Start is called before the first frame update
    void Start()
    {
        // Initialize the Light component
        m_Light = GetComponent<Light>();
        if (m_Light == null)
        {
            Debug.LogError("No Light component found on this GameObject.");
            return;
        }

        
    }


    public void StartRandomLightning(){
        // Start the lightning coroutine
        StartCoroutine(RandomLightning());

    }
    // Coroutine for handling random lightning flashes
    private IEnumerator RandomLightning()
    {
        while (true) // Loop indefinitely for random flashes
        {
            float waitTime = Random.Range(minInterval, maxInterval); // Random interval
            yield return new WaitForSeconds(waitTime); // Wait for the interval

            // Flash the light
            m_Light.enabled = true;
            yield return new WaitForSeconds(flashDuration); // Keep the light on for the flash duration
            m_Light.enabled = false;
        }
    }
}
