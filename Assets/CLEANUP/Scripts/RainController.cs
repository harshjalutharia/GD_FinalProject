using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.ParticleSystem;

public class RainController : MonoBehaviour
{
    public static RainController current;

    [SerializeField, Tooltip("Rain particle system reference")] private ParticleSystem m_rainSystem;
    [SerializeField, Tooltip("Lightning particle system reference")] private GameObject LightningSystem;
    private ParticleSystem m_lightningSystem;
    [SerializeField, Tooltip("Emission rate over time for slow rain")] private float slowRainRate = 150f;
    [SerializeField, Tooltip("Emission rate over time for fast rain")] private float fastRainRate = 300f;
    [SerializeField, Tooltip("Fast rain angle values")] private float fastRainZMin = 8f;
    [SerializeField, Tooltip("Fast rain angle values")] private float fastRainZMax = 12f;
    [SerializeField, Tooltip("Slow rain angle values")] private float slowRainZMin = 4f;
    [SerializeField, Tooltip("Slow rain angle values")] private float slowRainZMax = 8f;

    [Header("Player Settings")]
    [SerializeField, Tooltip("Reference to the player GameObject")] private Transform player;
    [SerializeField, Tooltip("Offset from the player position")] private Vector3 offset = new Vector3(0f, 5f, 0f);


    // Start is called before the first frame update
    void Start()
    {
        current = this;
        m_rainSystem = GetComponent<ParticleSystem>();
        m_lightningSystem =  LightningSystem.GetComponent<ParticleSystem>();
        if (m_rainSystem != null)
            m_rainSystem.Stop(true);

        if (m_lightningSystem != null)
            m_lightningSystem.Stop(true);

        if (m_rainSystem == null) Debug.LogError("No Particle System set for rain");
        if (m_lightningSystem == null) Debug.LogError("No Particle System set for lightning");
    }

    void Update()
    {
        if (player != null)
        {
            // Update the position of the rain system to follow the player
            transform.position = player.position + offset;
        }
    }

    public void ToggleRain(bool enable, bool fastRain)
    {
        if (m_rainSystem == null) return;
        if (enable)
        {
            m_rainSystem.Play(true);
            var emission = m_rainSystem.emission;
            emission.rateOverTime = (fastRain ? fastRainRate : slowRainRate);

            var velocityOverLifetime = m_rainSystem.velocityOverLifetime;
            velocityOverLifetime.z = (fastRain ? new MinMaxCurve(fastRainZMin, fastRainZMax) : new MinMaxCurve(slowRainZMin, slowRainZMax));

            if (fastRain){

                m_lightningSystem.Play(true);
            }else{
                m_lightningSystem.Stop(true);
            }

        }
        else
        {
            m_rainSystem.Stop(true);
        }
    }
}
