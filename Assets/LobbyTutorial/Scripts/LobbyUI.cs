using System.Collections;
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
    private int neutralBotAmount = 3;
    private int botDifficulty = 1;
    private int selectedFlock = 0;
    private int amtOfFlocks = 2;

    [SerializeField] private Transform playerSingleTemplate;
    [SerializeField] private Transform container;




    [SerializeField] private GameObject changeFlockMenu;
    [SerializeField] private GameObject changeAmtOfFlocksUI;
    [SerializeField] private GameObject changeBotAmountSetting;
    [SerializeField] private GameObject flockBotsTitle;
    [SerializeField] private GameObject EnjoyerBotAmountSetting;
    [SerializeField] private GameObject PsychoBotAmountSetting;
    [SerializeField] private GameObject MinionBotAmountSetting;
    [SerializeField] private GameObject LooksMaxerBotAmountSetting;
    [SerializeField] private GameObject changeFlockButtonGameobject;
    [SerializeField] private GameObject startingGameDisplay;





    [SerializeField] private List<GameObject> hostButtons;
    [SerializeField] private TextMeshProUGUI lobbyNameText;
    [SerializeField] private TextMeshProUGUI lobbyMapText;
    [SerializeField] private TextMeshProUGUI neutralBotAmtText;
    [SerializeField] private TextMeshProUGUI enjoyerAmtText;
    [SerializeField] private TextMeshProUGUI psychoAmtText;
    [SerializeField] private TextMeshProUGUI minionAmtText;
    [SerializeField] private TextMeshProUGUI looksMaxerAmtText;
    [SerializeField] private TextMeshProUGUI flockAmtText;
    [SerializeField] private TextMeshProUGUI playerCountText;
    [SerializeField] private TextMeshProUGUI joinCodeText;
    [SerializeField] private TextMeshProUGUI botDifficultyText;
    [SerializeField] private Button leaveLobbyButton;
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button openBotMenu;




    private void Awake()
    {
        Instance = this;

        playerSingleTemplate.gameObject.SetActive(false);

        startGameButton.onClick.AddListener(() =>
        {
            startingGameDisplay.SetActive(true);
            MultiplayerManager.Instance.StartGame(botDifficulty, amtOfFlocks);
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
        MultiplayerManager.Instance.OnJoinedLobby += ResetLobbyDefaultValues;
        MultiplayerManager.Instance.OnJoinedLobby += UpdateLobby_Event;
        MultiplayerManager.Instance.OnJoinedLobbyUpdate += UpdateLobby_Event;
        MultiplayerManager.Instance.OnLobbySettingsChanged += UpdateLobby_Event;
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
        UpdateLobbySettingsAfterDelay();
    }
    public void ChangeDifficulty(bool left)
    {
        if (GameDataHolder.isSinglePlayer)
        {
            if (left)
            {
                GameDataHolder.botDifficulty -= 1;
                if (GameDataHolder.gameMode != 1)
                {
                    if (GameDataHolder.botDifficulty < 0)
                    {
                        GameDataHolder.botDifficulty = botDifficultyNames.Count - 2;
                    }

                }
                else
                {
                    if (GameDataHolder.botDifficulty < 0)
                    {
                        GameDataHolder.botDifficulty = botDifficultyNames.Count - 1;
                    }
                }

            }
            else
            {
                GameDataHolder.botDifficulty += 1;
                if (GameDataHolder.gameMode != 1)
                {
                    if (GameDataHolder.botDifficulty > botDifficultyNames.Count - 2)
                    {
                        GameDataHolder.botDifficulty = 0;
                    }

                }
                else
                {
                    if (GameDataHolder.botDifficulty > botDifficultyNames.Count - 1)
                    {
                        GameDataHolder.botDifficulty = 0;
                    }
                }

            }
        }
        else
        {
            if (left)
            {
                botDifficulty -= 1;
                if (MultiplayerManager.Instance.joinedLobby.Data[MultiplayerManager.KEY_GAMEMODE_NUMBER].Value != "1")
                {
                    if (botDifficulty < 0)
                    {
                        botDifficulty = botDifficultyNames.Count - 2;
                    }

                }
                else
                {
                    if (botDifficulty < 0)
                    {
                        botDifficulty = botDifficultyNames.Count - 1;
                    }
                }

            }
            else
            {
                botDifficulty += 1;
                if (MultiplayerManager.Instance.joinedLobby.Data[MultiplayerManager.KEY_GAMEMODE_NUMBER].Value != "1")
                {
                    if (botDifficulty > botDifficultyNames.Count - 2)
                    {
                        botDifficulty = 0;
                    }

                }
                else
                {
                    if (botDifficulty > botDifficultyNames.Count - 1)
                    {
                        botDifficulty = 0;
                    }
                }

            }
            GameDataHolder.botDifficulty = botDifficulty;
        }


        botDifficultyText.text = botDifficultyNames[GameDataHolder.botDifficulty];
        UpdateLobbySettingsAfterDelay();
    }

    public void ChangeNeutralBotAmount(bool left)
    {
        if (GameDataHolder.gameMode == 1)
        {
            if (GameDataHolder.isSinglePlayer)
            {
                if (neutralBotAmount > 3) neutralBotAmount = 3;
                if (left)
                {
                    Debug.Log(neutralBotAmount);
                    neutralBotAmount -= 1;
                    if (neutralBotAmount < 0)
                    {
                        neutralBotAmount = 0;
                    }
                }
                else
                {
                    neutralBotAmount += 1;
                    if (neutralBotAmount > 3)
                    {
                        neutralBotAmount = 3;
                    }
                }
            }
            else
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
                    if (neutralBotAmount > 4 - MultiplayerManager.Instance.joinedLobby.Players.Count)
                    {
                        neutralBotAmount = 4 - MultiplayerManager.Instance.joinedLobby.Players.Count;
                    }
                }
            }

        }
        else
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
        }
        Debug.Log(neutralBotAmount);

        GameDataHolder.botsToSpawn = neutralBotAmount;
        neutralBotAmtText.text = neutralBotAmount.ToString();
        UpdateLobbySettingsAfterDelay();
    }
    public void ChangeFlockAmount(bool left)
    {
        if (left)
        {
            amtOfFlocks -= 1;
            if (amtOfFlocks < 2)
            {
                amtOfFlocks = 2;
            }
        }
        else
        {
            amtOfFlocks += 1;
            if (amtOfFlocks > 4)
            {
                amtOfFlocks = 4;
            }
        }
        flockAmtText.text = amtOfFlocks.ToString();
        UpdateLobbySettingsAfterDelay();
    }
    public void ChangeSelectedFlock(int flock)
    {
        selectedFlock = flock;
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
                enjoyerAmtText.text = newAmt.ToString();
                break;
            case 2:
                GameDataHolder.botsFlock2 = newAmt;
                psychoAmtText.text = newAmt.ToString();
                break;
            case 3:
                GameDataHolder.botsFlock3 = newAmt;
                minionAmtText.text = newAmt.ToString();
                break;
            case 4:
                GameDataHolder.botsFlock4 = newAmt;
                looksMaxerAmtText.text = newAmt.ToString();
                break;
        }
        UpdateLobbySettingsAfterDelay();

    }



    public void OpenChangeFlockMenu()
    {
        changeFlockMenu.SetActive(true);
    }
    public void CloseAllPopUpMenus()
    {
        changeFlockMenu.SetActive(false);
    }
    private void LobbyManager_OnLeftLobby(object sender, System.EventArgs e)
    {
        ClearLobby();
        Hide();
    }

    private void UpdateLobby_Event(object sender, MultiplayerManager.LobbyEventArgs e)
    {
        UpdateLobby();
    }
    private void ResetLobbyDefaultValues(object sender, MultiplayerManager.LobbyEventArgs e)
    {
        selectedMap = 0;
        botDifficulty = 1;
        GameDataHolder.botsToSpawn = 0;
        GameDataHolder.botsFlock1 = 0;
        GameDataHolder.botsFlock2 = 0;
        GameDataHolder.botsFlock3 = 0;
        GameDataHolder.botsFlock4 = 0;

        GameDataHolder.botDifficulty = botDifficulty;
        switch (GameDataHolder.gameMode)
        {
            case 0:
                neutralBotAmount = 10;
                GameDataHolder.botsToSpawn = 10;
                break;
            case 1:
                GameDataHolder.botsToSpawn = 3;
                break;
            case 2:
                GameDataHolder.botsFlock1 = 5;
                GameDataHolder.botsFlock2 = 5;
                GameDataHolder.botsFlock3 = 5;
                GameDataHolder.botsFlock4 = 5;
                break;
        }
        neutralBotAmtText.text = GameDataHolder.botsToSpawn.ToString();
        selectedFlock = 0;
    }

    private void ResetLobbyDefaultValues()
    {
        selectedMap = 0;
        botDifficulty = 1;
        GameDataHolder.botsToSpawn = 0;
        GameDataHolder.botsFlock1 = 0;
        GameDataHolder.botsFlock2 = 0;
        GameDataHolder.botsFlock3 = 0;
        GameDataHolder.botsFlock4 = 0;

        GameDataHolder.botDifficulty = botDifficulty;
        switch (GameDataHolder.gameMode)
        {
            case 0:
                neutralBotAmount = 10;
                GameDataHolder.botsToSpawn = 10;
                break;
            case 1:
                neutralBotAmount = 3;
                GameDataHolder.botsToSpawn = 3;
                break;
            case 2:
                GameDataHolder.botsFlock1 = 5;
                GameDataHolder.botsFlock2 = 5;
                GameDataHolder.botsFlock3 = 5;
                GameDataHolder.botsFlock4 = 5;
                break;
        }
        selectedFlock = 0;
    }
    private void UpdateLobby()
    {
        UpdateLobby(MultiplayerManager.Instance.GetJoinedLobby());
    }

    private void UpdateLobby(Lobby lobby)
    {
        ClearLobby();

        if (GameDataHolder.isSinglePlayer)
        {
            AdjustLobbyButtonVisibility(GameDataHolder.gameMode);

            //Showing player in lobby
            Transform playerSingleTransform = Instantiate(playerSingleTemplate, container);
            playerSingleTransform.gameObject.SetActive(true);
            LobbyPlayerSingleUI lobbyPlayerSingleUI = playerSingleTransform.GetComponent<LobbyPlayerSingleUI>();
            lobbyPlayerSingleUI.SetKickPlayerButtonVisible(false);
            lobbyPlayerSingleUI.UpdatePlayerSinglePlayer();

            foreach (GameObject obj in hostButtons)
            {
                obj.SetActive(true);
            }

            lobbyNameText.text = SaveDataManager.playerName + "'s Game";
            playerCountText.text = "Singleplayer";
            joinCodeText.text = "N/A";

        }
        else
        {
            if (MultiplayerManager.Instance.IsLobbyHost())
            {
                //Enable Host Buttons
                foreach (GameObject obj in hostButtons)
                {
                    obj.SetActive(true);
                }
                if (GameDataHolder.gameMode == 1)
                {
                    if (lobby.Players.Count + neutralBotAmount > 4)
                    {
                        neutralBotAmount = 4 - lobby.Players.Count;
                        GameDataHolder.botsToSpawn = 4 - lobby.Players.Count;
                        UpdateLobbySettingsAfterDelay();

                    }
                }
                //Show Other Buttons
                AdjustLobbyButtonVisibility(int.Parse(lobby.Data[MultiplayerManager.KEY_GAMEMODE_NUMBER].Value));
            }
            else
            {
                //Update Game Data based on Lobby
                GameDataHolder.gameMode = int.Parse(lobby.Data[MultiplayerManager.KEY_GAMEMODE_NUMBER].Value);
                GameDataHolder.botsToSpawn = int.Parse(lobby.Data[MultiplayerManager.KEY_BOT_AMT].Value);
                GameDataHolder.botsFlock1 = int.Parse(lobby.Data[MultiplayerManager.KEY_ENJOYERBOTS].Value);
                GameDataHolder.botsFlock2 = int.Parse(lobby.Data[MultiplayerManager.KEY_PSYCHOBOTS].Value);
                GameDataHolder.botsFlock3 = int.Parse(lobby.Data[MultiplayerManager.KEY_MINIONBOTS].Value);
                GameDataHolder.botsFlock4 = int.Parse(lobby.Data[MultiplayerManager.KEY_LOOKSMAXERBOTS].Value);
                amtOfFlocks = int.Parse(lobby.Data[MultiplayerManager.KEY_FLOCK_AMT].Value);

                //Disable host only Buttons
                foreach (GameObject obj in hostButtons)
                {
                    obj.SetActive(false);
                }

                //Show Other Buttons
                AdjustLobbyButtonVisibility(int.Parse(lobby.Data[MultiplayerManager.KEY_GAMEMODE_NUMBER].Value));

                lobbyMapText.text = lobby.Data[MultiplayerManager.KEY_MAP_NAME].Value;
                botDifficultyText.text = lobby.Data[MultiplayerManager.KEY_DIFFICULTY].Value;
            }


            //Showing players in lobby
            foreach (Player player in lobby.Players)
            {
                Transform playerSingleTransform = Instantiate(playerSingleTemplate, container);
                playerSingleTransform.gameObject.SetActive(true);
                LobbyPlayerSingleUI lobbyPlayerSingleUI = playerSingleTransform.GetComponent<LobbyPlayerSingleUI>();
                lobbyPlayerSingleUI.SetKickPlayerButtonVisible(
                    MultiplayerManager.Instance.IsLobbyHost() &&
                    player.Id != AuthenticationService.Instance.PlayerId // Don't allow kick self
                );
                lobbyPlayerSingleUI.UpdatePlayer(player, lobby);
            }



            lobbyNameText.text = lobby.Name;
            playerCountText.text = lobby.Players.Count + "/" + lobby.MaxPlayers + " Player Pigeons";
            joinCodeText.text = lobby.LobbyCode;
        }
        Show();

    }
    private void AdjustLobbyButtonVisibility(int gameMode)
    {
        switch (gameMode)
        {
            case 0:
                changeBotAmountSetting.SetActive(true);
                changeFlockButtonGameobject.SetActive(false);
                changeAmtOfFlocksUI.SetActive(false);
                EnjoyerBotAmountSetting.SetActive(false);
                PsychoBotAmountSetting.SetActive(false);
                MinionBotAmountSetting.SetActive(false);
                LooksMaxerBotAmountSetting.SetActive(false);
                flockBotsTitle.SetActive(false);
                neutralBotAmtText.text = GameDataHolder.botsToSpawn.ToString();
                break;
            case 1:
                changeBotAmountSetting.SetActive(true);
                changeFlockButtonGameobject.SetActive(false);
                changeAmtOfFlocksUI.SetActive(false);
                EnjoyerBotAmountSetting.SetActive(false);
                PsychoBotAmountSetting.SetActive(false);
                MinionBotAmountSetting.SetActive(false);
                LooksMaxerBotAmountSetting.SetActive(false);
                flockBotsTitle.SetActive(false);
                neutralBotAmtText.text = GameDataHolder.botsToSpawn.ToString();
                break;
            case 2:
                changeBotAmountSetting.SetActive(false);
                changeFlockButtonGameobject.SetActive(true);
                changeAmtOfFlocksUI.SetActive(true);
                flockAmtText.text = amtOfFlocks.ToString();
                flockBotsTitle.SetActive(true);

                EnjoyerBotAmountSetting.SetActive(false);
                PsychoBotAmountSetting.SetActive(false);
                MinionBotAmountSetting.SetActive(false);
                LooksMaxerBotAmountSetting.SetActive(false);
                if (amtOfFlocks >= 2)
                {
                    EnjoyerBotAmountSetting.SetActive(true);
                    PsychoBotAmountSetting.SetActive(true);
                    enjoyerAmtText.text = GameDataHolder.botsFlock1.ToString();
                    psychoAmtText.text = GameDataHolder.botsFlock2.ToString();
                }
                if (amtOfFlocks >= 3)
                {
                    MinionBotAmountSetting.SetActive(true);
                    minionAmtText.text = GameDataHolder.botsFlock3.ToString();

                }
                if (amtOfFlocks == 4)
                {
                    LooksMaxerBotAmountSetting.SetActive(true);
                    looksMaxerAmtText.text = GameDataHolder.botsFlock4.ToString();
                }

                break;
        }
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

    private void UpdateLobbySettingsAfterDelay()
    {
        if (GameDataHolder.isSinglePlayer)
        {
            UpdateLobby(null);
            return;
        }
        StopAllCoroutines();
        StartCoroutine(DelayUpdate());
    }
    IEnumerator DelayUpdate()
    {
        yield return new WaitForSeconds(2f);
        MultiplayerManager.Instance.UpdateLobbySettings(mapNames[GameDataHolder.map], botDifficultyNames[botDifficulty], amtOfFlocks.ToString());
    }
    private void Show()
    {
        gameObject.SetActive(true);

    }

}