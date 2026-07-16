using System.Collections;
using UnityEngine;

namespace CardGameTemplate.Card
{
    /// <summary>
    /// 카드 오브젝트의 루트 컴포넌트. 데이터 바인딩과 앞/뒷면 전환을 담당한다
    /// </summary>
    [RequireComponent(typeof(CardView))]
    public class Card : MonoBehaviour
    {
        [Header("앞/뒷면 오브젝트")]
        [SerializeField] private GameObject frontRoot; // 카드 앞면 시각 요소 루트
        [SerializeField] private GameObject backRoot; // 카드 뒷면 시각 요소 루트

        [Header("뒤집기 연출")]
        [SerializeField] private float flipDuration = 0.15f; // 뒤집기 애니메이션 소요 시간
        [SerializeField] private bool animateFlip = true; // 뒤집기 시 스케일 애니메이션 사용 여부

        private CardView cardView;
        private Coroutine flipRoutine;

        /// <summary>이 카드에 바인딩된 원본 데이터</summary>
        public CardData Data { get; private set; }

        /// <summary>카드가 현재 앞면을 보이고 있는지 여부</summary>
        public bool IsFaceUp { get; private set; } = true;

        private void Awake()
        {
            cardView = GetComponent<CardView>();
        }

        /// <summary>카드 데이터를 설정하고 앞면 뷰를 갱신한다</summary>
        public void Setup(CardData data, bool faceUp = true)
        {
            Data = data;
            cardView.Bind(data);
            SetFaceUp(faceUp, instant: true);
        }

        /// <summary>현재 상태를 반전시켜 카드를 뒤집는다</summary>
        public void Flip()
        {
            SetFaceUp(!IsFaceUp);
        }

        /// <summary>앞/뒷면 상태를 직접 지정한다 (instant면 애니메이션 없이 즉시 전환)</summary>
        public void SetFaceUp(bool faceUp, bool instant = false)
        {
            IsFaceUp = faceUp;

            if (flipRoutine != null)
            {
                StopCoroutine(flipRoutine);
                flipRoutine = null;
            }

            if (!animateFlip || instant)
            {
                ApplyFaceState(faceUp);
                return;
            }

            flipRoutine = StartCoroutine(FlipRoutine(faceUp));
        }

        private void ApplyFaceState(bool faceUp)
        {
            if (frontRoot != null)
            {
                frontRoot.SetActive(faceUp);
            }

            if (backRoot != null)
            {
                backRoot.SetActive(!faceUp);
            }
        }

        private IEnumerator FlipRoutine(bool faceUp)
        {
            // 카드 뒤집기를 "가로 스케일을 0까지 줄였다가 다시 늘리는" 방식으로 흉내냄 (실제 3D 회전 없이 2D에서 간단히 구현)
            float half = flipDuration * 0.5f;
            Vector3 baseScale = transform.localScale;

            // 전반부: 가로 폭을 원래 크기 → 0으로 줄임 (옆에서 보면 얇아지는 것처럼 보임)
            float t = 0f;
            while (t < half)
            {
                t += Time.deltaTime;
                float x = Mathf.Lerp(baseScale.x, 0f, t / half);
                transform.localScale = new Vector3(x, baseScale.y, baseScale.z);
                yield return null;
            }

            // 폭이 0이 된 순간(화면상 안 보이는 타이밍)에 실제로 앞/뒷면을 교체 - 전환이 눈에 띄지 않음
            transform.localScale = new Vector3(0f, baseScale.y, baseScale.z);
            ApplyFaceState(faceUp);

            // 후반부: 0 → 원래 크기로 다시 넓힘
            t = 0f;
            while (t < half)
            {
                t += Time.deltaTime;
                float x = Mathf.Lerp(0f, baseScale.x, t / half);
                transform.localScale = new Vector3(x, baseScale.y, baseScale.z);
                yield return null;
            }

            transform.localScale = baseScale;
            flipRoutine = null;
        }
    }
}
