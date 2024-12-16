using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using Cinemachine;

public class CutsceneManager : MonoBehaviour
{
    public static CutsceneManager current;

    [Header("=== Loading Cutscene ===")]
    [SerializeField, Tooltip("The cutscene elements for the loading scene")] private Cutscene m_loadingCutscene;
    [SerializeField, Tooltip("The Image UI element that the cutscene slides are presented on")] private Image m_cutsceneImage;
    [SerializeField, Tooltip("The first, second, and third TextMeshProUGUI textboxes")]  private TextMeshProUGUI[] m_textboxes;

    [Header("=== Ending Cutscene ===")]
    [SerializeField, Tooltip("The cutscene elements for the ending screen")] private Cutscene m_endingCutscene;
    [SerializeField, Tooltip("The Image UI element that the cutscene slides are presented on")] private Image m_endingCutsceneImage;
    [SerializeField, Tooltip("The first, second, and third TextMeshProUGUI textboxes")]  private TextMeshProUGUI[] m_endingCutsceneTextboxes;
    [SerializeField, Tooltip("The cinemachine virtual camera that is used for the ending cutscene")]    private CinemachineVirtualCamera m_endingVirtualCamera;
    [SerializeField, Tooltip("The canvas group to show the final button groups")]   private CanvasGroup m_endingInteractableGroup;

    [Header("=== Outputs - READ ONLY ===")]
    [SerializeField, Tooltip("Current cutscene's slide index")] private int m_currentSlideIndex = 0;
    [SerializeField, Tooltip("Is there a cutscene playing?")]   private bool m_playing = false;
    public bool playing => m_playing;

    public UnityEvent onFinishedCutscene;

    private void Awake() {
        current = this;
    }

    public void InitializeEndingCameraPosition() {
        Vector3 camPosition = new Vector3(50f,100f,50f);
        Vector3 toLookAt = new Vector3(25f, 50f, 25f);
        if (TerrainManager.current != null) {
            camPosition = new Vector3(50f, TerrainManager.current.noiseRange.max, 50f);
            toLookAt = TerrainManager.current.worldCenter;
        }
        m_endingVirtualCamera.transform.position = camPosition;
        m_endingVirtualCamera.transform.LookAt(toLookAt);
    }
    public void DeactivateEndingCamera() {
        m_endingVirtualCamera.m_Priority = 0;
    }

    public void PlayLoadingCutscene() {
        StartCoroutine(PlaySlideshowCoroutine(m_loadingCutscene, m_cutsceneImage, m_textboxes));
    }

    public void PlayEndingCutscene() {
        m_endingVirtualCamera.m_Priority = 20;
        StartCoroutine(PlaySlideshowCoroutine(m_endingCutscene, m_endingCutsceneImage, m_endingCutsceneTextboxes, m_endingInteractableGroup));
    }

    private IEnumerator PlaySlideshowCoroutine(Cutscene cutscene, Image image, TextMeshProUGUI[] textboxes, CanvasGroup group = null) {
        // Initialize the boolean that indicates that the slideshow has started
        m_playing = true;

        // Initialize the slideshow image alpha to 0
        if (image != null && cutscene.slide != null) {
            Color color = image.color;
            color.a = 0f;
            image.color = color;

            image.sprite = cutscene.slide.slide;
            // Fade in the image
            yield return FadeImage(image, 0f, 1f, cutscene.slide.transitionTime);
            // Wait for display time
            yield return new WaitForSeconds(cutscene.slide.displayTime);
        }

        for(int i = 0; i < Mathf.Min(cutscene.texts.Count, textboxes.Length); i++) {
            SlideText t = cutscene.texts[i];
            TextMeshProUGUI textbox = textboxes[i];
            textbox.text = t.text;
            // Fade in the textbox
            yield return FadeTextbox(textbox, 0f, 1f, t.transitionTime);
            // wait for display time
            yield return new WaitForSeconds(t.displayTime);
        }

        if (group != null) {
            yield return FadeGroup(group, 0f, 1f, 2f);
        }

        m_playing = false;
        onFinishedCutscene?.Invoke();
    }

    /*
    private IEnumerator PlaySlideshowCoroutine(Cutscene cutscene) {
        // Initialize the boolean that indicates that the slideshow has started
        m_playing = true;

        // Initialize the slideshow image alpha to 0
        Color color = m_cutsceneImage.color;
        color.a = 0f;
        m_cutsceneImage.color = color;

        // For each slide
        for (int i = 0; i < cutscene.slides.Count; i++) {
            // Set the sprite
            m_cutsceneImage.sprite = cutscene.slides[i].slide;

            // Fade in the image
            yield return StartCoroutine(FadeImage(m_cutsceneImage, 0f, 1f, cutscene.slides[i].transitionTime));

            // Wait for display time
            yield return new WaitForSeconds(cutscene.slides[i].displayTime);

            // Fade out the image
            yield return StartCoroutine(FadeImage(m_cutsceneImage, 1f, 0f, cutscene.slides[i].transitionTime));
        }

        // After finishing the slideshow:
        m_playing = false;
        
        // For any events that are attached as listeners, call them.
        onFinishedCutscene?.Invoke();
    }
    */

    private static IEnumerator FadeImage(Image image, float startAlpha, float endAlpha, float duration) {
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

    private static IEnumerator FadeGroup(CanvasGroup group, float startAlpha, float endAlpha, float duration) {
        float time = 0f;
        while (time < duration) {
            time += Time.deltaTime;
            float t = time / duration;
            float alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            group.alpha = alpha;
            yield return null;
        }
        group.alpha = endAlpha;
    }

    private static IEnumerator FadeTextbox(TextMeshProUGUI textbox, float startAlpha, float endAlpha, float duration) {
        float time = 0f;
        while (time < duration) {
            time += Time.deltaTime;
            float t = time / duration;
            float alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            textbox.alpha = alpha;
            yield return null;
        }
        textbox.alpha = endAlpha;
    }

    private void OnDisable() {
        StopAllCoroutines();
    }
}
