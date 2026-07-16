using UnityEngine;

// 솟아오름(rise) -> 대기(hover) -> 왼쪽으로 등속 돌진(sweep) 3단계.
// 대기/돌진 내내 접촉 데미지 활성 (HazardBase의 기본 트리거 데미지 그대로 사용).
public class SweepHazard : HazardBase
{
    private enum State { Rising, Hovering, Sweeping }

    [Header("단계별 시간")]
    public float riseTime = 0.2f;
    public float hoverTime = 0.6f;

    [Header("돌진")]
    public float sweepSpeed = 9f;
    public Vector3 fullScale = Vector3.one;

    private State state;
    private float stateTimer;

    public void Launch(float rise, float hover, float speed, Vector3 targetScale)
    {
        riseTime = rise;
        hoverTime = hover;
        sweepSpeed = speed;
        fullScale = targetScale;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        state = State.Rising;
        stateTimer = 0f;
        transform.localScale = Vector3.zero;
    }

    protected override void Tick()
    {
        stateTimer += Time.deltaTime;

        switch (state)
        {
            case State.Rising:
                transform.localScale = riseTime > 0f
                    ? Vector3.Lerp(Vector3.zero, fullScale, stateTimer / riseTime)
                    : fullScale;
                if (stateTimer >= riseTime)
                {
                    transform.localScale = fullScale;
                    state = State.Hovering;
                    stateTimer = 0f;
                }
                break;

            case State.Hovering:
                if (stateTimer >= hoverTime)
                {
                    state = State.Sweeping;
                    stateTimer = 0f;
                }
                break;

            case State.Sweeping:
                transform.position += Vector3.left * sweepSpeed * Time.deltaTime;
                break;
        }
    }
}
