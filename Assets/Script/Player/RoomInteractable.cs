using UnityEngine;
using UnityEngine.SceneManagement;

public class RoomInteractable : MonoBehaviour
{
    [Header("Interaction")]
    public KeyCode interactKey = KeyCode.E;
    public string targetSceneName = "PuriTestScene";
    public GameObject promptObject;

    [Header("Transition")]
    public float fadeOutDuration = 1f;

    private bool canInteract;
    private bool isTransitioning;

    void Awake()
    {
        SetPrompt(false);
    }

    void Update()
    {
        if (!canInteract || isTransitioning) return;

        if (Input.GetKeyDown(interactKey))
        {
            StartTransition();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.GetComponentInParent<PlayerController>()) return;

        canInteract = true;
        SetPrompt(true);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.GetComponentInParent<PlayerController>()) return;

        canInteract = false;
        SetPrompt(false);
    }

    private void StartTransition()
    {
        isTransitioning = true;
        SetPrompt(false);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeState(GameState.Playing);
        }

        UIManager uiManager = FindFirstObjectByType<UIManager>();
        if (uiManager != null)
        {
            uiManager.FadeOut(fadeOutDuration, () => SceneManager.LoadScene(targetSceneName));
            return;
        }

        SceneManager.LoadScene(targetSceneName);
    }

    private void SetPrompt(bool active)
    {
        if (promptObject != null)
        {
            promptObject.SetActive(active);
        }
    }
}
