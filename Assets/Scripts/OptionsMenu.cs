using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class OptionsMenu : MonoBehaviour
{
    [SerializeField] AudioMixer audioMixer;
    [SerializeField] GameObject applicationMenu, soundMenu;
    [SerializeField] TMP_Dropdown resDropdown;
    [SerializeField] Slider sfxSlider, musicSlider, masterVolumeSlider;

    Resolution[] resolutions;


    private void Awake()
    {
        audioMixer.SetFloat("SFXVolume", SaveDataManager.soundEffectVolume);
        audioMixer.SetFloat("MusicVolume", SaveDataManager.musicVolume);
        audioMixer.SetFloat("MasterVolume", SaveDataManager.playerVolume);
        sfxSlider.value = SaveDataManager.soundEffectVolume;
        musicSlider.value = SaveDataManager.musicVolume;
        masterVolumeSlider.value = SaveDataManager.playerVolume;
        gameObject.SetActive(false);
    }

    private void Start()
    {
        resolutions = Screen.resolutions;
        resDropdown.ClearOptions();
        List<string> options = new List<string>();

        int currentRes = 0;
        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + " x " + resolutions[i].height + " @ " + resolutions[i].refreshRateRatio + "hz";
            options.Add(option);
            if (resolutions[i].width == Screen.width && resolutions[i].height == Screen.height)
            {
                currentRes = i;
            }
        }

        resDropdown.AddOptions(options);
        resDropdown.value = currentRes;
        resDropdown.RefreshShownValue();
    }

    public void SetResolution(int index)
    {
        Resolution resolution = resolutions[index];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
    }
    public void OpenOptionsMenu()
    {
        gameObject.SetActive(true);
    }
    public void CloseOptionMenu()
    {
        gameObject.SetActive(false);
        SaveDataManager.SaveGameData();
    }
    public void OpenAppMenu()
    {
        soundMenu.SetActive(false);
        applicationMenu.SetActive(true);
    }
    public void QuitGame()
    {
        Application.Quit();
    }

    public void OpenSoundMenu()
    {
        applicationMenu.SetActive(false);
        soundMenu.SetActive(true);
    }
    public void SetSFXVolume(float volume)
    {
        audioMixer.SetFloat("SFXVolume", volume);
        SaveDataManager.soundEffectVolume = volume;
    }
    public void SetMusicVolume(float volume)
    {
        audioMixer.SetFloat("MusicVolume", volume);
        SaveDataManager.musicVolume = volume;


    }
    public void SetMasterVolume(float volume)
    {
        audioMixer.SetFloat("MasterVolume", volume);
        SaveDataManager.playerVolume = volume;


    }
    public void SetFullScreen(bool isFullScreen)
    {
        Screen.fullScreen = isFullScreen;
    }
    public void SetQuality(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
    }
}
