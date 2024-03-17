using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyPlayerSingleUI : MonoBehaviour
{


    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private TextMeshProUGUI playerFlockText;
    [SerializeField] private Button kickPlayerButton;


    private Player player;


    private void Awake()
    {
        kickPlayerButton.onClick.AddListener(KickPlayer);
    }

    public void SetKickPlayerButtonVisible(bool visible)
    {
        kickPlayerButton.gameObject.SetActive(visible);
    }

    public void UpdatePlayer(Player player)
    {
        this.player = player;
        playerNameText.text = player.Data[MultiplayerManager.KEY_PLAYER_NAME].Value;
        switch (player.Data[MultiplayerManager.KEY_PLAYER_FLOCK].Value)
        {
            case "0":
                playerFlockText.text = "No Flock";
                playerFlockText.color = Color.white;
                break;
            case "1":
                playerFlockText.text = "Enjoyers";
                playerFlockText.color = Color.cyan;
                break;
            case "2":
                playerFlockText.text = "Psychos";
                playerFlockText.color = Color.red;
                break;
            case "3":
                playerFlockText.text = "Minions";
                playerFlockText.color = Color.yellow;
                break;
            case "4":
                playerFlockText.text = "Looksmaxers";
                playerFlockText.color = Color.green;
                break;
        }
        MultiplayerManager.PlayerCharacter playerCharacter =
            System.Enum.Parse<MultiplayerManager.PlayerCharacter>(player.Data[MultiplayerManager.KEY_PLAYER_SKIN].Value);
        //characterImage.sprite = LobbyAssets.Instance.GetSprite(playerCharacter);
    }

    private void KickPlayer()
    {
        if (player != null)
        {
            MultiplayerManager.Instance.KickPlayer(player.Id);
        }
    }


}