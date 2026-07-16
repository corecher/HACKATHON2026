using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChangeManager : MonoBehaviour
{
    public void SceneChange(string newScene)
    {
        SceneManager.LoadScene(newScene);
    }
}
