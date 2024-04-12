using System.Collections.Generic;

public static class SaveDataManager
{
    //Overall Game Data
    public static int chadCoins = 0;
    public static List<Skins> unlockedSkins = new List<Skins> { Skins.classic, Skins.chadgeon };


    //Player Prefs
    public static float playerVolume = 1f;
    public static float musicVolume = 0.75f;
    public static float soundEffectVolume = 1f;
    public static string playerName = "Chadgeon";

    //Player Skin
    public static int selectedSkinBase = 1;
    public static int selectedSkinBody = -1;
    public static int selectedSkinHead = -1;



    public enum Skins
    {
        classic = 0,
        chadgeon = 1,
        americanPigeon = 2,
        minion = 3,
        naziMinion = 4,
        forest = 5,
        iceCream = 6,
        whitedeath = 7,
        ryanGosling = 8,
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

    }
}
