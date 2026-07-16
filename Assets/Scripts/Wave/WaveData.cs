using System;
using System.Collections.Generic;
using UnityEngine;

namespace WaveSpawnerTemplate.Wave
{
    /// 한 웨이브 내에서 스폰할 항목 하나 (어떤 데이터를 몇 개 스폰할지)
    [Serializable]
    public class WaveEntry
    {
        [SerializeField] private SpawnableData spawnableData; // 스폰할 오브젝트 데이터
        [SerializeField] private int count = 1; // 스폰 개수

        public SpawnableData SpawnableData => spawnableData; // 데이터 접근자
        public int Count => count; // 개수 접근자
    }

    /// 웨이브 종료 조건 방식
    public enum WaveEndCondition
    {
        AllQuantitySpawned, // 정의된 수량을 모두 스폰하면 종료
        FixedDuration // 정해진 시간이 지나면 종료 (항목을 순환하며 계속 스폰)
    }

    /// 웨이브 하나를 정의하는 데이터 (무엇을, 몇 개, 얼마나 자주 스폰할지)
    [CreateAssetMenu(fileName = "NewWaveData", menuName = "Wave Spawner Template/Wave Data")]
    public class WaveData : ScriptableObject
    {
        [SerializeField] private string waveName = "New Wave"; // 웨이브 이름 (디버그/로그용)
        [SerializeField] private List<WaveEntry> entries = new List<WaveEntry>(); // 스폰 항목 목록
        [SerializeField] private float spawnInterval = 1f; // 스폰 간격 (초)
        [SerializeField] private WaveEndCondition endCondition = WaveEndCondition.AllQuantitySpawned; // 종료 조건
        [SerializeField] private float fixedDuration = 10f; // FixedDuration 선택 시 지속 시간 (초)

        public string WaveName => waveName; // 이름 접근자
        public IReadOnlyList<WaveEntry> Entries => entries; // 스폰 항목 목록 접근자
        public float SpawnInterval => spawnInterval; // 스폰 간격 접근자
        public WaveEndCondition EndCondition => endCondition; // 종료 조건 접근자
        public float FixedDuration => fixedDuration; // 고정 지속시간 접근자

        /// 이 웨이브에서 스폰될 전체 오브젝트 수 (AllQuantitySpawned 기준)
        public int TotalSpawnCount()
        {
            int total = 0;

            // 항목별 개수를 다 더함 (FixedDuration 모드에선 실제 스폰 수와 다를 수 있음 - 진행률 표시 등 참고용)
            foreach (WaveEntry entry in entries)
            {
                total += entry.Count;
            }

            return total;
        }
    }
}
