using UnityEngine;
using WaveSpawnerTemplate.Wave;

namespace WaveSpawnerTemplate.Demo
{
    // ============================================================================
    // 데모/해커톤 당일 테스트 편의용 스크립트입니다. 실제 출시 빌드에서는
    // 삭제하거나 실제 UI 버튼 입력으로 교체하세요.
    // ============================================================================

    /// 레거시 Input 클래스 기반 단축키로 WaveSpawner를 제어하는 디버그 입력 스크립트
    public class WaveSpawnerDebugInput : MonoBehaviour
    {
        [SerializeField] private WaveSpawner waveSpawner; // 제어할 대상 스포너
        [SerializeField] private KeyCode togglePauseKey = KeyCode.P; // 일시정지/재개 토글 키
        [SerializeField] private KeyCode skipWaveKey = KeyCode.N; // 다음 웨이브로 스킵하는 키

        private void Update()
        {
            if (waveSpawner == null)
            {
                return;
            }

            if (Input.GetKeyDown(togglePauseKey))
            {
                if (waveSpawner.IsPaused)
                {
                    waveSpawner.Resume();
                }
                else
                {
                    waveSpawner.Pause();
                }
            }

            if (Input.GetKeyDown(skipWaveKey))
            {
                waveSpawner.SkipToNextWave();
            }
        }
    }
}
