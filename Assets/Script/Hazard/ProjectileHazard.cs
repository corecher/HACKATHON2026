using UnityEngine;

// 지정 방향으로 직선 이동하는 장애물.
public class ProjectileHazard : HazardBase
{
    [Header("이동")]
    public float speed = 8f;
    public Vector2 direction = Vector2.left;

    public void Launch(Vector2 dir, float moveSpeed)
    {
        direction = dir.normalized;
        speed = moveSpeed;
    }

    protected override void Tick()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
    }
}
