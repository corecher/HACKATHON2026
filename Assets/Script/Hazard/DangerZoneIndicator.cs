using System.Collections;
using UnityEngine;

// 경고 표시 후 실제 타격까지 시차를 만드는 컴포넌트.
public enum DangerZoneMode
{
    DamageOnExpire, // 경고가 끝나는 순간 겹쳐 있는 플레이어에게 데미지 (경고 중엔 안전)
    TelegraphOnly   // 데미지 없이 경고만 표시 후 사라짐 (그 자리에 실제 장애물이 스폰되는 패턴용)
}

public class DangerZoneIndicator : MonoBehaviour
{
    [Header("연결 대상")]
    public SpriteRenderer spriteRenderer;

    [Header("경고 연출")]
    public Color warningColor = new Color(1f, 0f, 0f, 0.35f);

    private DangerZoneMode mode;
    private float warnTime;
    private Coroutine warnCoroutine;

    void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Setup(Vector2 size, float warnDuration, DangerZoneMode zoneMode)
    {
        mode = zoneMode;
        warnTime = warnDuration;
        transform.localScale = new Vector3(size.x, size.y, 1f);

        if (spriteRenderer != null)
        {
            spriteRenderer.color = warningColor;
            spriteRenderer.enabled = true;
        }

        if (warnCoroutine != null) StopCoroutine(warnCoroutine);
        warnCoroutine = StartCoroutine(CoWarn());
    }

    // PatternManager가 게임오버 시 강제 정리할 때 사용.
    public void Cancel()
    {
        if (warnCoroutine != null)
        {
            StopCoroutine(warnCoroutine);
            warnCoroutine = null;
        }
        gameObject.SetActive(false);
    }

    private IEnumerator CoWarn()
    {
        yield return new WaitForSeconds(warnTime);

        if (mode == DangerZoneMode.DamageOnExpire)
        {
            DamageOverlappingPlayer();
        }

        warnCoroutine = null;
        gameObject.SetActive(false);
    }

    private void DamageOverlappingPlayer()
    {
        Vector2 center = transform.position;
        Vector2 size = new Vector2(transform.localScale.x, transform.localScale.y);
        Collider2D hit = Physics2D.OverlapBox(center, size, 0f);
        if (hit == null) return;

        PlayerHealth playerHealth = hit.GetComponent<PlayerHealth>();
        if (playerHealth != null) playerHealth.TakeDamage();
    }
}
