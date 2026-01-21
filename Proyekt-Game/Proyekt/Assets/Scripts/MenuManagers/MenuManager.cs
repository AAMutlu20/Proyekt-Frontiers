using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    // Closes the game
    public void QuitGame()
    {
        Application.Quit();
    }

    // Reloads current scene 
    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Loads scene 0
    public void MainMenu()
    {
        SceneManager.LoadScene(0);
    }

    // Loads the next scene
    public void StartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }
}