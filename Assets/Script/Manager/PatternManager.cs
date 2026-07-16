using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 패턴을 랜덤하게 골라 순환 실행하고, 생존시간에 비례해 난이도를 올린다.
public class PatternManager : Singleton<PatternManager>
{
    [Header("아레나 경계")]
    public ArenaBounds arenaBounds;

    [Header("패턴 목록")]
    public List<PatternBase> patterns;

    [Header("레벨")]
    public float levelDuration = 60f; // 이 시간(초)마다 레벨 1 상승
    public int maxLevel = 6;

    [Header("패턴 선택 간격 (interval, base -> maxLevel)")]
    public float baseInterval = 2.5f;
    public float minInterval = 0.8f;

    [Header("생존시간 틱")]
    public float survivalTickInterval = 1f;

    [Header("클리어")]
    public float clearTime = 360f; // 이 생존시간(초)에 도달하면 클리어
    public bool lockPlayerInputOnClear = true; // 클리어 시 PlayerController를 비활성화해 조작 잠금

    [Header("최종 구간 패턴 겹침 (클리어까지 남은 시간이 이 값 이하일 때 활성화)")]
    public float overlapWindowDuration = 60f;
    [Range(0f, 1f)] public float overlapChance = 0.4f;
    public float overlapLeadMin = 0.3f;
    public float overlapLeadMax = 0.6f;

    // 레벨이 바뀌는 순간 발생. UI 등에서 현재 레벨 표시용으로 구독.
    public event System.Action<int> OnLevelUp;
    public int CurrentLevel => currentLevel;
    public float SurvivalTime => survivalTime;

    // 클리어 확정 순간 발생.
    public event System.Action OnGameClear;

    private class ActivePattern
    {
        public Coroutine coroutine;
        public float startTime;
        public float estimatedDuration;
    }

    private PlayerController playerController;
    private readonly List<GameObject> activeSpawns = new List<GameObject>();
    private readonly Queue<(float time, System.Action action)> spawnQueue = new Queue<(float, System.Action)>();
    private readonly List<PatternBase> availablePatterns = new List<PatternBase>();
    private readonly List<ActivePattern> activePatterns = new List<ActivePattern>();
    private PatternBase lastPattern;
    private float survivalTime;
    private int currentLevel;
    private float difficulty; // 0~1 정규화, (level-1)/(maxLevel-1)
    private bool cleared;

    void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged += HandleStateChanged;

            // GameManager.Start()가 먼저 실행되어 Playing으로 전환된 뒤 구독했다면
            // 이벤트를 놓치므로, 현재 상태를 직접 확인해서 챙긴다 (PlayerHealth와 동일 패턴).
            if (GameManager.Instance.CurrentState == GameState.Playing)
            {
                StartRun();
            }
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged -= HandleStateChanged;
        }
    }

    private void HandleStateChanged(GameState state)
    {
        if (state == GameState.Playing)
        {
            StartRun();
        }
        else if (state == GameState.GameOver)
        {
            // 클리어가 이미 확정된 뒤에 뒤늦게 들어온 GameOver 전환은 무시한다 (클리어 우선).
            if (cleared) return;
            StopRun();
        }
    }

    private void StartRun()
    {
        StopRun(); // 씬 재시작 등으로 남아있는 패턴/스폰이 있으면 먼저 정리

        // 씬 재시작 시 이전 플레이어는 파괴되므로 매 시작마다 다시 찾는다.
        playerController = FindFirstObjectByType<PlayerController>();
        if (arenaBounds == null) arenaBounds = ArenaBounds.Instance;

        survivalTime = 0f;
        currentLevel = 1;
        difficulty = 0f;
        cleared = false;
        lastPattern = null;
        spawnQueue.Clear();
        activePatterns.Clear();
        StartCoroutine(CoRunPatterns());
        StartCoroutine(CoTickSurvival());
        StartCoroutine(CoProcessSpawnQueue());
    }

    private void StopRun()
    {
        // 이 컴포넌트가 소유한 모든 코루틴을 정지한다.
        // 패턴들은 내부에서 PatternManager.Instance.StartCoroutine(...)으로만 코루틴을 걸기 때문에
        // 아래 한 줄로 실행 중인 패턴(겹침으로 여러 개 동시에 떠 있어도 전부) + 하위 코루틴까지 정리된다.
        StopAllCoroutines();
        spawnQueue.Clear();
        activePatterns.Clear();
        ClearAllSpawns();
    }

    // 패턴이 "경고가 뜬 순서 = 실제로 등장하는 순서"를 구조적으로 보장받기 위해 쓰는 큐.
    // 항상 큐의 맨 앞(가장 먼저 넣은 것)만 처리하므로 어떤 타이밍 값을 쓰든 순서가 뒤바뀔 수 없다.
    public void EnqueueSpawn(float atTime, System.Action action)
    {
        spawnQueue.Enqueue((atTime, action));
    }

    private IEnumerator CoProcessSpawnQueue()
    {
        while (true)
        {
            while (spawnQueue.Count > 0 && Time.time >= spawnQueue.Peek().time)
            {
                var (_, action) = spawnQueue.Dequeue();
                try
                {
                    action?.Invoke();
                }
                catch (System.Exception e)
                {
                    // 콜백 하나가 예외로 죽어도 이 코루틴 자체는 죽지 않게 막는다.
                    // (안 그러면 이후 큐에 쌓인 스폰이 전부 영구적으로 실행 안 됨)
                    Debug.LogError($"[PatternManager] 스폰 콜백 실행 중 예외, 해당 스폰만 건너뜀: {e}");
                }
            }
            yield return null;
        }
    }

    private IEnumerator CoTickSurvival()
    {
        while (true)
        {
            yield return new WaitForSeconds(survivalTickInterval);
            survivalTime += survivalTickInterval;
            GameManager.Instance?.ReportSurvivalTime(survivalTime);
            UpdateLevel();

            if (survivalTime >= clearTime)
            {
                TriggerClear();
                yield break;
            }
        }
    }

    private void TriggerClear()
    {
        if (cleared) return;
        cleared = true;

        // 게임오버 때와 동일한 정리 로직(코루틴 정지 + DangerZone/장애물 정리)을 그대로 재사용.
        StopRun();

        if (lockPlayerInputOnClear && playerController != null) playerController.enabled = false;

        // 클리어 최초 달성 여부만 기록해둔다 (재클리어 UI 노출은 이번 범위 밖).
        PlayerPrefs.SetInt("HasCleared", 1);

        OnGameClear?.Invoke();
        GameManager.Instance?.ChangeState(GameState.Clear);
    }

    private void UpdateLevel()
    {
        int newLevel = Mathf.Clamp(1 + Mathf.FloorToInt(survivalTime / levelDuration), 1, maxLevel);
        difficulty = maxLevel > 1 ? (float)(newLevel - 1) / (maxLevel - 1) : 1f;

        if (newLevel == currentLevel) return;

        currentLevel = newLevel;
        OnLevelUp?.Invoke(currentLevel);
    }

    private IEnumerator CoRunPatterns()
    {
        while (true)
        {
            bool inFinalStretch = (clearTime - survivalTime) <= overlapWindowDuration;
            bool doOverlap = inFinalStretch && activePatterns.Count > 0 && Random.value < overlapChance;

            if (doOverlap)
            {
                // 가장 최근에 시작한(=아직 실행 중일 가능성이 가장 큰) 패턴의 예상 종료 시각을 기준으로
                // overlapLead초 전에 다음 패턴을 겹쳐서 시작한다. EstimateDuration은 근사치라 정확히
                // "끝나기 몇 초 전"은 아니고, 자연 종료를 기다리지 않고 살짝 앞당겨 겹치는 정도로 동작한다.
                float lead = Random.Range(overlapLeadMin, overlapLeadMax);
                ActivePattern latest = activePatterns[activePatterns.Count - 1];
                float estimatedEnd = latest.startTime + latest.estimatedDuration;
                float waitSeconds = Mathf.Max(0f, estimatedEnd - lead - Time.time);
                yield return new WaitForSeconds(waitSeconds);
            }
            else
            {
                float interval = Mathf.Lerp(baseInterval, minInterval, difficulty);
                yield return new WaitForSeconds(interval);

                // 순차 모드에서는 이전 패턴이 완전히 끝날 때까지 기다린 뒤에만 다음 패턴을 시작한다.
                while (activePatterns.Count > 0) yield return null;
            }

            if (patterns == null || patterns.Count == 0) continue;

            PatternBase pattern = PickPattern();
            if (pattern == null) continue;

            lastPattern = pattern;

            // difficulty는 패턴 선택 시점에 스냅샷해서 그 패턴 실행 내내 고정값으로 쓴다.
            PatternContext ctx = new PatternContext(
                playerController != null ? playerController.transform : null,
                arenaBounds,
                difficulty);

            StartTrackedPattern(pattern, ctx);
        }
    }

    private void StartTrackedPattern(PatternBase pattern, PatternContext ctx)
    {
        ActivePattern entry = new ActivePattern
        {
            startTime = Time.time,
            estimatedDuration = pattern.EstimateDuration(ctx),
        };
        entry.coroutine = StartCoroutine(RunPatternTracked(pattern, ctx, entry));
        activePatterns.Add(entry);
    }

    private IEnumerator RunPatternTracked(PatternBase pattern, PatternContext ctx, ActivePattern entry)
    {
        yield return StartCoroutine(pattern.Execute(ctx));
        activePatterns.Remove(entry);
    }

    private PatternBase PickPattern()
    {
        availablePatterns.Clear();
        foreach (PatternBase p in patterns)
        {
            if (p != null && survivalTime >= p.unlockTime) availablePatterns.Add(p);
        }

        if (availablePatterns.Count == 0) return null;
        if (availablePatterns.Count == 1) return availablePatterns[0];

        PatternBase picked;
        do
        {
            picked = availablePatterns[Random.Range(0, availablePatterns.Count)];
        } while (picked == lastPattern);

        return picked;
    }

    // 패턴이 PoolManager로 스폰한 DangerZone/Hazard를 등록한다 (게임오버 시 일괄 정리용).
    public void TrackSpawn(GameObject obj)
    {
        if (obj != null) activeSpawns.Add(obj);
    }

    private void ClearAllSpawns()
    {
        foreach (var obj in activeSpawns)
        {
            if (obj == null || !obj.activeSelf) continue;

            DangerZoneIndicator zone = obj.GetComponent<DangerZoneIndicator>();
            if (zone != null) { zone.Cancel(); continue; }

            HazardBase hazard = obj.GetComponent<HazardBase>();
            if (hazard != null) { hazard.Cancel(); continue; }

            obj.SetActive(false);
        }
        activeSpawns.Clear();
    }
}
