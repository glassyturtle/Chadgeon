using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyCreateUI : MonoBehaviour
{


    public static LobbyCreateUI Instance { get; private set; }


    [SerializeField] private Button createButton;
    [SerializeField] private TMP_InputField lobbyNameField;
    [SerializeField] private TMP_Text sampleNameText;
    [SerializeField] private TMP_Text gamemodeText;
    [SerializeField] private string[] gamemodeNames;

    private string lobbyName = "Chadgeon's Game";
    private bool isPrivate = true;
    private int maxPlayers = 20;
    private int gameMode = 0;


    public void ChangeGamemode(bool left)
    {
        if (left)
        {
            gameMode -= 1;
            if (gameMode < 0)
            {
                gameMode = 0;
            }
        }
        else
        {
            gameMode += 1;
            if (gameMode > 2)
            {
                gameMode = 2;
            }
        }
        gamemodeText.text = gamemodeNames[gameMode];
    }
    private void Awake()
    {
        gameMode = 0;
        Instance = this;
        createButton.onClick.AddListener(() =>
        {
            if (lobbyNameField.text != "") lobbyName = lobbyNameField.text;

            if (gameMode
            == 1) maxPlayers = 4;
            else maxPlayers = 20;
            GameDataHolder.gameMode = gameMode;

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
        isPrivate = value;
    }
    public void ExpirimentalMaps(bool value)
    {
        Debug.Log(Time.time + " " + value);
        GameDataHolder.expirimentalMode = value;
    }
    private void Hide()
    {
        gameObject.SetActive(false);
    }
    public void Cancel()
    {
        Hide();
    }

    public void Show()
    {
        gameObject.SetActive(true);
        lobbyNameField.text = "";
        lobbyName = GameDataHolder.multiplayerName + "'s game";
        sampleNameText.text = lobbyName + "...";
        isPrivate = false;
        maxPlayers = 69;
        gameMode = 0;
    }

}