using HexGame.Gameplay;
using System;
using UnityEngine;

namespace HexGame.Core
{
    /// <summary>
    /// 游戏核心管理器 - 负责游戏流程控制、依赖注入和事件协调
    /// 采用依赖注入模式，保持各模块解耦
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("依赖配置")]
        [SerializeField] private GameConfig gameConfig;

        [Header("调试信息")]
        [SerializeField] private bool enableDebugLog = true;

        // 核心依赖组件
        private GameState gameState;
        private IInputHandler inputHandler;
        private IAIController aiController;
        private IWinConditionChecker winConditionChecker;
        private IBoardRenderer boardRenderer;
        private MoveValidator moveValidator;

        // 游戏事件
        public static event Action<PlayerType> OnPlayerChanged;
        public static event Action<GamePhase> OnPhaseChanged;
        public static event Action<Move> OnMoveExecuted;
        public static event Action<PlayerType> OnPlayerWin;
        public static event Action OnGameReset;
        public static event Action OnGameStarted;

        #region Unity生命周期

        private void Awake()
        {
            InitializeGameConfig();
            InitializeGameState();
            InitializeDependencies();
        }

        private void Start()
        {
            SubscribeToEvents();
            StartNewGame();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        #endregion

        #region 初始化

        private void InitializeGameConfig()
        {
            if (gameConfig == null)
            {
                LogError("GameConfig is not assigned! Creating default config...");
                gameConfig = ScriptableObject.CreateInstance<GameConfig>();
            }
        }

        private void InitializeGameState()
        {
            gameState = new GameState(gameConfig);
            gameState.OnPlayerChanged += HandlePlayerChanged;
            gameState.OnPhaseChanged += HandlePhaseChanged;
        }

        private void InitializeDependencies()
        {
            // 自动查找依赖组件
            inputHandler = FindObjectOfType<MonoBehaviour>() as IInputHandler;
            aiController = FindObjectOfType<MonoBehaviour>() as IAIController;
            winConditionChecker = FindObjectOfType<MonoBehaviour>() as IWinConditionChecker;
            boardRenderer = FindObjectOfType<MonoBehaviour>() as IBoardRenderer;

            // 如果找不到，则创建默认实现
            if (winConditionChecker == null)
            {
                var checkerGO = new GameObject("WinConditionChecker");
                checkerGO.transform.SetParent(transform);
                winConditionChecker = checkerGO.AddComponent<WinConditionChecker>();
            }

            // 创建移动验证器
            moveValidator = new MoveValidator(gameConfig);

            LogDebug("Dependencies initialized successfully");
        }

        #endregion

        #region 游戏流程控制

        public void StartNewGame()
        {
            LogDebug("Starting new game...");

            gameState.ResetGame();
            boardRenderer?.ClearBoard();

            ChangePhase(GamePhase.Playing);
            ChangeCurrentPlayer(PlayerType.Human);

            OnGameStarted?.Invoke();
            OnGameReset?.Invoke();

            LogDebug($"New game started. Board size: {gameConfig.boardRows}x{gameConfig.boardCols}");
        }

        public void ResetGame()
        {
            LogDebug("Resetting game...");
            StartNewGame();
        }

        public void PauseGame()
        {
            if (gameState.CurrentPhase == GamePhase.Playing)
            {
                ChangePhase(GamePhase.Paused);
                LogDebug("Game paused");
            }
        }

        public void ResumeGame()
        {
            if (gameState.CurrentPhase == GamePhase.Paused)
            {
                ChangePhase(GamePhase.Playing);
                LogDebug("Game resumed");
            }
        }

        #endregion

        #region 移动处理

        public bool TryExecuteMove(Vector2Int position)
        {
            if (gameState.CurrentPhase != GamePhase.Playing)
            {
                LogDebug("Cannot execute move: Game is not in playing state");
                return false;
            }

            var move = new Move(position, gameState.CurrentPlayer);
            var result = moveValidator.ValidateAndApplyMove(gameState, move);

            if (result.IsValid)
            {
                ExecuteValidatedMove(move);
                return true;
            }
            else
            {
                LogDebug($"Invalid move at ({position.x}, {position.y}): {result.ErrorMessage}");
                return false;
            }
        }

        private void ExecuteValidatedMove(Move move)
        {
            // 更新游戏状态
            gameState.ExecuteMove(move);

            // 更新视觉表现
            boardRenderer?.UpdateCellVisual(move.Position, move.Player);
            boardRenderer?.PlayMoveAnimation(move.Position);

            // 触发移动事件
            OnMoveExecuted?.Invoke(move);

            LogDebug($"Move executed: {move.Player} at ({move.Position.x}, {move.Position.y})");

            // 检查游戏是否结束
            CheckGameEnd();

            // 如果游戏未结束，切换玩家
            if (gameState.CurrentPhase == GamePhase.Playing)
            {
                SwitchToNextPlayer();
            }
        }

        #endregion

        #region 游戏结束检测

        private void CheckGameEnd()
        {
            var result = winConditionChecker.CheckGameResult(gameState);

            if (result.IsGameOver)
            {
                HandleGameEnd(result);
            }
        }

        private void HandleGameEnd(GameResult result)
        {
            ChangePhase(GamePhase.GameOver);

            if (result.Winner.HasValue)
            {
                LogDebug($"Game Over! Winner: {result.Winner.Value}");
                OnPlayerWin?.Invoke(result.Winner.Value);

                // 高亮获胜路径
                if (result.WinningPath != null && result.WinningPath.Count > 0)
                {
                    boardRenderer?.HighlightWinningPath(result.WinningPath);
                }
            }
            else
            {
                LogDebug("Game Over! Draw");
            }
        }

        #endregion

        #region 玩家切换

        private void SwitchToNextPlayer()
        {
            var nextPlayer = gameState.CurrentPlayer == PlayerType.Human ? PlayerType.AI : PlayerType.Human;
            ChangeCurrentPlayer(nextPlayer);
        }

        private void ChangeCurrentPlayer(PlayerType newPlayer)
        {
            if (gameState.CurrentPlayer != newPlayer)
            {
                gameState.SetCurrentPlayer(newPlayer);
                LogDebug($"Current player changed to: {newPlayer}");
            }
        }

        private void HandlePlayerChanged(PlayerType newPlayer)
        {
            OnPlayerChanged?.Invoke(newPlayer);

            // 如果轮到AI，延迟执行AI移动
            if (newPlayer == PlayerType.AI && gameState.CurrentPhase == GamePhase.Playing)
            {
                StartAIMove();
            }
        }

        #endregion

        #region AI处理

        private void StartAIMove()
        {
            if (aiController == null)
            {
                LogError("AIController is not available!");
                return;
            }

            LogDebug("AI is thinking...");

            // 使用协程或者线程池来避免阻塞主线程
            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                var aiMove = aiController.GetBestMove(gameState);
                if (aiMove != null)
                {
                    ExecuteValidatedMove(aiMove);
                }
                else
                {
                    LogError("AI failed to generate a move!");
                }
            });
        }

        #endregion

        #region 状态变更处理

        private void ChangePhase(GamePhase newPhase)
        {
            if (gameState.CurrentPhase != newPhase)
            {
                gameState.SetPhase(newPhase);
                LogDebug($"Game phase changed to: {newPhase}");
            }
        }

        private void HandlePhaseChanged(GamePhase newPhase)
        {
            OnPhaseChanged?.Invoke(newPhase);
        }

        #endregion

        #region 事件订阅

        private void SubscribeToEvents()
        {
            if (inputHandler != null)
            {
                inputHandler.OnCellClicked += HandleCellClicked;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (inputHandler != null)
            {
                inputHandler.OnCellClicked -= HandleCellClicked;
            }

            if (gameState != null)
            {
                gameState.OnPlayerChanged -= HandlePlayerChanged;
                gameState.OnPhaseChanged -= HandlePhaseChanged;
            }
        }

        private void HandleCellClicked(Vector2Int position)
        {
            // 只有当前是人类玩家回合时才处理点击
            if (gameState.CurrentPlayer == PlayerType.Human)
            {
                TryExecuteMove(position);
            }
        }

        #endregion

        #region 公共接口

        public GameState GetGameState() => gameState;
        public GameConfig GetGameConfig() => gameConfig;
        public bool IsGameActive() => gameState.CurrentPhase == GamePhase.Playing;
        public PlayerType GetCurrentPlayer() => gameState.CurrentPlayer;

        #endregion

        #region 调试和日志

        private void LogDebug(string message)
        {
            if (enableDebugLog)
            {
                Debug.Log($"[GameManager] {message}");
            }
        }

        private void LogError(string message)
        {
            Debug.LogError($"[GameManager] {message}");
        }

        #endregion
    }

    #region 相关接口定义

    public interface IInputHandler
    {
        event Action<Vector2Int> OnCellClicked;
    }

    public interface IAIController
    {
        Move GetBestMove(GameState gameState);
    }

    public interface IWinConditionChecker
    {
        GameResult CheckGameResult(GameState gameState);
        bool HasPlayerWon(HexCellState[,] board, PlayerType player);
        bool HasPlayerWon(HexCellState[,] board, int player);
    }

    public interface IBoardRenderer
    {
        void UpdateCellVisual(Vector2Int position, PlayerType player);
        void PlayMoveAnimation(Vector2Int position);
        void HighlightWinningPath(System.Collections.Generic.List<Vector2Int> path);
        void ClearBoard();
    }

    #endregion
}