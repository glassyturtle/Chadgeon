using Steamworks;
using Steamworks.Data;
using System.Collections.Generic;
using UnityEngine;

public class SteamIntegration : MonoBehaviour
{
    [SerializeField] GameObject dlcMessage;
    public static SteamIntegration instance;

    private void Start()
    {
        SaveDataManager.LoadGameData();


        instance = this;
        try
        {
            Steamworks.SteamClient.Init(2850650);
            IEnumerable<DlcInformation> dlcs = SteamApps.DlcInformation();

            // Check if the DLC is owned
            if (SteamApps.IsDlcInstalled(3070680))
            {
                // DLC is owned, unlock the maps
                Debug.Log("DLC is owned, unlock the maps");

                GameDataHolder.hasLiterallyMeDLC = true;
                if (!SaveDataManager.hasLiterallyMeDLCMessageRead)
                {
                    OpenMessage();
                }
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

    private void OpenMessage()
    {
        dlcMessage.SetActive(true);

    }
    public void Close()
    {

        dlcMessage.SetActive(false);
        SaveDataManager.hasLiterallyMeDLCMessageRead = true;
        SaveDataManager.chadCoins += 2049;
        SaveDataManager.unlockedSkins.Add(SaveDataManager.Skins.turtle);
        SaveDataManager.SaveGameData();
    }
    private void OnApplicationQuit()
    {
        Steamworks.SteamClient.Shutdown();
    }
    private void CheckDLC()
    {

        // Check if the DLC is owned
        bool ownsDLC = SteamApps.IsDlcInstalled(3070680);

        if (ownsDLC)
        {
            Debug.Log("DLC is owned!");
            // Enable DLC content or functionality
        }
        else
        {
            Debug.Log("DLC is not owned.");
            // Disable or restrict DLC content or functionality
        }
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
