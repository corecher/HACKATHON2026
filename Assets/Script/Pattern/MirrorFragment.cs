using UnityEngine;

public class MirrorFragment : MonoBehaviour
{
    [SerializeField] private float lifetime = 5f;            // 발사 후 강제 파괴 시간 (안전장치)
    [SerializeField] private float minVisibleGrace = 0.05f; // OnBecameInvisible 무시 시간 (스폰 직후 잘못된 invisible 판정 방지)

    private float moveSpeed;
    private float elapsedTime;

    public void SetDirectionAndSpeed(Vector2 direction, float speed)
    {
        moveSpeed = speed;
        elapsedTime = 0f;

        // 발사 방향에 맞춰 회전 (방향 변수는 제거 — 회전값에 이미 정보가 있음)
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }

    private void Update()
    {
        elapsedTime += Time.deltaTime;
        if (elapsedTime >= lifetime)
        {
            Destroy(gameObject);
            return;
        }

        // 회전된 transform.right를 사용해 별도 방향 변수 없이 직진
        transform.Translate(Vector3.right * moveSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Destroy(gameObject);
        }
    }

    private void OnBecameInvisible()
    {
        // 스폰 직후 짧은 시간 동안은 첫 invisible 판정을 무시
        // (Unity의 OnBecameInvisible은 첫 프레임에 잘못 발동될 수 있음)
        if (elapsedTime < minVisibleGrace) return;

        Destroy(gameObject);
    }
}
