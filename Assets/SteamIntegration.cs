using UnityEngine;

public class SteamIntegration : MonoBehaviour
{

    public static SteamIntegration instance;

    private void Start()
    {

        instance = this;
        try
        {
            Steamworks.SteamClient.Init(2850650);
        }
        catch (System.Exception e)
        {
            Debug.LogError(e.Message);
        }
    }

    private void OnApplicationQuit()
    {
        Steamworks.SteamClient.Shutdown();
    }

    public void UnlockAchivement(string id)
    {
        Debug.Log(id);
        try
        {
            var ach = new Steamworks.Data.Achievement(id);
            ach.Trigger();
        }
        catch
        {
        }

    }
}
