using TMPro;
using UnityEngine;

public class SkinButton : MonoBehaviour
{
    [SerializeField] int skinCost;
    [SerializeField] string skinName;
    public int skinID;
    public bool isLocked;
    public CustomizationManager.SpriteType type;


    //-1 is a normal skin this is used for clothes and hats that are unlocked with skins
    public int skinIDtoUnlock = -1;

    [SerializeField] TMP_Text costText;
    [SerializeField] GameObject costObject;
    [SerializeField] GameObject unlockedText;


    public enum SkinType
    {
        skinBasePigeon,
        head,
        body,
        skin
    }
    public void SelectSkin()
    {
        if (isLocked)
        {
            if (type != CustomizationManager.SpriteType.baseSkin)
            {
                MainMenuManager.Instance.OpenPurchaseSkinNotification(skinCost, skinName, skinIDtoUnlock);

            }
            else
            {
                MainMenuManager.Instance.OpenPurchaseSkinNotification(skinCost, skinName, skinID);
            }
        }
        else
        {
            if (skinID != -1 && (type == CustomizationManager.SpriteType.body || type == CustomizationManager.SpriteType.head))
            {
                SteamIntegration.instance.UnlockAchivement("paul_allen");
            }
            MainMenuManager.Instance.EquipSkin(skinID, type);
        }
    }
    public void UnlockSkin()
    {
        isLocked = false;
        costObject.SetActive(false);
        unlockedText.SetActive(true);
    }
    public void LockSkin()
    {

        isLocked = true;

        costObject.gameObject.SetActive(true);
        costText.text = skinCost.ToString();
        unlockedText.SetActive(false);
    }
}