using TMPro;
using UnityEngine;

public class EndScreenScript : MonoBehaviour
{
    [SerializeField] GameObject dominantVictory, gloriousDefeat;
    [SerializeField] TMP_Text pigeonsDefeatedCoins, foodCollectedCoins, levelsAchivedCoins, victoryCoins;
    [SerializeField] TMP_Text amtPigeonsDefeated, amtFoodCollected, amtLevelsAchived, victoryText;

    public void UpdateChadCoinWinStuff(bool pigeonWon)
    {
        int chadCoinsGained = 0;

        int defeatedCoins = Mathf.RoundToInt(GameDataHolder.kills / 0.25f);
        chadCoinsGained += defeatedCoins;
        pigeonsDefeatedCoins.text = defeatedCoins.ToString();


        int collectedCoins = Mathf.RoundToInt(GameDataHolder.conesCollected / 0.20f);
        chadCoinsGained += collectedCoins;
        foodCollectedCoins.text = collectedCoins.ToString();


        chadCoinsGained += Mathf.RoundToInt(GameManager.instance.player.level.Value / 0.5f);
        levelsAchivedCoins.text = Mathf.RoundToInt(GameManager.instance.player.level.Value / 0.5f).ToString();

        amtPigeonsDefeated.text = GameDataHolder.kills.ToString();
        amtFoodCollected.text = GameDataHolder.conesCollected.ToString();
        amtLevelsAchived.text = GameManager.instance.player.level.Value.ToString();

        if (pigeonWon)
        {
            dominantVictory.SetActive(true);
            victoryText.text = "Dominant Victory";
            victoryCoins.text = "20";
            chadCoinsGained += 20;
        }
        else
        {
            gloriousDefeat.SetActive(true);
            victoryText.text = "Glorious Defeat";
            victoryCoins.text = "5";
            chadCoinsGained += 5;
        }

        SaveDataManager.gamesPlayed++;
        SaveDataManager.chadCoins += chadCoinsGained;
        SaveDataManager.SaveGameData();
    }
}
