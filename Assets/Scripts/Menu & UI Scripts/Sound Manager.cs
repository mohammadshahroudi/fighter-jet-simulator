using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class SoundManager : MonoBehaviour
{

    [Header("Volume Sliders")]
    [SerializeField] private Slider masterVolSlider; 
    [SerializeField] private Slider musicVolSlider; 
    [SerializeField] private Slider sfxVolSlider;

    [Header("Music")] 
    // [SerializeField] private MusicClipRefsSO musicClipRefSO; 
    [SerializeField] private AudioClip tempMusic;
    
    [Header("SFX")] 
    // [SeralizeField] private SFXClipRefsSO SFXClipsSO; 
    [SerializeField] private AudioClip tempSFX;

    [Header("Sound Manager Object")] 
    [SerializeField] private GameObject soundManagerObject;

    public float _masterVol;   
    public float _musicVol; 
    public float _sfxVol;

    private void Awake()
    {
        LoadPlayerPrefs();
    }

    private void Start()
    {
        PlaySound(tempMusic, Camera.main.transform.position, _masterVol);
    }

     public void ChangeVolume()
    {
        _masterVol = masterVolSlider.value; 
        PlayerPrefs.SetFloat("_masterVol", _masterVol);
    }

    private void LoadPlayerPrefs()
    {
        if (PlayerPrefs.HasKey("_masterVol"))
        {
            _masterVol = PlayerPrefs.GetFloat("_masterVol"); 
        }
        else
        {
            _masterVol = 1; 
        }
    }

    private void PlaySound(AudioClip audioClip, Vector3 position, float volume)
    {
        AudioSource.PlayClipAtPoint(audioClip, position, volume);
    }
    private void PlaySound(AudioClip[] audioClipArray, Vector3 position, float volume)
    {
        PlaySound(audioClipArray[Random.Range(0, audioClipArray.Length)], position, volume);
    }
}
