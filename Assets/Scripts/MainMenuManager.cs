using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    public static MainMenuManager Instance;

    [Header("Menus")]
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject lobbyListMenu;
    [SerializeField] private GameObject playMenu;
    [SerializeField] private GameObject customizationMenu;
    [SerializeField] private GameObject credits;
    [SerializeField] private GameObject purchaseMenu;
    [SerializeField] private GameObject veiwLobbiesCard;
    [SerializeField] private GameObject skins, skinBody, skinHead, skinBase;
    [SerializeField] private TMP_Text chadCoinsText;
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private TMP_FontAsset russianFont;

    [Header("Unlockables")]
    [SerializeField] SkinButton[] skinButtons;
    [SerializeField] SkinButton[] skinBaseButtons;
    [SerializeField] SkinButton[] skinHatButtons;
    [SerializeField] SkinButton[] skinClothesButtons;

    [Header("Customization stuff")]
    [SerializeField] TMP_Text skinChadCoinsCostText;
    [SerializeField] TMP_Text skinNameText;
    [SerializeField] Image pigeonBodyImage;
    [SerializeField] Image pigeonHeadImage;
    [SerializeField] Image pigeonBaseImage;

    [SerializeField] AudioSource musicSorce;
    [SerializeField] AudioClip mainMenuMusic;
    [SerializeField] AudioClip customizationMusic;
    [SerializeField] AudioClip creditsMusic;
    [SerializeField] CreditsScrollScript creditsScroll;

    private SaveDataManager.Skins skinTryingToUnlock = SaveDataManager.Skins.classic;
    private int skinCost;

    private void Awake()
    {
        Instance = this;
        chadCoinsText.text = SaveDataManager.chadCoins.ToString();
        nameInput.text = SaveDataManager.playerName.ToString();
        GameDataHolder.ResetStats();
        RefreshShopButtons();
        if (GameDataHolder.isSinglePlayer) veiwLobbiesCard.gameObject.SetActive(false);
        if (SaveDataManager.gamesPlayed >= 20)
        {
            SteamIntegration.instance.UnlockAchivement("average_chadgeon");
            if (SaveDataManager.gamesPlayed >= 100)
            {
                SteamIntegration.instance.UnlockAchivement("average_enjoyer");
            }
        }

    }
    private void Start()
    {

        MultiplayerManager.Instance.OnLeftLobby += LobbyManager_OnLeftLobby;
        MultiplayerManager.Instance.OnKickedFromLobby += LobbyManager_OnLeftLobby;
    }
    public void OpenPurchaseSkinNotification(int selectedSkinCost, string skinName, int skinID)
    {
        purchaseMenu.SetActive(true);
        skinChadCoinsCostText.text = selectedSkinCost.ToString();
        skinNameText.text = "Unlock " + skinName + " skin?";
        skinTryingToUnlock = (SaveDataManager.Skins)skinID;
        skinCost = selectedSkinCost;
    }
    public void ClosePurchaseSkinNotification()
    {
        purchaseMenu.SetActive(false);
    }
    public void EquipSkin(int skinID, CustomizationManager.SpriteType skinType)
    {
        ClosePurchaseSkinNotification();
        int id = skinID;
        if (id == -1)
        {
            switch (skinType)
            {
                case CustomizationManager.SpriteType.baseSkin:
                    pigeonBaseImage.sprite = null;
                    pigeonBaseImage.gameObject.SetActive(false);
                    SaveDataManager.selectedSkinBase = -1;
                    break;
                case CustomizationManager.SpriteType.body:
                    pigeonBodyImage.sprite = null;
                    pigeonBodyImage.gameObject.SetActive(false);
                    SaveDataManager.selectedSkinBody = -1;

                    break;
                case CustomizationManager.SpriteType.head:
                    pigeonHeadImage.sprite = null;
                    pigeonHeadImage.gameObject.SetActive(false);
                    SaveDataManager.selectedSkinHead = -1;

                    break;

            }
        }
        else
        {
            switch (skinType)
            {
                case CustomizationManager.SpriteType.baseSkin:
                    pigeonBaseImage.sprite = CustomizationManager.Instance.GetSprite(skinType, skinID, 0);
                    pigeonBaseImage.gameObject.SetActive(true);
                    SaveDataManager.selectedSkinBase = skinID;
                    break;
                case CustomizationManager.SpriteType.body:
                    pigeonBodyImage.sprite = CustomizationManager.Instance.GetSprite(skinType, skinID, 0);
                    pigeonBodyImage.gameObject.SetActive(true);
                    SaveDataManager.selectedSkinBody = skinID;

                    break;
                case CustomizationManager.SpriteType.head:
                    pigeonHeadImage.sprite = CustomizationManager.Instance.GetSprite(skinType, skinID, 0);
                    pigeonHeadImage.gameObject.SetActive(true);
                    SaveDataManager.selectedSkinHead = skinID;
                    break;

            }
        }

    }
    public void UnlockSkin()
    {
        if (SaveDataManager.chadCoins < skinCost) return;
        SaveDataManager.unlockedSkins.Add(skinTryingToUnlock);
        Debug.Log(skinTryingToUnlock);
        SaveDataManager.chadCoins -= skinCost;
        chadCoinsText.text = SaveDataManager.chadCoins.ToString();
        purchaseMenu.SetActive(false);
        RefreshShopButtons();
        SaveDataManager.SaveGameData();
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
    public void OpenCredits()
    {
        musicSorce.clip = creditsMusic;
        musicSorce.Play();
        creditsScroll.StartScroll();
        credits.SetActive(true);
        mainMenu.SetActive(false);
    }
    public void OpenCustomizations()
    {
        musicSorce.clip = customizationMusic;
        musicSorce.Play();
        if (SaveDataManager.selectedSkinBase != -1) pigeonBaseImage.sprite = CustomizationManager.Instance.GetSprite(CustomizationManager.SpriteType.baseSkin, SaveDataManager.selectedSkinBase, 0);
        else
        {
            pigeonBaseImage.sprite = null;
            pigeonBaseImage.gameObject.SetActive(false);
        }
        if (SaveDataManager.selectedSkinBody != -1) pigeonBodyImage.sprite = CustomizationManager.Instance.GetSprite(CustomizationManager.SpriteType.body, SaveDataManager.selectedSkinBody, 0);
        else
        {
            pigeonBodyImage.sprite = null;
            pigeonBodyImage.gameObject.SetActive(false);
        }
        if (SaveDataManager.selectedSkinHead != -1) pigeonHeadImage.sprite = CustomizationManager.Instance.GetSprite(CustomizationManager.SpriteType.head, SaveDataManager.selectedSkinHead, 0);
        else
        {
            pigeonHeadImage.sprite = null;
            pigeonHeadImage.gameObject.SetActive(false);
        }


        customizationMenu.SetActive(true);
        mainMenu.SetActive(false);
    }
    public void CloseCustomizations()
    {
        musicSorce.clip = mainMenuMusic;
        musicSorce.Play();
        customizationMenu.SetActive(false);
        mainMenu.SetActive(true);
        SaveDataManager.SaveGameData();
    }
    public void ClosePlayMenu()
    {
        playMenu.SetActive(false);
        mainMenu.SetActive(true);
    }
    public void CloseCredits()
    {
        musicSorce.clip = mainMenuMusic;
        musicSorce.Play();
        credits.SetActive(false);
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
        GameDataHolder.gameMode = mode;
        MultiplayerManager.Instance.QuickJoinLobby(mode);
    }
    private void LobbyManager_OnLeftLobby(object sender, EventArgs e)
    {
        playMenu.SetActive(true);
    }
    private void RefreshShopButtons()
    {
        for (int i = 0; i < skinButtons.Length; i++)
        {
            if (SaveDataManager.unlockedSkins.Contains((SaveDataManager.Skins)skinButtons[i].skinID) || skinButtons[i].skinID == -1)
            {
                skinButtons[i].UnlockSkin();
            }
            else
            {
                skinButtons[i].LockSkin();
            }
        }


        //Outfits
        for (int i = 0; i < skinBaseButtons.Length; i++)
        {
            if (SaveDataManager.unlockedSkins.Contains((SaveDataManager.Skins)skinBaseButtons[i].skinIDtoUnlock))
            {
                skinBaseButtons[i].UnlockSkin();
            }
            else
            {
                skinBaseButtons[i].LockSkin();
            }
        }
        for (int i = 0; i < skinHatButtons.Length; i++)
        {
            if (SaveDataManager.unlockedSkins.Contains((SaveDataManager.Skins)skinHatButtons[i].skinIDtoUnlock))
            {
                skinHatButtons[i].UnlockSkin();
            }
            else
            {
                skinHatButtons[i].LockSkin();
            }
        }
        for (int i = 0; i < skinClothesButtons.Length; i++)
        {
            if (SaveDataManager.unlockedSkins.Contains((SaveDataManager.Skins)skinClothesButtons[i].skinIDtoUnlock))
            {
                skinClothesButtons[i].UnlockSkin();
            }
            else
            {
                skinClothesButtons[i].LockSkin();
            }
        }
    }


    public void OpenTwitter()
    {

        Application.OpenURL("https://twitter.com/GlassyTurtle");
    }


    public void OpenYT()
    {

        Application.OpenURL("https://www.youtube.com/channel/UCQdwhf-zvwglBAa761oexWQ");
    }
    public void OpenSongs()
    {

        Application.OpenURL("https://youtube.com/playlist?list=PLUYNLp0A5I3PStgzZdJwoqV9LUWLZl9Id&si=clO00wd0pqjOSIDk");
    }
    public void OpenDiscord()
    {

        Application.OpenURL("https://t.co/6wGb4HO8pV");
    }
}
