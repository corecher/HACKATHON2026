using UnityEngine;

// 움직이는 장애물 공통 베이스. 플레이어 접촉 시 데미지, 아레나 밖/수명 초과 시 자동 파괴.
public abstract class HazardBase : MonoBehaviour
{
    [Header("수명")]
    public float lifeTime = 5f;
    public float outOfBoundsMargin = 1f; // ArenaBounds 밖으로 이 값만큼 더 나가야 파괴 (월드 유닛)
    public float outOfBoundsGracePeriod = 0.2f; // 스폰 직후 이 시간 동안은 화면 밖 판정을 하지 않는다

    protected float elapsed;

    protected virtual void OnEnable()
    {
        elapsed = 0f;
    }

    protected virtual void Update()
    {
        elapsed += Time.deltaTime;

        // 패턴들은 종종 스폰 지점을 경계 바로 바깥(크기만큼)으로 잡는다.
        // grace period 없이 매 프레임 판정하면 스폰 직후 그 프레임에 바로 파괴돼버릴 수 있다.
        bool outOfBounds = elapsed >= outOfBoundsGracePeriod && IsOutOfBounds();
        if (elapsed >= lifeTime || outOfBounds)
        {
            Despawn();
            return;
        }

        Tick();
    }

    protected abstract void Tick();

    // 좌표 소스는 항상 ArenaBounds 하나로 통일 (Camera 등 다른 기준 사용 금지).
    protected virtual bool IsOutOfBounds()
    {
        ArenaBounds bounds = ArenaBounds.Instance;
        if (bounds == null) return false;

        Vector3 pos = transform.position;
        return pos.x < bounds.LeftX - outOfBoundsMargin || pos.x > bounds.RightX + outOfBoundsMargin
            || pos.y < bounds.FloorY - outOfBoundsMargin || pos.y > bounds.CeilingY + outOfBoundsMargin;
    }

    // 풀에서 스스로 물러남 (PoolManager엔 별도 디스폰 API가 없어 SetActive(false)로 반환).
    protected virtual void Despawn()
    {
        gameObject.SetActive(false);
    }

    // PatternManager가 게임오버 시 강제 정리할 때 사용.
    public void Cancel()
    {
        Despawn();
    }

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
        if (playerHealth != null) playerHealth.TakeDamage();
    }
}
