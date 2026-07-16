using System.Collections;
using UnityEngine;

// 공중에 사각형 하나가 나타나 잠시 체공하다가 작은 사각형 여러 개로 "분열"해 방사형으로 흩어지는 패턴.
// 실제 물리적 분열이 아니라 원본을 지우고 그 자리에 파편을 동시 생성하는 방식.
//
// 경고 규칙의 의도된 예외: 기존 "경고 1개당 장애물 1개, 순차 등장" 규칙은 여러 장애물이 서로 다른 시점에
// 나타나는 패턴을 위한 것. SplitBurst는 파편 전부가 동시에 발생하는 단일 이벤트이므로
// "하나의 결합된 영역 경고 -> 동시 다발 스폰"으로 구현한다 (순서 규칙 위반이 아니라 별도 케이스).
public class SplitBurstPattern : PatternBase
{
    [Header("풀 이름")]
    public string dangerZonePoolName = "DangerZone"; // 체공 중인 원본 표시 + 분열 경고 겸용
    public string fragmentPoolName = "ProjectileHazard"; // 파편은 ProjectileHazard 재사용

    [Header("체공 위치")]
    [Range(0f, 1f)] public float hoverHeightRatio = 0.6f; // floorY(0)~ceilingY(1) 사이 비율
    public Vector2 coreSize = new Vector2(1.2f, 1.2f);
    public Color harmlessColor = new Color(1f, 1f, 1f, 0.3f); // 체공 중엔 무해함을 나타내는 반투명 색

    [Header("체공 (difficulty 무관 고정)")]
    public float hoverTime = 0.8f;

    [Header("분열 경고 (base -> maxLevel)")]
    public float baseBurstWarnTime = 0.6f;
    public float maxBurstWarnTime = 0.4f;
    public float minBurstWarnTime = 0.4f; // 안전 클램프

    [Header("파편 (base -> maxLevel)")]
    public int baseFragmentCount = 6;
    public int maxFragmentCount = 9;
    public float baseFragmentSpeed = 6f;
    public float maxFragmentSpeed = 6.9f; // base 대비 최대 1.15배
    public float fragmentLifetime = 1.2f;
    public Vector2 fragmentSize = new Vector2(0.5f, 0.5f);

    public override IEnumerator Execute(PatternContext ctx)
    {
        ArenaBounds bounds = ctx.arenaBounds;
        if (bounds == null) yield break;

        float burstWarnTime = Mathf.Max(minBurstWarnTime, Mathf.Lerp(baseBurstWarnTime, maxBurstWarnTime, ctx.difficulty));
        int fragmentCount = Mathf.RoundToInt(Mathf.Lerp(baseFragmentCount, maxFragmentCount, ctx.difficulty));
        float fragmentSpeed = Mathf.Lerp(baseFragmentSpeed, maxFragmentSpeed, ctx.difficulty);

        float x = Random.Range(bounds.LeftX + coreSize.x * 0.5f, bounds.RightX - coreSize.x * 0.5f);
        float y = Mathf.Lerp(bounds.FloorY, bounds.CeilingY, hoverHeightRatio);
        Vector3 corePos = new Vector3(x, y, 0f);

        // 1. 경고 없이 바로 등장, 무해 상태 (DangerZoneIndicator를 재사용하되 Setup()은 아직 호출 안 함 -
        //    Setup을 안 부르면 자체 타이머/데미지 로직이 전혀 안 돌아서 순수 비주얼 표시로만 쓸 수 있음).
        GameObject coreObj = PoolManager.Instance.SpawnFromPool(dangerZonePoolName, corePos, Quaternion.identity);
        DangerZoneIndicator core = coreObj?.GetComponent<DangerZoneIndicator>();
        if (core != null)
        {
            coreObj.transform.localScale = new Vector3(coreSize.x, coreSize.y, 1f);
            if (core.spriteRenderer != null)
            {
                core.spriteRenderer.color = harmlessColor;
                core.spriteRenderer.enabled = true;
            }
        }
        PatternManager.Instance.TrackSpawn(coreObj);

        // 2. 체공 (무해, difficulty와 무관하게 고정된 시간)
        yield return new WaitForSeconds(hoverTime);

        // 3. 분열 직전 경고 - 파편이 도달 가능한 전체 범위(반경 = fragmentSpeed x fragmentLifetime)를 표시.
        float burstRadius = fragmentSpeed * fragmentLifetime;
        Vector2 warnSize = new Vector2(burstRadius * 2f, burstRadius * 2f);
        core?.Setup(warnSize, burstWarnTime, DangerZoneMode.TelegraphOnly);

        yield return new WaitForSeconds(burstWarnTime);

        // 4. 원본 삭제 + 파편 fragmentCount개 동시 생성 (균등한 각도, 방사형).
        core?.Cancel();

        for (int i = 0; i < fragmentCount; i++)
        {
            float angle = (360f / fragmentCount) * i * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            GameObject fragObj = PoolManager.Instance.SpawnFromPool(fragmentPoolName, corePos, Quaternion.identity);
            if (fragObj == null) continue;

            fragObj.transform.localScale = new Vector3(fragmentSize.x, fragmentSize.y, 1f);
            ProjectileHazard frag = fragObj.GetComponent<ProjectileHazard>();
            if (frag != null)
            {
                frag.lifeTime = fragmentLifetime;
                frag.Launch(dir, fragmentSpeed);
            }
            PatternManager.Instance.TrackSpawn(fragObj);
        }

        // 파편들이 실제로 사라질 때까지 코루틴을 붙잡아둔다 (activePatterns 조기 이탈 방지, 다른 패턴과 동일 원칙).
        yield return new WaitForSeconds(fragmentLifetime);
    }

    public override float EstimateDuration(PatternContext ctx)
    {
        float burstWarnTime = Mathf.Max(minBurstWarnTime, Mathf.Lerp(baseBurstWarnTime, maxBurstWarnTime, ctx.difficulty));
        return hoverTime + burstWarnTime + fragmentLifetime;
    }
}
