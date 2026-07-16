using System.Collections;
using UnityEngine;

public class MirrorCore : MonoBehaviour
{
    [SerializeField] private GameObject fragmentPrefab;
    [SerializeField] private int fragmentCount = 8;
    [SerializeField] private float fragmentSpeed = 5f;
    [SerializeField] private float delayBeforeShatter = 1.5f;

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

        for (int i = 0; i < fragmentCount; i++)
        {
            float angle = i * angleStep;
            Vector3 direction = Quaternion.Euler(0, 0, angle) * Vector3.right;
            Vector3 targetPosition = transform.position + direction * 15f;

            lineRenderer.SetPosition(i * 2, transform.position);
            lineRenderer.SetPosition(i * 2 + 1, targetPosition);
        }
    }

    private void Shatter()
    {
        float angleStep = 360f / fragmentCount;

        for (int i = 0; i < fragmentCount; i++)
        {
            float angle = i * angleStep;
            Vector3 direction = Quaternion.Euler(0, 0, angle) * Vector3.right;

            GameObject fragment = Instantiate(fragmentPrefab, transform.position, Quaternion.identity);
            MirrorFragment fragmentScript = fragment.GetComponent<MirrorFragment>();
            
            if (fragmentScript != null)
            {
                fragmentScript.SetDirectionAndSpeed(direction, fragmentSpeed);
            }
        }
    }
}