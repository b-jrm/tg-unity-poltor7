using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public GameObject mainMenu;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void OpenMainMenu()
    {
        mainMenu.SetActive(true);
    }

    // Update is called once per frame
    public void QuitGame()
    {
        Application.Quit();
    }
    public void PlayGame()
    {
        SceneManager.LoadScene("Poltor7");
    }

}
