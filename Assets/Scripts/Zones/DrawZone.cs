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
            // 이미 카드가 놓여있으면 다시 안 채움 (중복 스폰 방지)
            if (currentCard != null || cardPrefab == null)
            {
                return;
            }

            // 덱에 남은 카드가 없으면 빈 상태로 둠 (플레이어가 볼 때 "이제 뽑을 카드 없음"으로 보임)
            if (DeckManager.Instance == null || DeckManager.Instance.RemainingCount <= 0)
            {
                currentCard = null;
                return;
            }

            // Setup(null, faceUp: false)로 - 실제 어떤 카드인지는 아직 안 정하고 페이스다운 상태로만 보여줌
            // (실제 카드 데이터는 드래그해서 꺼낼 때(HandleDraggedOut)가 되어서야 덱에서 뽑아 확정됨)
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
            // 지금 막 드래그된 게 현재 추적 중인 소스 카드라면 참조를 비워서 Refill()이 새로 채울 수 있게 함
            if (currentCard != null && draggedCard.gameObject == currentCard.gameObject)
            {
                currentCard = null;
            }

            // 페이스다운 더미 오브젝트는 실제 카드가 아니므로 파괴하고, 진짜 카드는 아래에서 새로 생성함
            Destroy(draggedCard.gameObject);

            if (DeckManager.Instance == null)
            {
                return;
            }

            // 이 시점에 실제로 덱에서 한 장 뽑아 손패에 추가 (앞면 공개 애니메이션은 HandManager.AddDrawnCard가 처리)
            List<CardData> drawn = DeckManager.Instance.Draw(1);
            if (drawn.Count > 0 && HandManager.Instance != null)
            {
                HandManager.Instance.AddDrawnCard(drawn[0]);
            }

            Refill(); // 다음에 뽑을 수 있도록 드로우 존을 다시 채움
        }
    }
}
