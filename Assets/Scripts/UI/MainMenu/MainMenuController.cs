using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "SampleScene";

    private void Start()
    {
        if (SoundManager.GetOrCreate() != null)
        {
            SoundManager.GetOrCreate().PlayTheme();
        }
    }

    public void StartGame()
    {
        Debug.Log("Starting Game...");
        SceneManager.LoadScene(gameSceneName);
    }

    public void ExitGame()
    {
        Debug.Log("Exiting Game...");
        
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
