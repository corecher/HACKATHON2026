using System.Collections;
using UnityEngine;

// 다양한 크기의 블럭이 천장 위에서 스폰되어 낙하, floorY에 닿으면 파괴(또는 잠깐 잔류 후 파괴).
// 경고는 "블럭의 낙하 경로 전체"(블럭 폭 x ceilingY~floorY 세로 스트립)에 표시한다.
public class DropPattern : PatternBase
{
    [Header("풀 이름")]
    public string dangerZonePoolName = "DangerZone";
    public string hazardPoolName = "FallingHazard";

    [Header("블럭 개수 (base -> maxLevel)")]
    public int baseBlockCount = 5;
    public int maxBlockCount = 9;

    [Header("블럭 크기 (폭 x 높이, 고정)")]
    public Vector2 blockSize = new Vector2(1f, 1f);

    [Header("낙하 속도 (base -> maxLevel)")]
    public float baseFallSpeed = 9f;
    public float maxFallSpeed = 10.35f; // base 대비 최대 1.15배

    [Header("스폰 간격")]
    public float spawnInterval = 0.35f;

    [Header("착지 후 잔류")]
    public float lingerAfterLandDuration = 0.3f;

    [Header("경고 (base -> maxLevel)")]
    public float baseWarnTime = 0.7f;
    public float maxWarnTime = 0.45f;
    public float minWarnTime = 0.4f; // 안전 클램프

    [Header("x 배치")]
    public float minXSeparation = 2f;
    public int placementAttempts = 20;

    public override IEnumerator Execute(PatternContext ctx)
    {
        ArenaBounds bounds = ctx.arenaBounds;
        if (bounds == null) yield break;

        int count = Mathf.RoundToInt(Mathf.Lerp(baseBlockCount, maxBlockCount, ctx.difficulty));
        float fallSpeed = Mathf.Lerp(baseFallSpeed, maxFallSpeed, ctx.difficulty);
        float warnTime = Mathf.Max(minWarnTime, Mathf.Lerp(baseWarnTime, maxWarnTime, ctx.difficulty));

        float lastX = float.NaN;

        for (int i = 0; i < count; i++)
        {
            float x = PickX(bounds, blockSize.x, lastX);
            lastX = x;

            float stripCenterY = (bounds.FloorY + bounds.CeilingY) * 0.5f;
            Vector2 stripSize = new Vector2(blockSize.x, bounds.CeilingY - bounds.FloorY);

            GameObject zoneObj = PoolManager.Instance.SpawnFromPool(dangerZonePoolName, new Vector3(x, stripCenterY, 0f), Quaternion.identity);
            DangerZoneIndicator indicator = zoneObj?.GetComponent<DangerZoneIndicator>();
            indicator?.Setup(stripSize, warnTime, DangerZoneMode.TelegraphOnly);
            PatternManager.Instance.TrackSpawn(zoneObj);

            Vector3 spawnPos = new Vector3(x, bounds.CeilingY + blockSize.y, 0f);
            // 경고가 뜬 순서대로만 등장하도록 큐에 등록 (독립 코루틴으로 각자 대기시키지 않는다).
            // 이 경고(indicator) 자신을 캡처해서 스폰과 동시에 직접 지운다 (자체 타이머에만 의존하지 않음).
            PatternManager.Instance.EnqueueSpawn(Time.time + warnTime, () => SpawnBlock(spawnPos, fallSpeed, blockSize, indicator));

            yield return new WaitForSeconds(spawnInterval);
        }

        // 마지막 반복에서 건 EnqueueSpawn은 spawnInterval보다 늦게(warnTime 후에) 발동한다.
        // 코루틴이 그보다 먼저 끝나버리면 activePatterns에서 너무 일찍 빠져서 다음 패턴이 조기 시작해버리므로,
        // 실제 마지막 블럭이 스폰될 때까지 남은 시간만큼 더 대기한다.
        yield return new WaitForSeconds(Mathf.Max(0f, warnTime - spawnInterval));
    }

    public override float EstimateDuration(PatternContext ctx)
    {
        int count = Mathf.RoundToInt(Mathf.Lerp(baseBlockCount, maxBlockCount, ctx.difficulty));
        float warnTime = Mathf.Max(minWarnTime, Mathf.Lerp(baseWarnTime, maxWarnTime, ctx.difficulty));
        return count * spawnInterval + Mathf.Max(0f, warnTime - spawnInterval);
    }

    private float PickX(ArenaBounds bounds, float width, float lastX)
    {
        float min = bounds.LeftX + width * 0.5f;
        float max = bounds.RightX - width * 0.5f;

        if (float.IsNaN(lastX)) return Random.Range(min, max);

        float candidate = lastX;
        for (int attempt = 0; attempt < placementAttempts; attempt++)
        {
            candidate = Random.Range(min, max);
            if (Mathf.Abs(candidate - lastX) >= minXSeparation) return candidate;
        }

        return Mathf.Clamp(lastX + minXSeparation, min, max);
    }

    private void SpawnBlock(Vector3 spawnPos, float fallSpeed, Vector2 size, DangerZoneIndicator indicator)
    {
        indicator?.Cancel(); // 장애물 스폰과 동시에 해당 경고 즉시 제거

        GameObject hazardObj = PoolManager.Instance.SpawnFromPool(hazardPoolName, spawnPos, Quaternion.identity);
        hazardObj?.GetComponent<FallingHazard>()?.Launch(fallSpeed, 0f, size, lingerAfterLandDuration);
        PatternManager.Instance.TrackSpawn(hazardObj);
    }
}
