using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    [Header("무적")]
    public float invincibleDuration = 1.5f;
    public float blinkInterval = 0.1f;

    [Header("스프라이트")]
    public SpriteRenderer spriteRenderer;

    public event Action<int, int> OnHeartsChanged;

    public int CurrentHearts { get; private set; }
    public int MaxHearts => GameManager.Instance.maxPlayerHp;

    private bool isInvincible;
    private float regenTimer;
    private GameState lastState;
    private Coroutine blinkCoroutine;

    void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        FillHearts();
    }

    void OnEnable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged += HandleStateChanged;
            lastState = GameManager.Instance.CurrentState;
        }
    }

    void Start()
    {
        // 이 컴포넌트는 씬 로컬이라 씬이 로드될 때마다 새로 생성된다.
        // 씬 진입 타이밍에 상관없이 최신 최대 하트 기준으로 항상 풀피에서 시작한다.
        FillHearts();
    }

    void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged -= HandleStateChanged;
        }
    }

    void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Playing) return;

        int regenLevel = GameManager.Instance.regenLevel;
        if (regenLevel < 1 || CurrentHearts >= MaxHearts) return;

        regenTimer += Time.deltaTime;
        float interval = 100f / regenLevel;
        if (regenTimer >= interval)
        {
            regenTimer = 0f;
            CurrentHearts = Mathf.Min(MaxHearts, CurrentHearts + 1);
            OnHeartsChanged?.Invoke(CurrentHearts, MaxHearts);
        }
    }

    private void HandleStateChanged(GameState state)
    {
        // Pause에서 돌아온 게 아니라 새로 Playing에 진입했을 때만 하트를 풀 충전한다.
        if (state == GameState.Playing && lastState != GameState.Pause)
        {
            FillHearts();
        }
        lastState = state;
    }

    private void FillHearts()
    {
        CurrentHearts = MaxHearts;
        regenTimer = 0f;
        isInvincible = false;
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }
        if (spriteRenderer != null) spriteRenderer.enabled = true;
        OnHeartsChanged?.Invoke(CurrentHearts, MaxHearts);
    }

    public void TakeDamage()
    {
        if (isInvincible || CurrentHearts <= 0) return;
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Playing) return;

        CurrentHearts--;
        OnHeartsChanged?.Invoke(CurrentHearts, MaxHearts);

        if (CurrentHearts <= 0)
        {
            GameManager.Instance.ChangeState(GameState.GameOver);
            
            // [수정] FadeOut이 끝난 뒤(onComplete 콜백) 실행할 코드를 람다식(() => {})으로 전달합니다.
            UIManager.Instance.FadeOut(2f, () => 
            {
                SceneManager.LoadScene("StoreScene");
            });
            
            return;
        }

        if (blinkCoroutine != null) StopCoroutine(blinkCoroutine);
        blinkCoroutine = StartCoroutine(CoInvincible());
    }

    private IEnumerator CoInvincible()
    {
        isInvincible = true;
        float elapsed = 0f;

        while (elapsed < invincibleDuration)
        {
            if (spriteRenderer != null) spriteRenderer.enabled = !spriteRenderer.enabled;
            yield return new WaitForSeconds(blinkInterval);
            elapsed += blinkInterval;
        }

        if (spriteRenderer != null) spriteRenderer.enabled = true;
        isInvincible = false;
        blinkCoroutine = null;
    }
}
