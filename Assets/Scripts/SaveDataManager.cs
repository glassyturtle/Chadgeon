using System.Collections.Generic;
using UnityEngine;

public class SaveDataManager : MonoBehaviour
{
    //Overall Game Data
    public static int chadCoins = 0;
    public static List<Skins> unlockedSkins = new List<Skins>();


    //Player Prefs
    public static float playerVolume;
    public static float musicVolume;
    public static float soundEffectVolume;
    public static string playerName;

    public enum Skins
    {
        classic = 0,
        chadgeon = 1,
        americanPigeon = 2,
        minion = 3,
        naziMinion = 4,
        forest = 5,
        iceCream = 6,
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
        }
        else
        {
            //If has not played before, creates new data for the player
            //Game Data
            ES3.Save("chadCoins", 0, path);

            List<Skins> defaultSkins = new List<Skins>
            {
                Skins.chadgeon,
                Skins.classic
            };
            ES3.Save("unlockedSkins", defaultSkins, path);

            //Player Prefs
            ES3.Save("playerVolume", 0, path);
            ES3.Save("musicVolume", 0, path);
            ES3.Save("soundEffectVolume", 0, path);
            ES3.Save("playerName", "Chadgeon", path);
        }
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
    }
}
