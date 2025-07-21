using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using HexGame.Core;

namespace HexGame.AI
{
    /// <summary>
    /// 位置评估策略 - 基于启发式规则的棋盘位置评分系统
    /// 保留原有的评分逻辑，同时提供可扩展的权重调节机制
    /// </summary>
    public class PositionalEvaluationStrategy : IAIStrategy, IConfigurable
    {
        private GameConfig config;
        private IWinConditionChecker winConditionChecker;

        // 评分权重配置 (可通过配置文件调节)
        [System.Serializable]
        public class EvaluationWeights
        {
            [Header("核心威胁权重")]
            public float winMoveWeight = 10000f;      // 一步必胜
            public float blockMoveWeight = 9000f;     // 一步必防

            [Header("位置评分权重")]
            public float lastMoveEvalWeight = 1.0f;   // 基于敌方上次落子的评分权重
            public float noiseWeight = 0.1f;          // 随机噪声权重

            [Header("AI强度调节")]
            [Range(0f, 1f)]
            public float aiStrength = 1.0f;           // AI强度 (0-1, 影响决策准确性)
            public bool enableRandomness = true;      // 是否启用随机性

            [Header("调试设置")]
            public bool enableDetailedLog = false;    // 是否显示详细评分日志
        }

        private EvaluationWeights weights;
        private LastMoveEvaluator lastMoveEvaluator;
        private System.Random randomGenerator;

        public PositionalEvaluationStrategy(GameConfig config, IWinConditionChecker winConditionChecker = null)
        {
            this.config = config ?? throw new System.ArgumentNullException(nameof(config));
            this.winConditionChecker = winConditionChecker;

            InitializeWeights();
            InitializeComponents();

            randomGenerator = new System.Random();
        }

        #region 初始化

        private void InitializeWeights()
        {
            weights = new EvaluationWeights
            {
                winMoveWeight = config.winScore,
                blockMoveWeight = config.blockScore,
                lastMoveEvalWeight = 1.0f,
                noiseWeight = 0.1f,
                aiStrength = 1.0f,
                enableRandomness = true,
                enableDetailedLog = config.enableDebugMode
            };
        }

        private void InitializeComponents()
        {
            lastMoveEvaluator = new LastMoveEvaluator(config);
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

            Vector2Int bestPosition = Vector2Int.zero;
            float bestScore = float.MinValue;
            var scoreDetails = new List<(Vector2Int pos, float score, string details)>();

            foreach (var position in availableMoves)
            {
                float score = EvaluatePosition(gameState, position);
                scoreDetails.Add((position, score, GetScoreBreakdown(gameState, position)));

                if (score > bestScore)
                {
                    bestScore = score;
                    bestPosition = position;
                }
            }

            // 调试日志
            if (weights.enableDetailedLog)
            {
                LogDetailedScores(scoreDetails);
            }

            // 应用AI强度和随机性
            bestPosition = ApplyAIStrengthAndRandomness(scoreDetails, bestPosition);

            return new Move(bestPosition, PlayerType.AI);
        }

        public float EvaluatePosition(GameState gameState, Vector2Int position)
        {
            float totalScore = 0f;
            var scoreBreakdown = new Dictionary<string, float>();

            // 1. 威胁检测评分 (最高优先级)
            float threatScore = EvaluateThreatScore(gameState, position);
            totalScore += threatScore;
            scoreBreakdown["威胁"] = threatScore;

            // 如果是关键威胁，直接返回高分
            if (threatScore >= weights.blockMoveWeight)
            {
                return totalScore;
            }

            // 2. 基于敌方上次落子的位置评分
            float lastMoveScore = EvaluateLastMoveScore(gameState, position);
            totalScore += lastMoveScore * weights.lastMoveEvalWeight;
            scoreBreakdown["上次落子"] = lastMoveScore;

            // 3. 添加随机噪声 (用于调节AI强度)
            if (weights.enableRandomness)
            {
                float noise = GetRandomNoise();
                totalScore += noise;
                scoreBreakdown["噪声"] = noise;
            }

            return totalScore;
        }

        #endregion

        #region 核心评分逻辑

        /// <summary>
        /// 威胁评分：检测一步必胜和一步必防
        /// </summary>
        private float EvaluateThreatScore(GameState gameState, Vector2Int position)
        {
            if (winConditionChecker == null)
                return 0f;

            // 克隆棋盘状态进行模拟
            var boardClone = gameState.CloneBoardState();

            // 检测一步必胜
            var aiMove = new Move(position, PlayerType.AI);
            ApplyMoveToBoard(boardClone, aiMove);

            if (winConditionChecker.HasPlayerWon(boardClone, PlayerType.AI))
            {
                return weights.winMoveWeight;
            }

            // 恢复棋盘状态，检测一步必防
            boardClone = gameState.CloneBoardState();
            var humanMove = new Move(position, PlayerType.Human);
            ApplyMoveToBoard(boardClone, humanMove);

            if (winConditionChecker.HasPlayerWon(boardClone, PlayerType.Human))
            {
                return weights.blockMoveWeight;
            }

            return 0f;
        }

        /// <summary>
        /// 基于敌方上次落子的评分 (保留原有逻辑)
        /// </summary>
        private float EvaluateLastMoveScore(GameState gameState, Vector2Int position)
        {
            var lastPlayerMove = gameState.GetLastMoveByPlayer(PlayerType.Human);
            if (lastPlayerMove == null)
                return 0f;

            return lastMoveEvaluator.EvaluatePosition(gameState.Board, position, lastPlayerMove.Position);
        }

        /// <summary>
        /// 生成随机噪声
        /// </summary>
        private float GetRandomNoise()
        {
            // 基于AI强度调节噪声大小
            float noiseRange = weights.noiseWeight * (1f - weights.aiStrength);
            return (float)(randomGenerator.NextDouble() * 2 - 1) * noiseRange;
        }

        #endregion

        #region AI强度和随机性处理

        /// <summary>
        /// 根据AI强度和随机性调整最终选择
        /// </summary>
        private Vector2Int ApplyAIStrengthAndRandomness(List<(Vector2Int pos, float score, string details)> scoreDetails,
                                                       Vector2Int originalBest)
        {
            // 如果AI强度为最高，直接返回最佳选择
            if (weights.aiStrength >= 1.0f || !weights.enableRandomness)
            {
                return originalBest;
            }

            // 根据AI强度决定是否偶尔选择次优解
            var sortedMoves = scoreDetails.OrderByDescending(x => x.score).ToList();

            // 计算选择前几个候选的概率
            int candidateCount = Mathf.Max(1, Mathf.RoundToInt(sortedMoves.Count * (1f - weights.aiStrength) + 1));
            candidateCount = Mathf.Min(candidateCount, sortedMoves.Count);

            // 从前N个候选中随机选择
            int selectedIndex = randomGenerator.Next(candidateCount);
            return sortedMoves[selectedIndex].pos;
        }

        #endregion

        #region 配置和调试

        public void UpdateConfig(GameConfig newConfig)
        {
            config = newConfig;
            weights.winMoveWeight = config.winScore;
            weights.blockMoveWeight = config.blockScore;
            weights.enableDetailedLog = config.enableDebugMode;

            lastMoveEvaluator?.UpdateConfig(newConfig);
        }

        /// <summary>
        /// 设置AI强度 (0=随机, 1=最强)
        /// </summary>
        public void SetAIStrength(float strength)
        {
            weights.aiStrength = Mathf.Clamp01(strength);
        }

        /// <summary>
        /// 设置评分权重
        /// </summary>
        public void SetEvaluationWeights(EvaluationWeights newWeights)
        {
            weights = newWeights ?? weights;
        }

        /// <summary>
        /// 获取当前权重配置
        /// </summary>
        public EvaluationWeights GetCurrentWeights()
        {
            return weights;
        }

        /// <summary>
        /// 获取位置评分的详细分解
        /// </summary>
        private string GetScoreBreakdown(GameState gameState, Vector2Int position)
        {
            if (!weights.enableDetailedLog)
                return "";

            var breakdown = new System.Text.StringBuilder();

            float threatScore = EvaluateThreatScore(gameState, position);
            if (threatScore > 0)
            {
                if (threatScore >= weights.winMoveWeight)
                    breakdown.AppendLine($"  必胜手: {threatScore}");
                else if (threatScore >= weights.blockMoveWeight)
                    breakdown.AppendLine($"  必防手: {threatScore}");
            }

            float lastMoveScore = EvaluateLastMoveScore(gameState, position);
            if (lastMoveScore > 0)
            {
                breakdown.AppendLine($"  位置分: {lastMoveScore:F1}");
            }

            return breakdown.ToString();
        }

        private void LogDetailedScores(List<(Vector2Int pos, float score, string details)> scoreDetails)
        {
            var sortedScores = scoreDetails.OrderByDescending(x => x.score).Take(5);

            Debug.Log("=== AI位置评分详情 (前5名) ===");
            foreach (var item in sortedScores)
            {
                Debug.Log($"位置({item.pos.x},{item.pos.y}): {item.score:F1}分\n{item.details}");
            }
        }

        #endregion
    }

    /// <summary>
    /// 基于敌方上次落子的评分器 (保留原有逻辑)
    /// </summary>
    public class LastMoveEvaluator
    {
        private GameConfig config;
        private List<OffsetScore> offsetScores;

        public LastMoveEvaluator(GameConfig config)
        {
            this.config = config;
            InitializeOffsetScores();
        }

        private void InitializeOffsetScores()
        {
            // 使用配置文件中的评分权重，如果没有则使用默认值
            if (config.evaluationWeights != null && config.evaluationWeights.Count > 0)
            {
                offsetScores = new List<OffsetScore>(config.evaluationWeights);
            }
            else
            {
                // 默认的18个偏移位置评分 (沿用原有逻辑)
                offsetScores = new List<OffsetScore>
                {
                    new OffsetScore(new Vector2Int(0, 1), 10f, "右"),
                    new OffsetScore(new Vector2Int(0, -1), 10f, "左"),
                    new OffsetScore(new Vector2Int(-1, 1), 10f, "右上"),
                    new OffsetScore(new Vector2Int(1, -1), 10f, "左下"),
                    new OffsetScore(new Vector2Int(-2, 1), 10f, "远右上"),
                    new OffsetScore(new Vector2Int(1, 1), 10f, "右下"),
                    new OffsetScore(new Vector2Int(-1, -1), 10f, "左上"),
                    new OffsetScore(new Vector2Int(2, -1), 10f, "远左下"),
                    new OffsetScore(new Vector2Int(-1, 0), 8f, "上"),
                    new OffsetScore(new Vector2Int(1, 0), 8f, "下"),
                    new OffsetScore(new Vector2Int(-2, 0), 5f, "远上"),
                    new OffsetScore(new Vector2Int(-1, -2), 5f, "左上远"),
                    new OffsetScore(new Vector2Int(0, -2), 5f, "远左"),
                    new OffsetScore(new Vector2Int(1, -2), 5f, "左下远"),
                    new OffsetScore(new Vector2Int(2, 0), 5f, "远下"),
                    new OffsetScore(new Vector2Int(1, 2), 5f, "右下远"),
                    new OffsetScore(new Vector2Int(-1, 2), 10f, "远右"),
                    new OffsetScore(new Vector2Int(2, -2), 10f, "远左")
                };
            }
        }

        public float EvaluatePosition(HexCellState[,] board, Vector2Int position, Vector2Int lastEnemyMove)
        {
            float score = 0f;

            foreach (var offsetScore in offsetScores)
            {
                Vector2Int targetPos = lastEnemyMove + offsetScore.offset;

                // 检查目标位置是否就是要评估的位置
                if (targetPos == position)
                {
                    // 确保该位置在棋盘范围内且未被占据
                    if (IsValidPosition(targetPos) && !board[targetPos.x, targetPos.y].isOccupied)
                    {
                        score += offsetScore.score;
                    }
                }
            }

            return score;
        }

        public void UpdateConfig(GameConfig newConfig)
        {
            config = newConfig;
            InitializeOffsetScores();
        }

        private bool IsValidPosition(Vector2Int position)
        {
            return position.x >= 0 && position.x < config.boardRows &&
                   position.y >= 0 && position.y < config.boardCols;
        }
    }
}