namespace WaveSpawnerTemplate.Pooling
{
    /// 풀링 대상 오브젝트가 스폰/반환 시점에 상태를 초기화할 수 있게 해주는 콜백 인터페이스
    public interface IPoolable
    {
        /// 풀에서 꺼내져 활성화될 때 호출됨 (예: 체력 초기화)
        void OnSpawned();

        /// 풀로 반환되어 비활성화될 때 호출됨 (예: 진행 중이던 코루틴 정리)
        void OnDespawned();
    }
}
