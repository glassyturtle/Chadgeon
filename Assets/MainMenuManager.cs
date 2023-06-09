using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : NetworkBehaviour
{
    [SerializeField] Button hostGameButton, createGameButton, startGameButton;
    [SerializeField] TMP_InputField inputCode;
    [SerializeField] TMP_Text playersConnectedText, codeText, joinCodeText;
    [SerializeField] testRelay realy;
    [SerializeField] GameObject mainMenu, joinMenu, connectingMenu;


    private void Awake()
    {
        if (hostGameButton)
        {
            hostGameButton.onClick.AddListener(() =>
            {
                realy.CreateRelay();
            });
        }
        if (createGameButton)
        {
            createGameButton.onClick.AddListener(() =>
            {
                GoToConnectingMenu();
                realy.JoinRelay(inputCode.text);
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

    public void GoToJoinMenu()
    {
        HideAllMenus();
        joinMenu.SetActive(true);
    }
    public void GoToMainMenu()
    {
        HideAllMenus();
        mainMenu.SetActive(true);
    }
    public void GoToConnectingMenu()
    {
        HideAllMenus();
        connectingMenu.SetActive(true);
    }
    private void HideAllMenus()
    {
        connectingMenu.SetActive(false);
        mainMenu.SetActive(false);
        joinMenu.SetActive(false);
    }
    private void Update()
    {
        if (playersConnectedText)
        {
            joinCodeText.text = "Join Code:" + GameDataHolder.joinCode;
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
