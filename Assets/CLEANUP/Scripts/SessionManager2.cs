using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SessionManager2 : MonoBehaviour
{
    public static SessionManager2 current;

    [Header("=== Core Settings ===")]
    [SerializeField, Tooltip("Seed for randomization")] private int m_seed;
    public int seed => m_seed;
    
    public virtual void SetSeed(string newSeed) {
        if (newSeed.Length > 0 && int.TryParse(newSeed, out int validNewSeed)) {    m_seed = validNewSeed;  return; }
        m_seed = UnityEngine.Random.Range(0, 1000001);
    }
    public virtual void SetSeed(int newSeed) {  m_seed = newSeed;   }

    private void Awake() {
        current = this;    
    }

    private void Start() {
        // Get seed from SessionMemory, if SessionMemory exists
        if (SessionMemory.current != null) SetSeed(SessionMemory.current.seed);
        
        // Chck that we have the necessary generators
        if (!TryCheckGenerators()) {
            Debug.LogError("Session Manager 2: Cannot proceed without generators (Terrain Generator, Voronoi, etc.");
            return;
        }

        // Assuming we reach this far, start the terrain generation first and foremost
        TerrainManager.current.SetSeed(m_seed);
        TerrainManager.current.onGenerationEnd.AddListener(this.OnTerrainGenerated);
        TerrainManager.current.Generate();
    }

    public bool TryCheckGenerators() {
        return TerrainManager.current != null
            && Voronoi.current != null
            && VegetationGenerator2.current != null
            && GemGenerator2.current != null
            && LandmarkGenerator.current != null;
    }
    public void OnTerrainGenerated() { 
        Debug.Log("Session Manager 2: Terrain Generation Acknowledged. Starting Voronoi Generation");
        // Initialize voronoi generation
        Voronoi.current.SetSeed(m_seed);
        Voronoi.current.SetDimensions(TerrainManager.current.width, TerrainManager.current.height);
        Voronoi.current.onGenerationEnd.AddListener(this.OnVoronoiGenerated);
        Voronoi.current.Generate();
    }
    public void OnVoronoiGenerated() {
        Debug.Log("Session Manager 2: Voronoi Generation Acknowledged. Starting Vegetation Generation");
        // Initialize vegetation generation
        VegetationGenerator2.current.SetSeed(m_seed);
        VegetationGenerator2.current.SetDimensions(TerrainManager.current.width, TerrainManager.current.height);
        VegetationGenerator2.current.onGenerationEnd.AddListener(this.OnVegetationGenerated);
        VegetationGenerator2.current.Generate();

    }
    public void OnVegetationGenerated() {
        Debug.Log("Session Manager 2: Vegetation Generated");
        // Initialize gem generation
        GemGenerator2.current.SetSeed(m_seed);
        GemGenerator2.current.SetDimensions(TerrainManager.current.width, TerrainManager.current.height);
        GemGenerator2.current.onGenerationEnd.AddListener(this.OnGemsGenerated);
        GemGenerator2.current.Generate();
    }
    public void OnGemsGenerated() {
        Debug.Log("Session Manager 2: Gems Generated");
        // Initialize landmark generation
        LandmarkGenerator.current.onGenerationEnd.AddListener(this.OnLandmarksGenerated);
        LandmarkGenerator.current.GenerateLandmarksNew();
    }
    public void OnLandmarksGenerated() {
        Debug.Log("Session Manager 2: Landmarks Generated");
        // Initialize transition from loading to player.
    }
    
    public void InitializePlayer() {
        
    }

    private void OnDestroy() {
        StopAllCoroutines();
        if (TerrainManager.current != null) TerrainManager.current.onGenerationEnd.RemoveListener(this.OnTerrainGenerated);
        if (Voronoi.current != null) Voronoi.current.onGenerationEnd.RemoveListener(this.OnVoronoiGenerated);
        if (VegetationGenerator2.current != null) VegetationGenerator2.current.onGenerationEnd.RemoveListener(this.OnVegetationGenerated);
        if (GemGenerator2.current != null) GemGenerator2.current.onGenerationEnd.RemoveListener(this.OnGemsGenerated);
        if (LandmarkGenerator.current != null) LandmarkGenerator.current.onGenerationEnd.RemoveListener(this.OnLandmarksGenerated);
    }
}
