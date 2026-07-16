#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 헤드리스 Play Mode로 패턴 시스템이 실제로 동작하는지 스모크 테스트한다.
// 실행: -executeMethod PatternSystemPlayTest.Run (batchmode에서 -quit 없이 EditorApplication.Exit로 직접 종료)
//
// Play Mode 진입 시 스크립트 도메인이 리로드되어 일반 static 필드/이벤트 구독은 초기화된다.
// [InitializeOnLoad]로 리로드 후에도 다시 구독되게 하고, 진행 상태는 SessionState로 넘긴다.
[InitializeOnLoad]
public static class PatternSystemPlayTest
{
    private const string ScenePath = "Assets/Scenes/PuriTestScene.unity";
    private const string SessionKeyActive = "PatternSystemPlayTest_Active";
    private const string SessionKeyCheckIndex = "PatternSystemPlayTest_CheckIndex";

    private static readonly float[] CheckPoints = { 1f, 3f, 5f, 8f };

    static PatternSystemPlayTest()
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
        PatternManager pm = Object.FindFirstObjectByType<PatternManager>();
        PlayerHealth ph = Object.FindFirstObjectByType<PlayerHealth>();

        int activeHazards = CountActive<HazardBase>();
        int activeZones = CountActive<DangerZoneIndicator>();

        Debug.Log($"[PlayTest] t={elapsed:F1}s state={gm?.CurrentState} survivalTime={pm?.SurvivalTime:F1} hearts={ph?.CurrentHearts}/{ph?.MaxHearts} activeHazards={activeHazards} activeZones={activeZones} patternManagerFound={pm != null}");
    }

    private static int CountActive<T>() where T : Component
    {
        T[] all = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int count = 0;
        foreach (T item in all)
        {
            if (item.gameObject.activeSelf) count++;
        }
        return count;
    }

    private static void FinishAndExit()
    {
        Debug.Log("[PlayTest] 종료");
        EditorApplication.isPlaying = false;
        EditorApplication.Exit(0);
    }
}
#endif
