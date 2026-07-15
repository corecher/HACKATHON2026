using UnityEngine;

namespace CardGameTemplate.Card
{
    /// <summary>
    /// 카드 한 장의 고정 데이터를 담는 ScriptableObject (에셋으로 저장되는 카드 원본 데이터)
    /// </summary>
    [CreateAssetMenu(fileName = "NewCardData", menuName = "Card Game Template/Card Data", order = 0)]
    public class CardData : ScriptableObject
    {
        [Header("기본 정보")]
        [SerializeField] private string cardId = "card_000"; // 카드 고유 식별자
        [SerializeField] private string cardName = "New Card"; // 카드 이름
        [TextArea(2, 4)]
        [SerializeField] private string description = ""; // 카드 설명 텍스트

        [Header("비주얼")]
        [SerializeField] private Sprite icon; // 카드 아이콘 스프라이트

        [Header("수치")]
        [SerializeField] private int cost = 1; // 카드 사용 코스트
        [SerializeField] private int power = 1; // 카드 공격력(혹은 임의의 수치값)

        /// <summary>카드 고유 식별자를 반환한다</summary>
        public string CardId => cardId;

        /// <summary>카드 이름을 반환한다</summary>
        public string CardName => cardName;

        /// <summary>카드 설명을 반환한다</summary>
        public string Description => description;

        /// <summary>카드 아이콘 스프라이트를 반환한다</summary>
        public Sprite Icon => icon;

        /// <summary>카드 코스트를 반환한다</summary>
        public int Cost => cost;

        /// <summary>카드 공격력(수치값)을 반환한다</summary>
        public int Power => power;
    }
}
