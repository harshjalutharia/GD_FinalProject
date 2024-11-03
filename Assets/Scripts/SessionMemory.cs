using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SessionMemory : MonoBehaviour
{
    public static SessionMemory current;

    [Header("=== Stored Memory ===")]
    [Tooltip("The seed integer that'll be shared across scenes")]   public int seed = -1;

    private void Awake() {
        // Prevent any new ones from appearing
        if (current != null) {
            Destroy(gameObject);
            return;
        }
        
        // Set this component as the singleton class
        current = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("Session Memory Active");
    }

    public void SetSeed(int newSeed) {
        seed = newSeed;
    }
}


