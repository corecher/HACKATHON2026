using UnityEngine;

// 스케일 0에서 솟아오름 -> 유지 -> 내려가며 파괴 (기둥/가시용).
// baseY에 바닥을 고정한 채로 자라고 줄어든다 (중심 피벗 스프라이트가 바닥 밑으로 파고드는 것 방지).
public class RisingHazard : HazardBase
{
    private enum RiseState { Rising, Holding, Falling }

    [Header("솟아오름")]
    public float riseDuration = 0.2f;
    public float holdDuration = 1.0f;
    public float fallDuration = 0.2f;
    public float safetyMargin = 0.5f; // 수명(lifeTime) 계산 시 여유값
    public Vector3 fullScale = Vector3.one;
    public float baseSpriteHeight = 1f; // 스케일 1일 때 스프라이트의 실제 월드 높이

    private RiseState state;
    private float stateTimer;
    private float baseY;

    public void Launch(float rise, float hold, float fall, Vector3 targetScale, float baseY)
    {
        riseDuration = rise;
        holdDuration = hold;
        fallDuration = fall;
        fullScale = targetScale;
        this.baseY = baseY;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        state = RiseState.Rising;
        stateTimer = 0f;
        transform.localScale = Vector3.zero;
        lifeTime = riseDuration + holdDuration + fallDuration + safetyMargin;
    }

    protected override void Tick()
    {
        stateTimer += Time.deltaTime;

        switch (state)
        {
            case RiseState.Rising:
                transform.localScale = Vector3.Lerp(Vector3.zero, fullScale, stateTimer / riseDuration);
                if (stateTimer >= riseDuration) { state = RiseState.Holding; stateTimer = 0f; }
                break;

            case RiseState.Holding:
                if (stateTimer >= holdDuration) { state = RiseState.Falling; stateTimer = 0f; }
                break;

            case RiseState.Falling:
                transform.localScale = Vector3.Lerp(fullScale, Vector3.zero, stateTimer / fallDuration);
                if (stateTimer >= fallDuration) { Despawn(); return; }
                break;
        }

        Vector3 pos = transform.position;
        pos.y = baseY + transform.localScale.y * baseSpriteHeight * 0.5f;
        transform.position = pos;
    }
}
