using System.Collections.Generic;
using CardGameTemplate.Card;
using UnityEngine;

namespace CardGameTemplate.Deck
{
    /// <summary>
    /// 덱(카드 뭉치)을 보관하고 셔플/드로우 기능을 제공하는 매니저
    /// </summary>
    public class DeckManager : MonoBehaviour
    {
        /// <summary>씬 내 유일한 DeckManager 인스턴스</summary>
        public static DeckManager Instance { get; private set; }

        [Header("덱 구성")]
        [SerializeField] private List<CardData> initialDeckList = new List<CardData>(); // 초기 덱을 구성하는 카드 데이터 목록
        [SerializeField] private bool shuffleOnStart = true; // 시작 시 자동으로 셔플할지 여부

        private readonly List<CardData> runtimeDeck = new List<CardData>();

        /// <summary>현재 덱에 남은 카드 수</summary>
        public int RemainingCount => runtimeDeck.Count;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            ResetDeck();
        }

        private void Start()
        {
            if (shuffleOnStart)
            {
                Shuffle();
            }
        }

        /// <summary>초기 카드 목록으로 런타임 덱을 다시 채운다</summary>
        public void ResetDeck()
        {
            runtimeDeck.Clear();
            runtimeDeck.AddRange(initialDeckList);
        }

        /// <summary>Fisher-Yates 알고리즘으로 덱을 무작위로 섞는다</summary>
        public void Shuffle()
        {
            // 뒤에서부터 앞으로 훑으며 각 위치를 아직 안 섞인 범위(0~i) 중 무작위 위치와 교환 - 편향 없이 고르게 섞임
            for (int i = runtimeDeck.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (runtimeDeck[i], runtimeDeck[j]) = (runtimeDeck[j], runtimeDeck[i]);
            }
        }

        /// <summary>덱 맨 위에서 최대 count장을 뽑아 반환한다 (부족하면 남은 만큼만 반환)</summary>
        public List<CardData> Draw(int count)
        {
            List<CardData> drawn = new List<CardData>();

            // 리스트 맨 끝(Count-1)을 "덱의 맨 위"로 취급 - 끝에서 제거하면 앞쪽 요소들 안 밀리니 성능상 유리
            for (int i = 0; i < count && runtimeDeck.Count > 0; i++)
            {
                int topIndex = runtimeDeck.Count - 1;
                drawn.Add(runtimeDeck[topIndex]);
                runtimeDeck.RemoveAt(topIndex);
            }

            return drawn;
        }
    }
}
