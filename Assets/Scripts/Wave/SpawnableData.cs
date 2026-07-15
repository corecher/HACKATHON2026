using UnityEngine;

namespace WaveSpawnerTemplate.Wave
{
    /// 스폰 카테고리 태그 (스폰 지점 필터링용, 필요에 맞게 항목 추가/변경 가능)
    public enum SpawnableCategory
    {
        Default,
        TypeA,
        TypeB,
        TypeC
    }

    /// 스폰 가능한 오브젝트 하나를 정의하는 데이터 (프리팹 + 기본 스탯)
    [CreateAssetMenu(fileName = "NewSpawnableData", menuName = "Wave Spawner Template/Spawnable Data")]
    public class SpawnableData : ScriptableObject
    {
        [SerializeField] private string spawnableName = "New Spawnable"; // 스폰 오브젝트 이름
        [SerializeField] private GameObject prefab; // 스폰할 프리팹
        [SerializeField] private SpawnableCategory category = SpawnableCategory.Default; // 분류 태그

        [Header("Base Stats (확장용, 비워둬도 됨)")]
        [SerializeField] private float baseHealth; // 기본 체력 (선택 사항, 실제 로직은 사용처에서 구현)
        [SerializeField] private float baseSpeed; // 기본 이동 속도 (선택 사항)
        [SerializeField] private float baseDamage; // 기본 공격력 (선택 사항)

        public string SpawnableName => spawnableName; // 이름 접근자
        public GameObject Prefab => prefab; // 프리팹 접근자
        public SpawnableCategory Category => category; // 카테고리 접근자
        public float BaseHealth => baseHealth; // 기본 체력 접근자
        public float BaseSpeed => baseSpeed; // 기본 속도 접근자
        public float BaseDamage => baseDamage; // 기본 공격력 접근자
    }
}
