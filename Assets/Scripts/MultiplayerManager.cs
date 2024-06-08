using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MultiplayerManager : MonoBehaviour
{
    public static MultiplayerManager Instance { get; private set; }

    private float lobbyPollTimer;
    public Lobby joinedLobby { get; private set; }

    public event EventHandler OnLeftLobby;

    public event EventHandler<LobbyEventArgs> OnJoinedLobby;
    public event EventHandler<LobbyEventArgs> OnJoinedLobbyUpdate;
    public event EventHandler<LobbyEventArgs> OnKickedFromLobby;
    public event EventHandler<LobbyEventArgs> OnLobbySettingsChanged;

    public const string KEY_PLAYER_FLOCK = "Flock";
    public const string KEY_PLAYER_NAME = "PlayerName";
    public const string KEY_PLAYER_RANK = "PlayerRank";
    public const string KEY_PLAYER_SKIN = "PlayerSkin";
    public const string KEY_START_GAME = "Start";
    public const string KEY_BOT_AMT = "BotCount";
    public const string KEY_MAP_NAME = "MapName";
    public const string KEY_FLOCK_AMT = "FlockAmount";
    public const string KEY_DIFFICULTY = "Difficulty";
    public const string KEY_PLAYER_SKINBODY = "PlayerBody";
    public const string KEY_PLAYER_SKINHEAD = "PlayerHead";
    public const string KEY_GAMEMODE = "GameMode";
    public const string KEY_GAMEMODE_NUMBER = "0";
    public const string KEY_ENJOYERBOTS = "Enjoyers";
    public const string KEY_PSYCHOBOTS = "Psychos";
    public const string KEY_MINIONBOTS = "Minions";
    public const string KEY_LOOKSMAXERBOTS = "Looksmaxers";



    [SerializeField] private GameObject couldNotJoinTitle;
    [SerializeField] private GameObject connectingTitle;
    [SerializeField] private GameObject startingQPTitle;

    public class LobbyEventArgs : EventArgs
    {
        public Lobby lobby;
    }

    public event EventHandler<OnLobbyListChangedEventArgs> OnLobbyListChanged;
    public class OnLobbyListChangedEventArgs : EventArgs
    {
        public List<Lobby> lobbyList;
    }



    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        HandleLobbyHeartbeat();
        HandleLobbyPolling();
    }

    #region Lobby

    private Lobby hostLobby;
    private float heartBeatTimer = 15;

    public async void CreateLobby(string lobbyName, int maxPlayers, bool isPrivate, int gameMode)
    {
        if (GameDataHolder.isSinglePlayer)
        {
            OnJoinedLobby?.Invoke(this, new LobbyEventArgs { lobby = null });
            return;
        }
        Player player = GetPlayer();


        string gamemodeName = "Supremacy";

        switch (gameMode)
        {
            case 0:
                gamemodeName = "Supremacy";
                break;
            case 1:
                gamemodeName = "Ice-cream Ops";
                break;
            case 2:
                gamemodeName = "Flock Supremacy";
                break;

        }


        CreateLobbyOptions options = new CreateLobbyOptions
        {
            Player = player,
            IsPrivate = isPrivate,

            Data = new Dictionary<string, DataObject> {
                {KEY_START_GAME, new DataObject(DataObject.VisibilityOptions.Member, "0")},
                {KEY_BOT_AMT, new DataObject(DataObject.VisibilityOptions.Member, "5")},
                {KEY_DIFFICULTY, new DataObject(DataObject.VisibilityOptions.Public, "Chad")},
                {KEY_GAMEMODE, new DataObject(DataObject.VisibilityOptions.Public, gamemodeName)},
                {KEY_MAP_NAME, new DataObject(DataObject.VisibilityOptions.Member, "Kaiserslautern")},
                {KEY_FLOCK_AMT, new DataObject(DataObject.VisibilityOptions.Member, "2")},
                {KEY_LOOKSMAXERBOTS, new DataObject(DataObject.VisibilityOptions.Member, "5")},
                {KEY_MINIONBOTS, new DataObject(DataObject.VisibilityOptions.Member, "5")},
                {KEY_ENJOYERBOTS, new DataObject(DataObject.VisibilityOptions.Member, "5")},
                {KEY_PSYCHOBOTS, new DataObject(DataObject.VisibilityOptions.Member, "5")},
                {KEY_GAMEMODE_NUMBER, new DataObject(DataObject.VisibilityOptions.Public, gameMode.ToString(), DataObject.IndexOptions.S1)},

            }
        };

        Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);

        joinedLobby = lobby;

        OnJoinedLobby?.Invoke(this, new LobbyEventArgs { lobby = lobby });

        Debug.Log("Created Lobby " + lobby.Name);
    }

    private async void HandleLobbyHeartbeat()
    {
        if (hostLobby != null)
        {
            heartBeatTimer -= Time.deltaTime;
            if (heartBeatTimer < 0)
            {
                float heartBeatTimerMax = heartBeatTimer;
                heartBeatTimer = heartBeatTimerMax;

                await LobbyService.Instance.SendHeartbeatPingAsync(hostLobby.Id);
            }
        }
    }
    public async void JoinLobbyByCode(string lobbyCode)
    {
        try
        {
            Player player = GetPlayer();


            joinedLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode, new JoinLobbyByCodeOptions
            {
                Player = player
            });


            OnJoinedLobby?.Invoke(this, new LobbyEventArgs { lobby = joinedLobby });

        }
        catch (Exception)
        {
            connectingTitle.SetActive(false);
            couldNotJoinTitle.SetActive(true);
            Debug.Log("Could Not Connect to game");
        }
    }

    private Player GetPlayer()
    {
        return new Player
        {
            Data = new Dictionary<string, PlayerDataObject>
                    {
                        {"PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, GameDataHolder.multiplayerName) },
                        {"PlayerSkin", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, SaveDataManager.selectedSkinBase.ToString()) },
                        {"PlayerBody", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, SaveDataManager.selectedSkinBody.ToString()) },
                        {"PlayerRank", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, Mathf.FloorToInt(1 + (SaveDataManager.totalPigeonXPEarned / 10000f) + (SaveDataManager.gamesPlayed / 5f)).ToString()) },
                        {"PlayerHead", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, SaveDataManager.selectedSkinHead.ToString()) },
                        {"Flock", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, "0") },
                    }
        };
    }
    public async void UpdateLobbySettings(string mapName, string botDiff, string flockAmt)
    {
        try
        {
            Lobby lobby = await Lobbies.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject> {
                    {KEY_START_GAME, new DataObject(DataObject.VisibilityOptions.Member, "0")},
                    {KEY_BOT_AMT, new DataObject(DataObject.VisibilityOptions.Member, GameDataHolder.botsToSpawn.ToString())},
                    {KEY_DIFFICULTY, new DataObject(DataObject.VisibilityOptions.Public, botDiff)},
                    {KEY_GAMEMODE, new DataObject(DataObject.VisibilityOptions.Public, joinedLobby.Data[KEY_GAMEMODE].Value)},
                    {KEY_MAP_NAME, new DataObject(DataObject.VisibilityOptions.Member, mapName)},
                    {KEY_FLOCK_AMT, new DataObject(DataObject.VisibilityOptions.Member, flockAmt)},
                    {KEY_LOOKSMAXERBOTS, new DataObject(DataObject.VisibilityOptions.Member, GameDataHolder.botsFlock4.ToString())},
                    {KEY_MINIONBOTS, new DataObject(DataObject.VisibilityOptions.Member, GameDataHolder.botsFlock3.ToString())},
                    {KEY_ENJOYERBOTS, new DataObject(DataObject.VisibilityOptions.Member,GameDataHolder.botsFlock1.ToString())},
                    {KEY_PSYCHOBOTS, new DataObject(DataObject.VisibilityOptions.Member,GameDataHolder.botsFlock2.ToString())},
                }
            });

            joinedLobby = lobby;

            OnLobbySettingsChanged?.Invoke(this, new LobbyEventArgs { lobby = joinedLobby });
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }
    private async void HandleLobbyPolling()
    {
        if (joinedLobby != null)
        {
            lobbyPollTimer -= Time.deltaTime;
            if (lobbyPollTimer < 0f)
            {
                float lobbyPollTimerMax = 1.1f;
                lobbyPollTimer = lobbyPollTimerMax;

                joinedLobby = await LobbyService.Instance.GetLobbyAsync(joinedLobby.Id);

                if (joinedLobby.Data[KEY_START_GAME].Value != "0")
                {
                    if (!IsLobbyHost())
                    {
                        //loby host already joined relay
                        JoinRelay(joinedLobby.Data[KEY_START_GAME].Value);
                        joinedLobby = null;
                    }
                    else
                    {
                        joinedLobby = null;
                        return;
                    }
                }

                if (!IsPlayerInLobby())
                {
                    // Player was kicked out of this lobby
                    Debug.Log("Kicked from Lobby!");

                    OnKickedFromLobby?.Invoke(this, new LobbyEventArgs { lobby = joinedLobby });

                    joinedLobby = null;

                    return;
                }
                else
                {
                    OnJoinedLobbyUpdate?.Invoke(this, new LobbyEventArgs { lobby = joinedLobby });
                }


            }
        }
    }

    private bool IsPlayerInLobby()
    {
        if (joinedLobby != null && joinedLobby.Players != null)
        {
            foreach (Player player in joinedLobby.Players)
            {
                if (player.Id == AuthenticationService.Instance.PlayerId)
                {
                    // This player is in this lobby
                    return true;
                }
            }
        }
        return false;
    }
    public async void UpdatePlayerFlock(string newFlock)
    {
        try
        {
            switch (newFlock)
            {
                case "0":
                    GameDataHolder.flock = 0;
                    break;
                case "1":
                    GameDataHolder.flock = 1;
                    break;
                case "2":
                    GameDataHolder.flock = 2;
                    break;
                case "3":
                    GameDataHolder.flock = 3;
                    break;
                case "4":
                    GameDataHolder.flock = 4;
                    break;
            }

            if (GameDataHolder.isSinglePlayer)
            {
                OnJoinedLobbyUpdate?.Invoke(this, new LobbyEventArgs { lobby = joinedLobby });
                return;
            }
            await LobbyService.Instance.UpdatePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId, new UpdatePlayerOptions
            {
                Data = new Dictionary<string, PlayerDataObject>
                    {
                        {"PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, GameDataHolder.multiplayerName) },
                                                {"PlayerSkin", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, SaveDataManager.selectedSkinBase.ToString()) },
                        {"PlayerBody", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, SaveDataManager.selectedSkinBody.ToString()) },
                        {"PlayerRank", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, Mathf.FloorToInt(1 + (SaveDataManager.totalPigeonXPEarned / 10000f) + (SaveDataManager.gamesPlayed / 5f)).ToString())  },
                        {"PlayerHead", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, SaveDataManager.selectedSkinHead.ToString()) },
                        {"Flock", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, newFlock) },
                    }
            });
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }
    public async void LeaveLobby()
    {
        if (GameDataHolder.isSinglePlayer)
        {
            OnLeftLobby?.Invoke(this, EventArgs.Empty);
        }
        if (joinedLobby != null)
        {
            try
            {
                await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId);
                joinedLobby = null;

                OnLeftLobby?.Invoke(this, EventArgs.Empty);
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }
        }
    }

    public async void KickPlayer(string playerId)
    {
        if (IsLobbyHost())
        {
            try
            {
                await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, playerId);

            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }
        }
    }
    public bool IsLobbyHost()
    {
        return joinedLobby != null && joinedLobby.HostId == AuthenticationService.Instance.PlayerId;
    }
    public async void RefreshLobbyList()
    {
        try
        {
            QueryLobbiesOptions options = new QueryLobbiesOptions();
            options.Count = 25;

            // Filter for open lobbies only
            options.Filters = new List<QueryFilter> {
                new QueryFilter(
                    field: QueryFilter.FieldOptions.AvailableSlots,
                    op: QueryFilter.OpOptions.GT,
                    value: "0")
            };

            // Order by newest lobbies first
            options.Order = new List<QueryOrder> {
                new QueryOrder(
                    asc: false,
                    field: QueryOrder.FieldOptions.Created)
            };

            QueryResponse lobbyListQueryResponse = await Lobbies.Instance.QueryLobbiesAsync();

            OnLobbyListChanged?.Invoke(this, new OnLobbyListChangedEventArgs { lobbyList = lobbyListQueryResponse.Results });
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }
    public async void JoinLobby(Lobby lobby)
    {
        Player player = GetPlayer();

        joinedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobby.Id, new JoinLobbyByIdOptions
        {
            Player = player
        });

        OnJoinedLobby?.Invoke(this, new LobbyEventArgs { lobby = lobby });
    }

    public Lobby GetJoinedLobby()
    {
        return joinedLobby;
    }
    public async void StartGame(int BofDiff, int flocks)
    {
        if (GameDataHolder.gameMode == 2) GameDataHolder.gameMode = 0;
        if (flocks == 2)
        {
            GameDataHolder.botsFlock3 = 0;
            GameDataHolder.botsFlock4 = 0;
        }
        if (flocks == 3)
        {
            GameDataHolder.botsFlock4 = 0;
        }

        if (GameDataHolder.isSinglePlayer)
        {
            NetworkManager.Singleton.StartHost();

            switch (GameDataHolder.map)
            {
                case 0:
                    NetworkManager.Singleton.SceneManager.LoadScene("KTown", LoadSceneMode.Single);
                    break;
                case 1:
                    NetworkManager.Singleton.SceneManager.LoadScene("Yu Gardens", LoadSceneMode.Single);
                    break;
                case 2:
                    NetworkManager.Singleton.SceneManager.LoadScene("Central Park", LoadSceneMode.Single);
                    break;
            }
            GameDataHolder.playerCount = 1;
        }
        if (IsLobbyHost())
        {
            try
            {
                GameDataHolder.botDifficulty = BofDiff;

                string relayCode = await CreateRelay();
                Lobby lobby = await Lobbies.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions
                {
                    Data = new Dictionary<string, DataObject>
                    {
                        {KEY_START_GAME, new DataObject(DataObject.VisibilityOptions.Member, relayCode)}
                    }
                });

                switch (GameDataHolder.map)
                {
                    case 0:
                        NetworkManager.Singleton.SceneManager.LoadScene("KTown", LoadSceneMode.Single);
                        break;
                    case 1:
                        NetworkManager.Singleton.SceneManager.LoadScene("Yu Gardens", LoadSceneMode.Single);
                        break;
                    case 2:
                        NetworkManager.Singleton.SceneManager.LoadScene("Central Park", LoadSceneMode.Single);
                        break;
                }
                GameDataHolder.playerCount = lobby.Players.Count;
                joinedLobby = null;
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }
        }
    }
    public async void QuickJoinLobby(int mode)
    {
        try
        {
            Player player = GetPlayer();

            startingQPTitle.SetActive(true);
            QuickJoinLobbyOptions options = new QuickJoinLobbyOptions
            {
                Player = player,
                Filter = new List<QueryFilter>
                {
                    new QueryFilter(QueryFilter.FieldOptions.S1, mode.ToString(), QueryFilter.OpOptions.EQ)
                }
            };

            Lobby lobby = await LobbyService.Instance.QuickJoinLobbyAsync(options);
            joinedLobby = lobby;
            startingQPTitle.SetActive(false);

            OnJoinedLobby?.Invoke(this, new LobbyEventArgs { lobby = lobby });
        }
        catch (Exception)
        {
            startingQPTitle.SetActive(false);
            switch (mode)
            {
                case 0:
                    CreateLobby(GameDataHolder.multiplayerName + "'s game", 20, false, 0);
                    break;
                case 1:
                    CreateLobby(GameDataHolder.multiplayerName + "'s Co-op game", 4, false, 1);
                    break;
                case 2:
                    CreateLobby(GameDataHolder.multiplayerName + "'s game", 20, false, 2);
                    break;

            }
        }
    }

    #endregion

    #region Relay
    public async Task<string> CreateRelay()
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(69);

            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);



            RelayServerData relayServerData = new RelayServerData(allocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
            NetworkManager.Singleton.StartHost();
            return joinCode;
        }
        catch (RelayServiceException e)
        {
            Debug.LogException(e);
            return null;
        }
    }
    public async void JoinRelay(string joinCode)
    {
        try
        {

            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            GameDataHolder.joinCode = joinCode;
            RelayServerData relayServerData = new RelayServerData(joinAllocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
            NetworkManager.Singleton.StartClient();
        }
        catch (RelayServiceException e)
        {
            Debug.Log(e);
        }
    }
    #endregion



}
