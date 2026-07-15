using UnityEngine;
using WaveSpawnerTemplate.Pooling;

namespace WaveSpawnerTemplate.Demo
{
    // ============================================================================
    // 데모 전용 예시 스크립트입니다. 실제 프로젝트에 적용할 때는 삭제하고
    // 실제 게임 로직(적 AI, 투사체 이동, 타워 사거리 판정 등)으로 교체하세요.
    // ============================================================================

    /// 일정 시간 후 또는 경계를 벗어나면 자동으로 풀에 반환되는 데모 오브젝트
    public class PooledObjectDemo : MonoBehaviour, IPoolable
    {
        private enum ReturnMode
        {
            AfterTime, // 일정 시간 후 반환
            OutOfBounds // 경계를 벗어나면 반환
        }

        [SerializeField] private ReturnMode returnMode = ReturnMode.AfterTime; // 반환 방식
        [SerializeField] private float lifeTime = 3f; // AfterTime 모드에서 살아있을 시간 (초)
        [SerializeField] private float boundsRadius = 12f; // OutOfBounds 모드에서 허용 반경

        private float aliveTimer; // 스폰된 이후 경과 시간

        /// 풀에서 꺼내져 활성화될 때 상태 초기화 (IPoolable 콜백)
        public void OnSpawned()
        {
            aliveTimer = 0f;
        }

        /// 풀로 반환되어 비활성화될 때 정리 (IPoolable 콜백, 데모에는 별도 로직 없음)
        public void OnDespawned()
        {
        }

        private void Update()
        {
            if (returnMode == ReturnMode.AfterTime)
            {
                aliveTimer += Time.deltaTime;

                if (aliveTimer >= lifeTime)
                {
                    ReturnToPool();
                }
            }
            else if (transform.position.magnitude > boundsRadius)
            {
                ReturnToPool();
            }
        }

        /// 이 오브젝트를 PoolManager를 통해 풀로 반환
        private void ReturnToPool()
        {
            if (PoolManager.Instance != null)
            {
                PoolManager.Instance.Release(gameObject);
            }
        }
    }
}
