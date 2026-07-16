using UnityEngine;

public class RoomCameraBounds : MonoBehaviour
{
    [Header("References")]
    public Camera targetCamera;
    public PlayerController playerController;

    [Header("Horizontal Clamp")]
    public float horizontalPadding = 0.5f;
    public bool updateContinuously = true;

    public float LeftX { get; private set; }
    public float RightX { get; private set; }

    void Awake()
    {
        ApplyBounds();
    }

    void LateUpdate()
    {
        if (updateContinuously)
        {
            ApplyBounds();
        }
    }

    public void ApplyBounds()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        if (playerController == null) playerController = FindFirstObjectByType<PlayerController>();
        if (targetCamera == null || playerController == null || !targetCamera.orthographic) return;

        float halfWidth = targetCamera.orthographicSize * targetCamera.aspect;
        LeftX = targetCamera.transform.position.x - halfWidth + horizontalPadding;
        RightX = targetCamera.transform.position.x + halfWidth - horizontalPadding;

        playerController.minX = LeftX;
        playerController.maxX = RightX;
    }
}
