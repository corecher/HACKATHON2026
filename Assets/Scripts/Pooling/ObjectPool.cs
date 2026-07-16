using System.Collections.Generic;
using UnityEngine;

namespace WaveSpawnerTemplate.Pooling
{
    /// 프리팹 하나를 관리하는 오브젝트 풀 (Queue 기반 Get/Release)
    public class ObjectPool
    {
        private readonly GameObject prefab; // 이 풀이 관리하는 원본 프리팹
        private readonly Transform poolParent; // 비활성 오브젝트를 정리해둘 부모 트랜스폼
        private readonly int maxSize; // 풀 최대 크기 (0 이하면 무제한)
        private readonly Queue<GameObject> pool = new Queue<GameObject>(); // 비활성 오브젝트 큐
        private int totalCreated; // 지금까지 생성된 전체 오브젝트 수

        public ObjectPool(GameObject prefab, int initialSize, int maxSize, Transform poolParent)
        {
            this.prefab = prefab;
            this.maxSize = maxSize;
            this.poolParent = poolParent;

            // maxSize가 설정돼있으면 그 이상 미리 만들지 않도록 initialSize를 제한 (0 이하 = 무제한이라 그대로 사용)
            int warmupCount = maxSize > 0 ? Mathf.Min(initialSize, maxSize) : initialSize;

            // 생성자에서 미리 initialSize만큼 만들어두고 비활성 상태로 큐에 넣음
            // (런타임 중 Instantiate 스파이크를 피하려고 미리 "워밍업"하는 것)
            for (int i = 0; i < warmupCount; i++)
            {
                GameObject obj = CreateNew();
                obj.SetActive(false);
                pool.Enqueue(obj);
            }
        }

        /// 새 인스턴스를 생성하고 원본 프리팹 태그를 붙임
        private GameObject CreateNew()
        {
            GameObject obj = Object.Instantiate(prefab, poolParent);
            PooledObjectTag tag = obj.AddComponent<PooledObjectTag>();
            tag.SetSourcePrefab(prefab);
            totalCreated++;
            return obj;
        }

        /// 풀에서 오브젝트 하나를 꺼내 지정 위치/회전으로 활성화
        public GameObject Get(Vector3 position, Quaternion rotation)
        {
            GameObject obj;

            if (pool.Count > 0)
            {
                // 이미 만들어둔 비활성 오브젝트가 있으면 그걸 재사용 (Instantiate 비용 없음)
                obj = pool.Dequeue();
            }
            else
            {
                // 풀이 비어있으면 새로 생성 (maxSize 초과 시에도 Instantiate로 대체하여 계속 동작)
                obj = CreateNew();
            }

            obj.transform.SetPositionAndRotation(position, rotation);
            obj.SetActive(true);

            // IPoolable을 구현한 컴포넌트가 있으면 "방금 스폰됐다"는 걸 알려줘서 상태 초기화 기회를 줌
            if (obj.TryGetComponent(out IPoolable poolable))
            {
                poolable.OnSpawned();
            }

            return obj;
        }

        /// 오브젝트를 비활성화하고 풀로 반환
        public void Release(GameObject obj)
        {
            // 비활성화되기 전에 정리할 기회를 줌 (진행 중이던 코루틴/타이머 등)
            if (obj.TryGetComponent(out IPoolable poolable))
            {
                poolable.OnDespawned();
            }

            obj.SetActive(false);
            pool.Enqueue(obj); // 다음 Get() 호출 때 재사용되도록 다시 큐에 넣음
        }
    }
}
