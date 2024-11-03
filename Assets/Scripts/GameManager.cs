using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager current;

    [Header("=== Start Menu ===")]
    [SerializeField, Tooltip("Start menu canvas group")]    private CanvasGroup m_startMenuGroup;
    [SerializeField, Tooltip("Ref. to Seed Input Text")]    private TMP_InputField m_startSeedInputField;
    [SerializeField, Tooltip("Start camera fader")]         private CameraFader m_startCameraFader;
    [SerializeField, Tooltip("Start transition time")]      private float m_startTransitionTime = 1f;

    [Header("=== Generation Engines ===")]
    [SerializeField, Tooltip("The user's set seed via UI")]                 private int m_userSeed;
    [SerializeField, Tooltip("Terrain Generator - usually a Combine Map")]  private NoiseMap m_terrainGenerator;

    [Header("=== UI References ===")]
    [SerializeField] private CanvasGroup m_winMenuGroup;
    [SerializeField] private CanvasGroup m_downloadGroup;
    [SerializeField] private CanvasGroup m_loseMenuGroup;
    [SerializeField] private CanvasGroup m_inGameMenuGroup;

    [Header("=== Player Avatar References ===")]
    [SerializeField] private CameraFader m_playerCameraFader;
    //  [SerializeField] private GameObject m_playerAvatar;
    //  [SerializeField] private PlayerHealthAndStamina m_playerHS;
    //  [SerializeField] private PlayerInteraction m_playerInteraction;
    //  [SerializeField] private GameTracker m_gameTracker;
    //  [SerializeField] private GameObject m_destinationRef;

    /*
    [Header("=== Generation Engines ===")]
    [SerializeField] private TerrainGenerator m_terrainGenerator;
    [SerializeField] private LightingManager m_lightingManager;

    [Header("=== Settings ===")]
    [SerializeField] private float m_minDistanceBetweenStartAndEnd = 20f;
    [SerializeField] private Vector3 m_playerStart;
    [SerializeField] private Vector3 m_playerDestination;
    [SerializeField] private bool m_isPlaying = false;
    */

    private void Awake() {
        current = this;
    }

    private void Start() {
        // Given the Session Memory, set the session seed already
        SetSeed(SessionMemory.current.seed);

        // === Start Camera ===
        m_startCameraFader.gameObject.SetActive(true);
        m_startCameraFader.FadeIn();

        // === Start Canvas ===
        // ToggleCanvasGroup(m_winMenuGroup, false);
        // ToggleCanvasGroup(m_loseMenuGroup, false);
        // ToggleCanvasGroup(m_inGameMenuGroup, false);
        StartCoroutine(ToggleCanvasGroupCoroutine(m_startMenuGroup, true, m_startTransitionTime));
        

        /* 
        m_playerAvatar.SetActive(false);
        m_playerAvatar.GetComponent<FirstPersonMovement>().TogglePlayerMovement(false);
        m_destinationRef.SetActive(false);
        m_lightingManager.UpdateLighting(10f);
        m_lightingManager.enabled = false;
        Cursor.lockState = CursorLockMode.None;
        m_isPlaying = false;
        */
    }

    public void SetSeed(string inputSeed) {
        if (inputSeed.Length > 0 && int.TryParse(inputSeed, out int parsedInt)) {
            m_userSeed = parsedInt;
            return;
        }
        RandomizeSeed();
    }
    public void SetSeed(int inputSeed) {
        m_userSeed = inputSeed;
    }
    public void RandomizeSeed() {
        m_userSeed = UnityEngine.Random.Range(0, 100000);
    }

    public void StartSession() {
        // We push everythjing into a coroutine to manage timing
        StartCoroutine(StartSessionCoroutine());

        /*
        // Set the start and end destinations for the player
        m_terrainGenerator.GetStartAndEnd(m_minDistanceBetweenStartAndEnd, out m_playerStart, out m_playerDestination);
        
        // Place the player at the start, and the destination prefab at the destination
        m_playerAvatar.transform.position = new Vector3(m_playerStart.x, m_playerStart.y+1f, m_playerStart.z);
        m_destinationRef.transform.position = m_playerDestination;

        // Initialize lighting manager
        m_lightingManager.enabled = true;

        // Disable the start menu group as well as the win and lose screens
        ToggleCanvasGroup(m_startMenuGroup, false);
        ToggleCanvasGroup(m_winMenuGroup, false);
        ToggleCanvasGroup(m_loseMenuGroup, false);
        ToggleCanvasGroup(m_inGameMenuGroup, false);

        // activate the player
        m_startCameraFader.gameObject.SetActive(false);
        m_destinationRef.SetActive(true);
        Cursor.lockState = CursorLockMode.Locked;
        m_playerAvatar.SetActive(true);
        m_playerHS.ResetHealthAndStamina();
        m_playerInteraction.ResetInGameMenuState();
        m_playerAvatar.GetComponent<FirstPersonMovement>().TogglePlayerMovement(true);
        m_gameTracker.StartTracking();
        m_isPlaying = true;
        */
    }

    public IEnumerator StartSessionCoroutine() {
        // Let the session memory system memorize the saved seed. Check that the seed is... a legit seed
        SetSeed(m_startSeedInputField.text);
        SessionMemory.current.SetSeed(m_userSeed);

        // === Fade out the start camera. Wait for the delay to occur
        m_startCameraFader.FadeOut();
        yield return ToggleCanvasGroupCoroutine(m_startMenuGroup, false, m_startTransitionTime);

        // === Move to main scene ===
        SceneManager.LoadScene(1);
    }

    /*
    // Functionally similar to StartGame, but we do not get the start and end again.
    public void ResetGame() {        
        // Place the player at the start, and the destination prefab at the destination
        m_playerAvatar.transform.position = new Vector3(m_playerStart.x, m_playerStart.y+1f, m_playerStart.z);
        m_destinationRef.transform.position = m_playerDestination;

        // Initialize lighting manager
        m_lightingManager.enabled = true;

        // Disable the start menu group as well as the win and lose screens
        ToggleCanvasGroup(m_startMenuGroup, false);
        ToggleCanvasGroup(m_winMenuGroup, false);
        ToggleCanvasGroup(m_loseMenuGroup, false);
        ToggleCanvasGroup(m_inGameMenuGroup, false);

        // activate the player
        m_startCameraFader.gameObject.SetActive(false);
        m_destinationRef.SetActive(true);
        Cursor.lockState = CursorLockMode.Locked;
        m_playerAvatar.SetActive(true);
        m_playerHS.ResetHealthAndStamina();
        m_playerInteraction.ResetInGameMenuState();
        m_playerAvatar.GetComponent<FirstPersonMovement>().TogglePlayerMovement(true);
        m_isPlaying = true;
    }

    public void ShowWinMenu() {
        if (!m_isPlaying) return;
        m_playerAvatar.GetComponent<FirstPersonMovement>().TogglePlayerMovement(false);
        m_gameTracker.StopTracking();
        bool savingSupported = WebGLFileSaver.IsSavingSupported();
        ToggleCanvasGroup(m_downloadGroup, savingSupported);
        ToggleCanvasGroup(m_winMenuGroup, true);
        Cursor.lockState = CursorLockMode.None;
    }

    public void ShowLoseScreen() {
        if (!m_isPlaying) return;
        m_playerAvatar.GetComponent<FirstPersonMovement>().TogglePlayerMovement(false);
        ToggleCanvasGroup(m_loseMenuGroup, true);
        Cursor.lockState = CursorLockMode.None;
    }

    public void ToggleInGameMenu(bool setTo) {
        if (!m_isPlaying) return;
        int alpha = setTo ? 1 : 0;
        CursorLockMode cursorMode = setTo ? CursorLockMode.None : CursorLockMode.Locked;
        m_playerAvatar.GetComponent<FirstPersonMovement>().TogglePlayerMovement(!setTo);
        ToggleCanvasGroup(m_inGameMenuGroup, setTo);
        Cursor.lockState = cursorMode;
    }
    */

    public void ToggleCanvasGroup(CanvasGroup group, bool setTo) {
        float setToFloat = setTo ? 1f : 0f;
        group.alpha = setTo ? 1f : 0f;
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
}
