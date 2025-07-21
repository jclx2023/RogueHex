using HexGame.Gameplay;
using System;
using UnityEngine;

namespace HexGame.Core
{
    /// <summary>
    /// ��Ϸ���Ĺ����� - ������Ϸ���̿��ơ�����ע����¼�Э��
    /// ��������ע��ģʽ�����ָ�ģ�����
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("��������")]
        [SerializeField] private GameConfig gameConfig;

        [Header("������Ϣ")]
        [SerializeField] private bool enableDebugLog = true;

        // �����������
        private GameState gameState;
        private IInputHandler inputHandler;
        private IAIController aiController;
        private IWinConditionChecker winConditionChecker;
        private IBoardRenderer boardRenderer;
        private MoveValidator moveValidator;

        // ��Ϸ�¼�
        public static event Action<PlayerType> OnPlayerChanged;
        public static event Action<GamePhase> OnPhaseChanged;
        public static event Action<Move> OnMoveExecuted;
        public static event Action<PlayerType> OnPlayerWin;
        public static event Action OnGameReset;
        public static event Action OnGameStarted;

        #region Unity��������

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

        #region ��ʼ��

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
            // �Զ������������
            inputHandler = FindObjectOfType<MonoBehaviour>() as IInputHandler;
            aiController = FindObjectOfType<MonoBehaviour>() as IAIController;
            winConditionChecker = FindObjectOfType<MonoBehaviour>() as IWinConditionChecker;
            boardRenderer = FindObjectOfType<MonoBehaviour>() as IBoardRenderer;

            // ����Ҳ������򴴽�Ĭ��ʵ��
            if (winConditionChecker == null)
            {
                var checkerGO = new GameObject("WinConditionChecker");
                checkerGO.transform.SetParent(transform);
                winConditionChecker = checkerGO.AddComponent<WinConditionChecker>();
            }

            // �����ƶ���֤��
            moveValidator = new MoveValidator(gameConfig);

            LogDebug("Dependencies initialized successfully");
        }

        #endregion

        #region ��Ϸ���̿���

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

        #region �ƶ�����

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
            // ������Ϸ״̬
            gameState.ExecuteMove(move);

            // �����Ӿ�����
            boardRenderer?.UpdateCellVisual(move.Position, move.Player);
            boardRenderer?.PlayMoveAnimation(move.Position);

            // �����ƶ��¼�
            OnMoveExecuted?.Invoke(move);

            LogDebug($"Move executed: {move.Player} at ({move.Position.x}, {move.Position.y})");

            // �����Ϸ�Ƿ����
            CheckGameEnd();

            // �����Ϸδ�������л����
            if (gameState.CurrentPhase == GamePhase.Playing)
            {
                SwitchToNextPlayer();
            }
        }

        #endregion

        #region ��Ϸ�������

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

                // ������ʤ·��
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

        #region ����л�

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

            // ����ֵ�AI���ӳ�ִ��AI�ƶ�
            if (newPlayer == PlayerType.AI && gameState.CurrentPhase == GamePhase.Playing)
            {
                StartAIMove();
            }
        }

        #endregion

        #region AI����

        private void StartAIMove()
        {
            if (aiController == null)
            {
                LogError("AIController is not available!");
                return;
            }

            LogDebug("AI is thinking...");

            // ʹ��Э�̻����̳߳��������������߳�
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

        #region ״̬�������

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

        #region �¼�����

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
            // ֻ�е�ǰ��������һغ�ʱ�Ŵ�����
            if (gameState.CurrentPlayer == PlayerType.Human)
            {
                TryExecuteMove(position);
            }
        }

        #endregion

        #region �����ӿ�

        public GameState GetGameState() => gameState;
        public GameConfig GetGameConfig() => gameConfig;
        public bool IsGameActive() => gameState.CurrentPhase == GamePhase.Playing;
        public PlayerType GetCurrentPlayer() => gameState.CurrentPlayer;

        #endregion

        #region ���Ժ���־

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

    #region ��ؽӿڶ���

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