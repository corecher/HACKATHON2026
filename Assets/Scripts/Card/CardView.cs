using TMPro;
using UnityEngine;

namespace CardGameTemplate.Card
{
    /// <summary>
    /// CardData를 받아 카드 앞면의 스프라이트/텍스트를 실제로 갱신하는 뷰 컴포넌트
    /// </summary>
    public class CardView : MonoBehaviour
    {
        [Header("앞면 참조")]
        [SerializeField] private SpriteRenderer iconRenderer; // 카드 아이콘을 그리는 렌더러
        [SerializeField] private TMP_Text nameText; // 카드 이름 텍스트
        [SerializeField] private TMP_Text descriptionText; // 카드 설명 텍스트
        [SerializeField] private TMP_Text costText; // 코스트 수치 텍스트
        [SerializeField] private TMP_Text powerText; // 공격력 수치 텍스트

        /// <summary>현재 바인딩된 카드 데이터</summary>
        public CardData BoundData { get; private set; }

        /// <summary>카드 데이터를 받아 화면에 표시할 시각 요소를 전부 갱신한다</summary>
        public void Bind(CardData data)
        {
            BoundData = data;
            if (data == null)
            {
                return;
            }

            if (iconRenderer != null)
            {
                iconRenderer.sprite = data.Icon;
            }

            if (nameText != null)
            {
                nameText.text = data.CardName;
            }

            if (descriptionText != null)
            {
                descriptionText.text = data.Description;
            }

            if (costText != null)
            {
                costText.text = data.Cost.ToString();
            }

            if (powerText != null)
            {
                powerText.text = data.Power.ToString();
            }
        }
    }
}
