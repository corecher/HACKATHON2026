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
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            poolRoot = new GameObject("PoolRoot").transform;
            poolRoot.SetParent(transform);

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

            if (obj.TryGetComponent(out PooledObjectTag tag) && pools.TryGetValue(tag.SourcePrefab, out ObjectPool pool))
            {
                pool.Release(obj);
            }
            else
            {
                Destroy(obj);
            }

            OnReleased?.Invoke(obj);
        }
    }
}
