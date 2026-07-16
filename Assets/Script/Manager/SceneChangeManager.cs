using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChangeManager : MonoBehaviour
{
    public void SceneChange(string newScene)
    {
        GameManager.Instance.ChangeState(GameState.Playing);
        SceneManager.LoadScene(newScene);
    }
}
