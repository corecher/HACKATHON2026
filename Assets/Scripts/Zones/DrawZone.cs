using System.Collections.Generic;
using CardGameTemplate.Card;
using CardGameTemplate.Deck;
using CardGameTemplate.DragDrop;
using CardGameTemplate.UI;
using UnityEngine;

namespace CardGameTemplate.Zones
{
    /// <summary>
    /// 카드를 뽑아내는 존. 손패를 받아들이는 다른 존들과 반대로 카드를 내보내는 소스 역할이라
    /// CardZoneBase를 상속하지 않는 별도 클래스로 구현한다
    /// </summary>
    public class DrawZone : MonoBehaviour
    {
        [Header("소스 카드 설정")]
        [SerializeField] private Card.Card cardPrefab; // 드로우 존에 놓일 카드 프리팹
        [SerializeField] private Transform spawnPoint; // 카드가 스폰될 기준 위치 (비어있으면 자기 자신의 위치 사용)
        [SerializeField] private float dragThreshold = 0.3f; // 드로우로 인정할 최소 드래그 거리

        private Card.Card currentCard; // 현재 드로우 존에 떠 있는 페이스다운 카드

        private Vector3 SpawnPosition => spawnPoint != null ? spawnPoint.position : transform.position;

        private void Start()
        {
            Refill();
        }

        /// <summary>덱에 카드가 남아있다면 페이스다운 카드를 새로 스폰한다. 없으면 빈 상태로 둔다</summary>
        public void Refill()
        {
            if (currentCard != null || cardPrefab == null)
            {
                return;
            }

            if (DeckManager.Instance == null || DeckManager.Instance.RemainingCount <= 0)
            {
                currentCard = null;
                return;
            }

            currentCard = Instantiate(cardPrefab, SpawnPosition, spawnPoint != null ? spawnPoint.rotation : Quaternion.identity, transform);
            currentCard.Setup(null, faceUp: false);

            CardDragHandler dragHandler = currentCard.GetComponent<CardDragHandler>();
            if (dragHandler != null)
            {
                dragHandler.MarkAsDrawSource(this, dragThreshold);
            }
        }

        /// <summary>드로우 소스 카드가 임계 거리 이상 드래그되어 놓였을 때 호출된다. 실제 카드를 덱에서 뽑아 손패에 추가한다</summary>
        public void HandleDraggedOut(CardDragHandler draggedCard)
        {
            if (currentCard != null && draggedCard.gameObject == currentCard.gameObject)
            {
                currentCard = null;
            }

            Destroy(draggedCard.gameObject);

            if (DeckManager.Instance == null)
            {
                return;
            }

            List<CardData> drawn = DeckManager.Instance.Draw(1);
            if (drawn.Count > 0 && HandManager.Instance != null)
            {
                HandManager.Instance.AddDrawnCard(drawn[0]);
            }

            Refill();
        }
    }
}
