using System;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class SettingsPanel : MonoBehaviour
{
    public AudioMixerGroup masterMixer;

    public Slider musicSlider;
    public Slider sfxSlider;
    private float sfxPreviewTimer = 0f;

    public bool pauseGameWhenEnabled = false;
    public TextMeshProUGUI musicPercentageT;
    public TextMeshProUGUI sfxPercentageT;
    
    private void OnEnable()
    {
        if(pauseGameWhenEnabled)
            Time.timeScale = 0;

        LoadSettings();

    }

    private void OnDisable()
    {
        if(pauseGameWhenEnabled)
            Time.timeScale = 1;
    }

    public void LoadSettings()
    {
        if (WebGLPrefs.HasKey("MusicVolume"))
        {
            musicSlider.SetValueWithoutNotify(WebGLPrefs.GetFloat("MusicVolume"));
            masterMixer.audioMixer.SetFloat("MusicVolume", Mathf.Log10(musicSlider.value) * 20);    
            if (musicPercentageT)
                musicPercentageT.text = Mathf.RoundToInt(musicSlider.value * 100).ToString();
        }
        
        if (WebGLPrefs.HasKey("SFXVolume"))
        {
            sfxSlider.SetValueWithoutNotify(WebGLPrefs.GetFloat("SFXVolume"));
            masterMixer.audioMixer.SetFloat("SFXVolume", Mathf.Log10(sfxSlider.value) * 20);
            if (sfxPercentageT)
                sfxPercentageT.text =  Mathf.RoundToInt(sfxSlider.value * 100).ToString();
        }
    }
    
    public void ResetProgress()
    {
        /*
        WebGLPrefs.DeleteKey("Upgrades");
        WebGLPrefs.DeleteKey("ProgressData");
        */
    }

    public void OnMusicSliderChanged(float value)
    {
        masterMixer.audioMixer.SetFloat("MusicVolume", Mathf.Log10(value) * 20);
        WebGLPrefs.SetFloat("MusicVolume", value);
        if (musicPercentageT)
            musicPercentageT.text = Mathf.RoundToInt(musicSlider.value * 100).ToString();
    }

    public void OnSFXSliderChanged(float value)
    {
        masterMixer.audioMixer.SetFloat("SFXVolume", Mathf.Log10(value) * 20);
        WebGLPrefs.SetFloat("SFXVolume", value);

        if (sfxPercentageT)
            sfxPercentageT.text =  Mathf.RoundToInt(sfxSlider.value * 100).ToString();
        
        if (sfxPreviewTimer <= 0)
        {
            if (sfxSlider.TryGetComponent(out AudioSource audioSource))
            {
                audioSource.Play();
                sfxPreviewTimer = 0.25f;
            }    
        }
        
    }

    public void ReturnToMainMenu()
    {
        if (Time.timeScale == 0)
            Time.timeScale = 1f;
        LoadingManager.OnLoadingExpected?.Invoke("MainMenuScene");
    }
    
    // Update is called once per frame
    void Update()
    {
        if (sfxPreviewTimer > 0)
        {
            sfxPreviewTimer -= Time.deltaTime;
        }
    }
}
