using System;
using System.Collections.Generic;
using UnityEngine;

namespace WaveSpawnerTemplate.Wave
{
    /// 웨이브 시퀀스 내 한 스텝 (웨이브 데이터 + 시작 전 대기시간)
    [Serializable]
    public class WaveSequenceEntry
    {
        [SerializeField] private WaveData waveData; // 실행할 웨이브 데이터
        [SerializeField] private float delayBeforeStart; // 이 웨이브 시작 전 대기시간 (초)

        public WaveData WaveData => waveData; // 웨이브 데이터 접근자
        public float DelayBeforeStart => delayBeforeStart; // 대기시간 접근자
    }

    /// 하나의 스테이지/레벨을 구성하는 웨이브 순서 데이터
    [CreateAssetMenu(fileName = "NewWaveSequenceData", menuName = "Wave Spawner Template/Wave Sequence Data")]
    public class WaveSequenceData : ScriptableObject
    {
        [SerializeField] private string sequenceName = "New Stage"; // 시퀀스(스테이지) 이름
        [SerializeField] private List<WaveSequenceEntry> waves = new List<WaveSequenceEntry>(); // 순서대로 실행할 웨이브 목록

        public string SequenceName => sequenceName; // 이름 접근자
        public IReadOnlyList<WaveSequenceEntry> Waves => waves; // 웨이브 목록 접근자
    }
}
