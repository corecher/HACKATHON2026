using CardGameTemplate.Card;
using UnityEngine;

namespace CardGameTemplate.Zones
{
    /// <summary>
    /// 카드를 실제로 플레이하는 존. 드롭 시 카드 효과 발동을 로그로만 시뮬레이션한다
    /// </summary>
    public class PlayZone : CardZoneBase
    {
        [Header("이벤트")]
        [SerializeField] private CardZoneEvent onCardPlayed = new CardZoneEvent(); // 카드가 플레이됐을 때 실행되는 이벤트 (실제 게임 로직 연결 지점)

        /// <summary>onCardPlayed 이벤트 인스턴스 (인스펙터 외부에서 리스너를 추가로 연결할 때 사용)</summary>
        public CardZoneEvent OnCardPlayed => onCardPlayed;

        /// <summary>카드 효과 발동을 로그로 시뮬레이션하고 외부 리스너에게 알린다</summary>
        protected override void OnCardAccepted(Card.Card card, CardView cardView)
        {
            CardData data = card.Data;
            if (data != null)
            {
                Debug.Log($"[Card Played] {data.CardName}: {data.Description} (Power {data.Power}, Cost {data.Cost})");
            }

            onCardPlayed.Invoke(cardView);
        }
    }
}
