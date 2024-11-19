using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightingManager : MonoBehaviour
{
    public static LightingManager current;

    [SerializeField, Tooltip("Minimum fog value"), Range(0f,1f)] private float m_minFogDensity = 0f;
    [SerializeField, Tooltip("Maximum fog value"), Range(0f,1f)] private float m_maxFogDensity = 0.1f;
    [SerializeField, Tooltip("Total number of gems")]   private int m_numGems = 1;
    [SerializeField] private AnimationCurve m_fogCurve;
    
    private void Awake() {
        current = this;
    }

    public void Initialize(int numGems) {
        m_numGems = numGems;
    }

    public void SetFogByGem(int currentGemCount) {
        float gemRatio = (float)(currentGemCount / m_numGems);
        float fogVal = Mathf.Lerp(m_minFogDensity, m_maxFogDensity, m_fogCurve.Evaluate(gemRatio));
        RenderSettings.fogDensity = fogVal;
    }

    public void ResetFog() {
        RenderSettings.fogDensity = m_minFogDensity;
    }

}