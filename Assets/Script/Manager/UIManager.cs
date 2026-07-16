using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System;

public class UIManager : Singleton<UIManager>
{
    [Header("UI Panels")]
    public GameObject readyPanel;
    public GameObject ingamePanel;
    public GameObject pausePanel;
    public GameObject gameOverPanel;
    public GameObject clearPanel;

    [Header("Fade Effect")]
    [SerializeField] private Image fadeImage; 
    [SerializeField] private float defaultFadeInDuration = 1.5f; // 씬 시작 시 기본 페이드인 시간
    private Coroutine fadeCoroutine;

    [Header("클리어 -> 페이드아웃 -> 씬 전환")]
    public float clearHoldDuration = 1.5f; 
    public float clearFadeDuration = 1f;
    public string clearNextSceneName = "PuriTestScene"; 

    // 씬이 재로드될 때 파괴된 옛 오브젝트를 계속 가리키지 않도록, 최초 연결 시점의 이름을 캐싱해둔다.
    private string readyPanelName, ingamePanelName, pausePanelName, gameOverPanelName, clearPanelName, fadeImageName;

    void Start()
    {
        CacheOriginalNames();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged += UpdateUI;
            UpdateUI(GameManager.Instance.CurrentState);
        }
    }

    private void CacheOriginalNames()
    {
        if (readyPanel != null) readyPanelName = readyPanel.name;
        if (ingamePanel != null) ingamePanelName = ingamePanel.name;
        if (pausePanel != null) pausePanelName = pausePanel.name;
        if (gameOverPanel != null) gameOverPanelName = gameOverPanel.name;
        if (clearPanel != null) clearPanelName = clearPanel.name;
        if (fadeImage != null) fadeImageName = fadeImage.name;
    }

    private void RewireIfDestroyed(ref GameObject field, string cachedName)
    {
        if (field != null || string.IsNullOrEmpty(cachedName)) return;
        field = GameObject.Find(cachedName);
    }

    // --- 씬 로드 이벤트 구독 및 해제 ---
    private void OnEnable()
    {
        // 유니티의 씬 로드 이벤트에 OnSceneLoaded 메서드를 등록합니다.
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        // 오브젝트가 파괴되거나 꺼질 때 이벤트를 해제하여 메모리 누수를 방지합니다.
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // --- 새로운 씬이 로드될 때마다 실행되는 함수 ---
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 이 매니저는 DontDestroyOnLoad라 씬을 나갔다 돌아와도 그대로 살아있지만, 패널 참조들은
        // 파괴된 옛 씬의 오브젝트를 계속 가리키게 된다. 캐싱해둔 이름으로 새 씬에서 다시 찾아 재연결한다.
        RewireIfDestroyed(ref readyPanel, readyPanelName);
        RewireIfDestroyed(ref ingamePanel, ingamePanelName);
        RewireIfDestroyed(ref pausePanel, pausePanelName);
        RewireIfDestroyed(ref gameOverPanel, gameOverPanelName);
        RewireIfDestroyed(ref clearPanel, clearPanelName);

        if (fadeImage == null && !string.IsNullOrEmpty(fadeImageName))
        {
            GameObject found = GameObject.Find(fadeImageName);
            if (found != null) fadeImage = found.GetComponent<Image>();
        }

        if (fadeImage != null)
        {
            // 1. 새 씬이 켜질 때 화면이 잠깐이라도 보이는 것을 막기 위해, 즉시 완전한 불투명(Alpha = 1) 상태로 만듭니다.
            Color color = fadeImage.color;
            color.a = 1f;
            fadeImage.color = color;
            fadeImage.gameObject.SetActive(true);
            fadeImage.raycastTarget = true;

            // 2. 부드럽게 밝아지는 페이드 인을 실행합니다.
            FadeIn(defaultFadeInDuration);
        }

        // 재연결된 패널들이 현재 게임 상태와 맞는 것만 켜져 있도록 다시 맞춘다.
        if (GameManager.Instance != null) UpdateUI(GameManager.Instance.CurrentState);
    }

    public void UpdateUI(GameState state)
    {
        if (readyPanel) readyPanel.SetActive(state == GameState.Ready);
        if (ingamePanel) ingamePanel.SetActive(state == GameState.Playing);
        if (pausePanel) pausePanel.SetActive(state == GameState.Pause);
        if (gameOverPanel) gameOverPanel.SetActive(state == GameState.GameOver);
        if (clearPanel) clearPanel.SetActive(state == GameState.Clear);

        if (state == GameState.Clear)
        {
            StartCoroutine(CoClearThenLoadScene());
        }
    }

    private IEnumerator CoClearThenLoadScene()
    {
        yield return new WaitForSecondsRealtime(clearHoldDuration); 
        FadeOut(clearFadeDuration, () => SceneManager.LoadScene(clearNextSceneName));
    }

    #region Fade Functions

    public void FadeIn(float duration, Action onComplete = null)
    {
        if (fadeImage == null) return;
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        
        fadeCoroutine = StartCoroutine(CoFade(1f, 0f, duration, () => {
            fadeImage.raycastTarget = false; 
            onComplete?.Invoke();
        }));
    }

    public void FadeOut(float duration, Action onComplete = null)
    {
        if (fadeImage == null) return;
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);

        fadeImage.raycastTarget = true; 
        fadeCoroutine = StartCoroutine(CoFade(0f, 1f, duration, onComplete));
    }

    private IEnumerator CoFade(float startAlpha, float endAlpha, float duration, Action onComplete)
    {
        fadeImage.gameObject.SetActive(true);
        Color color = fadeImage.color;
        
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime; 
            float percent = elapsed / duration;
            
            color.a = Mathf.Lerp(startAlpha, endAlpha, percent);
            fadeImage.color = color;
            
            yield return null;
        }

        color.a = endAlpha;
        fadeImage.color = color;

        if (endAlpha <= 0f)
        {
            fadeImage.gameObject.SetActive(false);
        }

        onComplete?.Invoke();
    }

    #endregion

    public void ClickStartButton() => GameManager.Instance.ChangeState(GameState.Playing);
    public void ClickPauseButton() => GameManager.Instance.ChangeState(GameState.Pause);
    public void ClickResumeButton() => GameManager.Instance.ChangeState(GameState.Playing);
    public void ClickRestartButton() => UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
}
