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




    public void GoBackToPlayButtons()
    {
        playButtons.SetActive(true);
        connectingUI.SetActive(false);
    }
    public void StartLogin()
    {
        GameDataHolder.isSinglePlayer = false;
        backButton.SetActive(false);
        playButtons.SetActive(false);
        connectingUI.SetActive(true);
        connectingText.text = "Connecting To Server...";
        connectingText.gameObject.SetActive(true);
        LoginIntoChadgeonAsync();
    }
    public void StartSinglePlayer()
    {
        GameDataHolder.isSinglePlayer = true;
        backButton.SetActive(false);
        playButtons.SetActive(false);
        connectingUI.SetActive(true);
        connectingText.text = "Loading Game...";
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

            if (GameDataHolder.isSinglePlayer) connectingText.text = "Loading Game...";
            else connectingText.text = "Signing In...";


            await AuthenticationService.Instance.SignInAnonymouslyAsync();

            connectingText.text = "Starting Game...";

            await SceneManager.LoadSceneAsync("MainMenu");
        }
        catch (Exception e)
        {
            if (GameDataHolder.isSinglePlayer)
            {

                connectingText.text = "Starting Game...";

                await SceneManager.LoadSceneAsync("MainMenu");
            }
            else
            {
                connectingText.text = "Could not Connect to server";
                backButton.SetActive(true);
                Debug.LogException(e);
            }

        }
    }


}
