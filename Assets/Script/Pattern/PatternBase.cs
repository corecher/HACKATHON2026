using System.Collections;
using UnityEngine;

// 패턴 하나 = 코루틴 하나. PatternManager가 인스펙터에 등록된 목록에서 골라 실행한다.
public abstract class PatternBase : MonoBehaviour
{
    [Header("게이팅")]
    public float unlockTime = 0f; // survivalTime이 이 값 이상일 때만 선택 풀에 포함된다.

    public abstract IEnumerator Execute(PatternContext ctx);

    // Execute(ctx)가 대략 몇 초간 실행될지 추정치. PatternManager가 최종 구간 패턴 겹침
    // 스케줄링(다음 패턴을 몇 초 전에 겹쳐서 시작할지)에 사용한다. Execute()와 같은 공식으로 계산해야 한다.
    public virtual float EstimateDuration(PatternContext ctx) => 0f;
}
