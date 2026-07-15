using System;
using CardGameTemplate.Card;
using UnityEngine;
using UnityEngine.Events;

namespace CardGameTemplate.Zones
{
    /// <summary>카드 존 이벤트에서 공통으로 사용하는 UnityEvent (인스펙터에서 콜백 연결 가능)</summary>
    [Serializable]
    public class CardZoneEvent : UnityEvent<CardView> { }

    /// <summary>
    /// 손패 카드를 받아들이는 존(PlayZone, DiscardZone)의 공통 베이스.
    /// DrawZone은 카드를 내보내는 반대 방향 존이라 이 베이스를 상속하지 않는다
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public abstract class CardZoneBase : MonoBehaviour
    {
        [Header("존 설정")]
        [SerializeField] private Transform snapPoint; // 카드가 스냅될 기준 위치 (비어있으면 자기 자신의 위치 사용)

        /// <summary>이 존이 카드를 받을 수 있는지 판단하는 델리게이트. 기본값은 항상 true</summary>
        public Func<CardView, bool> CanAccept { get; set; } = _ => true;

        /// <summary>카드가 스냅될 월드 위치</summary>
        public Vector3 SnapPosition => snapPoint != null ? snapPoint.position : transform.position;

        private void Reset()
        {
            Collider2D collider = GetComponent<Collider2D>();
            if (collider != null)
            {
                collider.isTrigger = true;
            }
        }

        /// <summary>카드를 이 존에 드롭 시도한다. 수용 가능하면 하위 클래스의 처리 로직을 실행하고 true를 반환한다</summary>
        public bool TryAcceptCard(Card.Card card, CardView cardView)
        {
            if (card == null || cardView == null || !CanAccept(cardView))
            {
                return false;
            }

            OnCardAccepted(card, cardView);
            return true;
        }

        /// <summary>카드가 수용됐을 때 실제로 수행할 동작. 각 존 타입이 자신의 방식대로 구현한다</summary>
        protected abstract void OnCardAccepted(Card.Card card, CardView cardView);
    }
}
