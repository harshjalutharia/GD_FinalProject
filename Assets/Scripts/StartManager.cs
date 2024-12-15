using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class StartManager : MonoBehaviour
{
    public static StartManager current;

    [SerializeField, Tooltip("Start menu canvas group")]    private CanvasGroup m_startMenuGroup;
    [SerializeField, Tooltip("Ref. to Seed Input Text")]    private TMP_InputField m_startSeedInputField;
    [SerializeField, Tooltip("Start camera fader")]         private CameraFader m_startCameraFader;
    [SerializeField, Tooltip("Start transition time")]      private float m_startTransitionTime = 1f;
    [SerializeField, Tooltip("The user's set seed via UI")]                 private int m_userSeed;

    public void SetSeed(int inputSeed, bool setInputField = false) {
        m_userSeed = inputSeed;
        if (setInputField)  m_startSeedInputField.text = m_userSeed.ToString();
    }
    public void SetSeed(string newSeed, bool setInputField = false) {
        if (newSeed.Length > 0 && int.TryParse(newSeed, out int validNewSeed)) {   
            m_userSeed = validNewSeed;
        }
        if (setInputField)  m_startSeedInputField.text = m_userSeed.ToString();
    }
    public void RandomizeSeed() {
        m_userSeed = UnityEngine.Random.Range(0, 100000);
    }

    private void Awake() {
        current = this;
    }

    private void Start() {
        // Given the Session Memory, set the session seed already
        SetSeed(SessionMemory.current.seed, true);

        // Ensure that cursor is not locked and is visible
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // === Start Camera ===
        m_startCameraFader.gameObject.SetActive(true);
        m_startCameraFader.FadeIn();

        // === Start Canvas ===
        StartCoroutine(ToggleCanvasGroupCoroutine(m_startMenuGroup, true, m_startTransitionTime));
    }

    public void StartSession() {
        // We push everythjing into a coroutine to manage timing
        StartCoroutine(StartSessionCoroutine());
    }

    public IEnumerator StartSessionCoroutine() {
        // Let the session memory system memorize the saved seed. Check that the seed is... a legit seed
        SetSeed(m_startSeedInputField.text);
        SessionMemory.current.SetSeed(m_userSeed);

        // === Fade out the start camera. Wait for the delay to occur
        yield return ToggleCanvasGroupCoroutine(m_startMenuGroup, false, m_startTransitionTime);
        m_startCameraFader.FadeOut();
        // === Move to main scene ===
        yield return new WaitForSeconds(2f);
        SceneManager.LoadScene("TestFinalTerrain");
    }

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
