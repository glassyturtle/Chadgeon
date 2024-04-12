using TMPro;
using UnityEngine;

public class MainMenuManager : MonoBehaviour
{
    [Header("Menus")]
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject lobbyListMenu;
    [SerializeField] private GameObject playMenu;
    [SerializeField] private GameObject customizationMenu;
    [SerializeField] private GameObject skins, skinBody, skinHead, skinBase;
    [SerializeField] private TMP_Text chadCoinsText;
    [SerializeField] private TMP_InputField nameInput;


    private void Awake()
    {
        chadCoinsText.text = SaveDataManager.chadCoins.ToString();
        nameInput.text = SaveDataManager.playerName.ToString();
    }

    public void QuitGame()
    {
        Application.Quit();
    }
    public void OpenLobbyList()
    {
        playMenu.SetActive(false);

        lobbyListMenu.SetActive(true);
        MultiplayerManager.Instance.RefreshLobbyList();
    }
    public void ChangeMultiplayerName(string name)
    {
        GameDataHolder.multiplayerName = name;
        SaveDataManager.playerName = name;
    }
    public void OpenPlayMenu()
    {
        playMenu.SetActive(true);
        mainMenu.SetActive(false);
    }
    public void OpenCustomizations()
    {
        customizationMenu.SetActive(true);
        mainMenu.SetActive(false);
    }
    public void CloseCustomizations()
    {
        customizationMenu.SetActive(false);
        mainMenu.SetActive(true);
        SaveDataManager.SaveGameData();
    }
    public void ClosePlayMenu()
    {
        playMenu.SetActive(false);
        mainMenu.SetActive(true);
    }
    public void ShowSkins()
    {
        skins.SetActive(true);

        skinHead.SetActive(false);
        skinBody.SetActive(false);
        skinBase.SetActive(false);
    }
    public void ShowOutfits()
    {
        skins.SetActive(false);

        skinHead.SetActive(true);
        skinBody.SetActive(true);
        skinBase.SetActive(true);
    }
    public void StartQuickplay(int mode)
    {
        playMenu.SetActive(false);
        MultiplayerManager.Instance.QuickJoinLobby();
    }

}
