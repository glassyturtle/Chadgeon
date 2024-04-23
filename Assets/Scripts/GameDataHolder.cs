public static class GameDataHolder
{
    public static string joinCode = "ERROR";

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
    }
}
