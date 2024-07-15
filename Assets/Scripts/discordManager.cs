using UnityEngine;

public class discordManager : MonoBehaviour
{
    Discord.Discord discord;


    long time;
    // Start is called before the first frame update
    void Start()
    {
        discord = new Discord.Discord(1246462952991887421, (ulong)Discord.CreateFlags.NoRequireDiscord);
        time = System.DateTimeOffset.Now.ToUnixTimeMilliseconds();
        ChangeActivity();
    }
    private void OnDisable()
    {
        discord.Dispose();
    }
    public void ChangeActivity()
    {
        var activityManager = discord.GetActivityManager();
        var activity = new Discord.Activity
        {
            State = "Looksmaxing",
            Assets =
            {
                LargeImage = "logodiscord"
            },
            Timestamps =
            {
                Start =time,
            }

        };
        activityManager.UpdateActivity(activity, (res) =>
        {
            Debug.Log("Activity Updated");
        });
    }

    private void Update()
    {
        discord.RunCallbacks();
    }
}
