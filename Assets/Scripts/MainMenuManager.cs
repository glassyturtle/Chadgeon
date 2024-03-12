using Unity.Netcode;
using UnityEngine;

public class MainMenuManager : NetworkBehaviour
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
    }

}
