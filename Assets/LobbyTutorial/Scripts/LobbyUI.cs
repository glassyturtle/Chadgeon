using System.Collections.Generic;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour
{


    public static LobbyUI Instance { get; private set; }

    [SerializeField] private List<string> mapNames;
    [SerializeField] private List<string> botDifficultyNames;
    private int selectedMap = 0;
    private int neutralBotAmount = 0;
    private int botDifficulty = 1;
    private int selectedFlock = 0;

    [SerializeField] private Transform playerSingleTemplate;
    [SerializeField] private Transform container;
    [SerializeField] private GameObject addBotsMenu;
    [SerializeField] private GameObject manageFlocksMenu;
    [SerializeField] private GameObject changeFlockMenu;
    [SerializeField] private TextMeshProUGUI lobbyNameText;
    [SerializeField] private TextMeshProUGUI lobbyMapText;
    [SerializeField] private TextMeshProUGUI neutralBotAmtText;
    [SerializeField] private TextMeshProUGUI selectedFlockBotsText;
    [SerializeField] private TextMeshProUGUI playerCountText;
    [SerializeField] private TextMeshProUGUI selectedFoxNameText;
    [SerializeField] private TextMeshProUGUI gameModeText;
    [SerializeField] private TextMeshProUGUI joinCodeText;
    [SerializeField] private TextMeshProUGUI botDifficultyText;
    [SerializeField] private Button leaveLobbyButton;
    [SerializeField] private Button changeGameModeButton;
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button openBotMenu;
    [SerializeField] private Button addBotButton;
    [SerializeField] private Button closeBotMenuButton;



    private void Awake()
    {
        Instance = this;

        playerSingleTemplate.gameObject.SetActive(false);

        startGameButton.onClick.AddListener(() =>
        {
            MultiplayerManager.Instance.StartGame(botDifficulty, neutralBotAmount);
        });

        leaveLobbyButton.onClick.AddListener(() =>
        {
            MultiplayerManager.Instance.LeaveLobby();
        });
        /*
        changeGameModeButton.onClick.AddListener(() =>
        {
            MultiplayerManager.Instance.ChangeGameMode();
        });
        */
    }

    private void Start()
    {
        MultiplayerManager.Instance.OnJoinedLobby += UpdateLobby_Event;
        MultiplayerManager.Instance.OnJoinedLobbyUpdate += UpdateLobby_Event;
        MultiplayerManager.Instance.OnLobbyGameModeChanged += UpdateLobby_Event;
        MultiplayerManager.Instance.OnLeftLobby += LobbyManager_OnLeftLobby;
        MultiplayerManager.Instance.OnKickedFromLobby += LobbyManager_OnLeftLobby;

        Hide();
    }
    public void ChangeMap(bool left)
    {
        if (left)
        {
            selectedMap -= 1;
            if (selectedMap < 0)
            {
                selectedMap = mapNames.Count - 1;
            }
        }
        else
        {
            selectedMap += 1;
            if (selectedMap > mapNames.Count - 1)
            {
                selectedMap = 0;
            }
        }
        GameDataHolder.map = selectedMap;
        lobbyMapText.text = mapNames[selectedMap];
    }
    public void ChangeDifficulty(bool left)
    {
        if (left)
        {
            botDifficulty -= 1;
            if (botDifficulty < 0)
            {
                botDifficulty = botDifficultyNames.Count - 1;
            }
        }
        else
        {
            botDifficulty += 1;
            if (botDifficulty > botDifficultyNames.Count - 1)
            {
                botDifficulty = 0;
            }
        }
        botDifficultyText.text = botDifficultyNames[botDifficulty];
    }

    public void ChangeNeutralBotAmount(bool left)
    {
        if (left)
        {
            neutralBotAmount -= 1;
            if (neutralBotAmount < 0)
            {
                neutralBotAmount = 0;
            }
        }
        else
        {
            neutralBotAmount += 1;
        }
        neutralBotAmtText.text = neutralBotAmount.ToString();
    }
    public void ChangeSelectedFactionBots(bool left)
    {
        int newAmt = 0;
        switch (selectedFlock)
        {
            case 0:
                return;
            case 1:
                newAmt = GameDataHolder.botsFlock1;
                break;
            case 2:
                newAmt = GameDataHolder.botsFlock2;
                break;
            case 3:
                newAmt = GameDataHolder.botsFlock3;
                break;
            case 4:
                newAmt = GameDataHolder.botsFlock4;
                break;
        }
        if (left)
        {
            newAmt -= 1;
            if (newAmt < 0)
            {
                newAmt = 0;
            }
        }
        else
        {
            newAmt += 1;
        }
        switch (selectedFlock)
        {
            case 0:
                return;
            case 1:
                GameDataHolder.botsFlock1 = newAmt;
                break;
            case 2:
                GameDataHolder.botsFlock2 = newAmt;
                break;
            case 3:
                GameDataHolder.botsFlock3 = newAmt;
                break;
            case 4:
                GameDataHolder.botsFlock4 = newAmt;
                break;
        }
        selectedFlockBotsText.text = newAmt.ToString();
    }
    public void SelectFlockToEdit(int flock)
    {
        selectedFlock = flock;
        switch (selectedFlock)
        {
            case 0:
                return;
            case 1:
                selectedFoxNameText.text = "Enjoyers";
                selectedFlockBotsText.text = GameDataHolder.botsFlock1.ToString();
                break;
            case 2:
                selectedFoxNameText.text = "Psychos";
                selectedFlockBotsText.text = GameDataHolder.botsFlock2.ToString();

                break;
            case 3:
                selectedFoxNameText.text = "Minons";
                selectedFlockBotsText.text = GameDataHolder.botsFlock3.ToString();

                break;
            case 4:
                selectedFoxNameText.text = "Looksmaxers";
                selectedFlockBotsText.text = GameDataHolder.botsFlock4.ToString();

                break;
        }
    }

    public void OpenAddBotMenu()
    {
        addBotsMenu.SetActive(true);
    }
    public void OpenManageFlockMenu()
    {
        manageFlocksMenu.SetActive(true);
    }
    public void OpenChangeFlockMenu()
    {
        changeFlockMenu.SetActive(true);
    }
    public void CloseAllPopUpMenus()
    {
        changeFlockMenu.SetActive(false);
        manageFlocksMenu.SetActive(false);
        addBotsMenu.SetActive(false);

    }
    private void LobbyManager_OnLeftLobby(object sender, System.EventArgs e)
    {
        ClearLobby();
        Hide();
    }
    public void AddBot()
    {

    }
    private void UpdateLobby_Event(object sender, MultiplayerManager.LobbyEventArgs e)
    {
        UpdateLobby();
    }

    private void UpdateLobby()
    {
        UpdateLobby(MultiplayerManager.Instance.GetJoinedLobby());
    }

    private void UpdateLobby(Lobby lobby)
    {
        ClearLobby();

        foreach (Player player in lobby.Players)
        {
            Transform playerSingleTransform = Instantiate(playerSingleTemplate, container);
            playerSingleTransform.gameObject.SetActive(true);
            LobbyPlayerSingleUI lobbyPlayerSingleUI = playerSingleTransform.GetComponent<LobbyPlayerSingleUI>();

            lobbyPlayerSingleUI.SetKickPlayerButtonVisible(
                MultiplayerManager.Instance.IsLobbyHost() &&
                player.Id != AuthenticationService.Instance.PlayerId // Don't allow kick self
            );
            lobbyPlayerSingleUI.UpdatePlayer(player);
            lobbyPlayerSingleUI.UpdatePlayer(player);
        }

        //changeGameModeButton.gameObject.SetActive(MultiplayerManager.Instance.IsLobbyHost());

        lobbyNameText.text = lobby.Name;
        playerCountText.text = lobby.Players.Count + "/" + lobby.MaxPlayers + " Player Pigeons";
        joinCodeText.text = lobby.LobbyCode;
        //gameModeText.text = lobby.Data[MultiplayerManager.KEY_GAME_MODE].Value;

        Show();
    }

    private void ClearLobby()
    {
        foreach (Transform child in container)
        {
            if (child == playerSingleTemplate) continue;
            Destroy(child.gameObject);
        }
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }

    private void Show()
    {
        gameObject.SetActive(true);
    }

}