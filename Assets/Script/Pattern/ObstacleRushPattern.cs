using System.Collections;
using UnityEngine;

// 허들형 장애물이 화면 오른쪽 밖에서 스폰되어 왼쪽으로 등속 이동. 회피는 점프.
// 이 패턴만 경고/인디케이터를 전혀 표시하지 않는다 — 의도된 예외 (반사적으로 피해야 하는 패턴으로 설계됨).
public class ObstacleRushPattern : PatternBase
{
    [Header("풀 이름")]
    public string hazardPoolName = "ProjectileHazard";

    [Header("허들 개수 (base -> maxLevel)")]
    public int baseHurdleCount = 4;
    public int maxHurdleCount = 7;
    public Vector2 hurdleSize = new Vector2(1f, 1.2f);

    [Header("스폰 간격 (base -> maxLevel)")]
    public float baseSpawnInterval = 1.0f;
    public float maxSpawnInterval = 0.6f;
    public float minSpawnInterval = 0.6f; // 점프 후 재점프 가능한 시간 기준 안전 클램프

    [Header("이동 속도 (base -> maxLevel)")]
    public float baseSpeed = 7f;
    public float maxSpeed = 8.05f; // base 대비 최대 1.15배

    [Header("스폰 마진")]
    public float spawnMargin = 1f;

    public override IEnumerator Execute(PatternContext ctx)
    {
        ArenaBounds bounds = ctx.arenaBounds;
        if (bounds == null) yield break;

        float spawnInterval = Mathf.Max(minSpawnInterval, Mathf.Lerp(baseSpawnInterval, maxSpawnInterval, ctx.difficulty));
        float speed = Mathf.Lerp(baseSpeed, maxSpeed, ctx.difficulty);
        int hurdleCount = Mathf.RoundToInt(Mathf.Lerp(baseHurdleCount, maxHurdleCount, ctx.difficulty));

        float hurdleY = bounds.FloorY + hurdleSize.y * 0.5f;

        // 경고 없이 바로 등장한다 (이 패턴만의 의도된 예외).
        for (int i = 0; i < hurdleCount; i++)
        {
            Vector3 spawnPos = new Vector3(bounds.RightX + spawnMargin, hurdleY, 0f);
            SpawnHurdle(spawnPos, speed);

            if (i < hurdleCount - 1) yield return new WaitForSeconds(spawnInterval);
        }
    }

    public override float EstimateDuration(PatternContext ctx)
    {
        float spawnInterval = Mathf.Max(minSpawnInterval, Mathf.Lerp(baseSpawnInterval, maxSpawnInterval, ctx.difficulty));
        int hurdleCount = Mathf.RoundToInt(Mathf.Lerp(baseHurdleCount, maxHurdleCount, ctx.difficulty));
        return Mathf.Max(0, hurdleCount - 1) * spawnInterval;
    }

    private void SpawnHurdle(Vector3 spawnPos, float speed)
    {
        GameObject hazardObj = PoolManager.Instance.SpawnFromPool(hazardPoolName, spawnPos, Quaternion.identity);
        if (hazardObj == null) return;

        hazardObj.transform.localScale = new Vector3(hurdleSize.x, hurdleSize.y, 1f);
        hazardObj.GetComponent<ProjectileHazard>()?.Launch(Vector2.left, speed);
        PatternManager.Instance.TrackSpawn(hazardObj);
    }
}
