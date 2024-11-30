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
}
