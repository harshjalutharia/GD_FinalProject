using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSoundManager : MonoBehaviour
{
    [Tooltip("Audio Source Component")] private AudioSource audioSource;
    [Tooltip("Sound of collecting gem")] public AudioClip collectGem;
    [Tooltip("Sound of cheating")] public AudioClip cheat;
    
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }
    

    public void PlaySound(AudioClip audioClip)
    {
        audioSource.PlayOneShot(audioClip);
    }
}
