using UnityEngine;
using UnityEngine.UI;

public class ShopUIManager : MonoBehaviour
{
    [Header("UI Texts")]
    public Text moneyText;
    
    [Header("파괴력 업그레이드 UI")]
    public Text destructionLevelText;
    public Text destructionCostText;
    public Button destructionBuyButton;

    [Header("단련 업그레이드 UI (최대 5)")]
    public Text fortitudeLevelText;
    public Text fortitudeCostText;
    public Button fortitudeBuyButton;

    [Header("재생 업그레이드 UI (최대 5)")]
    public Text regenLevelText;
    public Text regenCostText;
    public Button regenBuyButton;

    [Header("도약 업그레이드 UI (최대 1)")]
    public Text leapLevelText;
    public Text leapCostText;
    public Button leapBuyButton;

    void Start()
    {
        // 모든 버튼 클릭 이벤트 연결
        destructionBuyButton.onClick.AddListener(OnBuyDestructionClicked);
        fortitudeBuyButton.onClick.AddListener(OnBuyFortitudeClicked);
        regenBuyButton.onClick.AddListener(OnBuyRegenClicked);
        leapBuyButton.onClick.AddListener(OnBuyLeapClicked);

        // 초기 UI 업데이트
        UpdateShopUI();
    }

    // --- [버튼 클릭 이벤트 핸들러] ---

    public void OnBuyDestructionClicked()
    {
        bool success = GameManager.Instance.BuyDestruction();
        if (success)
        {
            Debug.Log("파괴력 업그레이드 성공!");
            UpdateShopUI(); 
        }
        else
        {
            Debug.Log("돈이 부족합니다!");
        }
    }

    public void OnBuyFortitudeClicked()
    {
        bool success = GameManager.Instance.BuyFortitude();
        if (success)
        {
            Debug.Log("단련 업그레이드 성공!");
            UpdateShopUI();
        }
        else
        {
            if (GameManager.Instance.fortitudeLevel >= 5)
                Debug.Log("이미 최대 레벨입니다!");
            else
                Debug.Log("돈이 부족합니다!");
        }
    }

    public void OnBuyRegenClicked()
    {
        bool success = GameManager.Instance.BuyRegen();
        if (success)
        {
            Debug.Log("재생 업그레이드 성공!");
            UpdateShopUI();
        }
        else
        {
            if (GameManager.Instance.regenLevel >= 5)
                Debug.Log("이미 최대 레벨입니다!");
            else
                Debug.Log("돈이 부족합니다!");
        }
    }

    public void OnBuyLeapClicked()
    {
        bool success = GameManager.Instance.BuyLeap();
        if (success)
        {
            Debug.Log("도약 업그레이드 성공!");
            UpdateShopUI();
        }
        else
        {
            if (GameManager.Instance.leapLevel >= 1)
                Debug.Log("이미 최대 레벨입니다!");
            else
                Debug.Log("돈이 부족합니다!");
        }
    }

    // --- [UI 갱신 로직] ---

    // UI에 현재 텍스트들을 갱신하는 함수
    public void UpdateShopUI()
    {
        // 현재 소지 금액 표시
        moneyText.text = $"보유 코인: {GameManager.Instance.money}";

        // 1. 파괴력 정보 갱신 (만렙 없음)
        if (GameManager.Instance.destructionLevel >= 5)
        {
            destructionLevelText.text = "몽유 조작(MAX)";
            destructionCostText.text = "-";
            destructionBuyButton.interactable = false; // 만렙이면 버튼 비활성화
        }
        else
        {
            destructionLevelText.text = $"몽유 조작\n(Lv.{GameManager.Instance.destructionLevel}/5)";
            destructionCostText.text = $"{GameManager.Instance.CostDestruction} 코인";
            destructionBuyButton.interactable = true;
        }
        if(GameManager.Instance.destructionLevel == 0)
        {
            destructionLevelText.text = $"몽유 조작\n(Lv.{GameManager.Instance.destructionLevel}/5)\n구매시 공격가능";
            destructionCostText.text = $"{GameManager.Instance.CostDestruction} 코인";
            destructionBuyButton.interactable = true;
        }
        // 3. 단련 정보 갱신 (만렙 5)
        if (GameManager.Instance.fortitudeLevel >= 5)
        {
            fortitudeLevelText.text = "수면제(MAX)";
            fortitudeCostText.text = "-";
            fortitudeBuyButton.interactable = false; // 만렙이면 버튼 비활성화
        }
        else
        {
            fortitudeLevelText.text = $"수면제\n(Lv.{GameManager.Instance.fortitudeLevel}/5)";
            fortitudeCostText.text = $"{GameManager.Instance.CostFortitude} 코인";
            fortitudeBuyButton.interactable = true;
        }

        // 4. 재생 정보 갱신 (만렙 5)
        if (GameManager.Instance.regenLevel >= 5)
        {
            regenLevelText.text = "수면질 향상(MAX)";
            regenCostText.text = "-";
            regenBuyButton.interactable = false; // 만렙이면 버튼 비활성화
        }
        else
        {
            regenLevelText.text = $"수면질 향상\n(Lv.{GameManager.Instance.regenLevel}/5)";
            regenCostText.text = $"{GameManager.Instance.CostRegen} 코인";
            regenBuyButton.interactable = true;
        }

        // 5. 도약 정보 갱신 (만렙 1)
        if (GameManager.Instance.leapLevel >= 1)
        {
            leapLevelText.text = "가위 눌림 해제됨";
            leapCostText.text = "-";
            leapBuyButton.interactable = false; // 만렙이면 버튼 비활성화
        }
        else
        {
            leapLevelText.text = $"가위 눌림 해제"; // 최대 1레벨이므로 0/1로 표기
            leapCostText.text = $"{GameManager.Instance.CostLeap} 코인";
            leapBuyButton.interactable = true;
        }
    }
}
