using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 상점 destructionLevel로 해금/강화되는 폭발 공격. 좌클릭 발동, 쿨타임 존재, 플레이어 위치 중심 원형 범위 공격.
public class PlayerExplosionAttack : MonoBehaviour
{
    [Header("쿨타임")]
    public float cooldownTime = 3f;

    [Header("최대 반경 (base -> destructionLevel당 증가)")]
    public float baseMaxRadius = 2.0f;
    public float radiusPerLevel = 0.5f;

    [Header("연출")]
    public float burstDuration = 0.2f;
    public Sprite effectSprite;
    public Color effectColor = new Color(1f, 0.8f, 0.2f, 0.6f);
    public int effectSortingOrder = 20;

    private float cooldownTimer;

    void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Playing) return;

        // destructionLevel 0(미구매)이면 기능 자체 비활성 - 좌클릭해도 아무 반응 없음.
        if (GameManager.Instance.destructionLevel <= 0) return;

        if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;

        if (Input.GetMouseButtonDown(0) && cooldownTimer <= 0f)
        {
            cooldownTimer = cooldownTime; // 발동 즉시 쿨타임 시작
            StartCoroutine(CoExplode());
        }
    }

    private float GetMaxRadius()
    {
        int level = GameManager.Instance.destructionLevel;
        return baseMaxRadius + Mathf.Max(0, level - 1) * radiusPerLevel;
    }

    private IEnumerator CoExplode()
    {
        float maxRadius = GetMaxRadius();
        Vector3 center = transform.position;

        GameObject effectObj = new GameObject("ExplosionEffect");
        effectObj.transform.position = center;
        effectObj.transform.localScale = Vector3.zero;
        SpriteRenderer sr = effectObj.AddComponent<SpriteRenderer>();
        sr.sprite = effectSprite;
        sr.color = effectColor;
        sr.sortingOrder = effectSortingOrder;

        // 이번 폭발에서 이미 처리한 장애물은 중복 처리하지 않도록 추적.
        HashSet<HazardBase> alreadyHit = new HashSet<HazardBase>();
        float elapsed = 0f;

        while (elapsed < burstDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / burstDuration);
            float radius = Mathf.Lerp(0f, maxRadius, t);

            // 스프라이트가 지름 1유닛 기준이라 스케일 = 반경 x 2.
            effectObj.transform.localScale = new Vector3(radius * 2f, radius * 2f, 1f);
            Color c = effectColor;
            c.a = Mathf.Lerp(effectColor.a, 0f, t);
            sr.color = c;

            Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius);
            foreach (Collider2D col in hits)
            {
                HazardBase hazard = col.GetComponent<HazardBase>();
                if (hazard != null && hazard.gameObject.activeSelf && alreadyHit.Add(hazard))
                {
                    // Destroy가 아니라 풀로 반환 (기존 HazardBase.Cancel()이 이미 이 용도의 public API).
                    hazard.Cancel();
                }
            }

            yield return null;
        }

        // 이펙트 자체는 풀링 대상이 아니라 매번 새로 만들고 파괴한다.
        Destroy(effectObj);
    }
}
