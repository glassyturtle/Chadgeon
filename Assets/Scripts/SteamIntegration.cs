using Steamworks;
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

            // Check if the DLC is owned
            if (SteamApps.IsDlcInstalled(3070680))
            {
                // DLC is owned, unlock the maps
                Debug.Log("DLC is owned, unlock the maps");
                GameDataHolder.hasLiterallyMeDLC = true;
            }
            else
            {
                // DLC is not owned, do not unlock the maps
                Debug.Log("DLC is not owned");
                GameDataHolder.hasLiterallyMeDLC = false;
            }
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
    public void LockAchivement(string id)
    {
        Debug.Log(id);
        try
        {
            var ach = new Steamworks.Data.Achievement(id);
            ach.Clear();
        }
        catch
        {
        }

    }
}
