using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;

public class OptionsMenu : MonoBehaviour
{
    [SerializeField] AudioMixer audioMixer;
    [SerializeField] GameObject applicationMenu, soundMenu;
    [SerializeField] TMP_Dropdown resDropdown;

    Resolution[] resolutions;

    private void Start()
    {
        resolutions = Screen.resolutions;
        resDropdown.ClearOptions();
        List<string> options = new List<string>();


        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + " X " + resolutions[i].height;
            options.Add(option);
        }

        resDropdown.AddOptions(options);
    }


    public void OpenOptionsMenu()
    {
        gameObject.SetActive(true);
    }
    public void CloseOptionMenu()
    {
        gameObject.SetActive(false);
    }
    public void OpenAppMenu()
    {
        soundMenu.SetActive(false);
        applicationMenu.SetActive(true);
    }

    public void OpenSoundMenu()
    {
        applicationMenu.SetActive(false);
        soundMenu.SetActive(true);
    }
    public void SetSFXVolume(float volume)
    {
        audioMixer.SetFloat("SFXVolume", volume);
    }
    public void SetMusicVolume(float volume)
    {
        audioMixer.SetFloat("MusicVolume", volume);

    }
    public void SetMasterVolume(float volume)
    {
        audioMixer.SetFloat("MasterVolume", volume);

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
