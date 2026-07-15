using UnityEngine;
using WaveSpawnerTemplate.Wave;

namespace WaveSpawnerTemplate.Demo
{
    // ============================================================================
    // 데모 전용 예시 스크립트입니다. 실제 프로젝트에서는 이 콘솔 로그 대신
    // 점수 계산, UI 갱신, 승리 조건 판정 등 실제 게임 로직으로 교체하세요.
    // ============================================================================

    /// WaveSpawner 이벤트를 구독해 콘솔에 출력하는 데모 로거 (이벤트 훅 사용 예시)
    public class WaveSpawnerEventLogger : MonoBehaviour
    {
        [SerializeField] private WaveSpawner waveSpawner; // 이벤트를 구독할 대상 스포너

        private void OnEnable()
        {
            if (waveSpawner == null)
            {
                return;
            }

            waveSpawner.OnWaveStarted += HandleWaveStarted;
            waveSpawner.OnWaveCompleted += HandleWaveCompleted;
            waveSpawner.OnAllWavesCompleted += HandleAllWavesCompleted;
            waveSpawner.OnObjectSpawned += HandleObjectSpawned;
        }

        private void OnDisable()
        {
            if (waveSpawner == null)
            {
                return;
            }

            waveSpawner.OnWaveStarted -= HandleWaveStarted;
            waveSpawner.OnWaveCompleted -= HandleWaveCompleted;
            waveSpawner.OnAllWavesCompleted -= HandleAllWavesCompleted;
            waveSpawner.OnObjectSpawned -= HandleObjectSpawned;
        }

        private void HandleWaveStarted(int waveIndex)
        {
            Debug.Log($"[Wave] 웨이브 {waveIndex} 시작");
        }

        private void HandleWaveCompleted(int waveIndex)
        {
            Debug.Log($"[Wave] 웨이브 {waveIndex} 완료");
        }

        private void HandleAllWavesCompleted()
        {
            Debug.Log("[Wave] 모든 웨이브 완료");
        }

        private void HandleObjectSpawned(GameObject obj, SpawnableData data)
        {
            Debug.Log($"[Wave] 스폰됨: {data.SpawnableName} at {obj.transform.position}");
        }
    }
}
