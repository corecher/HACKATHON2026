using UnityEngine;
using WaveSpawnerTemplate.Wave;

namespace WaveSpawnerTemplate.Spawning
{
    /// 씬에 배치하는 스폰 위치 마커. 카테고리로 스폰 허용 여부를 필터링 가능
    public class SpawnPoint : MonoBehaviour
    {
        [SerializeField] private bool restrictByCategory; // 카테고리 필터링 사용 여부
        [SerializeField] private SpawnableCategory[] allowedCategories; // 허용할 카테고리 목록 (필터링 사용 시)
        [SerializeField] private float gizmoRadius = 0.3f; // 씬 뷰 표시용 기즈모 반경

        /// 이 스폰 지점이 해당 카테고리를 스폰할 수 있는지 여부
        public bool CanSpawn(SpawnableCategory category)
        {
            if (!restrictByCategory)
            {
                return true;
            }

            foreach (SpawnableCategory allowed in allowedCategories)
            {
                if (allowed == category)
                {
                    return true;
                }
            }

            return false;
        }

        /// 스폰 위치 (이 오브젝트의 월드 좌표)
        public Vector3 Position => transform.position;

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, gizmoRadius);
        }
    }
}
