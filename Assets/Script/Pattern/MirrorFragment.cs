using UnityEngine;

public class MirrorFragment : MonoBehaviour
{
    private Vector2 moveDirection;
    private float moveSpeed;

    public void SetDirectionAndSpeed(Vector2 direction, float speed)
    {
        moveDirection = direction.normalized;
        moveSpeed = speed;
        
        float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }

    private void Update()
    {
        transform.Translate(Vector3.right * moveSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Destroy(gameObject);
        }
    }

    private void OnBecameInvisible()
    {
        Destroy(gameObject);
    }
}