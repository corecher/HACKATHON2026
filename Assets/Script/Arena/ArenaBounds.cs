using UnityEngine;

// 패턴 스폰 좌표의 단일 기준점. 모든 패턴은 좌표 계산 시 이 컴포넌트만 참조한다.
public class ArenaBounds : MonoBehaviour
{
    private static ArenaBounds instance;
    public static ArenaBounds Instance
    {
        get
        {
            if (instance == null) instance = FindFirstObjectByType<ArenaBounds>();
            return instance;
        }
    }

    [Header("직접 입력 (아래 참조가 비어있을 때 사용)")]
    public float floorY = -4f;
    public float ceilingY = 5f;
    public float leftX = -8f;
    public float rightX = 8f;

    [Header("또는 씬 오브젝트 참조로 계산 (지정 시 위 값 대신 사용)")]
    public Transform floorReference;
    public Transform ceilingReference;
    public Transform leftReference;
    public Transform rightReference;

    public float FloorY => floorReference != null ? floorReference.position.y : floorY;
    public float CeilingY => ceilingReference != null ? ceilingReference.position.y : ceilingY;
    public float LeftX => leftReference != null ? leftReference.position.x : leftX;
    public float RightX => rightReference != null ? rightReference.position.x : rightX;

    void Awake()
    {
        instance = this;
    }

    void OnDrawGizmos()
    {
        float floor = FloorY;
        float ceiling = CeilingY;
        float left = LeftX;
        float right = RightX;

        Vector3 bl = new Vector3(left, floor, 0f);
        Vector3 br = new Vector3(right, floor, 0f);
        Vector3 tl = new Vector3(left, ceiling, 0f);
        Vector3 tr = new Vector3(right, ceiling, 0f);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(bl, tl);
        Gizmos.DrawLine(br, tr);

        Gizmos.color = Color.green;
        Gizmos.DrawLine(bl, br);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(tl, tr);
    }
}
