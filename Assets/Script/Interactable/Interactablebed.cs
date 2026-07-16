using UnityEngine;
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
        Debug.Log("침대와 상호작용함");
    }
}