using UnityEngine;
using System;

public enum GameState { Ready, Playing, Pause, GameOver }

public class GameManager : Singleton<GameManager>
{
    public GameState CurrentState { get; private set; } = GameState.Ready;
    
    public event Action<GameState> OnStateChanged;

    [Header("재화 및 점수")]
    public int score { get; private set; }
    public int bestScore { get; private set; }
    public int money { get; private set; }

    [Header("상점 강화 레벨")]
    public int destructionLevel { get; private set; } = 0;
    public int fortitudeLevel { get; private set; } = 0;   
    public int regenLevel { get; private set; } = 0;       
    public int leapLevel { get; private set; } = 0;        

    public int CostDestruction => 100 + (destructionLevel * 50);
    public int CostFortitude => 200 + (fortitudeLevel * 100);    
    public int CostRegen => 300 + (regenLevel * 150);            
    public int CostLeap => 1000;                                 

    public int maxPlayerHp => 3 + fortitudeLevel;                
    public int playerDamage => 10 + destructionLevel;            
    public bool canDoubleJump => leapLevel > 0;                  
    
    void Start()
    {
        score = 0; 
        
        bestScore = PlayerPrefs.GetInt("BestScore", 0);
        
        money = 5000; 
        
        ChangeState(GameState.Playing);
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

        if (score > bestScore)
        {
            bestScore = score;
            PlayerPrefs.SetInt("BestScore", bestScore);
        }
    }

    public void AddMoney(int amount)
    {
        money += amount;
    }

    public bool BuyDestruction()
    {
        if (money >= CostDestruction)
        {
            money -= CostDestruction;
            destructionLevel++;
            return true;
        }
        return false;
    }

    public bool BuyFortitude()
    {
        if (fortitudeLevel < 5 && money >= CostFortitude)
        {
            money -= CostFortitude;
            fortitudeLevel++;
            return true;
        }
        return false; 
    }

    public bool BuyRegen()
    {
        if (regenLevel < 5 && money >= CostRegen)
        {
            money -= CostRegen;
            regenLevel++;
            return true;
        }
        return false;
    }

    public bool BuyLeap()
    {
        if (leapLevel < 1 && money >= CostLeap)
        {
            money -= CostLeap;
            leapLevel++;
            return true;
        }
        return false;
    }
}