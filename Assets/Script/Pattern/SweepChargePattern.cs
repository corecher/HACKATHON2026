using System.Collections;
using UnityEngine;

// 큰 사각형이 화면 오른쪽에서 솟아올라 잠시 대기하다가 왼쪽으로 빠르게 훑고 지나가는 패턴.
// 경고는 "사각형이 지나갈 예정인 전체 이동 경로"(leftX~rightX 전폭 x 사각형 높이)에 표시한다.
public class SweepChargePattern : PatternBase
{
    [Header("풀 이름")]
    public string dangerZonePoolName = "DangerZone";
    public string hazardPoolName = "SweepHazard";

    [Header("크기")]
    public float hazardWidth = 2.5f;
    // 실측 최대 점프 높이(jumpForce=18, gravityScale=5 기준 약 3.3유닛)보다 낮게 잡아 점프로 통과 가능하게 함.
    public float hazardHeight = 2.0f;

    [Header("솟아오름")]
    public float riseTime = 0.2f;

    [Header("대기 (base -> maxLevel)")]
    public float baseHoverTime = 0.6f;
    public float maxHoverTime = 0.35f;
    public float minHoverTime = 0.35f; // 안전 클램프 (반응 여지 유지)

    [Header("돌진 속도 (base -> maxLevel)")]
    public float baseSweepSpeed = 9f;
    public float maxSweepSpeed = 10.35f; // base 대비 최대 1.15배

    [Header("경고 (base -> maxLevel)")]
    public float baseWarnTime = 0.7f;
    public float maxWarnTime = 0.45f;
    public float minWarnTime = 0.4f; // 안전 클램프

    public override IEnumerator Execute(PatternContext ctx)
    {
        ArenaBounds bounds = ctx.arenaBounds;
        if (bounds == null) yield break;

        float hoverTime = Mathf.Max(minHoverTime, Mathf.Lerp(baseHoverTime, maxHoverTime, ctx.difficulty));
        float sweepSpeed = Mathf.Lerp(baseSweepSpeed, maxSweepSpeed, ctx.difficulty);
        float warnTime = Mathf.Max(minWarnTime, Mathf.Lerp(baseWarnTime, maxWarnTime, ctx.difficulty));

        float sweepY = bounds.FloorY + hazardHeight * 0.5f;
        Vector3 stripCenter = new Vector3((bounds.LeftX + bounds.RightX) * 0.5f, sweepY, 0f);
        Vector2 stripSize = new Vector2(bounds.RightX - bounds.LeftX, hazardHeight);

        GameObject zoneObj = PoolManager.Instance.SpawnFromPool(dangerZonePoolName, stripCenter, Quaternion.identity);
        DangerZoneIndicator indicator = zoneObj?.GetComponent<DangerZoneIndicator>();
        indicator?.Setup(stripSize, warnTime, DangerZoneMode.TelegraphOnly);
        PatternManager.Instance.TrackSpawn(zoneObj);

        yield return new WaitForSeconds(warnTime);

        // 경고 종료와 동시에 경고 제거 + 사각형 스폰(솟아오름 시작).
        indicator?.Cancel();

        // 오른쪽 바깥이 아니라 화면 안쪽 경계에 붙여서 스폰한다 (바깥에 스폰하면 대기 중 화면 밖 판정에 걸려
        // HazardBase가 즉시 파괴해버릴 수 있음 - DropPattern에서 겪었던 것과 같은 종류의 버그 방지).
        Vector3 spawnPos = new Vector3(bounds.RightX - hazardWidth * 0.5f, sweepY, 0f);
        GameObject hazardObj = PoolManager.Instance.SpawnFromPool(hazardPoolName, spawnPos, Quaternion.identity);

        float sweepDistance = (bounds.RightX - bounds.LeftX) + hazardWidth;
        float sweepDuration = sweepSpeed > 0f ? sweepDistance / sweepSpeed : 0f;

        if (hazardObj != null)
        {
            Vector3 targetScale = new Vector3(hazardWidth, hazardHeight, 1f);
            hazardObj.GetComponent<SweepHazard>()?.Launch(riseTime, hoverTime, sweepSpeed, targetScale);
            PatternManager.Instance.TrackSpawn(hazardObj);
        }

        // 실제로 화면을 다 훑고 지나갈 때까지 코루틴을 붙잡아둔다 (activePatterns 조기 이탈로 인한
        // 다음 패턴 조기 시작 방지 - Pillar/DropPattern에서 고쳤던 것과 같은 원칙).
        yield return new WaitForSeconds(riseTime + hoverTime + sweepDuration);
    }

    public override float EstimateDuration(PatternContext ctx)
    {
        ArenaBounds bounds = ctx.arenaBounds;
        float hoverTime = Mathf.Max(minHoverTime, Mathf.Lerp(baseHoverTime, maxHoverTime, ctx.difficulty));
        float sweepSpeed = Mathf.Lerp(baseSweepSpeed, maxSweepSpeed, ctx.difficulty);
        float warnTime = Mathf.Max(minWarnTime, Mathf.Lerp(baseWarnTime, maxWarnTime, ctx.difficulty));

        float sweepDistance = bounds != null ? (bounds.RightX - bounds.LeftX) + hazardWidth : 0f;
        float sweepDuration = sweepSpeed > 0f ? sweepDistance / sweepSpeed : 0f;

        return warnTime + riseTime + hoverTime + sweepDuration;
    }
}
