using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

public class CutsceneManager : MonoBehaviour
{
    public static CutsceneManager current;

    [Header("=== Cutscenes ===")]
    [SerializeField, Tooltip("The cutscene elements for the loading scene")] private Cutscene m_loadingCutscene;

    [Header("=== Settings ===")]
    [SerializeField, Tooltip("The Image UI element that the cutscene slides are presented on")] private Image m_cutsceneImage;
    [SerializeField, Tooltip("The first, second, and third TextMeshProUGUI textboxes")]  private TextMeshProUGUI[] m_textboxes;

    [Header("=== Outputs - READ ONLY ===")]
    [SerializeField, Tooltip("Current cutscene's slide index")] private int m_currentSlideIndex = 0;
    [SerializeField, Tooltip("Is there a cutscene playing?")]   private bool m_playing = false;
    public bool playing => m_playing;

    public UnityEvent onFinishedCutscene;

    private void Awake() {
        current = this;
    }

    public void PlayLoadingCutscene() {
        StartCoroutine(PlaySlideshowCoroutine(m_loadingCutscene));
    }

    private IEnumerator PlaySlideshowCoroutine(Cutscene cutscene) {
        // Initialize the boolean that indicates that the slideshow has started
        m_playing = true;

        // Initialize the slideshow image alpha to 0
        Color color = m_cutsceneImage.color;
        color.a = 0f;
        m_cutsceneImage.color = color;

        if (cutscene.slide != null) {
            m_cutsceneImage.sprite = cutscene.slide.slide;
            // Fade in the image
            yield return FadeImage(m_cutsceneImage, 0f, 1f, cutscene.slide.transitionTime);
            // Wait for display time
            yield return new WaitForSeconds(cutscene.slide.displayTime);
        }

        for(int i = 0; i < Mathf.Min(cutscene.texts.Count, m_textboxes.Length); i++) {
            SlideText t = cutscene.texts[i];
            TextMeshProUGUI textbox = m_textboxes[i];
            textbox.text = t.text;
            // Fade in the textbox
            yield return FadeTextbox(textbox, 0f, 1f, t.transitionTime);
            // wait for display time
            yield return new WaitForSeconds(t.displayTime);
        }
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
