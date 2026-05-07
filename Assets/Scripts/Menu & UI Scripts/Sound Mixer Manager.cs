using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class SoundMixerManager : MonoBehaviour
{
    [SerializeField] private AudioMixer audioMixer;

    [SerializeField] private Slider masterVolSlider; 
    [SerializeField] private Slider musicVolSlider; 
    [SerializeField] private Slider SFXVolSlider;

    void Start()
    {
        if (PlayerPrefs.HasKey("masterVol"))
        {
            masterVolSlider.value = PlayerPrefs.GetFloat("masterVol");
            SetMasterVolume(masterVolSlider.value);
        }
        else
        {
            SetMasterVolume(1f);
        }
        
        if (PlayerPrefs.HasKey("musicVol"))
        {
            musicVolSlider.value = PlayerPrefs.GetFloat("musicVol");
            SetMusicVolume(musicVolSlider.value);
        }
        else
        {
            SetMusicVolume(1f);
        }
        
        if (PlayerPrefs.HasKey("SFXVol"))
        {
            SFXVolSlider.value = PlayerPrefs.GetFloat("SFXVol");
            SetSFXVolume(musicVolSlider.value);
        }
        else
        {
            SetSFXVolume(1f);
        }
    }

    public void SetMasterVolume(float value)
    {
        audioMixer.SetFloat("Mixer_MasterVol", ValueToVolume(value));
        
        PlayerPrefs.SetFloat("masterVol", value);
    }
    
    public void SetMusicVolume(float value)
    {
        audioMixer.SetFloat("Mixer_MusicVol", ValueToVolume(value));
        
        PlayerPrefs.SetFloat("musicVol", value);
    }
    
    public void SetSFXVolume(float value)
    {
        audioMixer.SetFloat("Mixer_SFXVol", ValueToVolume(value));
        
        PlayerPrefs.SetFloat("SFXVol", value);
    }
    
    
    private float ValueToVolume(float value)
    {
        return Mathf.Log10(value) * 20f; 
    }
}
