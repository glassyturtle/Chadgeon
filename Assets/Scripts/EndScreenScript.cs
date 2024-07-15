using System.Collections.Generic;
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
            coinMultiplier = 0.5f;
        }
        else
        {
            coinMultiplier = 1;
        }


        int defeatedCoins = Mathf.FloorToInt(GameDataHolder.kills * 0.20f * coinMultiplier);
        chadCoinsGained += defeatedCoins;
        pigeonsDefeatedCoins.text = defeatedCoins.ToString();


        int collectedFood = Mathf.FloorToInt(GameDataHolder.conesCollected * 0.10f * coinMultiplier);
        chadCoinsGained += collectedFood;
        foodCollectedCoins.text = collectedFood.ToString();


        chadCoinsGained += Mathf.FloorToInt(GameManager.instance.player.level.Value * 0.20f * coinMultiplier);
        levelsAchivedCoins.text = Mathf.FloorToInt(GameManager.instance.player.level.Value * 0.20f * coinMultiplier).ToString();

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
                    victoryCoins.text = "7";
                    chadCoinsGained += 7;
                }
                else
                {
                    victoryText.text = "Escaped";
                    victoryCoins.text = "10";
                    chadCoinsGained += 10;
                }

                PigeonAI[] allAiPigeons = FindObjectsByType<PigeonAI>(FindObjectsSortMode.None);
                foreach (var ai in allAiPigeons)
                {
                    if (ai.pigeonName == "Ryan Gosling")
                    {
                        SteamIntegration.instance.UnlockAchivement("literally_me");
                    }
                }

                if (GameDataHolder.botDifficulty == 3)
                {
                    switch (GameDataHolder.map)
                    {
                        case 0:
                            SteamIntegration.instance.UnlockAchivement("k-town_mastery");
                            break;
                        case 1:
                            SteamIntegration.instance.UnlockAchivement("yu_gardens_mastery");
                            break;
                        case 2:
                            SteamIntegration.instance.UnlockAchivement("central_park_mastery");
                            break;
                    }
                }
            }
            else
            {
                if (GameManager.instance.player.flock != 0)
                {
                    //Tries to unlock Achivement
                    Dictionary<int, int> flocks = new Dictionary<int, int>();
                    flocks.Add(0, 0);
                    flocks.Add(1, 0);
                    flocks.Add(2, 0);
                    flocks.Add(3, 0);
                    flocks.Add(4, 0);
                    for (int i = 0; i < GameManager.instance.allpigeons.Count; i++)
                    {
                        if (flocks.TryGetValue(GameManager.instance.allpigeons[i].flock, out int value))
                        {
                            flocks[GameManager.instance.allpigeons[i].flock]++;
                        }
                    }
                    if (flocks[1] >= flocks[GameManager.instance.player.flock] * 2)
                    {
                        SteamIntegration.instance.UnlockAchivement("never_tell_us_the_odds");
                    }
                    if (flocks[2] >= flocks[GameManager.instance.player.flock] * 2)
                    {
                        SteamIntegration.instance.UnlockAchivement("never_tell_us_the_odds");
                    }
                    if (flocks[3] >= flocks[GameManager.instance.player.flock] * 2)
                    {
                        SteamIntegration.instance.UnlockAchivement("never_tell_us_the_odds");
                    }
                    if (flocks[4] >= flocks[GameManager.instance.player.flock] * 2)
                    {
                        SteamIntegration.instance.UnlockAchivement("never_tell_us_the_odds");
                    }
                }
                else
                {
                    switch (GameDataHolder.botDifficulty)
                    {
                        case 0:
                            if (GameDataHolder.botsToSpawn >= 10)
                            {
                                SteamIntegration.instance.UnlockAchivement("simp_abuser");
                            }
                            break;
                        case 1:
                            if (GameDataHolder.botsToSpawn >= 10)
                            {
                                SteamIntegration.instance.UnlockAchivement("chadgeon");
                            }
                            break;
                        case 2:
                            if (GameDataHolder.botsToSpawn >= 10)
                            {
                                SteamIntegration.instance.UnlockAchivement("sigma_male");
                                if (GameDataHolder.botsToSpawn >= 30)
                                {
                                    SteamIntegration.instance.UnlockAchivement("pigeon_valhalla");
                                }
                            }
                            break;
                    }
                }
                victoryText.text = "Glorious Victory";
                victoryCoins.text = "10";
                chadCoinsGained += 10;
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
