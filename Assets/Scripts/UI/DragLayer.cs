using UnityEngine;

namespace InventoryTemplate.UI
{
    /// <summary>
    /// 드래그 중인 아이콘이 그 어떤 슬롯보다 위에 그려지도록 하는 최상단 레이어
    /// </summary>
    public class DragLayer : MonoBehaviour
    {
        /// <summary>싱글톤 인스턴스</summary>
        public static DragLayer Instance { get; private set; }

        [SerializeField] private RectTransform root;
        /// <summary>드래그 아이콘이 실제로 생성될 부모 RectTransform</summary>
        public RectTransform Root => root;

        private void Awake()
        {
            Instance = this;
        }
    }
}
