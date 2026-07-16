using UnityEngine;
using System;

public enum GameState { Ready, Playing, Pause, GameOver }

public class GameManager : Singleton<GameManager>
{
    public GameState CurrentState { get; private set; } = GameState.Ready;
    public int playerHp;
    
    // 해커톤 단골 데이터
    public int score { get; private set; }
    public int money { get; private set; }
    
    // 상태 변경 시 UI나 이펙트를 켜기 위한 이벤트
    public event Action<GameState> OnStateChanged;
    
    void Start()
    {
        score = PlayerPrefs.GetInt("BestScore", 0);
        ChangeState(GameState.Ready);
    }

    public void ChangeState(GameState newState)
    {
        CurrentState = newState;
        OnStateChanged?.Invoke(newState);
    }

    public void AddScore(int amount)
    {
        if (CurrentState != GameState.Playing) return;
        score += amount;
    }

}
