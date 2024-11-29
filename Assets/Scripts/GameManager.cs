using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
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

    [Header("=== Slideshow Settings ===")]
    [SerializeField, Tooltip("Slideshow canvas group")] private CanvasGroup m_slideshowCanvasGroup;
    [SerializeField, Tooltip("Image component for displaying slides")] private Image m_slideshowImage;
    [SerializeField, Tooltip("List of slides to display")] private List<Sprite> m_slides;
    [SerializeField, Tooltip("Time to display each slide")] private float m_slideDisplayTime = 2f;
    [SerializeField, Tooltip("Transition time between slides")] private float m_slideTransitionTime = 1f;


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
        if (m_userSeed == 0) {
            RandomizeSeed();
        }
    }

    private void Start() {
        // Given the Session Memory, set the session seed already
        SetSeed(SessionMemory.current.seed, true);

        // === Start Camera ===
        m_startCameraFader.gameObject.SetActive(true);
        m_startCameraFader.FadeIn();

        // === Start Canvas ===
        // ToggleCanvasGroup(m_winMenuGroup, false);
        // ToggleCanvasGroup(m_loseMenuGroup, false);
        // ToggleCanvasGroup(m_inGameMenuGroup, false);
        ToggleCanvasGroup(m_slideshowCanvasGroup, false);
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

    public void SetSeed(string inputSeed, bool setInputField = false) {
        if (inputSeed.Length > 0 && int.TryParse(inputSeed, out int parsedInt)) {
            m_userSeed = parsedInt;
            if (setInputField)  m_startSeedInputField.text = inputSeed;
        }
        else {
            RandomizeSeed();
            if (setInputField)  m_startSeedInputField.text = "";
        }
        m_terrainGenerator.SetSeed(m_userSeed);
       
    }
    public void SetSeed(int inputSeed, bool setInputField = false) {
        m_userSeed = inputSeed;
        if (setInputField)  m_startSeedInputField.text = m_userSeed.ToString();
        m_terrainGenerator.SetSeed(m_userSeed);
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

    private IEnumerator PlaySlideshowCoroutine()
    {
        // Activate the slideshow canvas group
        ToggleCanvasGroup(m_slideshowCanvasGroup, true);

        // Initialize the slideshow image alpha to 0
        Color color = m_slideshowImage.color;
        color.a = 0f;
        m_slideshowImage.color = color;

        // For each slide
        for (int i = 0; i < m_slides.Count; i++)
        {
            // Set the sprite
            m_slideshowImage.sprite = m_slides[i];

            // Fade in the image
            yield return StartCoroutine(FadeImage(m_slideshowImage, 0f, 1f, m_slideTransitionTime));

            // Wait for display time
            yield return new WaitForSeconds(m_slideDisplayTime);

            // Fade out the image
            yield return StartCoroutine(FadeImage(m_slideshowImage, 1f, 0f, m_slideTransitionTime));
        }

        // Deactivate the slideshow canvas group
        ToggleCanvasGroup(m_slideshowCanvasGroup, false);

        // After slideshow is complete, proceed to load the next scene
        SceneManager.LoadScene(1);
    }

    private IEnumerator FadeImage(Image image, float startAlpha, float endAlpha, float duration)
    {
        float time = 0f;
        Color color = image.color;
        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            float alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            color.a = alpha;
            image.color = color;
            yield return null;
        }
        color.a = endAlpha;
        image.color = color;
    }



    public IEnumerator StartSessionCoroutine() {
        // Let the session memory system memorize the saved seed. Check that the seed is... a legit seed
        SetSeed(m_startSeedInputField.text);
        SessionMemory.current.SetSeed(m_userSeed);

        // === Fade out the start camera. Wait for the delay to occur
        yield return ToggleCanvasGroupCoroutine(m_startMenuGroup, false, m_startTransitionTime);
        yield return StartCoroutine(PlaySlideshowCoroutine());
        m_startCameraFader.FadeOut();
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
