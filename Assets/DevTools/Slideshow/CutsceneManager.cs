using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class CutsceneManager : MonoBehaviour
{
    public static CutsceneManager current;

    [Header("=== Cutscenes ===")]
    [SerializeField, Tooltip("The cutscene elements for the loading scene")] private Cutscene m_loadingCutscene;

    [Header("=== Settings ===")]
    [SerializeField, Tooltip("The Image UI element that the cutscene slides are presented on")] private Image m_cutsceneImage;
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

        // For each slide
        for (int i = 0; i < cutscene.slides.Count; i++) {
            // Set the sprite
            m_cutsceneImage.sprite = cutscene.slides[i];

            // Fade in the image
            yield return StartCoroutine(FadeImage(m_cutsceneImage, 0f, 1f, cutscene.slideTransitionTime));

            // Wait for display time
            yield return new WaitForSeconds(cutscene.slideDisplayTime);

            // Fade out the image
            yield return StartCoroutine(FadeImage(m_cutsceneImage, 1f, 0f, cutscene.slideTransitionTime));
        }

        // After finishing the slideshow:
        m_playing = false;
        
        // For any events that are attached as listeners, call them.
        onFinishedCutscene?.Invoke();
    }

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

    private void OnDisable() {
        StopAllCoroutines();
    }
}
