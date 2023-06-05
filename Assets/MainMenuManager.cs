using TMPro;
using Unity.Jobs;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : NetworkBehaviour
{
    [SerializeField] Button hostGameButton, createGameButton, startGameButton;
    [SerializeField] TMP_Text playersConnectedText;



    private void Awake()
    {
        if (hostGameButton)
        {
            hostGameButton.onClick.AddListener(() =>
            {
                NetworkManager.Singleton.StartHost();
                NetworkManager.Singleton.SceneManager.LoadScene("LobbyMenu", LoadSceneMode.Single);
            });
        }
        if (createGameButton)
        {
            createGameButton.onClick.AddListener(() =>
            {
                NetworkManager.Singleton.StartClient();
            });
        }
        if (startGameButton)
        {
            startGameButton.onClick.AddListener(() =>
            {
                NetworkManager.Singleton.SceneManager.LoadScene("SimpMode", LoadSceneMode.Single);
            });
        }



    }
    private void Update()
    {
        if (playersConnectedText)
        {
            if (IsOwner)
                SetPlayerCountServerRpc();
            else playersConnectedText.text = "Connected To Host!";
        }
    }
    [ServerRpc]
    private void SetPlayerCountServerRpc()
    {
        playersConnectedText.text = "Players Connected:" + NetworkManager.Singleton.ConnectedClients.Count.ToString();

    }
}
