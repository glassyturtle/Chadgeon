using UnityEngine;

public class MainMenuManager : MonoBehaviour
{
    [Header("Menus")]
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject lobbyListMenu;


    public void QuitGame()
    {
        Application.Quit();
    }
    public void OpenLobbyList()
    {
        mainMenu.SetActive(false);

        lobbyListMenu.SetActive(true);
        MultiplayerManager.Instance.RefreshLobbyList();
    }

}
