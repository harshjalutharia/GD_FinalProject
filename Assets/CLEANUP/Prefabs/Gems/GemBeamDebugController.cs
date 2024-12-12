using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GemBeamDebugController : MonoBehaviour
{
    [Header("Particle System Reference")]
    [SerializeField] private ParticleSystem particleSystem; // Reference to the particle system

    private bool isParticleActive = false; // Track the particle system state

    void Start()
    {
        if (particleSystem == null)
        {
            Debug.LogError("ParticleSystem is not assigned in the Inspector!");
        }
        particleSystem.Stop(); // Stop the particle system
    }

    void Update()
    {
        // Check for P key press
        if (Input.GetKeyDown(KeyCode.P))
        {
            ToggleParticleSystem();
        }
    }

    private void ToggleParticleSystem()
    {
        if (particleSystem != null)
        {
            isParticleActive = !isParticleActive; // Toggle the state
            if (isParticleActive)
            {
                particleSystem.Play(); // Start the particle system
            }
            else
            {
                particleSystem.Stop(); // Stop the particle system
            }
        }
    }
}
