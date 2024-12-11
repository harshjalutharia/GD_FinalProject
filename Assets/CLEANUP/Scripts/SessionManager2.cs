using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class SessionManager2 : MonoBehaviour
{
    public static SessionManager2 current;

    [Header("=== Core Settings ===")]
    [SerializeField, Tooltip("Seed for randomization")] private int m_seed;
    public int seed => m_seed;

    [Header("=== Player Input Action References ===")]
    [SerializeField, Tooltip("The action reference that allows the player to skip cutscenes")]  private InputActionReference m_skipCutsceneAction;
    [SerializeField, Tooltip("The action reference that allows the player to  jump")]           private InputActionReference m_playerJumpAction;
    
    [Header("== Core references ===")]
    [SerializeField, Tooltip("The player character's transform")]   private Transform m_playerRef;
    public Transform playerRef => m_playerRef;

    [Header("=== Checks ===")]
    [SerializeField, Tooltip("Has the environment finished generating?")]   private bool m_environmentGenerated = false;
    [SerializeField, Tooltip("Has the loading cutscene finished playing?")] private bool m_loadingCutsceneFinished = false;
    [SerializeField, Tooltip("Has gameplay been initialized?")]  private bool m_gameplayInitialized = false;

    public virtual void SetSeed(string newSeed) {
        if (newSeed.Length > 0 && int.TryParse(newSeed, out int validNewSeed)) {    m_seed = validNewSeed;  return; }
        m_seed = UnityEngine.Random.Range(0, 1000001);
    }
    public virtual void SetSeed(int newSeed) {  m_seed = newSeed;   }

    private void Awake() {
        current = this;
        m_skipCutsceneAction.action.performed += InitializeGameplayAction;
    }

    private void Start() {
        // Get seed from SessionMemory, if SessionMemory exists
        if (SessionMemory.current != null) SetSeed(SessionMemory.current.seed);

        // Make sure to show the loading screen first.
        if (CanvasController.current != null) CanvasController.current.ToggleLoadingScreen(true, true);
        if (CutsceneManager.current != null) {
            CutsceneManager.current.onFinishedCutscene.AddListener(OnLoadingCutsceneFinished);
            CutsceneManager.current.PlayLoadingCutscene();
        }

        // Disable key actions
        m_skipCutsceneAction.action.Disable();
        m_playerJumpAction.action.Disable();
        
        // Chck that we have the necessary generators
        if (!TryCheckGenerators()) {
            Debug.LogError("Session Manager 2: Cannot proceed without generators (Terrain Generator, Voronoi, etc.");
            return;
        }

        //Assuming we reach this far, we can now start the terrain generation first and foremost
        TerrainManager.current.SetSeed(m_seed);
        TerrainManager.current.onGenerationEnd.AddListener(this.OnTerrainGenerated);
        TerrainManager.current.Generate();
    }

    public bool TryCheckGenerators() {
        return TerrainManager.current != null
            && Voronoi.current != null
            && VegetationGenerator2.current != null
            && GemGenerator2.current != null
            && LandmarkGenerator2.current != null;
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
        // Toggle the loading screen to show that terrain generation has finished, if canvas controller exists
        if (CanvasController.current != null) CanvasController.current.ToggleLoadedIcon("Terrain");
        // Initialize vegetation generation
        VegetationGenerator2.current.SetSeed(m_seed);
        VegetationGenerator2.current.SetDimensions(TerrainManager.current.width, TerrainManager.current.height);
        VegetationGenerator2.current.onGenerationEnd.AddListener(this.OnVegetationGenerated);
        VegetationGenerator2.current.Generate();

    }
    public void OnVegetationGenerated() {
        Debug.Log("Session Manager 2: Vegetation Generated");
        // Toggle the loading screen to show that vegetation generation has finished, if canvas controller exists
        if (CanvasController.current != null) CanvasController.current.ToggleLoadedIcon("Trees");
        // Initialize gem generation
        GemGenerator2.current.SetSeed(m_seed);
        GemGenerator2.current.SetDimensions(TerrainManager.current.width, TerrainManager.current.height);
        GemGenerator2.current.onGenerationEnd.AddListener(this.OnGemsGenerated);
        GemGenerator2.current.Generate();
    }
    public void OnGemsGenerated() {
        Debug.Log("Session Manager 2: Gems Generated");
        // Toggle the loading screen to show that gem generation has finished, if canvas controller exists
        if (CanvasController.current != null) CanvasController.current.ToggleLoadedIcon("Gems");
        // Initialize landmark generation
        LandmarkGenerator2.current.SetSeed(m_seed);
        LandmarkGenerator2.current.onGenerationEnd.AddListener(this.OnLandmarksGenerated);
        LandmarkGenerator2.current.Generate();
    }
    public void OnLandmarksGenerated() {
        Debug.Log("Session Manager 2: Landmarks Generated");
        // Initialize transition from loading to skip.
        if (CanvasController.current != null) CanvasController.current.ToggleLoadingIconsGroup(false);
        m_environmentGenerated = true;

        // At this point, we very much can check if the cutscene is finished loading or not.
        if (!m_gameplayInitialized) {
            // If not finished, enabled the skip cutscene feature
            m_skipCutsceneAction.action.Enable();
        }
    }

    public void OnLoadingCutsceneFinished() {
        // Indicate that we finished playing the cutscene
        if (CutsceneManager.current != null) CutsceneManager.current.onFinishedCutscene.RemoveListener(OnLoadingCutsceneFinished);
        m_loadingCutsceneFinished = true;
    }
    
    private void InitializeGameplayAction(InputAction.CallbackContext ctx) { InitializeGameplay(); }
    public void InitializeGameplay() {
        // Let the system know that we've initialized gameplay
        m_gameplayInitialized = true;

        // Disable the input action to prevent double-clicking
        m_skipCutsceneAction.action.Disable();
        m_playerJumpAction.action.Enable();

        // All canvas-related
        if (CanvasController.current != null) {
            CanvasController.current.ToggleLoadingScreen(false, false);
            CanvasController.current.ToggleStamina(true);
        }
    }

    private void OnDestroy() {
        StopAllCoroutines();
        // All managers - remove listeners
        if (TerrainManager.current != null) TerrainManager.current.onGenerationEnd.RemoveListener(this.OnTerrainGenerated);
        if (Voronoi.current != null) Voronoi.current.onGenerationEnd.RemoveListener(this.OnVoronoiGenerated);
        if (VegetationGenerator2.current != null) VegetationGenerator2.current.onGenerationEnd.RemoveListener(this.OnVegetationGenerated);
        if (GemGenerator2.current != null) GemGenerator2.current.onGenerationEnd.RemoveListener(this.OnGemsGenerated);
        if (LandmarkGenerator.current != null) LandmarkGenerator.current.onGenerationEnd.RemoveListener(this.OnLandmarksGenerated);
        // input actions - remove listeners
        m_skipCutsceneAction.action.performed -= InitializeGameplayAction;
    }
}
