using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyPlayerSingleUI : MonoBehaviour
{


    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private TextMeshProUGUI playerFlockText;
    [SerializeField] private Button kickPlayerButton;
    [SerializeField] Image pigeonBaseImage, pigeonBodyImage, pigeonHeadImage;
    [SerializeField] RankScript rs;

    private Player player;


    private void Awake()
    {
        kickPlayerButton.onClick.AddListener(KickPlayer);
    }

    public void SetKickPlayerButtonVisible(bool visible)
    {
        kickPlayerButton.gameObject.SetActive(visible);
    }

    public void UpdatePlayer(Player player, Lobby lobby)
    {
        this.player = player;
        playerNameText.text = player.Data[MultiplayerManager.KEY_PLAYER_NAME].Value;
        if (lobby.Data[MultiplayerManager.KEY_GAMEMODE].Value == "Supremacy")
        {
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
        }
        else
        {
            playerFlockText.text = "Lvl: " + player.Data[MultiplayerManager.KEY_PLAYER_RANK].Value;
            playerFlockText.color = Color.white;
        }

        rs.UpdateRank(int.Parse(player.Data[MultiplayerManager.KEY_PLAYER_RANK].Value) - 1);
        //Changes skin icon
        int skinID = int.Parse(player.Data[MultiplayerManager.KEY_PLAYER_SKIN].Value);
        if (skinID != -1) pigeonBaseImage.sprite = CustomizationManager.Instance.GetSprite(CustomizationManager.SpriteType.baseSkin, skinID, 0);
        else
        {
            pigeonBaseImage.sprite = null;
            pigeonBaseImage.gameObject.SetActive(false);
        }

        skinID = int.Parse(player.Data[MultiplayerManager.KEY_PLAYER_SKINBODY].Value);
        if (skinID != -1) pigeonBodyImage.sprite = CustomizationManager.Instance.GetSprite(CustomizationManager.SpriteType.body, skinID, 0);
        else
        {
            pigeonBodyImage.sprite = null;
            pigeonBodyImage.gameObject.SetActive(false);
        }

        skinID = int.Parse(player.Data[MultiplayerManager.KEY_PLAYER_SKINHEAD].Value);
        if (skinID != -1) pigeonHeadImage.sprite = CustomizationManager.Instance.GetSprite(CustomizationManager.SpriteType.head, skinID, 0);
        else
        {
            pigeonHeadImage.sprite = null;
            pigeonHeadImage.gameObject.SetActive(false);
        }

    }
    public void UpdatePlayerSinglePlayer()
    {
        playerNameText.text = SaveDataManager.playerName;
        if (GameDataHolder.gameMode == 0)
        {
            switch (GameDataHolder.flock)
            {
                case 0:
                    playerFlockText.text = "No Flock";
                    playerFlockText.color = Color.white;
                    break;
                case 1:
                    playerFlockText.text = "Enjoyers";
                    playerFlockText.color = Color.cyan;
                    break;
                case 2:
                    playerFlockText.text = "Psychos";
                    playerFlockText.color = Color.red;
                    break;
                case 3:
                    playerFlockText.text = "Minions";
                    playerFlockText.color = Color.yellow;
                    break;
                case 4:
                    playerFlockText.text = "Looksmaxers";
                    playerFlockText.color = Color.green;
                    break;
            }
        }
        else
        {
            playerFlockText.text = "Lvl: " + (1 + Mathf.FloorToInt((SaveDataManager.totalPigeonXPEarned / 10000f) + (SaveDataManager.gamesPlayed / 5f))).ToString();
            playerFlockText.color = Color.white;
        }

        rs.UpdateRank(Mathf.FloorToInt((SaveDataManager.totalPigeonXPEarned / 10000f) + (SaveDataManager.gamesPlayed / 5f)));
        //Changes skin icon
        int skinID = SaveDataManager.selectedSkinBase;
        if (skinID != -1) pigeonBaseImage.sprite = CustomizationManager.Instance.GetSprite(CustomizationManager.SpriteType.baseSkin, skinID, 0);
        else
        {
            pigeonBaseImage.sprite = null;
            pigeonBaseImage.gameObject.SetActive(false);
        }

        skinID = SaveDataManager.selectedSkinBody;
        if (skinID != -1) pigeonBodyImage.sprite = CustomizationManager.Instance.GetSprite(CustomizationManager.SpriteType.body, skinID, 0);
        else
        {
            pigeonBodyImage.sprite = null;
            pigeonBodyImage.gameObject.SetActive(false);
        }

        skinID = SaveDataManager.selectedSkinHead;
        if (skinID != -1) pigeonHeadImage.sprite = CustomizationManager.Instance.GetSprite(CustomizationManager.SpriteType.head, skinID, 0);
        else
        {
            pigeonHeadImage.sprite = null;
            pigeonHeadImage.gameObject.SetActive(false);
        }

    }

    private void KickPlayer()
    {
        if (player != null)
        {
            MultiplayerManager.Instance.KickPlayer(player.Id);
        }
    }


}