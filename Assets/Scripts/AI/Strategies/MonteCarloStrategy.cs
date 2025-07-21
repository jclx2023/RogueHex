using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using HexGame.Core;

namespace HexGame.AI
{
    /// <summary>
    /// 蒙特卡罗策略 - 基于随机模拟的AI决策策略
    /// 重构自原有的 MCTSStrategy，提供更高效和可配置的MCTS实现
    /// </summary>
    public class MonteCarloStrategy : IAIStrategy, IConfigurable
    {
        private GameConfig config;
        private IWinConditionChecker winConditionChecker;

        [System.Serializable]
        public class MCTSSettings
        {
            [Header("模拟参数")]
            [Range(10, 2000)]
            public int simulationsPerMove = 100;         // 每个候选位置的模拟次数

            [Range(1, 50)]
            public int maxCandidates = 10;               // 最大候选位置数量

            [Header("模拟优化")]
            public bool useSmartSimulation = true;       // 是否使用智能模拟
            public bool enableEarlyTermination = true;   // 是否启用提前终止

            [Range(0.1f, 0.9f)]
            public float earlyTerminationThreshold = 0.8f; // 提前终止阈值

            [Header("随机性控制")]
            [Range(0f, 1f)]
            public float explorationRate = 0.3f;        // 探索率（随机选择vs最优选择）

            public bool useProgressiveWidening = true;   // 是否使用渐进式扩展

            [Header("性能设置")]
            public bool enableParallelSimulation = false; // 是否启用并行模拟
            public int maxSimulationDepth = 100;         // 最大模拟深度
        }

        private MCTSSettings settings;
        private System.Random randomGenerator;

        // 性能统计
        private int totalSimulations = 0;
        private float totalSimulationTime = 0f;
        private System.Diagnostics.Stopwatch simulationTimer;

        public MonteCarloStrategy(GameConfig config, IWinConditionChecker winConditionChecker)
        {
            this.config = config ?? throw new System.ArgumentNullException(nameof(config));
            this.winConditionChecker = winConditionChecker ?? throw new System.ArgumentNullException(nameof(winConditionChecker));

            InitializeSettings();
            randomGenerator = new System.Random();
            simulationTimer = new System.Diagnostics.Stopwatch();
        }

        #region 初始化

        private void InitializeSettings()
        {
            settings = new MCTSSettings
            {
                simulationsPerMove = config.mctsSimulations,
                maxCandidates = 10,
                useSmartSimulation = true,
                enableEarlyTermination = true,
                earlyTerminationThreshold = 0.8f,
                explorationRate = 0.3f,
                useProgressiveWidening = true,
                enableParallelSimulation = false,
                maxSimulationDepth = 100
            };
        }

        #endregion

        #region 棋盘操作辅助方法

        /// <summary>
        /// 在克隆的棋盘上应用移动
        /// </summary>
        private void ApplyMoveToBoard(HexCellState[,] board, Move move)
        {
            if (board != null && move != null)
            {
                var cell = board[move.Position.x, move.Position.y];
                cell.SetOccupied(move.Player);
            }
        }

        #endregion

        #region IAIStrategy 接口实现

        public Move GetBestMove(GameState gameState, List<Vector2Int> availableMoves)
        {
            if (availableMoves == null || availableMoves.Count == 0)
                return null;

            simulationTimer.Restart();

            try
            {
                // 获取候选位置（限制数量以提升性能）
                var candidates = GetTopCandidates(gameState, availableMoves);

                LogDebug($"MCTS评估 {candidates.Count} 个候选位置，每个位置 {settings.simulationsPerMove} 次模拟");

                // 评估每个候选位置
                var evaluationResults = new List<(Vector2Int position, float winRate, int simulations)>();

                foreach (var candidate in candidates)
                {
                    float winRate = EvaluatePosition(gameState, candidate);
                    int actualSimulations = settings.simulationsPerMove; // 可能因提前终止而减少

                    evaluationResults.Add((candidate, winRate, actualSimulations));

                    // 如果发现极高胜率且启用提前终止，可以停止后续评估
                    if (settings.enableEarlyTermination && winRate >= settings.earlyTerminationThreshold)
                    {
                        LogDebug($"发现高胜率位置 ({candidate.x},{candidate.y})：{winRate:P1}，提前终止评估");
                        break;
                    }
                }

                // 选择最佳位置
                var bestResult = evaluationResults.OrderByDescending(x => x.winRate).First();

                LogDebug($"MCTS选择位置 ({bestResult.position.x},{bestResult.position.y})，" +
                        $"胜率: {bestResult.winRate:P1}，模拟次数: {bestResult.simulations}");

                return new Move(bestResult.position, PlayerType.AI);
            }
            finally
            {
                simulationTimer.Stop();
                RecordPerformanceStats();
            }
        }

        public float EvaluatePosition(GameState gameState, Vector2Int position)
        {
            int winCount = 0;
            int totalSims = settings.simulationsPerMove;

            for (int i = 0; i < totalSims; i++)
            {
                // 克隆棋盘状态
                var boardClone = gameState.CloneBoardState();

                // 应用初始移动
                var initialMove = new Move(position, PlayerType.AI);
                ApplyMoveToBoard(boardClone, initialMove);

                // 进行随机模拟
                PlayerType? winner = SimulateRandomGame(boardClone, PlayerType.Human); // 下一个轮到人类

                if (winner == PlayerType.AI)
                {
                    winCount++;
                }

                // 提前终止检查
                if (settings.enableEarlyTermination && i >= 20) // 至少模拟20次后才考虑提前终止
                {
                    float currentWinRate = (float)winCount / (i + 1);

                    // 如果胜率已经很高或很低，可以提前终止
                    if (currentWinRate >= settings.earlyTerminationThreshold ||
                        currentWinRate <= (1f - settings.earlyTerminationThreshold))
                    {
                        totalSims = i + 1; // 更新实际模拟次数
                        break;
                    }
                }
            }

            float winRate = totalSims > 0 ? (float)winCount / totalSims : 0f;
            totalSimulations += totalSims;

            return winRate;
        }

        #endregion

        #region 核心MCTS算法

        /// <summary>
        /// 模拟随机游戏直到结束
        /// </summary>
        private PlayerType? SimulateRandomGame(HexCellState[,] boardState, PlayerType currentPlayer)
        {
            int moveCount = 0;
            int maxMoves = settings.maxSimulationDepth;

            while (moveCount < maxMoves)
            {
                // 检查游戏是否结束
                if (winConditionChecker.HasPlayerWon(boardState, PlayerType.AI))
                    return PlayerType.AI;
                if (winConditionChecker.HasPlayerWon(boardState, PlayerType.Human))
                    return PlayerType.Human;

                // 获取可用移动
                var availableMoves = GetAvailableMovesFromBoard(boardState);
                if (availableMoves.Count == 0)
                    break; // 平局

                // 选择下一步移动
                Vector2Int nextMove = SelectNextMoveInSimulation(boardState, availableMoves, currentPlayer);

                // 应用移动
                var move = new Move(nextMove, currentPlayer);
                ApplyMoveToBoard(boardState, move);

                // 切换玩家
                currentPlayer = currentPlayer == PlayerType.AI ? PlayerType.Human : PlayerType.AI;
                moveCount++;
            }

            return null; // 平局或达到最大深度
        }

        /// <summary>
        /// 在模拟中选择下一步移动
        /// </summary>
        private Vector2Int SelectNextMoveInSimulation(HexCellState[,] boardState, List<Vector2Int> availableMoves, PlayerType player)
        {
            if (settings.useSmartSimulation)
            {
                // 智能模拟：结合随机性和启发式选择
                return SelectSmartMove(boardState, availableMoves, player);
            }
            else
            {
                // 纯随机选择
                return availableMoves[randomGenerator.Next(availableMoves.Count)];
            }
        }

        /// <summary>
        /// 智能移动选择：平衡随机性和策略性
        /// </summary>
        private Vector2Int SelectSmartMove(HexCellState[,] boardState, List<Vector2Int> availableMoves, PlayerType player)
        {
            // 根据探索率决定是随机选择还是策略选择
            if (randomGenerator.NextDouble() < settings.explorationRate)
            {
                // 随机探索
                return availableMoves[randomGenerator.Next(availableMoves.Count)];
            }
            else
            {
                // 策略选择：优先选择连接度高的位置
                return SelectStrategicMove(boardState, availableMoves, player);
            }
        }

        /// <summary>
        /// 战略性移动选择
        /// </summary>
        private Vector2Int SelectStrategicMove(HexCellState[,] boardState, List<Vector2Int> availableMoves, PlayerType player)
        {
            var scoredMoves = new List<(Vector2Int position, float score)>();

            foreach (var move in availableMoves)
            {
                float score = EvaluateMoveInSimulation(boardState, move, player);
                scoredMoves.Add((move, score));
            }

            // 选择分数最高的移动，但加入一些随机性
            var sortedMoves = scoredMoves.OrderByDescending(x => x.score).ToList();

            // 从前几个最佳选择中随机选择
            int topChoices = Mathf.Min(3, sortedMoves.Count);
            int selectedIndex = randomGenerator.Next(topChoices);

            return sortedMoves[selectedIndex].position;
        }

        /// <summary>
        /// 在模拟中评估移动的价值
        /// </summary>
        private float EvaluateMoveInSimulation(HexCellState[,] boardState, Vector2Int position, PlayerType player)
        {
            float score = 0f;

            // 1. 连接价值：与己方棋子的连接数
            int connections = CountPlayerConnections(boardState, position, player);
            score += connections * 2f;

            // 2. 边界价值：靠近目标边界的价值
            float boundaryValue = EvaluateBoundaryDistance(position, player);
            score += boundaryValue;

            // 3. 中心价值：适度偏向中心位置
            float centerValue = EvaluateCenterDistance(position);
            score += centerValue * 0.5f;

            return score;
        }

        #endregion

        #region 辅助评估方法

        private int CountPlayerConnections(HexCellState[,] boardState, Vector2Int position, PlayerType player)
        {
            int connections = 0;
            var directions = new Vector2Int[]
            {
                new Vector2Int(-1, 0), new Vector2Int(-1, 1),
                new Vector2Int(0, -1), new Vector2Int(0, 1),
                new Vector2Int(1, -1), new Vector2Int(1, 0)
            };

            foreach (var dir in directions)
            {
                var neighborPos = position + dir;
                if (IsValidPosition(neighborPos))
                {
                    var neighbor = boardState[neighborPos.x, neighborPos.y];
                    if (neighbor.IsOccupiedBy(player))
                    {
                        connections++;
                    }
                }
            }

            return connections;
        }

        private float EvaluateBoundaryDistance(Vector2Int position, PlayerType player)
        {
            if (player == PlayerType.Human)
            {
                // 人类玩家：左右连通，距离左右边界越近越好
                float leftDistance = position.y;
                float rightDistance = config.boardCols - 1 - position.y;
                return (config.boardCols - Mathf.Min(leftDistance, rightDistance)) / (float)config.boardCols;
            }
            else
            {
                // AI玩家：上下连通，距离上下边界越近越好
                float topDistance = position.x;
                float bottomDistance = config.boardRows - 1 - position.x;
                return (config.boardRows - Mathf.Min(topDistance, bottomDistance)) / (float)config.boardRows;
            }
        }

        private float EvaluateCenterDistance(Vector2Int position)
        {
            var center = new Vector2(config.boardRows / 2f, config.boardCols / 2f);
            float maxDistance = Mathf.Max(config.boardRows, config.boardCols) / 2f;
            float distance = Vector2.Distance(position, center);
            return (maxDistance - distance) / maxDistance;
        }

        private List<Vector2Int> GetAvailableMovesFromBoard(HexCellState[,] boardState)
        {
            var availableMoves = new List<Vector2Int>();
            int rows = boardState.GetLength(0);
            int cols = boardState.GetLength(1);

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    if (!boardState[i, j].isOccupied)
                    {
                        availableMoves.Add(new Vector2Int(i, j));
                    }
                }
            }

            return availableMoves;
        }

        private List<Vector2Int> GetTopCandidates(GameState gameState, List<Vector2Int> availableMoves)
        {
            if (availableMoves.Count <= settings.maxCandidates)
            {
                return availableMoves;
            }

            // 使用简单的启发式方法选择候选位置
            var candidates = new List<(Vector2Int position, float priority)>();

            foreach (var move in availableMoves)
            {
                float priority = CalculateMovePriority(gameState, move);
                candidates.Add((move, priority));
            }

            // 返回优先级最高的候选
            return candidates
                .OrderByDescending(x => x.priority)
                .Take(settings.maxCandidates)
                .Select(x => x.position)
                .ToList();
        }

        private float CalculateMovePriority(GameState gameState, Vector2Int position)
        {
            float priority = 0f;
            var board = gameState.Board;

            // 1. 连接价值
            int aiConnections = CountPlayerConnections(board, position, PlayerType.AI);
            int humanConnections = CountPlayerConnections(board, position, PlayerType.Human);
            priority += aiConnections * 3f + humanConnections * 2f; // 阻断对手也有价值

            // 2. 边界价值
            priority += EvaluateBoundaryDistance(position, PlayerType.AI) * 2f;

            // 3. 中心偏好
            priority += EvaluateCenterDistance(position);

            return priority;
        }

        private bool IsValidPosition(Vector2Int position)
        {
            return position.x >= 0 && position.x < config.boardRows &&
                   position.y >= 0 && position.y < config.boardCols;
        }

        #endregion

        #region 配置和性能

        public void UpdateConfig(GameConfig newConfig)
        {
            config = newConfig;
            settings.simulationsPerMove = config.mctsSimulations;
        }

        public void SetMCTSSettings(MCTSSettings newSettings)
        {
            settings = newSettings ?? settings;
        }

        public MCTSSettings GetCurrentSettings()
        {
            return settings;
        }

        private void RecordPerformanceStats()
        {
            totalSimulationTime += simulationTimer.ElapsedMilliseconds;
        }

        public string GetPerformanceStats()
        {
            if (totalSimulations == 0)
                return "暂无MCTS统计数据";

            float avgSimTime = totalSimulationTime / totalSimulations * 1000f; // 转换为微秒

            return $"MCTS性能统计:\n" +
                   $"- 总模拟次数: {totalSimulations}\n" +
                   $"- 平均每次模拟时间: {avgSimTime:F2}μs\n" +
                   $"- 每步模拟数: {settings.simulationsPerMove}\n" +
                   $"- 智能模拟: {(settings.useSmartSimulation ? "启用" : "禁用")}\n" +
                   $"- 提前终止: {(settings.enableEarlyTermination ? "启用" : "禁用")}";
        }

        public void ResetStats()
        {
            totalSimulations = 0;
            totalSimulationTime = 0f;
        }

        #endregion

        #region 调试

        private void LogDebug(string message)
        {
            if (config.enableDebugMode)
            {
                Debug.Log($"[MonteCarloStrategy] {message}");
            }
        }

        #endregion
    }
}