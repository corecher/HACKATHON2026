using System.Collections.Generic;
using CardGameTemplate.Card;
using CardGameTemplate.Deck;
using UnityEngine;

namespace CardGameTemplate.UI
{
    /// <summary>
    /// 드로우된 카드를 실제 프리팹으로 생성하고 손패 배치를 관리하는 매니저
    /// </summary>
    public class HandManager : MonoBehaviour
    {
        /// <summary>씬 내 유일한 HandManager 인스턴스</summary>
        public static HandManager Instance { get; private set; }

        [Header("프리팹/컨테이너")]
        [SerializeField] private Card.Card cardPrefab; // 손패에 생성할 카드 프리팹
        [SerializeField] private Transform handContainer; // 손패 카드들의 부모 겸 배치 기준점

        [Header("배치 설정")]
        [SerializeField] private bool useFanLayout = true; // true면 부채꼴, false면 일렬 배치
        [SerializeField] private float cardSpacing = 1.2f; // 카드 사이 가로 간격
        [SerializeField] private float fanAngleStep = 6f; // 부채꼴 배치 시 카드 한 장당 회전 각도
        [SerializeField] private float fanArcHeight = 0.15f; // 부채꼴 배치 시 가장자리 카드가 낮아지는 정도

        [Header("테스트용")]
        [SerializeField] private int drawOnStartCount = 3; // 씬 시작 시 자동으로 드로우할 카드 수 (0이면 사용 안 함)

        private readonly List<Card.Card> handCards = new List<Card.Card>();

        /// <summary>현재 손패에 들어있는 카드 목록 (읽기 전용)</summary>
        public IReadOnlyList<Card.Card> HandCards => handCards;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            if (drawOnStartCount > 0)
            {
                DrawToHand(drawOnStartCount);
            }
        }

        /// <summary>덱에서 count장을 드로우하여 손패 프리팹으로 생성한다 (즉시 앞면 공개)</summary>
        public void DrawToHand(int count)
        {
            if (DeckManager.Instance == null || cardPrefab == null || handContainer == null)
            {
                return;
            }

            List<CardData> drawn = DeckManager.Instance.Draw(count);
            foreach (CardData data in drawn)
            {
                CreateHandCardInstance(data, faceUp: true);
            }

            RelayoutHand();
        }

        /// <summary>DrawZone처럼 이미 뽑아둔 카드 1장을 페이스다운으로 손패에 넣고 애니메이션으로 뒤집는다</summary>
        public Card.Card AddDrawnCard(CardData data)
        {
            if (cardPrefab == null || handContainer == null || data == null)
            {
                return null;
            }

            Card.Card newCard = CreateHandCardInstance(data, faceUp: false);
            RelayoutHand();
            newCard.SetFaceUp(true);
            return newCard;
        }

        /// <summary>손패 카드 프리팹을 실제로 인스턴스화하고 목록에 등록하는 공통 로직</summary>
        private Card.Card CreateHandCardInstance(CardData data, bool faceUp)
        {
            Card.Card newCard = Instantiate(cardPrefab, handContainer);
            newCard.Setup(data, faceUp);
            handCards.Add(newCard);
            return newCard;
        }

        /// <summary>손패에서 카드를 제거하고 나머지 카드를 재정렬한다</summary>
        public void RemoveFromHand(Card.Card card, bool destroyObject = false)
        {
            if (!handCards.Remove(card))
            {
                return;
            }

            if (destroyObject && card != null)
            {
                Destroy(card.gameObject);
            }

            RelayoutHand();
        }

        /// <summary>CardZoneEvent(CardView) 등 UnityEvent에 그대로 연결할 수 있는 오버로드</summary>
        public void RemoveFromHand(CardView cardView)
        {
            if (cardView == null)
            {
                return;
            }

            Card.Card matched = handCards.Find(c => c != null && c.GetComponent<CardView>() == cardView);
            RemoveFromHand(matched);
        }

        /// <summary>손패 카드 개수를 기준으로 전체 카드 위치/회전을 재계산해 적용한다</summary>
        public void RelayoutHand()
        {
            int count = handCards.Count;
            for (int i = 0; i < count; i++)
            {
                Card.Card card = handCards[i];
                if (card == null)
                {
                    continue;
                }

                (Vector3 localPos, float rotZ) = ComputeSlotTransform(i, count);
                card.transform.SetParent(handContainer, true);
                card.transform.localPosition = localPos;
                card.transform.localRotation = Quaternion.Euler(0f, 0f, rotZ);
            }
        }

        /// <summary>손패 슬롯 인덱스에 대응하는 로컬 위치와 회전각을 계산한다</summary>
        private (Vector3 localPos, float rotZ) ComputeSlotTransform(int index, int count)
        {
            float mid = (count - 1) * 0.5f;
            float offsetFromMid = index - mid;

            float x = offsetFromMid * cardSpacing;

            if (!useFanLayout)
            {
                return (new Vector3(x, 0f, 0f), 0f);
            }

            float rotZ = -offsetFromMid * fanAngleStep;
            float y = -Mathf.Abs(offsetFromMid) * fanArcHeight;
            return (new Vector3(x, y, 0f), rotZ);
        }
    }
}
