using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Extensions;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

public class SessionManager : MonoBehaviour
{
    public static SessionManager current;

    [Header("=== Player Settings ===")]
    [SerializeField, Tooltip("The player themselves")]          private GameObject m_player;
    [SerializeField, Tooltip("Player camera")]                  private CameraFader m_playerCameraFader;
    [SerializeField, Tooltip("The player's start position")]    private Vector3 m_playerStartPosition = Vector3.zero;
    [SerializeField, Tooltip("The player's end position")]      private Vector3 m_playerEndPosition = Vector3.zero;
    public Vector3 playerStartPosition => m_playerStartPosition;
    public Vector3 playerEndPosition => m_playerEndPosition;

    [Header("=== Necessary Generators/Maps/Managers ===")]
    [SerializeField, Tooltip("The noise map that generates terrain.")]          private NoiseMap m_terrainGenerator;
    [SerializeField, Tooltip("Specific refrence to the voronoi map used")]      private VoronoiMap m_voronoiMap;

    [Header("=== Optional Generators/Maps/Managers ===")]
    [SerializeField, Tooltip("The Fustrum Manager that manages fustrum chunks")]                private FustrumManager m_fustrumManager;
    [SerializeField, Tooltip("The landmark generator that places buildings")]                   private LandmarkGenerator m_landmarkGenerator;
    [SerializeField, Tooltip("The vegetation generator to place trees")]                        private VegetationGenerator m_vegetationGenerator;
    [SerializeField, Tooltip("The rock generator to place trees")]                              private VegetationGenerator m_rockGenerator;
    [SerializeField, Tooltip("The grass generator to place trees")]                             private VegetationGenerator m_flowerGenerator;
    [SerializeField, Tooltip("The grass generator to place grass")]                             private VegetationGenerator m_grassGenerator;
    [SerializeField, Tooltip("The grass generator to place grass locally around the player")]   private GrassGenerator m_localGrassGenerator;
    [SerializeField, Tooltip("The gem generator to place gems")]                                private GemGenerator m_gemGenerator;

    [Header("=== Menus ===")]
    [SerializeField, Tooltip("The camera that acts as the load screen cam, due to cam fader")]  private Camera m_loadCamera;
    [SerializeField, Tooltip("The main camera for the main gameplay")]                          private Camera m_mainCamera;
    [SerializeField, Tooltip("The first person camera")]                                        private Camera m_firstPersonCamera;
    [SerializeField, Tooltip("The gem camera")]                                                 private Camera m_gemCamera;
    [Space]
    [SerializeField, Tooltip("The canvas group for the initial loading menu")]                  private CanvasGroup m_loadingMenuGroup;
    [SerializeField, Tooltip("The transition time for menus to appear/disappear.")] private float m_pauseMenuTransitionTime = 1f;
    [SerializeField, Tooltip("The canvas group for the pause menu")]                private CanvasGroup m_pauseMenuGroup;
    [SerializeField, Tooltip("The canvas group for the win screen")]                private CanvasGroup m_winMenuGroup;
    [SerializeField, Tooltip("The transition time for the win screen to appear")]   private float m_winMenuTransitionTime = 1f;
    [SerializeField, Tooltip("The canvas group for the player movement debug.")]    private CanvasGroup m_movementDebugGroup;
    private InputAction m_movementDebugInput;
    private InputAction m_pauseMenuInput;

    [Header("=== Held Map Settings ===")]
    [SerializeField, Tooltip("The held map object transform itself")]       private Transform m_heldMap;
    [SerializeField, Tooltip("Ref. point for the visible map position")]    private Transform m_heldMapVisiblePosRef;
    [SerializeField, Tooltip("Ref. point for the inviisble map position")]  private Transform m_heldMapInvisiblePosRef;
    [SerializeField, Tooltip("The transition time for the map transition")] private float m_heldMapTransitionTime = 0.25f;
    [SerializeField, Tooltip("Is the map being shown currently?")]          private bool m_isShowingMap;
    public bool isShowingMap => m_isShowingMap;
    private InputAction m_showMapInput;
    private Vector3 m_heldMapVelocity = Vector3.zero;

    [Header("=== Scene Transition Settings ===")]
    [SerializeField, Tooltip("Transition time to move between scenes")]             private float m_sceneTransitionTime = 2f;
    [SerializeField, Tooltip("Is the player transitioning between scenes?")]        private bool m_isSceneTransitioning = false;
    private CanvasGroup m_currentActiveCanvasGroup = null;

    private void Awake() {
        current = this;
        var controls = InputManager.Instance.controls;
        m_pauseMenuInput = controls.Player.Menu;
        m_showMapInput = controls.Player.Map;
        m_movementDebugInput = controls.Debug.DebugUI;
    }

    private void Start() {
        // At the start, we expect to be able to read the seed info from SessionMemory. We then apply that seed to ALL randomization engines.
        // SetSeed() will do this for us automatically.
        SetSeed(SessionMemory.current.seed);

        // Show the loading menu
        ToggleCanvasGroup(m_loadingMenuGroup, true);
        m_loadCamera.gameObject.SetActive(true);
        m_mainCamera.gameObject.SetActive(false);
        m_firstPersonCamera.gameObject.SetActive(false);
        m_gemCamera.gameObject.SetActive(false);

        // Hide any other menus
        ToggleCanvasGroup(m_pauseMenuGroup, false);
        ToggleCanvasGroup(m_winMenuGroup, false);
        ToggleCanvasGroup(m_movementDebugGroup, false);
        
        // Initialize terrain generation
        m_terrainGenerator.GenerateMap();

        // Designate the start and end positions of the player.
        // The start and end positions rely on a representative voronoi map to let us know which regions are safe.
        GenerateStartAndEnd(
            m_terrainGenerator, m_voronoiMap, 
            150f, 20, 
            out m_playerStartPosition, out int playerStartPositionIndex, 
            out m_playerEndPosition, out int playerEndPositionIndex
        );

        // Teleport the player to the start postiion
        m_player.transform.position = m_playerStartPosition;

        // After generating the terrain, we can now initiate a bunch of other managers, generators, etc.
        if (m_fustrumManager != null)       m_fustrumManager.Initialize();                                                                                  // Fustrum culling
        if (m_landmarkGenerator != null)    m_landmarkGenerator.GenerateLandmarks(m_playerEndPosition, m_playerStartPosition, m_terrainGenerator);          // Landmark generation
        if (m_vegetationGenerator != null)  m_vegetationGenerator.GenerateVegetation();                                                                     // Vegetation generation
        if (m_rockGenerator != null)        m_rockGenerator.GenerateVegetation();                                                                           // Rock generation
        if (m_flowerGenerator != null)      m_flowerGenerator.GenerateVegetation();                                                                         // Flower generation
        if (m_grassGenerator != null)       m_grassGenerator.GenerateVegetation();                                                                          // Grass generation
        if (m_localGrassGenerator != null)  m_localGrassGenerator.Initialize();                                                                             // Local grass generation
        // Gem generation
        if (m_gemGenerator != null) {
            m_gemGenerator.GenerateSmallGems();
            m_gemGenerator.GenerateDestinationGem(m_playerEndPosition);
        }
        // Game tracker
        if (GameTracker.current != null)    GameTracker.current.StartTracking();     

        StartCoroutine(m_player.GetComponent<PlayerMovement>().ActivatePlayer());        
        Invoke(nameof(InitializePlayerView), 5f);
    }

    private void InitializePlayerView() {
        // Hide the loading menu
        ToggleCanvasGroup(m_loadingMenuGroup, false);
        m_loadCamera.gameObject.SetActive(false);
        m_mainCamera.gameObject.SetActive(true);
        m_firstPersonCamera.gameObject.SetActive(true);
        m_gemCamera.gameObject.SetActive(true);

        // Let the camera fade in
        m_playerCameraFader.FadeIn();                

        // If a gem generator exists, toggle the view cam
        if (m_gemGenerator != null) m_gemGenerator.ToggleViewCheck(true);
    }

    private void Update() {
        if (m_isSceneTransitioning) return;
        
        // Menu stuff
        //if (Input.GetKeyDown(m_pauseMenuKeyCode)) OpenPauseMenu();
        //if (Input.GetKeyDown(m_movementDebugKeyCode)) ToggleDebugMenu();
        if (m_pauseMenuInput.WasPressedThisFrame()) OpenPauseMenu();
        if (m_movementDebugInput.WasPressedThisFrame()) ToggleDebugMenu();
        
        // Held map stuff =====> now moved to PlayerMovement.cs
        //m_isShowingMap = Input.GetKey(m_showMapKey);
        //m_isShowingMap = m_showMapInput.IsPressed();
        //Vector3 m_heldMapTarget = (m_isShowingMap) ? m_heldMapVisiblePosRef.position : m_heldMapInvisiblePosRef.position;
        //m_heldMap.position = Vector3.SmoothDamp(m_heldMap.position, m_heldMapTarget, ref m_heldMapVelocity, m_heldMapTransitionTime);
        //m_heldMap.gameObject.SetActive(m_isShowingMap || Vector3.Distance(m_heldMap.position, m_heldMapInvisiblePosRef.position) >= 0.1f);


    }

    public void SetSeed(string newSeed) {
        int validNewSeed;
        if (newSeed.Length > 0 && int.TryParse(newSeed, out validNewSeed)) {
            SetSeed(validNewSeed);
            return;
        }
        SetSeed(Random.Range(0, 1000001));
    }
    public void SetSeed(int newSeed) {
        Random.InitState(newSeed);
        m_terrainGenerator.SetSeed(newSeed);
        if (m_vegetationGenerator != null)  m_vegetationGenerator.SetSeed(newSeed);
        if (m_rockGenerator != null)        m_rockGenerator.SetSeed(newSeed);
        if (m_flowerGenerator != null)      m_flowerGenerator.SetSeed(newSeed);
        if (m_localGrassGenerator != null)  m_localGrassGenerator.SetSeed(newSeed);
        if (m_grassGenerator != null)       m_grassGenerator.SetSeed(newSeed);
        if (m_gemGenerator != null)         m_gemGenerator.SetSeed(newSeed);
    }

    public void OpenPauseMenu() {   
        if (m_currentActiveCanvasGroup == m_pauseMenuGroup) return;
        Debug.Log("Opening Pause Menu");
        m_currentActiveCanvasGroup = m_pauseMenuGroup;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        StartCoroutine(ToggleCanvasGroupCoroutine(m_pauseMenuGroup, true, m_pauseMenuTransitionTime));
    }
    public void ClosePauseMenu() {  
        if (m_currentActiveCanvasGroup != m_pauseMenuGroup) return;
        Debug.Log("Closing Pause Menu");
        m_currentActiveCanvasGroup = null;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        StartCoroutine(ToggleCanvasGroupCoroutine(m_pauseMenuGroup, false, m_pauseMenuTransitionTime));  
    }

    public void OpenWinMenu() {
        Debug.Log("Openning win menu");
        m_currentActiveCanvasGroup = m_winMenuGroup;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (GameTracker.current != null) GameTracker.current.StopTracking();
        StartCoroutine(ToggleCanvasGroupCoroutine(m_winMenuGroup, true, m_winMenuTransitionTime));
    }

    public void ToggleDebugMenu() {
        if (m_movementDebugGroup.alpha > 0.5f)  SetCanvasGroupAlpha(m_movementDebugGroup, 0f);
        else                                    SetCanvasGroupAlpha(m_movementDebugGroup, 1f);
    }

    public void ReturnToStart() {   StartCoroutine(ReturnToStartCoroutine());   }
    private IEnumerator ReturnToStartCoroutine() {
        // Fade out the player and their controls
        m_isSceneTransitioning = true;
        m_playerCameraFader.FadeOut();
        
        // Fade out menues
        if (m_currentActiveCanvasGroup != null) {
            SetCanvasGroupInteractable(m_currentActiveCanvasGroup, false);
            yield return ToggleCanvasGroupCoroutine(m_currentActiveCanvasGroup, false, m_sceneTransitionTime);
        }
        yield return null;
        ToggleCanvasGroup(m_pauseMenuGroup, false);
        ToggleCanvasGroup(m_winMenuGroup, false);
        ToggleCanvasGroup(m_movementDebugGroup, false);

        // Migrate to start
        SceneManager.LoadScene(0);
    }

    public void SaveGameSession() {
        if (GameTracker.current != null) GameTracker.current.SaveUserData();
    }

    public void ToggleCanvasGroup(CanvasGroup group, bool setTo) {
        float setToFloat = setTo ? 1f : 0f;
        group.alpha = setTo ? 1f : 0f;
        group.interactable = setTo;
        group.blocksRaycasts = setTo;
    }
    public void SetCanvasGroupAlpha(CanvasGroup group, float setTo) {
        group.alpha = setTo;
    }
    public void SetCanvasGroupInteractable(CanvasGroup group, bool setTo) {
        group.interactable = setTo;
        group.blocksRaycasts = setTo;
    }
    private IEnumerator ToggleCanvasGroupCoroutine(CanvasGroup group, bool setTo, float transitionTime) {
        float endAlpha = 1f, startAlpha = 0f, timePassed = 0f;
        if (setTo) {
            startAlpha = 0f;
            endAlpha = 1f;
        } else {
            startAlpha = 1f;
            endAlpha = 0f;
        }
        
        while(timePassed/transitionTime < 1f) {
            timePassed += Time.deltaTime;
            group.alpha = Mathf.Lerp(startAlpha, endAlpha, timePassed/transitionTime);
            yield return null;
        }

        group.interactable = setTo;
        group.blocksRaycasts = setTo;
    }

    private void GenerateStartAndEnd(NoiseMap terrainMap, VoronoiMap vMap, float minDistance, int numAttempts, out Vector3 startPos, out int startIndex, out Vector3 endPos, out int endIndex) {
        VoronoiMap.VoronoiSegment[] segments = vMap.voronoiSegments;
        Vector2Int[] centroids = vMap.voronoiCentroids;

        startIndex = 0;
        endIndex = segments.Length-1;
        int curAttempts = 0;
        while(curAttempts < numAttempts) {
            startIndex = vMap.GetRandomSegmentIndex(true);
            endIndex = vMap.GetRandomSegmentIndex(true);
            if (segments[startIndex].isBorder || segments[endIndex].isBorder) {
                curAttempts += 1;
                continue;
            }
            Vector2Int startCentroid = centroids[startIndex];
            Vector2Int endCentroid = centroids[endIndex];
            if (Vector2Int.Distance(startCentroid, endCentroid) >= minDistance) break;
            curAttempts += 1;
        }
        Vector3 vStartPos = vMap.GetRandomPositionInSegment(startIndex, true, 100);
        Vector3 vEndPos = vMap.GetRandomPositionInSegment(endIndex, true, 100);
        float startHeight = terrainMap.QueryHeightAtWorldPos(vStartPos.x, vStartPos.z, out int startX, out int startY);
        float endHeight = terrainMap.QueryHeightAtWorldPos(vEndPos.x, vEndPos.z, out int endX, out int endY);
        startPos = new Vector3(vStartPos.x, startHeight, vStartPos.z);
        endPos = new Vector3(vEndPos.x, endHeight, vEndPos.z);
    }

}
