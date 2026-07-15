using System.Collections.Generic;
using UnityEngine;
using WaveSpawnerTemplate.Wave;

namespace WaveSpawnerTemplate.Spawning
{
    /// 스폰 지점 선택 전략
    public enum SpawnSelectionStrategy
    {
        Random, // 매번 무작위로 선택
        Sequential, // 등록 순서대로 진행 (끝에 도달하면 마지막 지점에 고정)
        RoundRobin // 등록 순서대로 반복 순환
    }

    /// 여러 SpawnPoint를 묶어 선택 전략에 따라 하나를 골라주는 그룹
    public class SpawnPointGroup : MonoBehaviour
    {
        [SerializeField] private List<SpawnPoint> spawnPoints = new List<SpawnPoint>(); // 그룹에 속한 스폰 지점 목록
        [SerializeField] private SpawnSelectionStrategy strategy = SpawnSelectionStrategy.Random; // 선택 전략

        private int nextIndex; // Sequential/RoundRobin 진행 인덱스
        private readonly List<SpawnPoint> candidateBuffer = new List<SpawnPoint>(); // 매 호출마다 재사용하는 후보 버퍼

        /// 카테고리 조건을 만족하는 스폰 지점 중 전략에 따라 하나를 선택
        public SpawnPoint SelectSpawnPoint(SpawnableCategory category)
        {
            candidateBuffer.Clear();

            foreach (SpawnPoint point in spawnPoints)
            {
                if (point != null && point.CanSpawn(category))
                {
                    candidateBuffer.Add(point);
                }
            }

            if (candidateBuffer.Count == 0)
            {
                return null;
            }

            switch (strategy)
            {
                case SpawnSelectionStrategy.Random:
                    return candidateBuffer[Random.Range(0, candidateBuffer.Count)];

                case SpawnSelectionStrategy.Sequential:
                    {
                        int index = Mathf.Min(nextIndex, candidateBuffer.Count - 1);
                        nextIndex = Mathf.Min(nextIndex + 1, candidateBuffer.Count - 1);
                        return candidateBuffer[index];
                    }

                case SpawnSelectionStrategy.RoundRobin:
                    {
                        int index = nextIndex % candidateBuffer.Count;
                        nextIndex++;
                        return candidateBuffer[index];
                    }

                default:
                    return candidateBuffer[0];
            }
        }
    }
}
