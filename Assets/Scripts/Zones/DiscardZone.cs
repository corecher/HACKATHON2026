using CardGameTemplate.Card;
using UnityEngine;

namespace CardGameTemplate.Zones
{
    /// <summary>
    /// 카드를 버리는 존. 별도 효과 없이 최소한의 로그만 남긴다
    /// </summary>
    public class DiscardZone : CardZoneBase
    {
        [Header("이벤트")]
        [SerializeField] private CardZoneEvent onCardDiscarded = new CardZoneEvent(); // 카드가 버려졌을 때 실행되는 이벤트 (실제 게임 로직 연결 지점)

        /// <summary>onCardDiscarded 이벤트 인스턴스 (인스펙터 외부에서 리스너를 추가로 연결할 때 사용)</summary>
        public CardZoneEvent OnCardDiscarded => onCardDiscarded;

        /// <summary>버림을 최소 로그로만 남기고 외부 리스너에게 알린다</summary>
        protected override void OnCardAccepted(Card.Card card, CardView cardView)
        {
            CardData data = card.Data;
            string cardName = data != null ? data.CardName : card.name;
            Debug.Log($"[Card Discarded] {cardName}");

            onCardDiscarded.Invoke(cardView);
        }
    }
}
