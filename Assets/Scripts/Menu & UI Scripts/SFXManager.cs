using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class SFXManager : MonoBehaviour
{
    // Create a singleton, given that only one SoundManager will exist at a time. 
    public static SFXManager instance;     
    
    [Header("Sound Sources")] 
    [SerializeField] private AudioSource SFXSource; 
    
    private void Awake()
    {
        if (instance == null)
        {
            instance = this; 
        }
    }

    public void PlaySFXClip(AudioClip audioClip, Transform spawnTransform, float volume = 1f)
    {
        AudioSource audioSource = Instantiate(SFXSource, spawnTransform.position, Quaternion.identity);

        audioSource.clip = audioClip;
        audioSource.volume = volume;
        
        audioSource.Play();

        float clipLength = audioSource.clip.length;
        Destroy(audioSource.gameObject, clipLength);
    }
    
    public void PlayRandomSFXClip(AudioClip[] audioClip, Transform spawnTransform, float volume = 1f)
    {
        int rand = Random.Range(0, audioClip.Length); 
        AudioSource audioSource = Instantiate(SFXSource, spawnTransform.position, Quaternion.identity);

        audioSource.clip = audioClip[rand];
        audioSource.volume = volume;

        float clipLength = audioSource.clip.length;
        Destroy(audioSource.gameObject, clipLength);

    }
}
