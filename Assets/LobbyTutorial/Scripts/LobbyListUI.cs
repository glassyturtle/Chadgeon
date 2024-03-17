using System;
using System.Collections.Generic;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyListUI : MonoBehaviour
{


    public static LobbyListUI Instance { get; private set; }


    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject joinGameMenu;
    [SerializeField] private Transform lobbySingleTemplate;
    [SerializeField] private Transform container;
    [SerializeField] private Button refreshButton;
    [SerializeField] private Button createLobbyButton;
    [SerializeField] private TMP_InputField codeInput;

    private void Awake()
    {
        Instance = this;

        lobbySingleTemplate.gameObject.SetActive(false);

        refreshButton.onClick.AddListener(RefreshButtonClick);
        createLobbyButton.onClick.AddListener(CreateLobbyButtonClick);
    }
    public void ChangeMultiplayerName(string name)
    {
        GameDataHolder.multiplayerName = name;
    }

    private void Start()
    {
        MultiplayerManager.Instance.OnLobbyListChanged += LobbyManager_OnLobbyListChanged;
        MultiplayerManager.Instance.OnJoinedLobby += LobbyManager_OnJoinedLobby;
        MultiplayerManager.Instance.OnLeftLobby += LobbyManager_OnLeftLobby;
        MultiplayerManager.Instance.OnKickedFromLobby += LobbyManager_OnKickedFromLobby;
        Hide();
    }
    public void CloseLobbyList()
    {
        mainMenu.SetActive(true);
        Hide();
    }
    private void LobbyManager_OnKickedFromLobby(object sender, MultiplayerManager.LobbyEventArgs e)
    {
        Show();
    }
    public void JoinLobbyWithCode()
    {
        MultiplayerManager.Instance.JoinLobbyByCode(codeInput.text);
    }
    public void CloseJoinMenu()
    {
        joinGameMenu.SetActive(false);
    }
    public void OpenJoinMenu()
    {
        joinGameMenu.SetActive(true);
    }

    private void LobbyManager_OnLeftLobby(object sender, EventArgs e)
    {
        Show();
    }

    private void LobbyManager_OnJoinedLobby(object sender, MultiplayerManager.LobbyEventArgs e)
    {
        Hide();
    }

    private void LobbyManager_OnLobbyListChanged(object sender, MultiplayerManager.OnLobbyListChangedEventArgs e)
    {
        UpdateLobbyList(e.lobbyList);
    }

    private void UpdateLobbyList(List<Lobby> lobbyList)
    {
        foreach (Transform child in container)
        {
            if (child == lobbySingleTemplate) continue;

            Destroy(child.gameObject);
        }

        foreach (Lobby lobby in lobbyList)
        {
            Transform lobbySingleTransform = Instantiate(lobbySingleTemplate, container);
            lobbySingleTransform.gameObject.SetActive(true);
            LobbyListSingleUI lobbyListSingleUI = lobbySingleTransform.GetComponent<LobbyListSingleUI>();
            lobbyListSingleUI.UpdateLobby(lobby);
        }
    }

    private void RefreshButtonClick()
    {
        MultiplayerManager.Instance.RefreshLobbyList();
    }

    private void CreateLobbyButtonClick()
    {
        LobbyCreateUI.Instance.Show();
    }

    private void Hide()
    {
        gameObject.SetActive(false);
        joinGameMenu.SetActive(false);
    }

    private void Show()
    {
        gameObject.SetActive(true);
    }

}