using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SessionManager : MonoBehaviour
{
    [Header("=== Terrain Generation ===")]
    [SerializeField, Tooltip("The noise map that generates terrain.")]  private NoiseMap m_terrainGenerator;
    
    private void Start() {
        // At the start, we ex[ect to be able to read the seed info from SessionMemory and use that to generate the terrain
        m_terrainGenerator.SetSeed(SessionMemory.current.seed);
    }

}
