using UnityEngine;
using UnityEngine.UI; // Image 컴포넌트를 쓰기 위해 필수
using System.Collections;
using System;

public class UIManager : Singleton<UIManager>
{
    [Header("UI Panels")]
    public GameObject readyPanel;
    public GameObject ingamePanel;
    public GameObject pausePanel;
    public GameObject gameOverPanel;

    [Header("Fade Effect")]
    [SerializeField] private Image fadeImage; // 에디터에서 페이드용 이미지 드래그 앤 드롭
    private Coroutine fadeCoroutine;

    void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged += UpdateUI;
            UpdateUI(GameManager.Instance.CurrentState);
        }

        // 시작할 때 페이드 인 (화면이 검은색에서 서서히 밝아짐)
        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            FadeIn(5.0f); // 1초 동안 페이드 인
        }
    }

    public void UpdateUI(GameState state)
    {
        if (readyPanel) readyPanel.SetActive(state == GameState.Ready);
        if (ingamePanel) ingamePanel.SetActive(state == GameState.Playing);
        if (pausePanel) pausePanel.SetActive(state == GameState.Pause);
        if (gameOverPanel) gameOverPanel.SetActive(state == GameState.GameOver);
    }

    #region Fade Functions

    // 화면이 서서히 밝아지는 기능 (검은 화면 -> 투명 화면)
    public void FadeIn(float duration, Action onComplete = null)
    {
        if (fadeImage == null) return;
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        
        fadeCoroutine = StartCoroutine(CoFade(1f, 0f, duration, () => {
            fadeImage.raycastTarget = false; // 완료되면 뒤의 UI 클릭 가능하게 함
            onComplete?.Invoke();
        }));
    }

    // 화면이 서서히 어두워지는 기능 (투명 화면 -> 검은 화면)
    public void FadeOut(float duration, Action onComplete = null)
    {
        if (fadeImage == null) return;
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);

        fadeImage.raycastTarget = true; // 페이드 시작하자마자 입력 차단
        fadeCoroutine = StartCoroutine(CoFade(0f, 1f, duration, onComplete));
    }

    // 실제 알파값을 조절하는 코루틴
    private IEnumerator CoFade(float startAlpha, float endAlpha, float duration, Action onComplete)
    {
        fadeImage.gameObject.SetActive(true);
        Color color = fadeImage.color;
        
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime; // 일시정지(Time.timeScale = 0) 상태에서도 작동하도록
            float percent = elapsed / duration;
            
            color.a = Mathf.Lerp(startAlpha, endAlpha, percent);
            fadeImage.color = color;
            
            yield return null;
        }

        color.a = endAlpha;
        fadeImage.color = color;

        // 완전히 투명해졌다면 오브젝트를 꺼서 최적화
        if (endAlpha <= 0f)
        {
            fadeImage.gameObject.SetActive(false);
        }

        onComplete?.Invoke();
    }

    #endregion

    // 버튼 연결용 함수 예시
    public void ClickStartButton() => GameManager.Instance.ChangeState(GameState.Playing);
    public void ClickPauseButton() => GameManager.Instance.ChangeState(GameState.Pause);
    public void ClickResumeButton() => GameManager.Instance.ChangeState(GameState.Playing);
    public void ClickRestartButton() => UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
}
