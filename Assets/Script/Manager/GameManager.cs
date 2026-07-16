using UnityEngine;
using System;

// 게임의 현재 진행 상태를 정의하는 열거형(Enum)입니다.
public enum GameState { Ready, Playing, Pause, GameOver }

// 싱글톤 패턴을 사용하여 게임 내에서 단 하나의 인스턴스만 존재하도록 관리하는 매니저 클래스입니다.
public class GameManager : Singleton<GameManager>
{
    // 현재 게임 상태를 나타냅니다. 외부에서는 읽기만 가능하고, 변경은 클래스 내부에서만 가능합니다.
    public GameState CurrentState { get; private set; } = GameState.Ready;
    
    // 게임 상태가 변경될 때마다 호출되는 이벤트입니다. UI 업데이트나 이펙트 실행 등에 쓰입니다.
    public event Action<GameState> OnStateChanged;

    [Header("재화 및 점수")]
    // 현재 플레이 중인 게임의 점수입니다.
    public int score { get; private set; }
    // 역대 최고 점수입니다.
    public int bestScore { get; private set; }
    // 플레이어가 보유한 현재 재화(코인/돈)입니다.
    public int money { get; private set; }

    [Header("상점 강화 레벨")]
    // 각 능력치의 강화 단계를 나타내는 변수들입니다.
    public int destructionLevel { get; private set; } = 0; // 파괴력
    public int fortitudeLevel { get; private set; } = 0;   // 단련 (최대 체력 관련)
    public int regenLevel { get; private set; } = 0;       // 재생 (체력 회복 관련)
    public int leapLevel { get; private set; } = 0;        // 도약 (더블 점프 등 관련)

    // --- 강화 비용 계산 ---
    // 각 강화 레벨에 비례하여 다음 레벨업에 필요한 비용을 계산하여 반환합니다.
    public int CostDestruction => 100 + (destructionLevel * 50); // 기본 100 + (레벨당 50 추가)
    public int CostFortitude => 200 + (fortitudeLevel * 100);    // 기본 200 + (레벨당 100 추가)
    public int CostRegen => 300 + (regenLevel * 150);            // 기본 300 + (레벨당 150 추가)
    public int CostLeap => 1000;                                 // 도약은 한 번만 구매하므로 1000 고정 비용

    // --- 플레이어 능력치 ---
    // 강화 레벨을 기반으로 실제 게임에 적용될 플레이어의 스탯을 계산합니다.
    public int maxPlayerHp => 3 + fortitudeLevel;                // 기본 체력 3 + 단련 레벨
    public int playerDamage => 10 + destructionLevel;            // 기본 데미지 10 + 파괴력 레벨
    public bool canDoubleJump => leapLevel > 0;                  // 도약 레벨이 1 이상이면 더블 점프 가능(true)
    
    void Start()
    {
        // 게임 시작 시 현재 점수를 0으로 초기화합니다.
        score = 0; 
        
        // 기기에 저장되어 있는 최고 점수를 불러옵니다. 저장된 값이 없다면 0을 가져옵니다.
        bestScore = PlayerPrefs.GetInt("BestScore", 0);
        
        // 상점 테스트를 위해 임시로 돈을 지급합니다. (실제 게임 출시 전에는 지워야 합니다)
        money = 5000; 
        
        // 플레이어 이동 테스트를 위해 임시로 Playing 상태로 시작합니다. (테스트 끝나면 Ready로 되돌릴 것)
        ChangeState(GameState.Playing);
    }

    // 게임 상태를 변경하는 메서드입니다.
    public void ChangeState(GameState newState)
    {
        // 새로운 상태로 값을 덮어씌웁니다.
        CurrentState = newState;
        
        // 상태가 변경되었다는 이벤트를 발생시켜, 이를 구독하고 있는 다른 스크립트들에게 알립니다.
        OnStateChanged?.Invoke(newState);
    }

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
            PlayerPrefs.SetInt("BestScore", bestScore);
        }
    }

    // 재화(돈)를 증가시키는 메서드입니다.
    public void AddMoney(int amount)
    {
        money += amount;
    }

    // --- 상점 구매(강화) 로직 ---
    // 구매 성공 여부를 bool 값으로 반환하여, 외부(UI 등)에서 성공 사운드나 에러 메시지를 출력하기 좋게 만듭니다.

    // 파괴력 강화 구매 시도
    public bool BuyDestruction()
    {
        // 현재 가진 돈이 업그레이드 비용보다 많거나 같다면
        if (money >= CostDestruction)
        {
            // 돈을 차감합니다.
            money -= CostDestruction;
            // 파괴력 레벨을 1 올립니다.
            destructionLevel++;
            // 구매에 성공했으므로 true를 반환합니다.
            return true;
        }
        // 돈이 부족하면 false를 반환합니다.
        return false;
    }

    // 단련(체력) 강화 구매 시도
    public bool BuyFortitude()
    {
        // 레벨이 최대치(5) 미만이고, 소지한 돈이 충분할 때만 실행합니다.
        if (fortitudeLevel < 5 && money >= CostFortitude)
        {
            money -= CostFortitude;
            fortitudeLevel++;
            return true;
        }
        // 만렙에 도달했거나 돈이 부족하면 구매 실패(false) 처리합니다.
        return false; 
    }

    // 재생력 강화 구매 시도
    public bool BuyRegen()
    {
        // 레벨이 최대치(5) 미만이고, 소지한 돈이 충분할 때만 실행합니다.
        if (regenLevel < 5 && money >= CostRegen)
        {
            money -= CostRegen;
            regenLevel++;
            return true;
        }
        return false;
    }

    // 도약 능력 구매 시도
    public bool BuyLeap()
    {
        // 한 번만 구매하는 능력이므로 최대 레벨이 1입니다.
        if (leapLevel < 1 && money >= CostLeap)
        {
            money -= CostLeap;
            leapLevel++;
            return true;
        }
        return false;
    }
}
