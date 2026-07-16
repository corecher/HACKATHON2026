using UnityEngine;
using UnityEngine.UI;

// 클리어 화면에 "생존 성공" 문구 + 최종 생존시간(=clearTime)을 표시한다.
public class ClearResultUI : MonoBehaviour
{
    public Text resultText;

    void OnEnable()
    {
        if (resultText == null) return;

        float clearTime = PatternManager.Instance != null ? PatternManager.Instance.clearTime : 0f;
        resultText.text = $"생존 성공!\n{clearTime:F0}초 생존";
    }
}
