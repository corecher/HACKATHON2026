using UnityEngine;
using UnityEngine.SceneManagement;
public class InteractableBed : MonoBehaviour, IInteractable
{
    [SerializeField] private GameObject interactionUI;

    private void Start()
    {
        if (interactionUI != null)
        {
            interactionUI.SetActive(false);
        }
    }

    public void ShowUI(bool visible)
    {
        if (interactionUI != null)
        {
            interactionUI.SetActive(visible);
        }
    }

    public void Interact()
    {
        SceneManager.LoadScene("PuriTestScene");
    }
}