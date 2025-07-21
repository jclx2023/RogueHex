using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using HexGame.Core;
using HexGame.Gameplay;

namespace HexGame.AI
{
    /// <summary>
    /// AI控制器 - 统一的AI决策入口，采用策略组合模式
    /// 负责协调不同AI策略，根据配置选择最佳决策方案
    /// </summary>
    public class AIController : MonoBehaviour, IAIController
    {
        [Header("AI策略配置")]
        [SerializeField] private AIMode currentAIMode = AIMode.Hybrid;
        [SerializeField] private bool enableStrategyCache = true;
        [SerializeField] private bool enableAsyncProcessing = true;

        [Header("调试设置")]
        [SerializeField] private bool enableDebugLog = true;
        [SerializeField] private bool showThinkingProcess = false;
        [SerializeField] private bool logDecisionDetails = false;

        // 依赖组件
        private GameConfig config;
        private IWinConditionChecker winConditionChecker;

        // AI策略列表
        private List<IAIStrategy> strategies;
        private Dictionary<System.Type, IAIStrategy> strategyCache;

        // 决策缓存
        private Dictionary<string, Move> moveCache;
        private const int MAX_CACHE_SIZE = 1000;

        // 性能统计
        private System.Diagnostics.Stopwatch decisionTimer;
        private float totalThinkingTime = 0f;
        private int totalDecisions = 0;

        #region Unity生命周期

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

        #region 初始化

        private void InitializeDependencies()
        {
            // 获取GameManager的配置
            var gameManager = FindObjectOfType<GameManager>();
            if (gameManager != null)
            {
                config = gameManager.GetGameConfig();
            }

            // 获取胜负检测器
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

            // 创建威胁检测策略
            var threatStrategy = new ThreatDetectionStrategy(config, winConditionChecker);
            RegisterStrategy(threatStrategy);

            // 创建蒙特卡罗策略
            var mctsStrategy = new MonteCarloStrategy(config, winConditionChecker);
            RegisterStrategy(mctsStrategy);

            // 创建位置评估策略
            var positionalStrategy = new PositionalEvaluationStrategy(config);
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

        #region IAIController接口实现

        /// <summary>
        /// 获取AI的最佳移动
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
                // 检查缓存
                Move cachedMove = GetCachedMove(gameState);
                if (cachedMove != null)
                {
                    LogDebug($"Using cached move: {cachedMove}");
                    return cachedMove;
                }

                // 获取所有可用移动
                var availableMoves = GetAvailableMoves(gameState);
                if (availableMoves.Count == 0)
                {
                    LogError("No available moves!");
                    return null;
                }

                // 根据AI模式选择决策策略
                Move bestMove = SelectBestMoveByMode(gameState, availableMoves);

                // 缓存决策结果
                CacheMove(gameState, bestMove);

                // 记录统计信息
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

        #region 决策策略选择

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
        /// 威胁优先模式 - 优先处理紧急情况
        /// </summary>
        private Move GetThreatFocusedMove(GameState gameState, List<Vector2Int> availableMoves)
        {
            var threatStrategy = GetStrategy<ThreatDetectionStrategy>();
            if (threatStrategy != null)
            {
                // 检查一步必胜
                var winningMove = threatStrategy.GetWinningMove(gameState, PlayerType.AI);
                if (winningMove != null)
                {
                    LogDebug("Found winning move!");
                    return winningMove;
                }

                // 检查一步必防
                var blockingMove = threatStrategy.GetBlockingMove(gameState, PlayerType.Human);
                if (blockingMove != null)
                {
                    LogDebug("Found blocking move!");
                    return blockingMove;
                }
            }

            // 回退到位置评估
            return GetPositionalMove(gameState, availableMoves);
        }

        /// <summary>
        /// MCTS优先模式 - 使用蒙特卡罗模拟
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
        /// 位置评估模式 - 基于启发式评分
        /// </summary>
        private Move GetPositionalMove(GameState gameState, List<Vector2Int> availableMoves)
        {
            var positionalStrategy = GetStrategy<PositionalEvaluationStrategy>();
            if (positionalStrategy != null)
            {
                return positionalStrategy.GetBestMove(gameState, availableMoves);
            }

            // 最后的回退：随机选择
            var randomPos = availableMoves[Random.Range(0, availableMoves.Count)];
            return new Move(randomPos, PlayerType.AI);
        }

        /// <summary>
        /// 混合模式 - 综合使用多种策略
        /// </summary>
        private Move GetHybridMove(GameState gameState, List<Vector2Int> availableMoves)
        {
            if (showThinkingProcess)
            {
                LogDebug("=== AI思考过程 ===");
            }

            // 1. 首先检查威胁（最高优先级）
            var threatStrategy = GetStrategy<ThreatDetectionStrategy>();
            if (threatStrategy != null)
            {
                var winningMove = threatStrategy.GetWinningMove(gameState, PlayerType.AI);
                if (winningMove != null)
                {
                    LogDebug("优先级1：找到必胜手！");
                    return winningMove;
                }

                var blockingMove = threatStrategy.GetBlockingMove(gameState, PlayerType.Human);
                if (blockingMove != null)
                {
                    LogDebug("优先级2：找到必防手！");
                    return blockingMove;
                }
            }

            // 2. 评估候选移动
            var candidateEvaluations = new List<(Vector2Int position, float score, string reason)>();

            // 使用位置评估策略
            var positionalStrategy = GetStrategy<PositionalEvaluationStrategy>();
            if (positionalStrategy != null)
            {
                foreach (var pos in availableMoves)
                {
                    float score = positionalStrategy.EvaluatePosition(gameState, pos);
                    candidateEvaluations.Add((pos, score, "位置评估"));
                }
            }

            // 3. 对于高分候选，使用MCTS进行验证
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
                    mctsResults.Add((candidate.position, winRate, $"MCTS胜率:{winRate:P1}"));

                    if (showThinkingProcess)
                    {
                        LogDebug($"候选位置 ({candidate.position.x},{candidate.position.y}): " +
                                $"位置分{candidate.score:F1}, 胜率{winRate:P1}");
                    }
                }

                // 选择胜率最高的移动
                var bestMCTS = mctsResults.OrderByDescending(x => x.winRate).First();

                if (showThinkingProcess)
                {
                    LogDebug($"最终选择: ({bestMCTS.position.x},{bestMCTS.position.y}) - {bestMCTS.reason}");
                }

                return new Move(bestMCTS.position, PlayerType.AI);
            }

            // 4. 回退到最高分的位置评估
            if (candidateEvaluations.Count > 0)
            {
                var bestCandidate = candidateEvaluations.OrderByDescending(x => x.score).First();

                if (showThinkingProcess)
                {
                    LogDebug($"使用位置评估最高分: ({bestCandidate.position.x},{bestCandidate.position.y}) " +
                            $"分数:{bestCandidate.score:F1}");
                }

                return new Move(bestCandidate.position, PlayerType.AI);
            }

            // 5. 最后的回退
            return GetFallbackMove(gameState);
        }

        #endregion

        #region 辅助方法

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
                // 选择中心附近的位置
                var center = new Vector2(config.boardRows / 2f, config.boardCols / 2f);
                var centerMove = availableMoves
                    .OrderBy(pos => Vector2.Distance(pos, center))
                    .First();

                LogDebug($"使用回退策略，选择中心附近位置: ({centerMove.x}, {centerMove.y})");
                return new Move(centerMove, PlayerType.AI);
            }

            LogError("No fallback move available!");
            return null;
        }

        #endregion

        #region 缓存管理

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
                        hash.Append($"{i},{j}:{(int)cell.occupiedBy};");
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
                // 限制缓存大小
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
            LogDebug("AI决策缓存已清除");
        }

        #endregion

        #region 统计和调试

        private void RecordDecisionStats()
        {
            totalThinkingTime += decisionTimer.ElapsedMilliseconds;
            totalDecisions++;
        }

        public string GetPerformanceStats()
        {
            if (totalDecisions == 0) return "暂无统计数据";

            float avgThinkingTime = totalThinkingTime / totalDecisions;
            return $"AI性能统计:\n" +
                   $"- 总决策次数: {totalDecisions}\n" +
                   $"- 平均思考时间: {avgThinkingTime:F1}ms\n" +
                   $"- 缓存命中数: {moveCache?.Count ?? 0}\n" +
                   $"- 当前AI模式: {currentAIMode}";
        }

        public void SetAIMode(AIMode newMode)
        {
            if (currentAIMode != newMode)
            {
                currentAIMode = newMode;
                ClearCache(); // 切换模式时清除缓存
                LogDebug($"AI模式已切换为: {newMode}");
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

        #region 公共配置接口

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

                // 更新所有策略的配置
                foreach (var strategy in strategies)
                {
                    if (strategy is IConfigurable configurable)
                    {
                        configurable.UpdateConfig(newConfig);
                    }
                }

                ClearCache(); // 配置变更时清除缓存
                LogDebug("AI配置已更新");
            }
        }

        #endregion
    }

    #region 相关接口定义

    /// <summary>
    /// AI策略接口
    /// </summary>
    public interface IAIStrategy
    {
        Move GetBestMove(GameState gameState, List<Vector2Int> availableMoves);
        float EvaluatePosition(GameState gameState, Vector2Int position);
    }

    /// <summary>
    /// 可配置接口
    /// </summary>
    public interface IConfigurable
    {
        void UpdateConfig(GameConfig config);
    }

    #endregion
}