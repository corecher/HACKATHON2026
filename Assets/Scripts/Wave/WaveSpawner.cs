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
            if (autoStart)
            {
                StartSequence();
            }
        }

        private void OnEnable()
        {
            if (PoolManager.Instance != null)
            {
                PoolManager.Instance.OnReleased += HandleObjectReleased;
            }
        }

        private void OnDisable()
        {
            if (PoolManager.Instance != null)
            {
                PoolManager.Instance.OnReleased -= HandleObjectReleased;
            }
        }

        /// 웨이브 시퀀스를 처음부터 시작
        public void StartSequence()
        {
            if (runningCoroutine != null)
            {
                StopCoroutine(runningCoroutine);
            }

            currentWaveIndex = -1;
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
            for (int i = 0; i < waveSequence.Waves.Count; i++)
            {
                WaveSequenceEntry entry = waveSequence.Waves[i];

                if (entry.DelayBeforeStart > 0f)
                {
                    yield return WaitSeconds(entry.DelayBeforeStart);
                }

                currentWaveIndex = i;
                skipRequested = false;
                aliveObjects.Clear();

                OnWaveStarted?.Invoke(i);

                yield return RunWave(entry.WaveData);

                OnWaveCompleted?.Invoke(i);
            }

            runningCoroutine = null;
            OnAllWavesCompleted?.Invoke();
        }

        private IEnumerator RunWave(WaveData wave)
        {
            if (wave.EndCondition == WaveEndCondition.FixedDuration)
            {
                yield return SpawnForDuration(wave);
            }
            else
            {
                yield return SpawnUntilQuantityExhausted(wave);
            }

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
            foreach (WaveEntry entry in wave.Entries)
            {
                for (int i = 0; i < entry.Count; i++)
                {
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
            int entryCursor = 0;

            while (elapsed < wave.FixedDuration && !skipRequested)
            {
                if (wave.Entries.Count > 0)
                {
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
            if (data == null || data.Prefab == null || PoolManager.Instance == null)
            {
                return;
            }

            SpawnPoint point = spawnPointGroup != null ? spawnPointGroup.SelectSpawnPoint(data.Category) : null;
            Vector3 position = point != null ? point.Position : transform.position;

            GameObject obj = PoolManager.Instance.Get(data.Prefab, position, Quaternion.identity);
            aliveObjects.Add(obj);

            OnObjectSpawned?.Invoke(obj, data);
        }

        private void HandleObjectReleased(GameObject obj)
        {
            aliveObjects.Remove(obj);
        }

        /// 일시정지 상태를 반영하며 주어진 시간만큼 대기
        private IEnumerator WaitSeconds(float seconds)
        {
            float remaining = seconds;

            while (remaining > 0f)
            {
                if (!isPaused)
                {
                    remaining -= Time.deltaTime;
                }

                yield return null;
            }
        }
    }
}
