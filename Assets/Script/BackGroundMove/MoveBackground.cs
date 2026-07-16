using UnityEngine;

public class MoveBackground : MonoBehaviour
{
    [Header("이동 속도")]
    public float speed = 5f;

    [Header("이동 및 재배치 좌표")]
    public float limitX = -19f; // 배경이 화면 왼쪽 밖으로 완전히 나가는 X 좌표
    public float startX = 19f;  // 다시 나타날 오른쪽 끝 X 좌표 (일반적으로 배경의 가로 길이)

    void Update()
    {
        // 1. 배경을 왼쪽으로 계속 이동시킵니다.
        transform.Translate(Vector3.left * speed * Time.deltaTime);

        // 2. 만약 배경이 limitX보다 더 왼쪽으로 넘어갔다면
        if (transform.position.x <= limitX)
        {
            // 3. startX 위치로 순간이동 시킵니다. (Y와 Z 위치는 그대로 유지)
            Vector3 newPos = new Vector3(startX, transform.position.y, transform.position.z);
            transform.position = newPos;
        }
    }
}