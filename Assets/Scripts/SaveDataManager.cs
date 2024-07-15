using System.Collections.Generic;

public static class SaveDataManager
{
    //Overall Game Data
    public static int chadCoins = 0;
    public static List<Skins> unlockedSkins = new List<Skins> { Skins.classic, Skins.chadgeon };


    //Player Prefs
    public static float playerVolume = 0;
    public static float musicVolume = -15;
    public static float soundEffectVolume = 0;
    public static string playerName = "Chadgeon";

    //Player Skin
    public static int selectedSkinBase = 1;
    public static int selectedSkinBody = -1;
    public static int selectedSkinHead = -1;

    //Stats
    public static int totalPigeonsKo = 0;
    public static int playerPigeonsKo = 0;
    public static int totalTimesKnockedOut = 0;
    public static int totalConesCollected = 0;
    public static int totalPigeonXPEarned = 0;
    public static int upgradesAquired = 0;
    public static int gamesPlayed = 0;


    public enum Skins
    {
        classic = 0,
        chadgeon = 1,
        americanPigeon = 7,
        minion = 5,
        naziMinion = 6,
        forest = 3,
        iceCream = 4,
        whitedeath = 2,
        ryanGosling = 8,
        blackDeath = 9,
        demon = 10,
        orange = 11,
        nigeon = 12,
        robot = 13,
        risk = 14,
        ice = 15,
        worm = 16,
        driver = 17,
        joker = 18,
        hisenBurg = 19,
        ken = 20,
        sigmaRule = 21,
    }


    public static void LoadGameData()
    {
        //Checks to see if player has played the game before
        string path = "data.es3";
        if (ES3.FileExists(path))
        {
            //Loading Game Data
            chadCoins = (int)ES3.Load("chadCoins", path);
            unlockedSkins = (List<Skins>)ES3.Load("unlockedSkins", path);


            //Loading Player Prefs
            playerVolume = (float)ES3.Load("playerVolume", path);
            musicVolume = (float)ES3.Load("musicVolume", path);
            soundEffectVolume = (float)ES3.Load("soundEffectVolume", path);
            playerName = (string)ES3.Load("playerName", path);


            //player equipt skin
            selectedSkinBase = (int)ES3.Load("skinBase", path);
            selectedSkinBody = (int)ES3.Load("skinBody", path);
            selectedSkinHead = (int)ES3.Load("skinHead", path);

            //Loading Stats
            totalPigeonsKo = ES3.Load("totalPigeonsKo", path, 0);
            playerPigeonsKo = ES3.Load("playerPigeonsKo", path, 0);
            totalTimesKnockedOut = ES3.Load("totalTimesKnockedOut", path, 0);
            totalConesCollected = ES3.Load("totalConesCollected", path, 0);
            totalPigeonXPEarned = ES3.Load("totalPigeonXPEarned", path, 0);
            upgradesAquired = ES3.Load("upgradesAquired", path, 0);
            gamesPlayed = ES3.Load("gamesPlayed", path, 0);
        }
        GameDataHolder.multiplayerName = playerName;

    }
    public static void SaveGameData()
    {
        //Saving Data
        string path = "data.es3";

        //Saves everything in the game data variables

        //Game Data
        ES3.Save("chadCoins", chadCoins, path);
        ES3.Save("unlockedSkins", unlockedSkins, path);

        //Player Prefs
        ES3.Save("playerVolume", playerVolume, path);
        ES3.Save("musicVolume", musicVolume, path);
        ES3.Save("soundEffectVolume", soundEffectVolume, path);
        ES3.Save("playerName", playerName, path);

        //Skin Data
        ES3.Save("skinBase", selectedSkinBase, path);
        ES3.Save("skinBody", selectedSkinBody, path);
        ES3.Save("skinHead", selectedSkinHead, path);

        ES3.Save("totalPigeonsKo", totalPigeonsKo, path);
        ES3.Save("playerPigeonsKo", playerPigeonsKo, path);
        ES3.Save("totalTimesKnockedOut", totalTimesKnockedOut, path);
        ES3.Save("totalConesCollected", totalConesCollected, path);
        ES3.Save("totalPigeonXPEarned", totalPigeonXPEarned, path);
        ES3.Save("upgradesAquired", upgradesAquired, path);
        ES3.Save("gamesPlayed", gamesPlayed, path);
    }
}
