using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MultiplayerManager : MonoBehaviour
{
    public static MultiplayerManager Instance { get; private set; }
    public const string KEY_GAME_MODE = "GameMode";

    private string playerSkin = "Chadgeon";

    private float lobbyPollTimer;
    private float startTimer = 3;
    private Lobby joinedLobby;

    public event EventHandler OnLeftLobby;

    public event EventHandler<LobbyEventArgs> OnJoinedLobby;
    public event EventHandler<LobbyEventArgs> OnJoinedLobbyUpdate;
    public event EventHandler<LobbyEventArgs> OnKickedFromLobby;
    public event EventHandler<LobbyEventArgs> OnLobbyGameModeChanged;

    public const string KEY_PLAYER_FLOCK = "Flock";
    public const string KEY_PLAYER_NAME = "PlayerName";
    public const string KEY_PLAYER_SKIN = "PlayerSkin";
    public const string KEY_START_GAME = "Start";
    public const string KEY_BOT_AMT = "BotCount";
    public const string KEY_MAP_NAME = "MapName";
    public const string KEY_FLOCK_AMT = "FlockAmount";
    private bool hasJoinedRelay = false;
    public class LobbyEventArgs : EventArgs
    {
        public Lobby lobby;
    }

    public event EventHandler<OnLobbyListChangedEventArgs> OnLobbyListChanged;
    public class OnLobbyListChangedEventArgs : EventArgs
    {
        public List<Lobby> lobbyList;
    }



    public enum GameMode
    {
        FlockDeathMatch,
        FreeForAll
    }

    public enum PlayerCharacter
    {
        Classic,
        Chadgeon,
        IceCream,
        Minion,
        WhereWereTheMinions,
        Forest,
        AmericanPigeon,
    }


    private void Awake()
    {
        Instance = this;
    }
    private async void Start()
    {
        try
        {
            await UnityServices.InitializeAsync();
            if (AuthenticationService.Instance.IsSignedIn) return;

            AuthenticationService.Instance.SignedIn += () =>
            {
                Debug.Log("signed in " + AuthenticationService.Instance.PlayerId);
            };
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

        }
        catch (RelayServiceException e)
        {
            Debug.LogException(e);
        }
    }
    private void Update()
    {
        HandleLobbyHeartbeat();
        HandleLobbyPolling();
    }

    #region Lobby

    private Lobby hostLobby;
    private float heartBeatTimer = 15;

    public async void CreateLobby(string lobbyName, int maxPlayers, bool isPrivate, GameMode gameMode)
    {
        Player player = GetPlayer();

        CreateLobbyOptions options = new CreateLobbyOptions
        {
            Player = player,
            IsPrivate = isPrivate,
            Data = new Dictionary<string, DataObject> {
                {KEY_GAME_MODE, new DataObject(DataObject.VisibilityOptions.Public, gameMode.ToString()) },
                {KEY_START_GAME, new DataObject(DataObject.VisibilityOptions.Member, "0")},
                {KEY_BOT_AMT, new DataObject(DataObject.VisibilityOptions.Member, "0")},
                {KEY_FLOCK_AMT, new DataObject(DataObject.VisibilityOptions.Member, "0")},
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
        catch (RelayServiceException e)
        {
            Debug.LogException(e);
        }
    }

    private Player GetPlayer()
    {
        return new Player
        {
            Data = new Dictionary<string, PlayerDataObject>
                    {
                        {"PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, GameDataHolder.multiplayerName) },
                        {"PlayerSkin", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerSkin) },
                        {"Flock", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerSkin) },
                    }
        };
    }
    public async void UpdateLobbyGameMode(GameMode gameMode)
    {
        try
        {
            Debug.Log("UpdateLobbyGameMode " + gameMode);

            Lobby lobby = await Lobbies.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject> {
                    { KEY_GAME_MODE, new DataObject(DataObject.VisibilityOptions.Public, gameMode.ToString()) }
                }
            });

            joinedLobby = lobby;

            OnLobbyGameModeChanged?.Invoke(this, new LobbyEventArgs { lobby = joinedLobby });
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

                OnJoinedLobbyUpdate?.Invoke(this, new LobbyEventArgs { lobby = joinedLobby });

                if (!IsPlayerInLobby())
                {
                    // Player was kicked out of this lobby
                    Debug.Log("Kicked from Lobby!");

                    OnKickedFromLobby?.Invoke(this, new LobbyEventArgs { lobby = joinedLobby });

                    joinedLobby = null;

                    return;
                }

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
                        startTimer -= 1f;
                        if (startTimer < 0f)
                        {
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
                            joinedLobby = null;
                        }
                    }
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
            await LobbyService.Instance.UpdatePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId, new UpdatePlayerOptions
            {
                Data = new Dictionary<string, PlayerDataObject>
                    {
                        {"PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, GameDataHolder.multiplayerName) },
                        {"PlayerSkin", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerSkin) },
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
    public async void UpdatePlayerFlock(PlayerCharacter playerFlock)
    {
        if (joinedLobby != null)
        {
            try
            {
                UpdatePlayerOptions options = new UpdatePlayerOptions();

                options.Data = new Dictionary<string, PlayerDataObject>() {
                    {
                        KEY_PLAYER_FLOCK, new PlayerDataObject(
                            visibility: PlayerDataObject.VisibilityOptions.Public,
                            value: playerFlock.ToString())
                    }
                };

                string playerId = AuthenticationService.Instance.PlayerId;

                Lobby lobby = await LobbyService.Instance.UpdatePlayerAsync(joinedLobby.Id, playerId, options);
                joinedLobby = lobby;

                OnJoinedLobbyUpdate?.Invoke(this, new LobbyEventArgs { lobby = joinedLobby });
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }
        }
    }
    public Lobby GetJoinedLobby()
    {
        return joinedLobby;
    }
    public async void StartGame(int BofDiff, int botToSpawn)
    {
        if (IsLobbyHost())
        {
            try
            {
                Debug.Log("Start Game");
                GameDataHolder.botDifficulty = BofDiff;
                GameDataHolder.botsToSpawn = botToSpawn;

                string relayCode = await CreateRelay();
                Lobby lobby = await Lobbies.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions
                {
                    Data = new Dictionary<string, DataObject>
                    {
                        {KEY_START_GAME, new DataObject(DataObject.VisibilityOptions.Member, relayCode)}
                    }
                });
                joinedLobby = lobby;
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
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