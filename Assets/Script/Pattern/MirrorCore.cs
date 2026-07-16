using System.Collections;
using UnityEngine;

public class MirrorCore : MonoBehaviour
{
    [SerializeField] private GameObject fragmentPrefab;
    [SerializeField] private int fragmentCount = 8;
    [SerializeField] private float fragmentSpeed = 5f;
    [SerializeField] private float delayBeforeShatter = 1.5f;
    [SerializeField] private float lineLength = 15f;

    private LineRenderer lineRenderer;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
    }

    private void Start()
    {
        StartCoroutine(ShatterRoutine());
    }

    private IEnumerator ShatterRoutine()
    {
        DrawWarningLines();
        yield return new WaitForSeconds(delayBeforeShatter);

        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 0;
        }

        Shatter();
        Destroy(gameObject);
    }

    private void DrawWarningLines()
    {
        if (lineRenderer == null) return;

        lineRenderer.positionCount = fragmentCount * 2;
        float angleStep = 360f / fragmentCount;
        Vector3 origin = transform.position;

        for (int i = 0; i < fragmentCount; i++)
        {
            Vector3 direction = Quaternion.Euler(0f, 0f, i * angleStep) * Vector3.right;
            Vector3 targetPosition = origin + direction * lineLength;

            lineRenderer.SetPosition(i * 2, origin);
            lineRenderer.SetPosition(i * 2 + 1, targetPosition);
        }
    }

    private void Shatter()
    {
        float angleStep = 360f / fragmentCount;
        Vector3 spawnPos = transform.position;

        for (int i = 0; i < fragmentCount; i++)
        {
            Vector3 direction = Quaternion.Euler(0f, 0f, i * angleStep) * Vector3.right;

            GameObject fragment = Instantiate(fragmentPrefab, spawnPos, Quaternion.identity);
            MirrorFragment fragmentScript = fragment.GetComponent<MirrorFragment>();

            if (fragmentScript != null)
            {
                fragmentScript.SetDirectionAndSpeed(direction, fragmentSpeed);
            }
        }
    }
}
