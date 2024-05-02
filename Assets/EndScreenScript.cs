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

        int defeatedCoins = Mathf.FloorToInt(GameDataHolder.kills * 0.25f);
        chadCoinsGained += defeatedCoins;
        pigeonsDefeatedCoins.text = defeatedCoins.ToString();


        int collectedFood = Mathf.FloorToInt(GameDataHolder.conesCollected * 0.10f);
        chadCoinsGained += collectedFood;
        foodCollectedCoins.text = collectedFood.ToString();


        chadCoinsGained += Mathf.FloorToInt(GameManager.instance.player.level.Value * 0.25f);
        levelsAchivedCoins.text = Mathf.FloorToInt(GameManager.instance.player.level.Value * 0.25f).ToString();

        amtPigeonsDefeated.text = GameDataHolder.kills.ToString();
        amtFoodCollected.text = GameDataHolder.conesCollected.ToString();
        amtLevelsAchived.text = GameManager.instance.player.level.Value.ToString();

        if (pigeonWon)
        {
            dominantVictory.SetActive(true);
            gloriousDefeat.SetActive(false);

            victoryText.text = "Dominant Victory";
            victoryCoins.text = "20";
            chadCoinsGained += 20;
        }
        else
        {
            dominantVictory.SetActive(false);
            gloriousDefeat.SetActive(true);
            victoryText.text = "Glorious Defeat";
            victoryCoins.text = "5";
            chadCoinsGained += 5;
        }

        SaveDataManager.gamesPlayed++;
        SaveDataManager.totalPigeonsKo += GameDataHolder.kills;
        SaveDataManager.chadCoins += chadCoinsGained;
        SaveDataManager.SaveGameData();
    }
}
