using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFader : MonoBehaviour
{
    // Special credit for inspiration: https://www.youtube.com/watch?v=a66Fa7Oh7TE
    public Color fadeColor;
    public float transitionSpeed = 1f;
    public float transitionDuration => 1f/transitionSpeed;
    [SerializeField] private AnimationCurve m_fadeCurve = new AnimationCurve(
        new Keyframe(0f, 1f),
        new Keyframe(0.5f, 0.5f, -1.5f, -1.5f),
        new Keyframe(1f, 0f)
    );

    [SerializeField] private bool m_startFadeIn = true;
    public bool disableDraw = false;

    [SerializeField] private float m_alpha = 0f;
    private Texture2D m_screenTexture;
    private int direction = 0;
    private float time = 0f;
    
    private void Start() {
        // The whole fade-in/out pipeline involves working with a Texture2D
        // While we can manually set the fade out color, what we want to manipulate is the alpha value
        // If the alpha is 0, then the player can see the game
        // Otherwise, if the alpha is > 0, then there's some fade out effect

        // We need to start by creating a texture 2D
        m_screenTexture = new Texture2D(1,1);

        // The alpha is actaully first set based on if we want to start with a fade in
        // If we want start with a fade-in effect, we need to start with alpha = 1f
        if (m_startFadeIn) m_alpha = 1f;

        // We then apply the fade color with alpha to the screen texture
        m_screenTexture.SetPixel(0,0,new Color(fadeColor.r, fadeColor.g, fadeColor.b, m_alpha));
        m_screenTexture.Apply();
    
        // Once more, if we want to start with a fade-in, then we need to call `FadeIn()`1
        if (m_startFadeIn) FadeIn();
    }

    public void FadeOut() {
        // Skip if we're still fading in/out
        if (direction != 0) return;

        // We want the target alpha to be 0 now
        m_alpha = 0f;

        // We set the time to be 1
        time = 1f;
        
        // We want the direction to -1;
        direction = -1;
    }

    public void FadeIn() {
        // Skip if we're still fading in/out
        if (direction != 0) return;

        // We want the target alpha to be 1 now
        m_alpha = 1f;

        // We set the time to be 0;
        time = 0f;

        // We want the direction to be 1
        direction = 1;
    }

    public void OnGUI() {
        if (disableDraw) return;
        // This is called during any scene event
        if (m_alpha > 0f) GUI.DrawTexture(new Rect(0,0,Screen.width, Screen.height), m_screenTexture);
        
        // If the direction is in movement, we adjust
        if (direction != 0) {
            // We update time, which we'll use to evaluate the intended alpha
            time += direction * Time.deltaTime * transitionSpeed;
            // Set the alpha based on the curve
            m_alpha = m_fadeCurve.Evaluate(time);
            // Apply the texture per pixel
            m_screenTexture.SetPixel(0,0,new Color(fadeColor.r, fadeColor.g, fadeColor.b, m_alpha));
            m_screenTexture.Apply();
            // We stop once our alpha level is either 0 or 1
            if (m_alpha <= 0f || m_alpha >= 1f) direction = 0;
        }
    }
}
