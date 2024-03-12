using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyCreateUI : MonoBehaviour
{


    public static LobbyCreateUI Instance { get; private set; }


    [SerializeField] private Button createButton;
    [SerializeField] private TMP_InputField lobbyNameField;

    private string lobbyName = "Chadgeon's Game";
    private bool isPrivate = true;
    private int maxPlayers = 69;
    private MultiplayerManager.GameMode gameMode;

    private void Awake()
    {
        Instance = this;

        createButton.onClick.AddListener(() =>
        {
            if (lobbyNameField.text != "") lobbyName = lobbyNameField.text;
            MultiplayerManager.Instance.CreateLobby(
                lobbyName,
                maxPlayers,
                isPrivate,
                gameMode
            );
            Hide();
        });

        Hide();
    }
    public void IsPrivateBoxTicked(bool value)
    {
        isPrivate = !value;
    }
    private void Hide()
    {
        gameObject.SetActive(false);
    }

    public void Show()
    {
        gameObject.SetActive(true);

        lobbyName = "MyLobby";
        isPrivate = false;
        maxPlayers = 4;
        gameMode = MultiplayerManager.GameMode.FreeForAll;
    }

}