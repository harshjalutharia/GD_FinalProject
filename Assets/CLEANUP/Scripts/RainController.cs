using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.ParticleSystem;

public class RainController : MonoBehaviour
{
    public static RainController current;

    [SerializeField, Tooltip("Rain particle system reference")] private ParticleSystem m_rainSystem;
    [SerializeField, Tooltip("Emission rate over time for slow rain")] private float slowRainRate = 150f;
    [SerializeField, Tooltip("Emission rate over time for fast rain")] private float fastRainRate = 300f;
    [SerializeField, Tooltip("Fast rain angle values")] private float fastRainZMin = 8f;
    [SerializeField, Tooltip("Fast rain angle values")] private float fastRainZMax = 12f;
    [SerializeField, Tooltip("Slow rain angle values")] private float slowRainZMin = 4f;
    [SerializeField, Tooltip("Slow rain angle values")] private float slowRainZMax = 8f;

    // Start is called before the first frame update
    void Start()
    {
        current = this;
        m_rainSystem = GetComponent<ParticleSystem>();
        if (m_rainSystem != null)
            m_rainSystem.Stop(true);
    }

    public void ToggleRain(bool enable, bool fastRain = false)
    {
        if (m_rainSystem == null) return;
        if (enable)
        {
            m_rainSystem.Play(true);
            var emission = m_rainSystem.emission;
            emission.rateOverTime = (fastRain ? fastRainRate : slowRainRate);

            var velocityOverLifetime = m_rainSystem.velocityOverLifetime;
            velocityOverLifetime.z = (fastRain ? new MinMaxCurve(fastRainZMin, fastRainZMax) : new MinMaxCurve(slowRainZMin, slowRainZMax));
        }
        else
        {
            m_rainSystem.Stop(true);
        }
    }
}
