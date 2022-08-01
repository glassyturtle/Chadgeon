using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [SerializeField] GameObject playMenu, mainMenu;
    [SerializeField] AudioSource source;

    public void OpenPlayMenu()
    {
        source.Play();
        playMenu.SetActive(true);
        mainMenu.SetActive(false);
    }

    public void StartGame(int difficulty)
    {
        SuperGM.difficulty = difficulty;
        SceneManager.LoadScene("SimpMode");
    }
}
