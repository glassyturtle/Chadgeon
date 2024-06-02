using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatsMenuScript : MonoBehaviour
{
    [SerializeField] TMP_Text pigeonNameText, pigeonLevelText, gamesPlayedText, pigeonsKOText, playersKOText, KOtext, foodCollectedText, xpText, upgradesText;
    [SerializeField] GameObject mainMenu;
    [SerializeField] RankScript rs;

    [SerializeField] Image pigeonBaseImage, pigeonBodyImage, pigeonHeadImage;
    public void OpenStatsMenu()
    {
        gameObject.SetActive(true);
        pigeonHeadImage.gameObject.SetActive(true);
        pigeonBaseImage.gameObject.SetActive(true);
        pigeonBodyImage.gameObject.SetActive(true);

        if (SaveDataManager.selectedSkinBase != -1) pigeonBaseImage.sprite = CustomizationManager.Instance.GetSprite(CustomizationManager.SpriteType.baseSkin, SaveDataManager.selectedSkinBase, 0);
        else
        {
            pigeonBaseImage.sprite = null;
            pigeonBaseImage.gameObject.SetActive(false);
        }
        if (SaveDataManager.selectedSkinBody != -1) pigeonBodyImage.sprite = CustomizationManager.Instance.GetSprite(CustomizationManager.SpriteType.body, SaveDataManager.selectedSkinBody, 0);
        else
        {
            pigeonBodyImage.sprite = null;
            pigeonBodyImage.gameObject.SetActive(false);
        }
        if (SaveDataManager.selectedSkinHead != -1) pigeonHeadImage.sprite = CustomizationManager.Instance.GetSprite(CustomizationManager.SpriteType.head, SaveDataManager.selectedSkinHead, 0);
        else
        {
            pigeonHeadImage.sprite = null;
            pigeonHeadImage.gameObject.SetActive(false);
        }


        mainMenu.SetActive(false);
        rs.UpdateRank(Mathf.FloorToInt((SaveDataManager.totalPigeonXPEarned / 10000f) + (SaveDataManager.gamesPlayed / 5f)));
        pigeonLevelText.text = (1 + Mathf.FloorToInt((SaveDataManager.totalPigeonXPEarned / 10000f) + (SaveDataManager.gamesPlayed / 5f))).ToString();
        pigeonNameText.text = SaveDataManager.playerName;
        gamesPlayedText.text = SaveDataManager.gamesPlayed.ToString();
        pigeonsKOText.text = SaveDataManager.totalPigeonsKo.ToString();
        playersKOText.text = SaveDataManager.playerPigeonsKo.ToString();
        KOtext.text = SaveDataManager.totalTimesKnockedOut.ToString();
        foodCollectedText.text = SaveDataManager.totalConesCollected.ToString();
        xpText.text = SaveDataManager.totalPigeonXPEarned.ToString();
        upgradesText.text = SaveDataManager.upgradesAquired.ToString();
    }

    public void HideSatsMenu()
    {
        gameObject.SetActive(false);
        mainMenu.SetActive(true);
    }
}
