using System.Collections;
using CardGameTemplate.Card;
using CardGameTemplate.UI;
using CardGameTemplate.Zones;
using UnityEngine;
using UnityEngine.Rendering;

namespace CardGameTemplate.DragDrop
{
    /// <summary>
    /// 마우스 드래그로 카드를 집어서 존에 놓는 상호작용을 담당한다.
    /// 손패 카드는 CardZoneBase 계열 존(PlayZone/DiscardZone)에, 드로우 소스 카드는 DrawZone에 처리를 위임한다
    /// </summary>
    [RequireComponent(typeof(Card.Card))]
    [RequireComponent(typeof(CardView))]
    [RequireComponent(typeof(SortingGroup))]
    public class CardDragHandler : MonoBehaviour
    {
        [Header("드래그 설정")]
        [SerializeField] private float dragZDepth = 0f; // 드래그 중 카드가 위치할 월드 z값 (카메라 기준 평면 고정)
        [SerializeField] private int draggingSortingOrder = 100; // 드래그 중 임시로 사용할 정렬 순서
        [SerializeField] private Vector2 dropCheckOffset = new Vector2(0f, -0.3f); // 드롭 판정에 사용할 카드 하단 기준점 오프셋
        [SerializeField] private float dropCheckRadius = 0.2f; // 드롭 판정 원 반지름
        [SerializeField] private LayerMask dropZoneLayerMask = ~0; // 드롭 존을 검출할 레이어 마스크

        [Header("복귀 애니메이션")]
        [SerializeField] private float returnDuration = 0.2f; // 원위치 복귀에 걸리는 시간
        [SerializeField] private AnimationCurve returnCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f); // 복귀 이징 커브

        [Header("호버 효과")]
        [SerializeField] private bool enableHoverEffect = true; // 호버 확대/이동 효과 사용 여부
        [SerializeField] private float hoverScaleMultiplier = 1.1f; // 호버 시 확대 배율
        [SerializeField] private float hoverMoveUp = 0.3f; // 호버 시 위로 이동하는 거리
        [SerializeField] private int hoverSortingOrder = 50; // 호버 시 임시 정렬 순서

        private Card.Card card;
        private CardView cardView;
        private SortingGroup sortingGroup;
        private Camera mainCamera;

        private Vector3 originPosition; // 드래그 시작 시점의 원래 위치
        private Transform originParent; // 드래그 시작 시점의 원래 부모
        private int restingSortingOrder; // 드래그/호버 적용 전 평상시 정렬 순서
        private Vector3 dragOffset; // 카드 중심과 클릭 지점 사이의 오프셋
        private Vector3 baseScale; // 호버 적용 전 원본 스케일

        private bool isDragging;
        private bool isHovering;
        private Coroutine returnRoutine;

        private bool isDrawSourceCard; // 이 카드가 DrawZone의 페이스다운 소스 카드인지 여부
        private DrawZone sourceDrawZone; // 소스 카드일 때 소속된 DrawZone
        private float drawDragThreshold; // 소스 카드일 때 드로우로 인정할 최소 드래그 거리

        /// <summary>드래그로 카드를 놓쳤을 때 원위치로 돌아가는 대신 커스텀 처리를 하고 싶다면 구독한다</summary>
        public System.Action<Card.Card, CardZoneBase> OnDroppedOnZone;

        /// <summary>이 카드를 DrawZone의 소스 카드로 표시한다. 일반 존 드롭 판정 대신 드로우 로직을 사용하게 된다</summary>
        public void MarkAsDrawSource(DrawZone zone, float threshold)
        {
            isDrawSourceCard = true;
            sourceDrawZone = zone;
            drawDragThreshold = threshold;
        }

        private void Awake()
        {
            card = GetComponent<Card.Card>();
            cardView = GetComponent<CardView>();
            sortingGroup = GetComponent<SortingGroup>();
            mainCamera = Camera.main;
            baseScale = transform.localScale;
            restingSortingOrder = sortingGroup.sortingOrder;
        }

        private void OnMouseDown()
        {
            // 복귀 애니메이션 도중에 다시 잡았으면 그 애니메이션부터 취소
            if (returnRoutine != null)
            {
                StopCoroutine(returnRoutine);
                returnRoutine = null;
            }

            isDragging = true;
            originPosition = transform.position; // 드롭 실패 시 돌아갈 위치
            originParent = transform.parent;

            // 마우스 클릭 지점과 카드 중심 사이의 차이를 기억해둬야, 드래그 중 카드가 마우스로 "순간이동"하지 않고
            // 처음 잡은 지점 그대로 붙어서 따라다님
            dragOffset = transform.position - GetMouseWorldPoint();
            sortingGroup.sortingOrder = draggingSortingOrder; // 드래그 중엔 다른 카드들보다 위에 그려지게
        }

        private void OnMouseDrag()
        {
            if (!isDragging)
            {
                return;
            }

            transform.position = GetMouseWorldPoint() + dragOffset;
        }

        private void OnMouseUp()
        {
            if (!isDragging)
            {
                return;
            }

            isDragging = false;

            // 드로우 존의 소스 카드는 일반 존 드롭과 다른 전용 로직으로 처리
            if (isDrawSourceCard)
            {
                HandleDrawSourceRelease();
                return;
            }

            // 카드 아래쪽 기준점과 겹치는 존을 찾아 수용 가능하면 그쪽에 카드를 넘김
            CardZoneBase targetZone = FindOverlappingZone();
            if (targetZone != null && targetZone.TryAcceptCard(card, cardView))
            {
                // 존이 카드를 받아들였으면 손패 목록에서는 제거 (오브젝트 자체는 존이 알아서 처리)
                if (HandManager.Instance != null)
                {
                    HandManager.Instance.RemoveFromHand(card, destroyObject: true);
                }

                OnDroppedOnZone?.Invoke(card, targetZone);
                return;
            }

            // 어느 존에도 못 놓였으면(빈 공간에 드롭) 원래 손패 위치로 되돌아감
            returnRoutine = StartCoroutine(ReturnToOrigin());
        }

        /// <summary>드로우 소스 카드가 놓였을 때, 충분히 드래그됐다면 실제 드로우를 트리거하고 아니면 원위치로 되돌린다</summary>
        private void HandleDrawSourceRelease()
        {
            float draggedDistance = Vector3.Distance(transform.position, originPosition);
            if (draggedDistance < drawDragThreshold || sourceDrawZone == null)
            {
                returnRoutine = StartCoroutine(ReturnToOrigin());
                return;
            }

            sourceDrawZone.HandleDraggedOut(this);
        }

        private void OnMouseEnter()
        {
            // 드래그 중엔 호버 효과를 따로 안 줌 (이미 드래그 중이라 의미 없음)
            if (!enableHoverEffect || isDragging)
            {
                return;
            }

            isHovering = true;
            sortingGroup.sortingOrder = hoverSortingOrder;
            transform.localScale = baseScale * hoverScaleMultiplier;
            transform.position += Vector3.up * hoverMoveUp;
        }

        private void OnMouseExit()
        {
            if (!enableHoverEffect || isDragging || !isHovering)
            {
                return;
            }

            // 호버 진입 때 적용했던 확대/이동을 정확히 반대로 되돌림
            isHovering = false;
            sortingGroup.sortingOrder = restingSortingOrder;
            transform.localScale = baseScale;
            transform.position -= Vector3.up * hoverMoveUp;
        }

        /// <summary>현재 마우스 스크린 좌표를 고정된 z값의 월드 좌표로 변환한다</summary>
        private Vector3 GetMouseWorldPoint()
        {
            // 스크린 좌표는 z가 "카메라로부터의 거리" 개념이라, 카드가 있는 z 평면까지의 거리를 구해서 그 깊이로 변환
            Vector3 screenPoint = Input.mousePosition;
            screenPoint.z = mainCamera.WorldToScreenPoint(new Vector3(0f, 0f, dragZDepth)).z;
            return mainCamera.ScreenToWorldPoint(screenPoint);
        }

        /// <summary>카드 하단 기준점과 겹치는 CardZoneBase 계열 존을 탐색한다 (PlayZone/DiscardZone 등)</summary>
        private CardZoneBase FindOverlappingZone()
        {
            // 카드 중심이 아니라 하단 기준점(dropCheckOffset)으로 판정해야 "카드를 존 위에 놓았다"는 느낌이 자연스러움
            Vector2 checkPoint = (Vector2)transform.position + dropCheckOffset;
            Collider2D[] hits = Physics2D.OverlapCircleAll(checkPoint, dropCheckRadius, dropZoneLayerMask);

            foreach (Collider2D hit in hits)
            {
                // 콜라이더가 자식 오브젝트에 있을 수도 있으니 부모 쪽까지 올라가며 CardZoneBase를 찾음
                CardZoneBase zone = hit.GetComponentInParent<CardZoneBase>();
                if (zone != null)
                {
                    return zone;
                }
            }

            return null;
        }

        private IEnumerator ReturnToOrigin()
        {
            // 드래그 중엔 부모에서 잠깐 분리됐을 수도 있으니 원래 부모로 되돌려놓고 애니메이션 시작
            transform.SetParent(originParent, true);

            Vector3 startPos = transform.position;
            float t = 0f;

            // 이징 커브를 적용해 현재 위치 → 원래 위치로 부드럽게 이동
            while (t < returnDuration)
            {
                t += Time.deltaTime;
                float eval = returnCurve.Evaluate(Mathf.Clamp01(t / returnDuration));
                transform.position = Vector3.Lerp(startPos, originPosition, eval);
                yield return null;
            }

            transform.position = originPosition;
            sortingGroup.sortingOrder = restingSortingOrder;
            returnRoutine = null;
        }
    }
}
