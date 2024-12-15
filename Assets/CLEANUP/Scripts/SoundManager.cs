using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager current;

    [System.Serializable]
    public class SoundClip {
        public string name;
        public AudioSource audioSource;
    }
    
    [Header("=== Sound Effects ===")]
    [SerializeField, Tooltip("Ref. to the BGM Audio sources for morning, evening and twilight")] private List<AudioSource> m_BGMAudioSources;
    [SerializeField, Tooltip("BGM fade out/in duration")] private float m_BGMFadeDuration = 5f;
    [SerializeField, Tooltip("Ref. to slow rain audio source")] private AudioSource m_slowRainAudioSource;
    [SerializeField, Tooltip("Ref. to fast rain audio source")] private AudioSource m_fastRainAudioSource;
    [SerializeField, Tooltip("Ref. to thunder audio source")] private AudioSource m_thunderAudioSource;
    [SerializeField, Tooltip("Thunder gap duration")] private float m_thunderGap = 20f;
    [SerializeField, Tooltip("Thunder gap randomness")] private float m_thunderGapRandomness = 5f;
    [SerializeField, Tooltip("Audio clips for thunder sfx")] private List<AudioClip> m_thunderClips;
    [SerializeField, Tooltip("Audio clips to use during the game")] private List<SoundClip> m_sfxClips;
    private Dictionary<string, AudioSource> m_sfxMapper;

    private int m_currentActiveBGMAudioSourcesIndex; // 0 for morning, 1 for twilight, 2 for evening, -1 for nothing being played
    private Coroutine m_thunderCoroutine;


    [Header("=======Lightning Light=======")]
    [SerializeField, Tooltip("Directional Light for lightning")] private GameObject Lightning;
    private Light lightninglight;

    public float minInterval = 2.0f; // Minimum time between flashes (adjust for less frequent flashes)
    public float maxInterval = 8.0f; // Maximum time between flashes (adjust for less frequent flashes)
    public float flashDuration = 1.0f; // Duration of each flash

    void Start(){
        lightninglight = Lightning.GetComponent<Light>();
        //lightninglight.enabled = false; // Turn off
    }

    private void Awake() {

        current = this;
        m_sfxMapper = new Dictionary<string, AudioSource>();
        m_currentActiveBGMAudioSourcesIndex = -1;
        foreach(SoundClip sc in m_sfxClips) {
            if(!m_sfxMapper.ContainsKey(sc.name)) m_sfxMapper.Add(sc.name, sc.audioSource);
        }
    }

    public void ToggleRainSound(bool enable, bool fastRain = false) {
        if (enable) {
            if (fastRain) {
                if (m_slowRainAudioSource.isPlaying) m_slowRainAudioSource.Stop();
                m_fastRainAudioSource.Play();
            }
            else {
                if (m_fastRainAudioSource.isPlaying) m_fastRainAudioSource.Stop();
                m_slowRainAudioSource.Play();
            }
        }
        else {
            if (m_slowRainAudioSource.isPlaying) m_slowRainAudioSource.Stop();
            if (m_fastRainAudioSource.isPlaying) m_fastRainAudioSource.Stop();
        }
    }

    // 0 for morning, 1 for twilight, 2 for evening, -1 for stopping BGM
    public void PlayBGM(int audioSourceIndex) {
        if (audioSourceIndex == -1) {
            for (int i = 0; i < m_BGMAudioSources.Count; i++) {
                if (m_BGMAudioSources[i].isPlaying) StartCoroutine(FadeOutBGM(i, m_BGMFadeDuration / 2));
            }
            return;
        }

        StartCoroutine(FadeOutBGM(m_currentActiveBGMAudioSourcesIndex, m_BGMFadeDuration / 2));
        StartCoroutine(FadeInBGM(audioSourceIndex, m_BGMFadeDuration));
        m_currentActiveBGMAudioSourcesIndex = audioSourceIndex;
    }

    // index must be from 0 to 2
    public IEnumerator FadeOutBGM(int audioSourceIndex, float fadeDuration) {
        if (audioSourceIndex >= 0 && audioSourceIndex <= 2 && m_BGMAudioSources.Count == 3) {
            float startVolume = m_BGMAudioSources[audioSourceIndex].volume;   //record initial volume
            float t = 0;
            while (m_BGMAudioSources[audioSourceIndex].volume > 0.05f) {
                m_BGMAudioSources[audioSourceIndex].volume = Mathf.Lerp(startVolume, 0, t / fadeDuration);
                t += Time.deltaTime;
                yield return null;
            }

            m_BGMAudioSources[audioSourceIndex].Stop();
            m_BGMAudioSources[audioSourceIndex].volume = startVolume; // reset the volume
        }
    }

    // index must be from 0 to 2
    public IEnumerator FadeInBGM(int audioSourceIndex, float fadeDuration) {
        if (audioSourceIndex >= 0 && audioSourceIndex <= 2 && m_BGMAudioSources.Count == 3) {
            float finalVolume = m_BGMAudioSources[audioSourceIndex].volume;   //record final volume
            m_BGMAudioSources[audioSourceIndex].Play();
            m_BGMAudioSources[audioSourceIndex].volume = 0.01f;
            float t = 0;
            while (m_BGMAudioSources[audioSourceIndex].volume < finalVolume - 0.05f) {
                m_BGMAudioSources[audioSourceIndex].volume = Mathf.Lerp(0, finalVolume, t / fadeDuration);
                t += Time.deltaTime;
                yield return null;
            }

            m_BGMAudioSources[audioSourceIndex].volume = finalVolume; // set the final volume
        }
    }

    public void ToggleThunderSFX(bool enable) {
        if (m_thunderCoroutine != null) StopCoroutine(m_thunderCoroutine);
        if (enable)                     m_thunderCoroutine = StartCoroutine(PlayThunderSFX());
        else                            lightninglight.enabled = false; // Turn off
    }

    public IEnumerator PlayThunderSFX() {
        while (true) {
            float randomTime = Random.Range(-m_thunderGapRandomness, m_thunderGapRandomness);
            yield return new WaitForSeconds(m_thunderGap + randomTime);

            float randomFlash= Random.Range(0.1f, flashDuration);

            int r = Random.Range(0, m_thunderClips.Count);
            m_thunderAudioSource.PlayOneShot(m_thunderClips[r]);

            lightninglight.enabled = true;
            yield return new WaitForSeconds(randomFlash); // Keep the light on for the flash duration
            lightninglight.enabled = false;
        }
    }

    public void PlaySFX(string name) {
        if (!m_sfxMapper.ContainsKey(name)) {
            Debug.LogError($"Cannot play SFX with name {name} - no sound clip registered");
            return;
        }
        m_sfxMapper[name].Play();
    }
    
    public void StopSFX(string name) {
        if (!m_sfxMapper.ContainsKey(name)) {
            Debug.LogError($"Cannot Stop SFX with name {name} - no sound clip registered");
            return;
        }
        m_sfxMapper[name].Stop();
    }
    
    public void FadeOutSFX(string name, float fadeDuration) {
        if (!m_sfxMapper.ContainsKey(name)) {
            Debug.LogError($"Cannot Stop SFX with name {name} - no sound clip registered");
            return;
        }

        StartCoroutine(FadeOutCoroutine(name, fadeDuration));
    }
    
    private IEnumerator FadeOutCoroutine(string name, float fadeDuration) {
        float startVolume = m_sfxMapper[name].volume;   //record initial volume
        float t = 0;
        while (m_sfxMapper[name].volume > 0.05f)
        {
            m_sfxMapper[name].volume = Mathf.Lerp(startVolume, 0, t / fadeDuration);
            t += Time.deltaTime;
            yield return null;
        }
        
        m_sfxMapper[name].Stop();
        m_sfxMapper[name].volume = startVolume; // reset the volume
    }
}
