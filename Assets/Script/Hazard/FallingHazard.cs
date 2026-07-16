using UnityEngine;

// 위에서 낙하하는 장애물. 가변 크기 + floorY 착지 감지를 지원 (DropPattern용).
public class FallingHazard : HazardBase
{
    [Header("낙하")]
    public float fallSpeed = 6f;
    public float driftSpeed = 0f; // 0이면 직선 낙하

    [Header("착지")]
    public float lingerAfterLandDuration = 0f; // 0이면 착지 즉시 파괴

    private bool landed;
    private float landTimer;

    public void Launch(float speed, float drift, Vector2 size, float lingerDuration)
    {
        fallSpeed = speed;
        driftSpeed = drift;
        lingerAfterLandDuration = lingerDuration;
        transform.localScale = new Vector3(size.x, size.y, 1f);
        landed = false;
        landTimer = 0f;
    }

    protected override void Tick()
    {
        if (landed)
        {
            landTimer += Time.deltaTime;
            if (landTimer >= lingerAfterLandDuration) Despawn();
            return;
        }

        transform.position += new Vector3(driftSpeed, -fallSpeed, 0f) * Time.deltaTime;

        ArenaBounds bounds = ArenaBounds.Instance;
        if (bounds == null) return;

        float halfHeight = transform.localScale.y * 0.5f;
        if (transform.position.y - halfHeight > bounds.FloorY) return;

        Vector3 pos = transform.position;
        pos.y = bounds.FloorY + halfHeight;
        transform.position = pos;

        landed = true;
        landTimer = 0f;
        if (lingerAfterLandDuration <= 0f) Despawn();
    }
}
