using UnityEngine;

namespace WaveSpawnerTemplate.Pooling
{
    /// 풀에서 생성된 오브젝트에 자동으로 붙어 원본 프리팹을 기억해두는 내부 태그
    [DisallowMultipleComponent]
    public class PooledObjectTag : MonoBehaviour
    {
        /// 이 오브젝트가 어떤 프리팹으로부터 생성됐는지 (Release 시 풀을 찾는 데 사용)
        public GameObject SourcePrefab { get; private set; }

        /// 원본 프리팹 정보를 기록
        public void SetSourcePrefab(GameObject prefab)
        {
            SourcePrefab = prefab;
        }
    }
}
