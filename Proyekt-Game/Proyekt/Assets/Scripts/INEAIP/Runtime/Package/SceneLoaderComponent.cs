



using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoaderComponent : MonoBehaviour
{
    [SerializeField] private string _sceneName;
    public void LoadScene()
    {
        if (_sceneName == "") { Debug.LogError("Could not load scene. No scene name specified."); return; }
        SceneManager.LoadScene(_sceneName);
    }
}
