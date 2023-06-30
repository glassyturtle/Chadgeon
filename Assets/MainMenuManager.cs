using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : NetworkBehaviour
{
    [SerializeField] Button hostGameButton, createGameButton, startGameButton, mainMenuButton;
    [SerializeField] TMP_InputField inputCode;
    [SerializeField] TMP_Text playersConnectedText, codeText, joinCodeText;
    [SerializeField] testRelay realy;
    [SerializeField] TMP_InputField nameInputField;
    [SerializeField] GameObject mainMenu, joinMenu, connectingMenu, creatingGameMenu;


    private void Awake()
    {
        if (nameInputField && GameDataHolder.multiplayerName != "") nameInputField.text = GameDataHolder.multiplayerName;
        if (hostGameButton)
        {
            hostGameButton.onClick.AddListener(() =>
            {
                GoToHostingGameMenu();
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
            if (!NetworkManager.Singleton.IsHost) startGameButton.gameObject.SetActive(false);
            startGameButton.onClick.AddListener(() =>
            {
                NetworkManager.Singleton.SceneManager.LoadScene("SimpMode", LoadSceneMode.Single);
            });
        }
        if (mainMenuButton)
        {
            mainMenuButton.onClick.AddListener(() =>
            {
                NetworkManager.Singleton.Shutdown();
                SceneManager.LoadScene("MainMenu");
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
    public void GoToHostingGameMenu()
    {
        HideAllMenus();
        creatingGameMenu.SetActive(true);
    }
    private void HideAllMenus()
    {
        connectingMenu.SetActive(false);
        mainMenu.SetActive(false);
        joinMenu.SetActive(false);
    }
    public void SetMultiplayerName(string name)
    {
        GameDataHolder.multiplayerName = name;
    }
    public void QuitGame()
    {
        Application.Quit();
    }
    private void Update()
    {
        if (playersConnectedText)
        {
            joinCodeText.text = "Join Code:" + GameDataHolder.joinCode;
            if (IsOwner) SetPlayerCountServerRpc();
        }
    }
    [ServerRpc]
    private void SetPlayerCountServerRpc()
    {
        SetPlayerCountClientRpc(NetworkManager.Singleton.ConnectedClients.Count);
    }
    [ClientRpc]
    private void SetPlayerCountClientRpc(int value)
    {
        playersConnectedText.text = "Players Connected:" + value.ToString();

    }

}
