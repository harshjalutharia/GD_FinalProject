using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GemBeamDebugController : MonoBehaviour
{
    [Header("Particle System Reference")]
    [SerializeField] private ParticleSystem particleSystem; // Reference to the particle system

    private bool isParticleActive = false; // Track the particle system state

    private void Start() {
        if (particleSystem == null) Debug.LogError("ParticleSystem is not assigned in the Inspector!");
        particleSystem.Stop(); // Stop the particle system
    }

    void Update() {
        if (Input.GetKeyDown(KeyCode.P)) ToggleParticleSystem();
    }

    private void ToggleParticleSystem() {
        if (particleSystem != null) {
            particleSystem.Play(); 
            StartCoroutine(StopParticle());
        }
    }

    private IEnumerator StopParticle(){
        yield return new WaitForSeconds(3f);
        particleSystem.Stop(); 
    }
}
