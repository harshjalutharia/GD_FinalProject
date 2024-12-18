using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;
using UnityEngine.SceneManagement;


public class SessionManager2 : MonoBehaviour
{
    public static SessionManager2 current;

    [Header("=== Core Settings ===")]
    [SerializeField, Tooltip("Seed for randomization")] private int m_seed;
    public int seed => m_seed;

    [Header("=== Player Input Action References ===")]
    [SerializeField, Tooltip("The action reference that allows the player to skip cutscenes")]  private InputActionReference m_skipCutsceneAction;
    [SerializeField, Tooltip("The action reference that allows the player to  jump")]           private InputActionReference m_playerJumpAction;
    [SerializeField, Tooltip("The action reference that allows the player to ring the bell")]   private InputActionReference m_playerRingBellAction;
    
    [Header("== Core references ===")]
    [SerializeField, Tooltip("The player character's transform")]   private Transform m_playerRef;
    public Transform playerRef => m_playerRef;
    [SerializeField, Tooltip("Camera fader ref")]   private CameraFader m_cameraFaderRef;
    [SerializeField, Tooltip("Gem Trail Prefab")]  private GemTrail m_gemTrailPrefab;

    [Header("=== Generation & Tutorial Checks ===")]
    [SerializeField, Tooltip("Has the environment finished generating?")]                   private bool m_environmentGenerated = false;
    [SerializeField, Tooltip("Has the loading cutscene finished playing?")]                 private bool m_loadingCutsceneFinished = false;
    [SerializeField, Tooltip("Has gameplay been initialized?")]                             private bool m_gameplayInitialized = false;
    public bool gameplayInitialized => m_gameplayInitialized;
    [Space]
    [SerializeField, Tooltip("Timestamp last time the player rang the bells")]                                          private float m_timeLastRung = 0f;
    [SerializeField, Tooltip("The amount of time we want to allow the player before they can ring the bells again")]    private float m_ringDelay = 3f;
    

    public virtual void SetSeed(string newSeed) {
        if (newSeed.Length > 0 && int.TryParse(newSeed, out int validNewSeed)) {    m_seed = validNewSeed;  return; }
        m_seed = UnityEngine.Random.Range(0, 1000001);
    }
    public virtual void SetSeed(int newSeed) {  m_seed = newSeed;   }

    private void Awake() {
        current = this;
        m_skipCutsceneAction.action.performed += InitializeGameplayAction;
        m_playerRingBellAction.action.performed += RingBellAction;
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
        m_playerRingBellAction.action.Disable();
        
        // Check that we have the necessary generators
        if (!TryCheckGenerators()) {
            Debug.LogError("Session Manager 2: Cannot proceed without generators (Terrain Generator, Voronoi, etc.");
            return;
        }

        // Assuming we reach this far, we can then toggle the player movement to set its boolean checks
        PlayerMovement.current.canMove = false;
        PlayerMovement.current.canJump = false;
        PlayerMovement.current.canSprint = true; // doesn't really matter if there's no other input

        // We can now start the terrain generation first and foremost
        TerrainManager.current.Generate();
    }

    public bool TryCheckGenerators() {
        // Return false if any of these generators are not present
        if (TerrainManager.current == null
            || Voronoi.current == null
            || VegetationGenerator2.current == null
            || LandmarkGenerator2.current == null
            || GemGenerator2.current == null) return false;

        // Update each generator with seed and dimensions
        // - Terrain Manager
        TerrainManager.current.SetSeed(m_seed);
        TerrainManager.current.onGenerationEnd.AddListener(this.OnTerrainGenerated);
        // - Voronoi
        Voronoi.current.SetSeed(m_seed);
        Voronoi.current.SetDimensions(TerrainManager.current.width, TerrainManager.current.height);
        Voronoi.current.onGenerationEnd.AddListener(this.OnVoronoiGenerated);
        // - Vegetation
        VegetationGenerator2.current.SetSeed(m_seed);
        VegetationGenerator2.current.SetDimensions(TerrainManager.current.width, TerrainManager.current.height);
        VegetationGenerator2.current.onGenerationEnd.AddListener(this.OnVegetationGenerated);
        // - Landmarks
        LandmarkGenerator2.current.SetSeed(m_seed);
        LandmarkGenerator2.current.onGenerationEnd.AddListener(this.OnLandmarksGenerated);
        // - Gems
        GemGenerator2.current.SetSeed(m_seed);
        GemGenerator2.current.SetDimensions(TerrainManager.current.width, TerrainManager.current.height);
        GemGenerator2.current.onGenerationEnd.AddListener(this.OnGemsGenerated);
        
        // Return that we've validated everything
        return true;
    }
    
    // Initialize voronoi generation
    public void OnTerrainGenerated() { 
        TerrainManager.current.TerminateCoroutines();
        Debug.Log("Session Manager 2: Terrain Generation Acknowledged. Starting Voronoi Generation");
        Voronoi.current.Generate();
    }
    // Initialize vegetation generation
    public void OnVoronoiGenerated() {
        Debug.Log("Session Manager 2: Voronoi Generation Acknowledged. Starting Vegetation Generation");
        // Toggle the loading screen to show that terrain generation has finished, if canvas controller exists
        if (CanvasController.current != null) CanvasController.current.ToggleLoadedIcon("Terrain");
        VegetationGenerator2.current.Generate();
    }
    // Initialize landmark generation
    public void OnVegetationGenerated() {
        VegetationGenerator2.current.TerminateCoroutines();
        Debug.Log("Session Manager 2: Vegetation Generated");
        // Toggle the loading screen to show that vegetation generation has finished, if canvas controller exists
        if (CanvasController.current != null) CanvasController.current.ToggleLoadedIcon("Trees");
        LandmarkGenerator2.current.Generate();
    }
    // Initialize gem generation
    public void OnLandmarksGenerated() {
        Debug.Log("Session Manager 2: Landmarks Generated");
        // Initialize transition from loading to skip.
        if (CanvasController.current != null) CanvasController.current.ToggleLoadingIconsGroup(false);
        GemGenerator2.current.Generate();
    }
    public void OnGemsGenerated() {
        Debug.Log("Session Manager 2: Gems Generated");
        // Toggle the loading screen to show that gem generation has finished, if canvas controller exists
        if (CanvasController.current != null) CanvasController.current.ToggleLoadedIcon("Gems");
        // At this point, we very much can check if the cutscene is finished loading or not.
        m_environmentGenerated = true;
        // Enable the end cutscene feature
        m_skipCutsceneAction.action.Enable();
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
        m_playerRingBellAction.action.Enable();

        // All canvas-related
        if (CanvasController.current != null) {
            CanvasController.current.ToggleLoadingScreen(false, false);
            CanvasController.current.ToggleGameplay(true);
        }

        // Determine the start position of the player. Determined by looking at Voronoi.current's first region major landmark's spawn
        Vector3 potentialPlayerStart = Voronoi.current.regions[0].towerLandmark.playerSpawnRef.position;
        TerrainManager.current.TryGetPointOnTerrain(potentialPlayerStart.x, potentialPlayerStart.z, out Vector3 playerStartPos, out Vector3 startNormal, out float startSteepness);
        m_playerRef.position = playerStartPos;

        // Tell TerrainManager to toggle LOD mode
        TerrainManager.current.ToggleLODMethod(TerrainManager.LODMethod.Grid);
        
        // Activate the player and camera
        if (m_cameraFaderRef != null) {
            m_cameraFaderRef.enabled = true;
            m_cameraFaderRef.FadeIn();
        }
        StartCoroutine(PlayerMovement.current.ActivatePlayer());
        ThirdPersonCam.current.transform.position = playerStartPos;

        // If tutorial exists, Tell them to start ring bell tutorial
        if (TutorialIconManager.current != null) {
            TutorialIconManager.current.ShowDeviceUI();
            TutorialIconManager.current.InitializeRingTutorial();
        }
        
        // If cutscene manager exists, tell it to initialize ending camera
        if (CutsceneManager.current != null) CutsceneManager.current.InitializeEndingCameraPosition();
    }

    public void CollectGem(Gem gem) {
        // Indicate we got the gem, turn it off
        Debug.Log($"Collected Gem Acknowledged\nGem Type: {gem.gemType.ToString()}\nRegion Index: {gem.regionIndex}");
        gem.gameObject.SetActive(false);

        // If voronoi regions is not null, then we proceed with updating that region's counter
        if (Voronoi.current != null) {
            // What region is this gem in?
            Region region = Voronoi.current.regions[gem.regionIndex];
            Debug.Log($"Detected Region: {region.attributes.name}");

            // Initialize the gem trailer to go back to the destination
            // The trail will make the gem "go back" to the primary landmark of this region
            if (m_gemTrailPrefab != null) {
                GemTrail gemTrail = Instantiate(m_gemTrailPrefab, m_playerRef.position, Quaternion.identity) as GemTrail;
                gemTrail.onLandmarkDestinationReached.AddListener(DestinationGemTrailReached);
                gemTrail.SetTrailGradient(region.attributes.gradient);
                gemTrail.InitializeAsGem(gem, region.towerLandmark);
            }

            // Depending on the gem type, we do different things
            if (gem.gemType == Gem.GemType.Destination) {
                // The region has collected its destination gem
                region.UpdateDestinationGem(gem);

                // Toggle the destination gem icon to TRUE as a result
                if (CanvasController.current != null) {
                    CanvasController.current.ToggleDestinationGemIcon(true);
                    CanvasController.current.ToggleGameplay(false);
                }
                
                // If this is a destination gem, then let's make the player see it return
                if (m_gemTrailPrefab != null) region.towerLandmark.ToggleShoulderCamera(true);
                if (LandmarkGenerator2.current != null) {
                    foreach (var landmark in LandmarkGenerator2.current.landmarks) {
                        if (landmark.regionIndex == gem.regionIndex) landmark.TogglePathTrails(true);
                    }
                }
                Debug.Log("Destination gem for this region now collected");
            }
            else {
                // This is a small gem
                region.UpdateSmallGem(gem);
                Debug.Log("Small gem for this region successfully collected");
            }
            
        }
    }
    
    public void DestinationGemTrailReached(Gem gem, Landmark destination) {
        StartCoroutine(DestinationGemTrailReachedCoroutine(gem, destination));
    }
    public IEnumerator DestinationGemTrailReachedCoroutine(Gem gem, Landmark destination) {
        // Which region are we in?
        Region region = Voronoi.current.regions[destination.regionIndex];
        // Has this region regained its destination gem yet? If so, ring it.
        if (gem.gemType == Gem.GemType.Destination && PlayerMovement.current.largeGemCollected < 4) {
            destination.PlayAudioSource();
            foreach(Gem g in region.smallGems) g.RingGem();
        }
        yield return new WaitForSeconds(2f);
        if (gem.gemType == Gem.GemType.Destination) {
            SkyboxController.current.TimeChangeAuto();  // change the time of the day
        }
        if (PlayerMovement.current.largeGemCollected < 4 && CanvasController.current != null) CanvasController.current.ToggleGameplay(true);
        destination.ToggleShoulderCamera(false);
        
        // show the dialogue
        int collected = PlayerMovement.current.largeGemCollected;
        if (gem.gemType == Gem.GemType.Destination && collected <= 3)
        {
            SpeechBubbleController.current.ShowText(SpeechBubbleController.current.textTemplates[collected].text);
        }
        
    }

    public void RingBellAction(InputAction.CallbackContext ctx) { RingBell(); }
    public void RingBell() {
        // Can't do anything if Voronoi isn't set
        if (Voronoi.current == null) return;

        // What's the player's current region? Tracked by Voronoi
        Region region = Voronoi.current.playerRegion;
        if (region == null) return;

        // Record the time.. but not before checking if we're even allowed to again
        if (Time.time - m_timeLastRung < m_ringDelay) return;
        m_timeLastRung = Time.time;

        // Has this region had its destination bell collected?
        if (region.destinationCollected) {
            // Collected. We ring the bell and highlight all the small gems
            region.towerLandmark.PlayAudioSource();
            foreach(Gem gem in region.smallGems) gem.RingGem();
        } else {
            // Not collected. We instead, we highlight just the gem. We do NOT ring the bell.
            region.destinationGem.RingGem();
        }
    }

    public void EndGameplay() {
        // This should be called when the player finally gets all four destination gems
        // Firstly, we make the cutscene controller show the loading screen again.
        // this time, we tell it to show the end cutscene, which should wrap up the game.
        // Disable all other movement options
        PlayerMovement.current.canMove = false;
        PlayerMovement.current.canJump = false;
        PlayerMovement.current.canSprint = false;
        m_playerJumpAction.action.Disable();
        m_playerRingBellAction.action.Disable();

        if (CanvasController.current != null) {
            CanvasController.current.ToggleGameplay(false);
            CanvasController.current.ToggleEnding(true, true);
        }
        if (CutsceneManager.current != null) CutsceneManager.current.PlayEndingCutscene();
    }

    public void ReturnToStart() {
        SceneManager.LoadScene("Start");
    }
    public void ResumeGame() {
        PlayerMovement.current.canMove = true;
        PlayerMovement.current.canJump = true;
        PlayerMovement.current.canSprint = true;
        m_playerJumpAction.action.Enable();
        m_playerRingBellAction.action.Enable();
        if (CanvasController.current != null) {
            CanvasController.current.ToggleGameplay(true);
            CanvasController.current.ToggleEnding(false, false);
        }
        if (CutsceneManager.current != null) CutsceneManager.current.DeactivateEndingCamera();
    }

    private void OnDestroy() {
        StopAllCoroutines();
        // Remove listeners
        TerrainManager.current.onGenerationEnd.RemoveListener(this.OnTerrainGenerated);
        Voronoi.current.onGenerationEnd.RemoveListener(this.OnVoronoiGenerated);
        VegetationGenerator2.current.onGenerationEnd.RemoveListener(this.OnVegetationGenerated);
        LandmarkGenerator2.current.onGenerationEnd.RemoveListener(this.OnLandmarksGenerated);
        GemGenerator2.current.onGenerationEnd.RemoveListener(this.OnGemsGenerated);
        // ---
        m_skipCutsceneAction.action.performed -= InitializeGameplayAction;
        m_playerRingBellAction.action.performed -= RingBellAction;
    }
}
