using UnityEngine;

// 패턴 실행에 필요한 정보를 담아 넘기는 컨텍스트.
// 좌표 계산은 항상 ArenaBounds 기준으로만 한다 (Camera, PlayerController 경계 등 다른 소스 사용 금지).
public class PatternContext
{
    public Transform playerTransform;
    public ArenaBounds arenaBounds;
    public float difficulty; // 생존시간에 비례해 증가, 각 패턴이 알아서 스케일링에 사용

    public PatternContext(Transform playerTransform, ArenaBounds arenaBounds, float difficulty)
    {
        this.playerTransform = playerTransform;
        this.arenaBounds = arenaBounds;
        this.difficulty = difficulty;
    }
}
