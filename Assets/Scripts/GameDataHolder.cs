public static class GameDataHolder
{

    public static bool hasLiterallyMeDLC = false;
    public static bool isSinglePlayer = true;
    public static bool expirimentalMode = false;
    public static string joinCode = "ERROR";
    public static int gameMode = 0;
    public static string multiplayerName = "Chadgeon";
    public static int map = 0;
    public static int flock = 0;
    public static int botsToSpawn = 0;
    public static int botDifficulty = 0;
    public static int botsFlock1 = 0;
    public static int botsFlock2 = 0;
    public static int botsFlock3 = 0;
    public static int botsFlock4 = 0;
    public static int playerCount;

    //Stats Earned In Game
    public static int kills = 0;
    public static int conesCollected = 0;

    public static void ResetStats()
    {
        kills = 0;
        conesCollected = 0;
        flock = 0;
        map = 0;
    }

}
