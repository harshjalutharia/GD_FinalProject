using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Extensions;

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

    [Header("=== Terrain Generation ===")]
    [SerializeField, Tooltip("The noise map that generates terrain.")]          private NoiseMap m_terrainGenerator;
    [SerializeField, Tooltip("Reference to the destination Game Object")]       private Transform m_destinationRef;
    [SerializeField, Tooltip("The landmark generator that places buildings")]   private LandmarkGenerator m_landmarkGenerator;

    [Header("=== Menus ===")]
    [SerializeField, Tooltip("The input button for pause menu")]                    private KeyCode m_pauseMenuKeyCode = KeyCode.Tab;
    [SerializeField, Tooltip("The transition time for menus to appear/disappear.")] private float m_pauseMenuTransitionTime = 1f;
    [SerializeField, Tooltip("The canvas group for the pause menu")]                private CanvasGroup m_pauseMenuGroup;
    [SerializeField, Tooltip("The canvas group for the win screen")]                private CanvasGroup m_winMenuGroup;
    [SerializeField, Tooltip("The transition time for the win screen to appear")]   private float m_winMenuTransitionTime = 1f;
    [SerializeField, Tooltip("The input button for movement debug canvas")]         private KeyCode m_movementDebugKeyCode = KeyCode.M;
    [SerializeField, Tooltip("The canvas group for the player movement debug.")]    private CanvasGroup m_movementDebugGroup;

    [Header("=== Held Map Settings ===")]
    [SerializeField, Tooltip("The held map object transform itself")]       private Transform m_heldMap;
    [SerializeField, Tooltip("Ref. point for the visible map position")]    private Transform m_heldMapVisiblePosRef;
    [SerializeField, Tooltip("Ref. point for the inviisble map position")]  private Transform m_heldMapInvisiblePosRef;
    [SerializeField, Tooltip("The key code input for holding the map")]     private KeyCode m_showMapKey = KeyCode.LeftShift;
    [SerializeField, Tooltip("The transition time for the map transition")] private float m_heldMapTransitionTime = 0.25f;
    [SerializeField, Tooltip("Is the map being shown currently?")]          private bool m_isShowingMap;
    public bool isShowingMap => m_isShowingMap;
    private Vector3 m_heldMapVelocity = Vector3.zero;

    [Header("=== Scene Transition Settings ===")]
    [SerializeField, Tooltip("Transition time to move between scenes")]             private float m_sceneTransitionTime = 2f;
    [SerializeField, Tooltip("Is the player transitioning between scenes?")]        private bool m_isSceneTransitioning = false;
    private CanvasGroup m_currentActiveCanvasGroup = null;

    private void Awake() {
        current = this;
    }

    private void Start() {
        // At the start, we expect to be able to read the seed info from SessionMemory and use that to generate the terrain
        SetSeed(SessionMemory.current.seed);

        // Initialize terrain generation        
        m_terrainGenerator.GenerateMap();

        // Hide any other menus
        ToggleCanvasGroup(m_pauseMenuGroup, false);
        ToggleCanvasGroup(m_winMenuGroup, false);
        ToggleCanvasGroup(m_movementDebugGroup, false);

        // Designate the start and end positions of the player
        Invoke(nameof(SetPlayerInitialPosition), 1f);
    }

    private void Update() {
        if (m_isSceneTransitioning) return;
        
        // Menu stuff
        if (Input.GetKeyDown(m_pauseMenuKeyCode)) OpenPauseMenu();
        if (Input.GetKeyDown(m_movementDebugKeyCode)) ToggleDebugMenu();

        // Held map stuff
        m_isShowingMap = Input.GetKey(m_showMapKey);
        Vector3 m_heldMapTarget = (m_isShowingMap) ? m_heldMapVisiblePosRef.position : m_heldMapInvisiblePosRef.position;
        m_heldMap.position = Vector3.SmoothDamp(m_heldMap.position, m_heldMapTarget, ref m_heldMapVelocity, m_heldMapTransitionTime);
        m_heldMap.gameObject.SetActive(m_isShowingMap || Vector3.Distance(m_heldMap.position, m_heldMapInvisiblePosRef.position) >= 0.1f);

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
        m_terrainGenerator.SetSeed(SessionMemory.current.seed);
        Random.InitState(newSeed);
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

    private void SetPlayerInitialPosition()
    {
        Debug.Log(m_player.transform.position);
        do {
            m_playerStartPosition = m_terrainGenerator.GetRandomPosition(false, 50);
            m_playerEndPosition = m_terrainGenerator.GetRandomPosition(false, 50);
            Debug.Log(m_playerStartPosition.ToString() + " | " + m_playerEndPosition.ToString());
        } while(Vector2.Distance(m_playerStartPosition.ToVector2(), m_playerEndPosition.ToVector2()) < 50f);

        // Teleport the player to the start postiion
        m_player.transform.position = m_playerStartPosition;
        Debug.Log(m_player.transform.position);

        // Teleport the destination ref to the destination point
        m_destinationRef.position = m_playerEndPosition;
        m_landmarkGenerator.GenerateLandmarks(m_playerEndPosition, m_terrainGenerator);

        // Let the camera fade in
        m_playerCameraFader.FadeIn();

        // Start the tracker, if exists
        if (GameTracker.current != null) GameTracker.current.StartTracking();
    }

}
