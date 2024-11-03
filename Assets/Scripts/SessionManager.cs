using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SessionManager : MonoBehaviour
{
    [Header("=== Player Settings ===")]
    [SerializeField, Tooltip("Player camera")]  private CameraFader m_playerCameraFader;

    [Header("=== Terrain Generation ===")]
    [SerializeField, Tooltip("The noise map that generates terrain.")]  private NoiseMap m_terrainGenerator;

    [Header("=== Menus ===")]
    [SerializeField, Tooltip("The input button for pause menu")]                    private KeyCode m_pauseMenuKeyCode = KeyCode.Tab;
    [SerializeField, Tooltip("The transition time for menus to appear/disappear.")] private float m_pauseMenuTransitionTime = 1f;
    [SerializeField, Tooltip("The canvas group for the pause menu")]                private CanvasGroup m_pauseMenuGroup;
    [SerializeField, Tooltip("Is the pause menu open?")]                            private bool m_pauseMenuOpen = false;
    [SerializeField, Tooltip("The input button for movement debug canvas")]         private KeyCode m_movementDebugKeyCode = KeyCode.M;
    [SerializeField, Tooltip("The canvas group for the player movement debug.")]    private CanvasGroup m_movementDebugGroup;

    [Header("=== Scene Transition Settings ===")]
    [SerializeField, Tooltip("Transition time to move between scenes")]             private float m_sceneTransitionTime = 2f;
    [SerializeField, Tooltip("Is the player transitioning between scenes?")]        private bool m_isSceneTransitioning = false;

    private void Start() {
        // At the start, we ex[ect to be able to read the seed info from SessionMemory and use that to generate the terrain
        m_terrainGenerator.SetSeed(SessionMemory.current.seed);
        m_terrainGenerator.GenerateMap();

        // Hide any other menus
        ToggleCanvasGroup(m_pauseMenuGroup, false);
        m_pauseMenuOpen = false;
        ToggleCanvasGroup(m_movementDebugGroup, false);

        // Let the camera fade in
        m_playerCameraFader.FadeIn();
    }

    private void Update() {
        if (m_isSceneTransitioning) return;
        if (Input.GetKeyDown(m_pauseMenuKeyCode)) OpenPauseMenu();
        if (Input.GetKeyDown(m_movementDebugKeyCode)) ToggleDebugMenu();
    }

    public void OpenPauseMenu() {   
        if (m_pauseMenuOpen) return;
        Debug.Log("Opening Pause Menu");
        m_pauseMenuOpen = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        StartCoroutine(ToggleCanvasGroupCoroutine(m_pauseMenuGroup, true, m_pauseMenuTransitionTime));
    }
    public void ClosePauseMenu() {  
        if (!m_pauseMenuOpen) return;
        Debug.Log("Closing Pause Menu");
        m_pauseMenuOpen = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        StartCoroutine(ToggleCanvasGroupCoroutine(m_pauseMenuGroup, false, m_pauseMenuTransitionTime));  
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
        SetCanvasGroupInteractable(m_pauseMenuGroup, false);
        ToggleCanvasGroup(m_movementDebugGroup, false);
        yield return ToggleCanvasGroupCoroutine(m_pauseMenuGroup, false, m_sceneTransitionTime);

        // Migrate to start
        SceneManager.LoadScene(0);
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

}