#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 클리어(생존 승리) 흐름을 짧은 시간으로 앞당겨서 헤드리스로 검증하는 스모크 테스트.
// clearTime은 런타임 인스턴스에서만 오버라이드하고 씬 저장값은 건드리지 않는다.
[InitializeOnLoad]
public static class PatternClearTest
{
    private const string ScenePath = "Assets/Scenes/PuriTestScene.unity";
    private const string SessionKeyActive = "PatternClearTest_Active";
    private const string SessionKeyCheckIndex = "PatternClearTest_CheckIndex";
    private const float OverrideClearTime = 2f;

    private static readonly float[] CheckPoints = { 0.5f, 1.5f, 2.5f, 4f };

    static PatternClearTest()
    {
        EditorApplication.update += OnUpdate;
    }

    public static void Run()
    {
        SessionState.SetBool(SessionKeyActive, true);
        SessionState.SetInt(SessionKeyCheckIndex, 0);
        EditorSceneManager.OpenScene(ScenePath);
        EditorApplication.isPlaying = true;
    }

    private static void OnUpdate()
    {
        if (!SessionState.GetBool(SessionKeyActive, false)) return;
        if (!EditorApplication.isPlaying) return;

        if (PatternManager.Instance != null && !Mathf.Approximately(PatternManager.Instance.clearTime, OverrideClearTime))
        {
            PatternManager.Instance.clearTime = OverrideClearTime;
        }

        int checkIndex = SessionState.GetInt(SessionKeyCheckIndex, 0);
        float elapsed = Time.realtimeSinceStartup;

        if (checkIndex < CheckPoints.Length && elapsed >= CheckPoints[checkIndex])
        {
            LogStatus(elapsed);
            checkIndex++;
            SessionState.SetInt(SessionKeyCheckIndex, checkIndex);
        }

        if (checkIndex >= CheckPoints.Length)
        {
            SessionState.SetBool(SessionKeyActive, false);
            FinishAndExit();
        }
    }

    private static void LogStatus(float elapsed)
    {
        GameManager gm = GameManager.Instance;
        UIManager ui = Object.FindFirstObjectByType<UIManager>();
        PlayerController pc = Object.FindFirstObjectByType<PlayerController>();
        bool clearPanelActive = ui != null && ui.clearPanel != null && ui.clearPanel.activeSelf;

        Debug.Log($"[ClearTest] t={elapsed:F1}s state={gm?.CurrentState} clearPanelActive={clearPanelActive} playerControllerEnabled={pc?.enabled}");
    }

    private static void FinishAndExit()
    {
        Debug.Log("[ClearTest] 종료");
        EditorApplication.isPlaying = false;
        EditorApplication.Exit(0);
    }
}
#endif
