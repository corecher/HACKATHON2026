using System;
using System.Collections.Generic;
using UnityEngine;

namespace WaveSpawnerTemplate.Pooling
{
    /// 여러 프리팹의 오브젝트 풀을 관리하는 싱글턴 매니저
    public class PoolManager : MonoBehaviour
    {
        [Serializable]
        public class PoolPreset
        {
            public GameObject prefab; // 미리 등록해둘 프리팹
            public int initialSize = 10; // 초기 생성 개수
            public int maxSize; // 최대 개수 (0 이하 = 무제한)
        }

        public static PoolManager Instance { get; private set; } // 싱글턴 인스턴스

        [SerializeField] private List<PoolPreset> presets = new List<PoolPreset>(); // 미리 등록할 프리팹 목록
        [SerializeField] private int defaultInitialSize = 5; // 미등록 프리팹 요청 시 기본 초기 크기
        [SerializeField] private int defaultMaxSize; // 미등록 프리팹 요청 시 기본 최대 크기 (0 이하 = 무제한)

        /// 오브젝트가 풀로 반환될 때 발생 (WaveSpawner 등이 생존 개체 수 추적에 사용)
        public event Action<GameObject> OnReleased;

        private readonly Dictionary<GameObject, ObjectPool> pools = new Dictionary<GameObject, ObjectPool>(); // 프리팹별 풀
        private Transform poolRoot; // 비활성 풀 오브젝트들을 모아둘 부모

        private void Awake()
        {
            // 씬에 하나만 있어야 하는 싱글턴 - 중복되면 나중 것을 파괴
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            // 풀링된(비활성) 오브젝트들을 하이어라키에서 한 곳에 모아두기 위한 부모
            poolRoot = new GameObject("PoolRoot").transform;
            poolRoot.SetParent(transform);

            // 인스펙터에 미리 등록해둔 프리팹들은 씬 시작하자마자 풀을 만들어둠 (런타임 첫 스폰 때 지연 없도록)
            foreach (PoolPreset preset in presets)
            {
                if (preset.prefab != null)
                {
                    RegisterPool(preset.prefab, preset.initialSize, preset.maxSize);
                }
            }
        }

        /// 프리팹에 대한 풀을 명시적으로 등록 (이미 등록되어 있으면 무시)
        public void RegisterPool(GameObject prefab, int initialSize, int maxSize)
        {
            // 이미 등록된 프리팹이면 중복 생성 방지
            if (prefab == null || pools.ContainsKey(prefab))
            {
                return;
            }

            pools[prefab] = new ObjectPool(prefab, initialSize, maxSize, poolRoot);
        }

        /// 프리팹 풀에서 오브젝트를 꺼냄 (미등록 프리팹이면 기본값으로 자동 등록 후 사용)
        public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null)
            {
                return null;
            }

            // 미리 등록 안 해둔 프리팹이어도 여기서 즉석으로 풀을 만들어 사용 - 프리셋 등록을 깜빡해도 동작은 함
            if (!pools.TryGetValue(prefab, out ObjectPool pool))
            {
                RegisterPool(prefab, defaultInitialSize, defaultMaxSize);
                pool = pools[prefab];
            }

            return pool.Get(position, rotation);
        }

        /// 오브젝트를 원래 프리팹의 풀로 반환
        public void Release(GameObject obj)
        {
            if (obj == null)
            {
                return;
            }

            // PooledObjectTag로 원본 프리팹을 역추적해서 맞는 풀에 반환. 태그가 없거나 풀을 못 찾으면 그냥 파괴
            if (obj.TryGetComponent(out PooledObjectTag tag) && pools.TryGetValue(tag.SourcePrefab, out ObjectPool pool))
            {
                pool.Release(obj);
            }
            else
            {
                Destroy(obj);
            }

            // 풀로 돌아갔든 파괴됐든 "반환됨" 이벤트는 항상 발생 - WaveSpawner가 생존 개체 수 추적에 사용
            OnReleased?.Invoke(obj);
        }
    }
}
