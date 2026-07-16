using UnityEngine;

public class GravityToggle : MonoBehaviour
{
    private Rigidbody2D rb;
    
    // 중력의 세기 (유니티 기본 중력은 9.81입니다)
    public float gravityStrength = 9.81f; 
    
    // 현재 중력의 방향을 저장하는 변수 (기본값: 아래)
    private Vector2 gravityDirection = Vector2.down;

    private void Start() 
    {
        rb = GetComponent<Rigidbody2D>(); 
        
        // 유니티 시스템의 기본 중력 영향을 받지 않도록 0으로 설정합니다.
        // 이제부터는 우리가 스크립트로 직접 중력을 만듭니다.
        rb.gravityScale = 0f; 
    }

    private void Update()
    {
        // 방향키에 따라 중력 방향과 캐릭터 회전 각도를 설정합니다.
        // 원하는 키가 있다면 KeyCode.UpArrow 등을 KeyCode.W 등으로 변경하세요.
        
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            ChangeGravity(Vector2.up, 180f); // 위로 떨어짐 (180도 뒤집힘)
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            ChangeGravity(Vector2.down, 0f); // 아래로 떨어짐 (원래 상태)
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            ChangeGravity(Vector2.left, 270f); // 왼쪽으로 떨어짐 (-90도 회전)
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            ChangeGravity(Vector2.right, 90f); // 오른쪽으로 떨어짐 (90도 회전)
        }
    }

    // 물리적인 힘을 가할 때는 Update가 아닌 FixedUpdate를 사용하는 것이 안정적입니다.
    private void FixedUpdate()
    {
        // 설정된 방향(gravityDirection)으로 지속적인 힘을 가합니다.
        // 객체의 질량(mass)에 비례하여 동일한 속도로 떨어지게 만듭니다.
        rb.AddForce(gravityDirection * gravityStrength * rb.mass);
    }

    // 중력 방향과 캐릭터의 회전을 한 번에 처리하는 도우미 함수입니다.
    private void ChangeGravity(Vector2 newDirection, float zRotation)
    {
        // 1. 중력 방향 갱신
        gravityDirection = newDirection;
        
        // 2. 캐릭터 회전
        // Rotate 대신 rotation = Quaternion.Euler를 사용해 절대 각도로 정확하게 맞춥니다.
        transform.rotation = Quaternion.Euler(0, 0, zRotation);
        
        // (선택 사항) 방향이 바뀔 때 기존에 이동하던 관성을 없애고 싶다면 아래 주석을 푸세요.
        // rb.velocity = Vector2.zero; 
    }
}
