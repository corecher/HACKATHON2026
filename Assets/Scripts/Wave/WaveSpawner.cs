using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WaveSpawnerTemplate.Pooling;
using WaveSpawnerTemplate.Spawning;

namespace WaveSpawnerTemplate.Wave
{
    /// 다음 웨이브로 넘어가는 조건 (스폰 완료 즉시 vs 스폰된 오브젝트가 모두 반환될 때까지 대기)
    public enum WaveAdvanceMode
    {
        OnSpawnFinished,
        OnAllSpawnedDespawned
    }

    /// WaveSequenceData를 실제로 실행하는 스포너 매니저
    public class WaveSpawner : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private WaveSequenceData waveSequence; // 실행할 웨이브 시퀀스 데이터
        [SerializeField] private SpawnPointGroup spawnPointGroup; // 스폰 위치를 결정할 그룹

        [Header("Settings")]
        [SerializeField] private WaveAdvanceMode advanceMode = WaveAdvanceMode.OnSpawnFinished; // 다음 웨이브 진행 조건
        [SerializeField] private bool autoStart = true; // Start 시 자동으로 시퀀스 시작 여부

        /// 웨이브 시작 시 발생 (웨이브 인덱스)
        public event Action<int> OnWaveStarted;

        /// 웨이브 완료 시 발생 (웨이브 인덱스)
        public event Action<int> OnWaveCompleted;

        /// 모든 웨이브 완료 시 발생
        public event Action OnAllWavesCompleted;

        /// 오브젝트 하나가 스폰될 때 발생 (스폰된 오브젝트, 스폰 데이터)
        public event Action<GameObject, SpawnableData> OnObjectSpawned;

        private int currentWaveIndex = -1; // 현재 진행 중인 웨이브 인덱스
        private bool isPaused; // 일시정지 상태 여부
        private bool skipRequested; // 다음 웨이브로 강제 스킵 요청 여부
        private Coroutine runningCoroutine; // 실행 중인 시퀀스 코루틴
        private readonly HashSet<GameObject> aliveObjects = new HashSet<GameObject>(); // 현재 웨이브에서 아직 반환되지 않은 오브젝트 집합

        public int CurrentWaveIndex => currentWaveIndex; // 현재 웨이브 인덱스 접근자
        public bool IsPaused => isPaused; // 일시정지 여부 접근자

        private void Start()
        {
            // autoStart가 켜져있으면 씬 시작하자마자 웨이브 진행 (인스펙터에서 끄면 외부 스크립트가 StartSequence()로 직접 시작)
            if (autoStart)
            {
                StartSequence();
            }
        }

        private void OnEnable()
        {
            // PoolManager가 오브젝트를 반환할 때마다 알림 받아서 aliveObjects에서 제거해야 하므로 구독
            if (PoolManager.Instance != null)
            {
                PoolManager.Instance.OnReleased += HandleObjectReleased;
            }
        }

        private void OnDisable()
        {
            // 이 오브젝트가 비활성화/파괴될 때 구독 해제 안 하면 메모리 누수 + 존재하지 않는 대상 호출 위험
            if (PoolManager.Instance != null)
            {
                PoolManager.Instance.OnReleased -= HandleObjectReleased;
            }
        }

        /// 웨이브 시퀀스를 처음부터 시작
        public void StartSequence()
        {
            // 이미 실행 중인 시퀀스가 있으면 먼저 멈춰야 중복 실행(웨이브 두 배 스폰 등)을 막을 수 있음
            if (runningCoroutine != null)
            {
                StopCoroutine(runningCoroutine);
            }

            currentWaveIndex = -1; // 첫 웨이브(인덱스 0) 시작 전 상태로 리셋
            runningCoroutine = StartCoroutine(RunSequence());
        }

        /// 일시정지 (스폰/대기 타이머 진행을 멈춤)
        public void Pause()
        {
            isPaused = true;
        }

        /// 일시정지 해제
        public void Resume()
        {
            isPaused = false;
        }

        /// 현재 웨이브의 남은 스폰을 건너뛰고 다음 웨이브로 즉시 진행
        public void SkipToNextWave()
        {
            skipRequested = true;
        }

        private IEnumerator RunSequence()
        {
            // 웨이브 시퀀스 데이터에 등록된 순서대로 웨이브를 하나씩 실행
            for (int i = 0; i < waveSequence.Waves.Count; i++)
            {
                WaveSequenceEntry entry = waveSequence.Waves[i];

                // 이 웨이브 시작 전에 지정된 대기시간만큼 먼저 기다림 (예: 다음 웨이브 전 3초 텀)
                if (entry.DelayBeforeStart > 0f)
                {
                    yield return WaitSeconds(entry.DelayBeforeStart);
                }

                currentWaveIndex = i;
                skipRequested = false; // 새 웨이브 시작이니 이전 스킵 요청은 초기화
                aliveObjects.Clear(); // 이전 웨이브에서 살아있던 오브젝트 추적 목록도 초기화

                OnWaveStarted?.Invoke(i);

                yield return RunWave(entry.WaveData);

                OnWaveCompleted?.Invoke(i);
            }

            runningCoroutine = null;
            OnAllWavesCompleted?.Invoke();
        }

        private IEnumerator RunWave(WaveData wave)
        {
            // 종료 조건에 따라 스폰 방식 자체가 다름 (수량 소진 vs 시간 고정)
            if (wave.EndCondition == WaveEndCondition.FixedDuration)
            {
                yield return SpawnForDuration(wave);
            }
            else
            {
                yield return SpawnUntilQuantityExhausted(wave);
            }

            // OnAllSpawnedDespawned 모드면 스폰된 오브젝트가 전부 풀로 돌아올 때까지(=죽거나 소멸할 때까지) 웨이브를 안 끝냄
            // (OnSpawnFinished 모드는 스폰만 끝나면 바로 다음 웨이브로 넘어가므로 이 대기 없이 여기서 그냥 리턴)
            if (advanceMode == WaveAdvanceMode.OnAllSpawnedDespawned)
            {
                while (aliveObjects.Count > 0 && !skipRequested)
                {
                    yield return null;
                }
            }
        }

        private IEnumerator SpawnUntilQuantityExhausted(WaveData wave)
        {
            // 각 항목(WaveEntry)마다 정해진 개수만큼, 순서대로, spawnInterval 간격으로 하나씩 스폰
            foreach (WaveEntry entry in wave.Entries)
            {
                for (int i = 0; i < entry.Count; i++)
                {
                    // 스킵 요청 들어오면 남은 스폰은 다 건너뛰고 즉시 코루틴 종료
                    if (skipRequested)
                    {
                        yield break;
                    }

                    SpawnOne(entry.SpawnableData);

                    yield return WaitSeconds(wave.SpawnInterval);
                }
            }
        }

        private IEnumerator SpawnForDuration(WaveData wave)
        {
            float elapsed = 0f;
            int entryCursor = 0; // 항목 목록을 순환하며 골고루 스폰하기 위한 커서

            // 정해진 시간(FixedDuration) 동안 계속 스폰 - 수량 제한 없이 항목을 돌아가며 반복
            while (elapsed < wave.FixedDuration && !skipRequested)
            {
                if (wave.Entries.Count > 0)
                {
                    // 나머지 연산으로 항목 목록을 순환 (0번, 1번, 2번, 다시 0번...)
                    WaveEntry entry = wave.Entries[entryCursor % wave.Entries.Count];
                    SpawnOne(entry.SpawnableData);
                    entryCursor++;
                }

                yield return WaitSeconds(wave.SpawnInterval);
                elapsed += wave.SpawnInterval;
            }
        }

        private void SpawnOne(SpawnableData data)
        {
            // 데이터/프리팹/풀매니저 중 하나라도 없으면 스폰 자체를 스킵 (인스펙터 설정 누락 방어)
            if (data == null || data.Prefab == null || PoolManager.Instance == null)
            {
                return;
            }

            // 카테고리에 맞는 스폰 지점을 찾고, 없으면 이 스포너 자신의 위치를 기본값으로 사용
            SpawnPoint point = spawnPointGroup != null ? spawnPointGroup.SelectSpawnPoint(data.Category) : null;
            Vector3 position = point != null ? point.Position : transform.position;

            GameObject obj = PoolManager.Instance.Get(data.Prefab, position, Quaternion.identity);
            aliveObjects.Add(obj); // OnAllSpawnedDespawned 모드에서 이 오브젝트가 반환될 때까지 추적하기 위해 등록

            OnObjectSpawned?.Invoke(obj, data);
        }

        private void HandleObjectReleased(GameObject obj)
        {
            // 다른 웨이브/다른 스포너가 만든 오브젝트일 수도 있지만, HashSet.Remove는 없는 항목이어도 안전하게 무시됨
            aliveObjects.Remove(obj);
        }

        /// 일시정지 상태를 반영하며 주어진 시간만큼 대기
        private IEnumerator WaitSeconds(float seconds)
        {
            float remaining = seconds;

            while (remaining > 0f)
            {
                // 일시정지 중엔 시간을 안 깎아서 사실상 타이머가 멈춘 것처럼 동작
                if (!isPaused)
                {
                    remaining -= Time.deltaTime;
                }

                yield return null;
            }
        }
    }
}
