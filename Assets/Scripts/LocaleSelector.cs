using System.Collections;
using UnityEngine;
using UnityEngine.Localization.Settings;

public class LocaleSelector : MonoBehaviour
{
    private bool isActive = false;
    [SerializeField] GameObject localizationMenu;
    [SerializeField] GameObject mainMenu;
    private void Start()
    {
        int ID = PlayerPrefs.GetInt("LocaleKey", 0);
        ChangeLocale(ID);
    }
    public void ChangeLocale(int localeID)
    {
        Debug.Log(isActive);
        if (isActive == true) return;
        StartCoroutine(SetLocal(localeID));
    }

    IEnumerator SetLocal(int _localID)
    {
        isActive = true;
        yield return LocalizationSettings.InitializationOperation;
        LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[_localID];
        PlayerPrefs.SetInt("LocaleKey", _localID);
        isActive = false;
        localizationMenu.SetActive(false);
        mainMenu.SetActive(true);
    }
    public void OpenLocaizationMenu()
    {
        localizationMenu.SetActive(true);
        mainMenu.SetActive(false);
    }

}
