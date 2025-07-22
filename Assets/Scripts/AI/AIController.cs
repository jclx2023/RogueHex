using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using HexGame.Core;
using HexGame.Gameplay;

namespace HexGame.AI
{
    /// <summary>
    /// AI������ - ͳһ��AI������ڣ����ò������ģʽ
    /// ����Э����ͬAI���ԣ���������ѡ����Ѿ��߷���
    /// </summary>
    public class AIController : MonoBehaviour, IAIController
    {
        [Header("AI��������")]
        [SerializeField] private AIMode currentAIMode = AIMode.Hybrid;
        [SerializeField] private bool enableStrategyCache = true;
        [SerializeField] private bool enableAsyncProcessing = true;

        [Header("��������")]
        [SerializeField] private bool enableDebugLog = true;
        [SerializeField] private bool showThinkingProcess = false;
        [SerializeField] private bool logDecisionDetails = false;

        // �������
        private GameConfig config;
        private IWinConditionChecker winConditionChecker;

        // AI�����б�
        private List<IAIStrategy> strategies;
        private Dictionary<System.Type, IAIStrategy> strategyCache;

        // ���߻���
        private Dictionary<string, Move> moveCache;
        private const int MAX_CACHE_SIZE = 1000;

        // ����ͳ��
        private System.Diagnostics.Stopwatch decisionTimer;
        private float totalThinkingTime = 0f;
        private int totalDecisions = 0;

        #region Unity��������

        private void Awake()
        {
            InitializeDependencies();
            InitializeStrategies();
            InitializeCache();
            decisionTimer = new System.Diagnostics.Stopwatch();
        }

        private void Start()
        {
            LogDebug($"AIController initialized with mode: {currentAIMode}");
        }

        #endregion

        #region ��ʼ��

        private void InitializeDependencies()
        {
            // ��ȡGameManager������
            var gameManager = FindObjectOfType<GameManager>();
            if (gameManager != null)
            {
                config = gameManager.GetGameConfig();
            }

            // ��ȡʤ�������
            winConditionChecker = FindObjectOfType<WinConditionChecker>();

            if (config == null)
            {
                LogError("GameConfig not found! Using default settings.");
                config = ScriptableObject.CreateInstance<GameConfig>();
            }
        }

        private void InitializeStrategies()
        {
            strategies = new List<IAIStrategy>();
            strategyCache = new Dictionary<System.Type, IAIStrategy>();

            // ������в������
            var threatStrategy = new ThreatDetectionStrategy(config, winConditionChecker);
            RegisterStrategy(threatStrategy);

            // �������ؿ��޲���
            var mctsStrategy = new MonteCarloStrategy(config, winConditionChecker);
            RegisterStrategy(mctsStrategy);

            // ����λ����������
            var positionalStrategy = new PositionalEvaluationStrategy(config, winConditionChecker);
            RegisterStrategy(positionalStrategy);

            LogDebug($"Initialized {strategies.Count} AI strategies");
        }

        private void InitializeCache()
        {
            if (enableStrategyCache)
            {
                moveCache = new Dictionary<string, Move>();
            }
        }

        private void RegisterStrategy(IAIStrategy strategy)
        {
            if (strategy != null)
            {
                strategies.Add(strategy);
                strategyCache[strategy.GetType()] = strategy;
            }
        }

        #endregion

        #region IAIController�ӿ�ʵ��

        /// <summary>
        /// ��ȡAI������ƶ�
        /// </summary>
        public Move GetBestMove(GameState gameState)
        {
            if (gameState == null)
            {
                LogError("GameState is null");
                return null;
            }

            decisionTimer.Restart();

            try
            {
                // ��黺��
                Move cachedMove = GetCachedMove(gameState);
                if (cachedMove != null)
                {
                    LogDebug($"Using cached move: {cachedMove}");
                    return cachedMove;
                }

                // ��ȡ���п����ƶ�
                var availableMoves = GetAvailableMoves(gameState);
                if (availableMoves.Count == 0)
                {
                    LogError("No available moves!");
                    return null;
                }

                // ����AIģʽѡ����߲���
                Move bestMove = SelectBestMoveByMode(gameState, availableMoves);

                // ������߽��
                CacheMove(gameState, bestMove);

                // ��¼ͳ����Ϣ
                RecordDecisionStats();

                LogDebug($"AI selected move: {bestMove} (thinking time: {decisionTimer.ElapsedMilliseconds}ms)");

                return bestMove;
            }
            catch (System.Exception ex)
            {
                LogError($"Error in AI decision making: {ex.Message}");
                return GetFallbackMove(gameState);
            }
            finally
            {
                decisionTimer.Stop();
            }
        }

        #endregion

        #region ���߲���ѡ��

        private Move SelectBestMoveByMode(GameState gameState, List<Vector2Int> availableMoves)
        {
            switch (currentAIMode)
            {
                case AIMode.ThreatFocused:
                    return GetThreatFocusedMove(gameState, availableMoves);

                case AIMode.MCTSFocused:
                    return GetMCTSFocusedMove(gameState, availableMoves);

                case AIMode.PositionalOnly:
                    return GetPositionalMove(gameState, availableMoves);

                case AIMode.Hybrid:
                default:
                    return GetHybridMove(gameState, availableMoves);
            }
        }

        /// <summary>
        /// ��в����ģʽ - ���ȴ����������
        /// </summary>
        private Move GetThreatFocusedMove(GameState gameState, List<Vector2Int> availableMoves)
        {
            var threatStrategy = GetStrategy<ThreatDetectionStrategy>();
            if (threatStrategy != null)
            {
                // ���һ����ʤ
                var winningMove = threatStrategy.GetWinningMove(gameState, PlayerType.AI);
                if (winningMove != null)
                {
                    LogDebug("Found winning move!");
                    return winningMove;
                }

                // ���һ���ط�
                var blockingMove = threatStrategy.GetBlockingMove(gameState, PlayerType.Human);
                if (blockingMove != null)
                {
                    LogDebug("Found blocking move!");
                    return blockingMove;
                }
            }

            // ���˵�λ������
            return GetPositionalMove(gameState, availableMoves);
        }

        /// <summary>
        /// MCTS����ģʽ - ʹ�����ؿ���ģ��
        /// </summary>
        private Move GetMCTSFocusedMove(GameState gameState, List<Vector2Int> availableMoves)
        {
            var mctsStrategy = GetStrategy<MonteCarloStrategy>();
            if (mctsStrategy != null)
            {
                return mctsStrategy.GetBestMove(gameState, availableMoves);
            }

            return GetPositionalMove(gameState, availableMoves);
        }

        /// <summary>
        /// λ������ģʽ - ��������ʽ����
        /// </summary>
        private Move GetPositionalMove(GameState gameState, List<Vector2Int> availableMoves)
        {
            var positionalStrategy = GetStrategy<PositionalEvaluationStrategy>();
            if (positionalStrategy != null)
            {
                return positionalStrategy.GetBestMove(gameState, availableMoves);
            }

            // ���Ļ��ˣ����ѡ��
            var randomPos = availableMoves[Random.Range(0, availableMoves.Count)];
            return new Move(randomPos, PlayerType.AI);
        }

        /// <summary>
        /// ���ģʽ - �ۺ�ʹ�ö��ֲ���
        /// </summary>
        private Move GetHybridMove(GameState gameState, List<Vector2Int> availableMoves)
        {
            if (showThinkingProcess)
            {
                LogDebug("=== AI˼������ ===");
            }

            // 1. ���ȼ����в��������ȼ���
            var threatStrategy = GetStrategy<ThreatDetectionStrategy>();
            if (threatStrategy != null)
            {
                var winningMove = threatStrategy.GetWinningMove(gameState, PlayerType.AI);
                if (winningMove != null)
                {
                    LogDebug("���ȼ�1���ҵ���ʤ�֣�");
                    return winningMove;
                }

                var blockingMove = threatStrategy.GetBlockingMove(gameState, PlayerType.Human);
                if (blockingMove != null)
                {
                    LogDebug("���ȼ�2���ҵ��ط��֣�");
                    return blockingMove;
                }
            }

            // 2. ������ѡ�ƶ�
            var candidateEvaluations = new List<(Vector2Int position, float score, string reason)>();

            // ʹ��λ����������
            var positionalStrategy = GetStrategy<PositionalEvaluationStrategy>();
            if (positionalStrategy != null)
            {
                foreach (var pos in availableMoves)
                {
                    float score = positionalStrategy.EvaluatePosition(gameState, pos);
                    candidateEvaluations.Add((pos, score, "λ������"));
                }
            }

            // 3. ���ڸ߷ֺ�ѡ��ʹ��MCTS������֤
            var topCandidates = candidateEvaluations
                .OrderByDescending(x => x.score)
                .Take(Mathf.Min(5, candidateEvaluations.Count))
                .ToList();

            var mctsStrategy = GetStrategy<MonteCarloStrategy>();
            if (mctsStrategy != null && topCandidates.Count > 1)
            {
                var mctsResults = new List<(Vector2Int position, float winRate, string reason)>();

                foreach (var candidate in topCandidates)
                {
                    float winRate = mctsStrategy.EvaluatePosition(gameState, candidate.position);
                    mctsResults.Add((candidate.position, winRate, $"MCTSʤ��:{winRate:P1}"));

                    if (showThinkingProcess)
                    {
                        LogDebug($"��ѡλ�� ({candidate.position.x},{candidate.position.y}): " +
                                $"λ�÷�{candidate.score:F1}, ʤ��{winRate:P1}");
                    }
                }

                // ѡ��ʤ����ߵ��ƶ�
                var bestMCTS = mctsResults.OrderByDescending(x => x.winRate).First();

                if (showThinkingProcess)
                {
                    LogDebug($"����ѡ��: ({bestMCTS.position.x},{bestMCTS.position.y}) - {bestMCTS.reason}");
                }

                return new Move(bestMCTS.position, PlayerType.AI);
            }

            // 4. ���˵���߷ֵ�λ������
            if (candidateEvaluations.Count > 0)
            {
                var bestCandidate = candidateEvaluations.OrderByDescending(x => x.score).First();

                if (showThinkingProcess)
                {
                    LogDebug($"ʹ��λ��������߷�: ({bestCandidate.position.x},{bestCandidate.position.y}) " +
                            $"����:{bestCandidate.score:F1}");
                }

                return new Move(bestCandidate.position, PlayerType.AI);
            }

            // 5. ���Ļ���
            return GetFallbackMove(gameState);
        }

        #endregion

        #region ��������

        private List<Vector2Int> GetAvailableMoves(GameState gameState)
        {
            return gameState.GetEmptyPositions();
        }

        private T GetStrategy<T>() where T : class, IAIStrategy
        {
            if (strategyCache.TryGetValue(typeof(T), out IAIStrategy strategy))
            {
                return strategy as T;
            }
            return null;
        }

        private Move GetFallbackMove(GameState gameState)
        {
            var availableMoves = GetAvailableMoves(gameState);
            if (availableMoves.Count > 0)
            {
                // ѡ�����ĸ�����λ��
                var center = new Vector2(config.boardRows / 2f, config.boardCols / 2f);
                var centerMove = availableMoves
                    .OrderBy(pos => Vector2.Distance(pos, center))
                    .First();

                LogDebug($"ʹ�û��˲��ԣ�ѡ�����ĸ���λ��: ({centerMove.x}, {centerMove.y})");
                return new Move(centerMove, PlayerType.AI);
            }

            LogError("No fallback move available!");
            return null;
        }

        #endregion

        #region �������

        private string GetBoardStateHash(GameState gameState)
        {
            if (!enableStrategyCache) return null;

            var hash = new System.Text.StringBuilder();
            var board = gameState.Board;

            for (int i = 0; i < config.boardRows; i++)
            {
                for (int j = 0; j < config.boardCols; j++)
                {
                    var cell = board[i, j];
                    if (cell.isOccupied)
                    {
                        hash.Append($"{i},{j}:{cell.occupiedBy};");
                    }
                }
            }

            return hash.ToString();
        }

        private Move GetCachedMove(GameState gameState)
        {
            if (!enableStrategyCache || moveCache == null) return null;

            string stateHash = GetBoardStateHash(gameState);
            if (stateHash != null && moveCache.TryGetValue(stateHash, out Move cachedMove))
            {
                return cachedMove;
            }

            return null;
        }

        private void CacheMove(GameState gameState, Move move)
        {
            if (!enableStrategyCache || moveCache == null || move == null) return;

            string stateHash = GetBoardStateHash(gameState);
            if (stateHash != null)
            {
                // ���ƻ����С
                if (moveCache.Count >= MAX_CACHE_SIZE)
                {
                    var keysToRemove = moveCache.Keys.Take(moveCache.Count / 2).ToList();
                    foreach (var key in keysToRemove)
                    {
                        moveCache.Remove(key);
                    }
                }

                moveCache[stateHash] = move;
            }
        }

        public void ClearCache()
        {
            moveCache?.Clear();
            LogDebug("AI���߻��������");
        }

        #endregion

        #region ͳ�ƺ͵���

        private void RecordDecisionStats()
        {
            totalThinkingTime += decisionTimer.ElapsedMilliseconds;
            totalDecisions++;
        }

        public string GetPerformanceStats()
        {
            if (totalDecisions == 0) return "����ͳ������";

            float avgThinkingTime = totalThinkingTime / totalDecisions;
            return $"AI����ͳ��:\n" +
                   $"- �ܾ��ߴ���: {totalDecisions}\n" +
                   $"- ƽ��˼��ʱ��: {avgThinkingTime:F1}ms\n" +
                   $"- ����������: {moveCache?.Count ?? 0}\n" +
                   $"- ��ǰAIģʽ: {currentAIMode}";
        }

        public void SetAIMode(AIMode newMode)
        {
            if (currentAIMode != newMode)
            {
                currentAIMode = newMode;
                ClearCache(); // �л�ģʽʱ�������
                LogDebug($"AIģʽ���л�Ϊ: {newMode}");
            }
        }

        private void LogDebug(string message)
        {
            if (enableDebugLog)
            {
                Debug.Log($"[AIController] {message}");
            }
        }

        private void LogError(string message)
        {
            Debug.LogError($"[AIController] {message}");
        }

        #endregion

        #region �������ýӿ�

        public void SetDebugMode(bool enabled)
        {
            enableDebugLog = enabled;
            showThinkingProcess = enabled;
            logDecisionDetails = enabled;
        }

        public void UpdateConfig(GameConfig newConfig)
        {
            if (newConfig != null)
            {
                config = newConfig;

                // �������в��Ե�����
                foreach (var strategy in strategies)
                {
                    if (strategy is IConfigurable configurable)
                    {
                        configurable.UpdateConfig(newConfig);
                    }
                }

                ClearCache(); // ���ñ��ʱ�������
                LogDebug("AI�����Ѹ���");
            }
        }

        #endregion
    }

    #region ��ؽӿڶ���

    /// <summary>
    /// AI���Խӿ�
    /// </summary>
    public interface IAIStrategy
    {
        Move GetBestMove(GameState gameState, List<Vector2Int> availableMoves);
        float EvaluatePosition(GameState gameState, Vector2Int position);
    }

    /// <summary>
    /// �����ýӿ�
    /// </summary>
    public interface IConfigurable
    {
        void UpdateConfig(GameConfig config);
    }

    #endregion
}