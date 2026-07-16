using UnityEngine;
using System;

<<<<<<< HEAD
=======
// 게임의 현재 진행 상태를 정의하는 열거형(Enum)입니다.
<<<<<<< HEAD
>>>>>>> parent of 80bee7b (패턴형 피하기 게임 코어 시스템 구현)
=======
>>>>>>> parent of 80bee7b (패턴형 피하기 게임 코어 시스템 구현)
public enum GameState { Ready, Playing, Pause, GameOver }

public class GameManager : Singleton<GameManager>
{
    public GameState CurrentState { get; private set; } = GameState.Ready;
    
    public event Action<GameState> OnStateChanged;

    [Header("재화 및 점수")]
<<<<<<< HEAD
<<<<<<< HEAD
    public int score { get; private set; }
    public int bestScore { get; private set; }
=======
=======
>>>>>>> parent of 80bee7b (패턴형 피하기 게임 코어 시스템 구현)
    // 현재 플레이 중인 게임의 점수입니다.
    public int score { get; private set; }
    // 역대 최고 점수입니다.
    public int bestScore { get; private set; }
    // 플레이어가 보유한 현재 재화(코인/돈)입니다.
>>>>>>> parent of 80bee7b (패턴형 피하기 게임 코어 시스템 구현)
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
<<<<<<< HEAD
<<<<<<< HEAD
        score = 0; 
        
        bestScore = PlayerPrefs.GetInt("BestScore", 0);
        
=======
=======
>>>>>>> parent of 80bee7b (패턴형 피하기 게임 코어 시스템 구현)
        // 게임 시작 시 현재 점수를 0으로 초기화합니다.
        score = 0; 
        
        // 기기에 저장되어 있는 최고 점수를 불러옵니다. 저장된 값이 없다면 0을 가져옵니다.
        bestScore = PlayerPrefs.GetInt("BestScore", 0);
        
        // 상점 테스트를 위해 임시로 돈을 지급합니다. (실제 게임 출시 전에는 지워야 합니다)
>>>>>>> parent of 80bee7b (패턴형 피하기 게임 코어 시스템 구현)
        money = 5000; 
        
        ChangeState(GameState.Playing);
    }

    public void ChangeState(GameState newState)
    {
        CurrentState = newState;
        
        OnStateChanged?.Invoke(newState);
    }

<<<<<<< HEAD
<<<<<<< HEAD
    public void AddScore(int amount)
    {
        if (CurrentState != GameState.Playing) return;
        
        score += amount;

        if (score > bestScore)
        {
            bestScore = score;
=======
    // 점수를 증가시키는 메서드입니다. 몬스터 처치나 아이템 획득 시 호출합니다.
    public void AddScore(int amount)
    {
        // 게임이 '플레이 중(Playing)' 상태가 아니라면 점수가 오르지 않도록 막아줍니다.
        if (CurrentState != GameState.Playing) return;
        
        // 점수를 추가합니다.
        score += amount;

        // 방금 획득한 점수를 더해 최고 점수를 경신했는지 확인합니다.
        if (score > bestScore)
        {
            bestScore = score;
            // 경신된 최고 점수를 기기에 바로 저장하여 게임을 껐다 켜도 유지되게 합니다.
>>>>>>> parent of 80bee7b (패턴형 피하기 게임 코어 시스템 구현)
=======
    // 점수를 증가시키는 메서드입니다. 몬스터 처치나 아이템 획득 시 호출합니다.
    public void AddScore(int amount)
    {
        // 게임이 '플레이 중(Playing)' 상태가 아니라면 점수가 오르지 않도록 막아줍니다.
        if (CurrentState != GameState.Playing) return;
        
        // 점수를 추가합니다.
        score += amount;

        // 방금 획득한 점수를 더해 최고 점수를 경신했는지 확인합니다.
        if (score > bestScore)
        {
            bestScore = score;
            // 경신된 최고 점수를 기기에 바로 저장하여 게임을 껐다 켜도 유지되게 합니다.
>>>>>>> parent of 80bee7b (패턴형 피하기 게임 코어 시스템 구현)
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