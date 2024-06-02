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

        float coinMultiplier = 1;
        if (GameDataHolder.gameMode == 1)
        {
            coinMultiplier = 0.25f;
        }
        else
        {
            coinMultiplier = 1;
        }


        int defeatedCoins = Mathf.FloorToInt(GameDataHolder.kills * 0.25f * coinMultiplier);
        chadCoinsGained += defeatedCoins;
        pigeonsDefeatedCoins.text = defeatedCoins.ToString();


        int collectedFood = Mathf.FloorToInt(GameDataHolder.conesCollected * 0.10f * coinMultiplier);
        chadCoinsGained += collectedFood;
        foodCollectedCoins.text = collectedFood.ToString();


        chadCoinsGained += Mathf.FloorToInt(GameManager.instance.player.level.Value * 0.25f * coinMultiplier);
        levelsAchivedCoins.text = Mathf.FloorToInt(GameManager.instance.player.level.Value * 0.25f * coinMultiplier).ToString();

        amtPigeonsDefeated.text = GameDataHolder.kills.ToString();
        amtFoodCollected.text = GameDataHolder.conesCollected.ToString();
        amtLevelsAchived.text = GameManager.instance.player.level.Value.ToString();

        if (pigeonWon)
        {
            dominantVictory.SetActive(true);
            gloriousDefeat.SetActive(false);
            if (GameDataHolder.gameMode == 1)
            {
                if (GameManager.instance.player.isKnockedOut.Value || !GameManager.instance.player.inEvacsite)
                {
                    victoryText.text = "Left behind";
                    victoryCoins.text = "5";
                    chadCoinsGained += 5;
                }
                else
                {
                    victoryText.text = "Escaped";
                    victoryCoins.text = "20";
                    chadCoinsGained += 20;
                }
            }
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
