using System.Collections;
using UnityEngine;

// 바닥에서 기둥이 솟아오름. 높은 기둥(점프로 못 넘음)과 낮은 기둥(점프로 넘김)을 섞어서 배치.
// 위치는 랜덤이 아니라 매 기둥 발동 직전 플레이어의 x 위치를 추적해서 정한다.
// 경고는 "기둥이 차지할 영역 전체"(폭 x 목표 높이, floorY부터)에 표시한다.
public class PillarPattern : PatternBase
{
    [Header("풀 이름")]
    public string dangerZonePoolName = "DangerZone";
    public string hazardPoolName = "RisingHazard";

    [Header("기둥 개수 (base -> maxLevel)")]
    public int basePillarCount = 3;
    public int maxPillarCount = 6;

    [Header("기둥 폭")]
    public float pillarWidth = 1f;

    [Header("기둥 높이 (플레이어 신장 배율)")]
    public float fallbackPlayerHeight = 1f; // 플레이어 SpriteRenderer를 못 찾을 때 기본값
    [Range(0f, 1f)] public float lowPillarChance = 0.3f;
    public float tallHeightMultiplierMin = 1.5f;
    public float tallHeightMultiplierMax = 2.5f;
    public float lowHeightMultiplierMin = 0.5f;
    public float lowHeightMultiplierMax = 0.9f;

    [Header("성장/유지/하강")]
    public float riseDuration = 0.2f;
    public float holdDuration = 0.6f;
    public float fallDuration = 0.2f;

    [Header("발동 간격")]
    public float activationInterval = 0.4f;

    [Header("경고 (base -> maxLevel)")]
    public float baseWarnTime = 0.9f;
    public float maxWarnTime = 0.5f;
    public float minWarnTime = 0.4f; // 안전 클램프

    public override IEnumerator Execute(PatternContext ctx)
    {
        ArenaBounds bounds = ctx.arenaBounds;
        if (bounds == null) yield break;

        int count = Mathf.RoundToInt(Mathf.Lerp(basePillarCount, maxPillarCount, ctx.difficulty));
        float warnTime = Mathf.Max(minWarnTime, Mathf.Lerp(baseWarnTime, maxWarnTime, ctx.difficulty));
        float playerHeight = GetPlayerHeight(ctx);
        float halfWidth = pillarWidth * 0.5f;

        for (int i = 0; i < count; i++)
        {
            // 발동 직전 플레이어의 현재 x 위치를 새로 캡처한다 (매 기둥마다).
            float rawX = ctx.playerTransform != null ? ctx.playerTransform.position.x : (bounds.LeftX + bounds.RightX) * 0.5f;
            float x = Mathf.Clamp(rawX, bounds.LeftX + halfWidth, bounds.RightX - halfWidth);

            bool isLow = Random.value < lowPillarChance;
            float multiplier = isLow
                ? Random.Range(lowHeightMultiplierMin, lowHeightMultiplierMax)
                : Random.Range(tallHeightMultiplierMin, tallHeightMultiplierMax);
            float height = playerHeight * multiplier;

            Vector3 stripCenter = new Vector3(x, bounds.FloorY + height * 0.5f, 0f);
            Vector2 stripSize = new Vector2(pillarWidth, height);

            GameObject zoneObj = PoolManager.Instance.SpawnFromPool(dangerZonePoolName, stripCenter, Quaternion.identity);
            DangerZoneIndicator indicator = zoneObj?.GetComponent<DangerZoneIndicator>();
            indicator?.Setup(stripSize, warnTime, DangerZoneMode.TelegraphOnly);
            PatternManager.Instance.TrackSpawn(zoneObj);

            float floorY = bounds.FloorY;
            // 경고가 뜬 순서대로만 등장하도록 큐에 등록 (독립 코루틴으로 각자 대기시키지 않는다).
            // 이 경고(indicator) 자신을 캡처해서 스폰과 동시에 직접 지운다 (자체 타이머에만 의존하지 않음).
            PatternManager.Instance.EnqueueSpawn(Time.time + warnTime, () => SpawnPillar(new Vector3(x, floorY, 0f), height, floorY, indicator));

            yield return new WaitForSeconds(activationInterval);
        }

        // 마지막 반복에서 건 EnqueueSpawn은 activationInterval보다 늦게(warnTime 후에) 발동한다.
        // 코루틴이 그보다 먼저 끝나버리면 activePatterns에서 너무 일찍 빠져서 다음 패턴이 조기 시작해버리므로,
        // 실제 마지막 장애물이 스폰될 때까지 남은 시간만큼 더 대기한다.
        yield return new WaitForSeconds(Mathf.Max(0f, warnTime - activationInterval));
    }

    public override float EstimateDuration(PatternContext ctx)
    {
        int count = Mathf.RoundToInt(Mathf.Lerp(basePillarCount, maxPillarCount, ctx.difficulty));
        float warnTime = Mathf.Max(minWarnTime, Mathf.Lerp(baseWarnTime, maxWarnTime, ctx.difficulty));
        return count * activationInterval + Mathf.Max(0f, warnTime - activationInterval);
    }

    private float GetPlayerHeight(PatternContext ctx)
    {
        if (ctx.playerTransform == null) return fallbackPlayerHeight;

        SpriteRenderer sr = ctx.playerTransform.GetComponent<SpriteRenderer>();
        return sr != null ? sr.bounds.size.y : fallbackPlayerHeight;
    }

    private void SpawnPillar(Vector3 spawnPos, float height, float floorY, DangerZoneIndicator indicator)
    {
        indicator?.Cancel(); // 장애물 스폰과 동시에 해당 경고 즉시 제거

        GameObject hazardObj = PoolManager.Instance.SpawnFromPool(hazardPoolName, spawnPos, Quaternion.identity);
        if (hazardObj == null) return;

        Vector3 targetScale = new Vector3(pillarWidth, height, 1f);
        hazardObj.GetComponent<RisingHazard>()?.Launch(riseDuration, holdDuration, fallDuration, targetScale, floorY);
        PatternManager.Instance.TrackSpawn(hazardObj);
    }
}
