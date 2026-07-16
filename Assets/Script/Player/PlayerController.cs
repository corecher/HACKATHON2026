using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;

public class PlayerController : MonoBehaviour
{
    [Header("이동")]
    public float moveSpeed = 6f;

    [Header("이동 경계 (X축, 월드 좌표)")]
    public float minX = -8f;
    public float maxX = 8f;

    [Header("점프")]
    public float jumpForce = 15f;

    [Header("착지 판정")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.1f;
    public LayerMask groundLayer;

    [Header("애니메이터")]
    public Animator animator;
    public SpriteRenderer renderer;

    private Rigidbody2D rb;
    private float moveInput;
    private bool jumpRequested;
    private bool isGrounded;
    private int jumpCount;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (renderer == null) renderer = GetComponent<SpriteRenderer>();
        if (animator == null) animator = GetComponent<Animator>();
    }

    void Update()
    {
        // Playing 상태가 아니면 입력을 받지 않는다.
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Playing)
        {
            moveInput = 0f;
            return;
        }

        moveInput = Input.GetAxisRaw("Horizontal"); // 기본 Input Manager: A/D, 좌우 화살표        
        animator.SetBool("Running", (moveInput != 0f));
        renderer.flipX = (moveInput < 0)  ? true : false;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            int maxJumpCount = GameManager.Instance.canDoubleJump ? 2 : 1;
            if (jumpCount < maxJumpCount)
            {
                jumpRequested = true;
            }
        }
    }

    void FixedUpdate()
    {
        isGrounded = groundCheck != null && Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        if (animator != null) animator.SetBool("isGrounded", isGrounded);

        // 착지 중이고 상승 중이 아니면 점프 카운트 리셋
        if (isGrounded && rb.linearVelocity.y <= 0f)
        {
            jumpCount = 0;
        }

        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);

        if (jumpRequested)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpCount++;
            if (animator != null) animator.SetTrigger("jump");
            jumpRequested = false;
        }

        Vector2 pos = rb.position;
        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        rb.position = pos;
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
