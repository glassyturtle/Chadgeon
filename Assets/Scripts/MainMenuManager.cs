using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : NetworkBehaviour
{
    [SerializeField] Button multiplayerButton, createGameButton, mainMenuButton;
    [SerializeField] TMP_InputField inputCode;
    [SerializeField] TMP_Text playersConnectedText, codeText, joinCodeText;
    [SerializeField] TMP_InputField nameInputField;
    [SerializeField] GameObject mainMenu, joinMenu, connectingMenu, creatingGameMenu;

    [SerializeField] TMP_Dropdown difficultySelector;
    [SerializeField] Slider botAmtSlider;
    [SerializeField] TMP_Text amtOfBots;
    [SerializeField] GameObject HostUI;

    [Header("Lobby List Menu")]
    [SerializeField] private Button refreshLobbiesButton;
    [SerializeField] private Button hostNewLobbyButton;
    [SerializeField] private Button exitLobbiesButton;


    [Header("Lobby Menu")]
    [SerializeField] private Button leaveLobbyButton;
    [SerializeField] private Button changeGameModeButton;
    [SerializeField] private Button addBotButton;
    [SerializeField] private Button startGameButton;
    [SerializeField] private TextMeshProUGUI lobbyNameText;
    [SerializeField] private TextMeshProUGUI playerCountText;
    [SerializeField] private TextMeshProUGUI gameModeText;

    private void Awake()
    {
        if (nameInputField && GameDataHolder.multiplayerName != "") nameInputField.text = GameDataHolder.multiplayerName;
        if (multiplayerButton)
        {
            multiplayerButton.onClick.AddListener(() =>
            {
                MultiplayerManager.Instance.CreateLobby("Chadgeon's Lobby", 69, false, MultiplayerManager.GameMode.FlockDeathMatch);
            });
        }
        if (createGameButton)
        {
            createGameButton.onClick.AddListener(() =>
            {
                GoToConnectingMenu();
                MultiplayerManager.Instance.JoinRelay(inputCode.text);
            });
        }
        if (startGameButton)
        {
            if (!NetworkManager.Singleton.IsHost) HostUI.SetActive(false);
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
            if (IsOwner)
            {
                SetPlayerCountServerRpc();
                amtOfBots.text = "Bots: " + botAmtSlider.value;
                GameDataHolder.botsToSpawn = Mathf.RoundToInt(botAmtSlider.value);
            }
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
    public void ChangeBotDifficulty(int amt)
    {
        GameDataHolder.botDifficulty = amt;
    }
}
