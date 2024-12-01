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
    [SerializeField, Tooltip("Ref. to the BGM Audio source, which is separate from SFX")]   private AudioSource m_BMGAudioSource;
    [SerializeField, Tooltip("Audio clips to use during the game")] private List<SoundClip> m_sfxClips;
    private Dictionary<string, AudioSource> m_sfxMapper;

    private void Awake() {
        current = this;
        m_sfxMapper = new Dictionary<string, AudioSource>();
        foreach(SoundClip sc in m_sfxClips) {
            if(!m_sfxMapper.ContainsKey(sc.name)) m_sfxMapper.Add(sc.name, sc.audioSource);
        }
    }

    public void PlayBGM() {
        if (!m_BMGAudioSource.isPlaying) m_BMGAudioSource.Play();
    }
    public void StopBMG() {
        if (m_BMGAudioSource.isPlaying) m_BMGAudioSource.Stop();
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
    
    private IEnumerator FadeOutCoroutine(string name, float fadeDuration)
    {
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
