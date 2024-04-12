using System;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoginScript : MonoBehaviour
{
    [SerializeField] GameObject playButtons;
    [SerializeField] GameObject connectingUI;
    [SerializeField] TMP_Text connectingText;
    [SerializeField] GameObject backButton;


    private void Awake()
    {
        SaveDataManager.LoadGameData();
    }


    public void GoBackToPlayButtons()
    {
        playButtons.SetActive(true);
        connectingUI.SetActive(false);
    }
    public void StartLogin()
    {
        backButton.SetActive(false);
        playButtons.SetActive(false);
        connectingUI.SetActive(true);
        connectingText.text = "Connecting To Server...";
        connectingText.gameObject.SetActive(true);
        LoginIntoChadgeonAsync();
    }
    private async void LoginIntoChadgeonAsync()
    {
        try
        {

            await UnityServices.InitializeAsync();
            if (AuthenticationService.Instance.IsSignedIn) return;

            AuthenticationService.Instance.SignedIn += () =>
            {
                Debug.Log("signed in " + AuthenticationService.Instance.PlayerId);
            };

            connectingText.text = "Signing In...";

            await AuthenticationService.Instance.SignInAnonymouslyAsync();

            connectingText.text = "Starting Game...";

            SceneManager.LoadSceneAsync("MainMenu");
        }
        catch (Exception e)
        {
            connectingText.text = "Could not Connect to server";
            backButton.SetActive(true);
            Debug.LogException(e);
        }
    }


}
