using UnityEngine;

namespace CardGameTemplate
{
    /// <summary>게임 진행 상태를 나타내는 열거형</summary>
    public enum GameState
    {
        Setup,      // 초기 세팅 단계
        PlayerTurn, // 플레이어 턴 진행 중
        Resolving,  // 카드/효과 처리 중
        GameOver    // 게임 종료
    }

    /// <summary>
    /// 턴 소유자와 게임 상태만 보관하는 최소 골격 매니저. 실제 규칙은 이 위에 구현한다
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        /// <summary>씬 내 유일한 GameManager 인스턴스</summary>
        public static GameManager Instance { get; private set; }

        [Header("상태")]
        [SerializeField] private GameState currentState = GameState.Setup; // 현재 게임 상태
        [SerializeField] private string currentTurnOwner = ""; // 현재 턴을 가진 주체 (플레이어 id 등)

        /// <summary>현재 게임 상태</summary>
        public GameState CurrentState => currentState;

        /// <summary>현재 턴 소유자</summary>
        public string CurrentTurnOwner => currentTurnOwner;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        /// <summary>Setup 상태로 전환한다 (실제 초기화 로직은 여기에 추가)</summary>
        public void EnterSetup()
        {
            currentState = GameState.Setup;
        }

        /// <summary>지정한 소유자의 턴으로 전환한다 (실제 턴 로직은 여기에 추가)</summary>
        public void EnterPlayerTurn(string owner)
        {
            currentTurnOwner = owner;
            currentState = GameState.PlayerTurn;
        }

        /// <summary>Resolving 상태로 전환한다 (카드 효과 처리 로직은 여기에 추가)</summary>
        public void EnterResolving()
        {
            currentState = GameState.Resolving;
        }

        /// <summary>GameOver 상태로 전환한다 (승패 판정 로직은 여기에 추가)</summary>
        public void EnterGameOver()
        {
            currentState = GameState.GameOver;
        }
    }
}
