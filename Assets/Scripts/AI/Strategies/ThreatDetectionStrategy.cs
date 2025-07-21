using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using HexGame.Core;

namespace HexGame.AI
{
    /// <summary>
    /// 威胁检测策略 - 专门检测紧急威胁和机会
    /// 重构自原有的 FrontierThreatStrategy，提供更高效的威胁检测
    /// </summary>
    public class ThreatDetectionStrategy : IAIStrategy, IConfigurable
    {
        private GameConfig config;
        private IWinConditionChecker winConditionChecker;

        // 前沿格子管理 - 只检测已占格子周围的空格，提高效率
        private HashSet<Vector2Int> aiFrontier;
        private HashSet<Vector2Int> humanFrontier;

        // 六边形方向
        private static readonly Vector2Int[] HexDirections = new Vector2Int[]
        {
            new Vector2Int(-1, 0),  // 上
            new Vector2Int(-1, 1),  // 右上
            new Vector2Int(0, -1),  // 左
            new Vector2Int(0, 1),   // 右
            new Vector2Int(1, -1),  // 左下
            new Vector2Int(1, 0)    // 下
        };

        [System.Serializable]
        public class ThreatWeights
        {
            [Header("威胁检测权重")]
            public float immediateWinWeight = 10000f;     // 一步必胜
            public float immediateBlockWeight = 9000f;    // 一步必防
            public float twoStepThreatWeight = 500f;      // 两步威胁
            public float connectionWeight = 100f;         // 连接价值

            [Header("检测深度")]
            public int maxThreatDepth = 2;                // 最大威胁检测深度
            public bool enableMultiStepThreat = true;     // 是否启用多步威胁检测

            [Header("性能优化")]
            public bool useFrontierOptimization = true;   // 是否使用前沿优化
            public int maxCandidatesCheck = 20;           // 最大候选检查数量
        }

        private ThreatWeights weights;
        private bool isInitialized = false;

        public ThreatDetectionStrategy(GameConfig config, IWinConditionChecker winConditionChecker)
        {
            this.config = config ?? throw new System.ArgumentNullException(nameof(config));
            this.winConditionChecker = winConditionChecker ?? throw new System.ArgumentNullException(nameof(winConditionChecker));

            InitializeWeights();
            InitializeFrontiers();
        }

        #region 初始化

        private void InitializeWeights()
        {
            weights = new ThreatWeights
            {
                immediateWinWeight = config.winScore,
                immediateBlockWeight = config.blockScore,
                twoStepThreatWeight = 500f,
                connectionWeight = 100f,
                maxThreatDepth = 2,
                enableMultiStepThreat = true,
                useFrontierOptimization = true,
                maxCandidatesCheck = 20
            };
        }

        private void InitializeFrontiers()
        {
            aiFrontier = new HashSet<Vector2Int>();
            humanFrontier = new HashSet<Vector2Int>();
            isInitialized = false;
        }

        /// <summary>
        /// 根据当前棋盘状态重建前沿格子集合
        /// </summary>
        private void RebuildFrontiers(HexCellState[,] board)
        {
            if (!weights.useFrontierOptimization)
                return;

            aiFrontier.Clear();
            humanFrontier.Clear();

            int rows = board.GetLength(0);
            int cols = board.GetLength(1);

            // 遍历所有已占格子，将其邻居空格加入对应的前沿
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    var cell = board[i, j];
                    if (cell.isOccupied && cell.occupiedBy >= 0)
                    {
                        PlayerType player = (PlayerType)cell.occupiedBy;
                        UpdateFrontierForCell(board, new Vector2Int(i, j), player);
                    }
                }
            }

            isInitialized = true;
        }

        private void UpdateFrontierForCell(HexCellState[,] board, Vector2Int position, PlayerType player)
        {
            var targetFrontier = player == PlayerType.AI ? aiFrontier : humanFrontier;

            foreach (var direction in HexDirections)
            {
                var neighborPos = position + direction;
                if (IsValidPosition(neighborPos) && !board[neighborPos.x, neighborPos.y].isOccupied)
                {
                    targetFrontier.Add(neighborPos);
                }
            }
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

            var board = gameState.Board;

            // 确保前沿已初始化
            if (!isInitialized)
            {
                RebuildFrontiers(board);
            }

            // 1. 检查一步必胜
            var winningMove = GetWinningMove(gameState, PlayerType.AI);
            if (winningMove != null)
            {
                LogDebug("发现一步必胜!");
                return winningMove;
            }

            // 2. 检查一步必防
            var blockingMove = GetBlockingMove(gameState, PlayerType.Human);
            if (blockingMove != null)
            {
                LogDebug("发现一步必防!");
                return blockingMove;
            }

            // 3. 如果没有紧急威胁，返回最高威胁值的位置
            Vector2Int bestPosition = Vector2Int.zero;
            float bestScore = float.MinValue;

            // 使用前沿优化：只检查前沿格子
            var candidatePositions = GetCandidatePositions(availableMoves);

            foreach (var position in candidatePositions)
            {
                float score = EvaluatePosition(gameState, position);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestPosition = position;
                }
            }

            if (bestScore > float.MinValue)
            {
                LogDebug($"威胁检测选择位置 ({bestPosition.x},{bestPosition.y})，威胁值: {bestScore:F1}");
                return new Move(bestPosition, PlayerType.AI);
            }

            return null; // 让其他策略处理
        }

        public float EvaluatePosition(GameState gameState, Vector2Int position)
        {
            float totalScore = 0f;

            // 1. 一步威胁检测
            float immediateScore = EvaluateImmediateThreat(gameState, position);
            totalScore += immediateScore;

            // 如果是关键威胁，直接返回
            if (immediateScore >= weights.immediateBlockWeight)
            {
                return immediateScore;
            }

            // 2. 多步威胁检测 (如果启用)
            if (weights.enableMultiStepThreat)
            {
                float multiStepScore = EvaluateMultiStepThreat(gameState, position);
                totalScore += multiStepScore;
            }

            // 3. 连接价值评估
            float connectionScore = EvaluateConnectionValue(gameState, position);
            totalScore += connectionScore;

            return totalScore;
        }

        #endregion

        #region 威胁检测核心方法

        /// <summary>
        /// 获取AI的一步必胜移动
        /// </summary>
        public Move GetWinningMove(GameState gameState, PlayerType player)
        {
            var candidatePositions = GetCandidatePositions(gameState.GetEmptyPositions());

            foreach (var position in candidatePositions)
            {
                if (IsWinningMove(gameState, position, player))
                {
                    return new Move(position, player);
                }
            }

            return null;
        }

        /// <summary>
        /// 获取阻止对手获胜的移动
        /// </summary>
        public Move GetBlockingMove(GameState gameState, PlayerType opponentPlayer)
        {
            var candidatePositions = GetCandidatePositions(gameState.GetEmptyPositions());

            foreach (var position in candidatePositions)
            {
                if (IsWinningMove(gameState, position, opponentPlayer))
                {
                    return new Move(position, PlayerType.AI); // AI占据这个位置来阻止对手
                }
            }

            return null;
        }

        /// <summary>
        /// 检查在指定位置落子是否能让指定玩家获胜
        /// </summary>
        private bool IsWinningMove(GameState gameState, Vector2Int position, PlayerType player)
        {
            // 克隆棋盘状态
            var boardClone = gameState.CloneBoardState();

            // 模拟落子
            var move = new Move(position, player);
            ApplyMoveToBoard(boardClone, move);

            // 检查是否获胜
            return winConditionChecker.HasPlayerWon(boardClone, player);
        }

        /// <summary>
        /// 评估一步威胁
        /// </summary>
        private float EvaluateImmediateThreat(GameState gameState, Vector2Int position)
        {
            // 检查AI一步必胜
            if (IsWinningMove(gameState, position, PlayerType.AI))
            {
                return weights.immediateWinWeight;
            }

            // 检查阻止人类一步必胜
            if (IsWinningMove(gameState, position, PlayerType.Human))
            {
                return weights.immediateBlockWeight;
            }

            return 0f;
        }

        /// <summary>
        /// 评估多步威胁
        /// </summary>
        private float EvaluateMultiStepThreat(GameState gameState, Vector2Int position)
        {
            float score = 0f;

            // 检查两步威胁：AI在此位置落子后，下一步是否有必胜机会
            var boardClone = gameState.CloneBoardState();
            var aiMove = new Move(position, PlayerType.AI);
            GameState.ApplyMoveToBoard(boardClone, aiMove);

            // 创建临时游戏状态
            var tempGameState = CreateTempGameState(boardClone);
            var nextMovePositions = tempGameState.GetEmptyPositions().Take(10); // 限制检查数量

            int aiWinningMoves = 0;
            foreach (var nextPos in nextMovePositions)
            {
                if (IsWinningMove(tempGameState, nextPos, PlayerType.AI))
                {
                    aiWinningMoves++;
                }
            }

            // 如果有多个后续必胜机会，这是一个强势位置
            if (aiWinningMoves > 0)
            {
                score += weights.twoStepThreatWeight * aiWinningMoves;
            }

            return score;
        }

        /// <summary>
        /// 评估连接价值
        /// </summary>
        private float EvaluateConnectionValue(GameState gameState, Vector2Int position)
        {
            float score = 0f;
            var board = gameState.Board;

            // 计算与己方棋子的连接数
            int aiConnections = 0;
            foreach (var direction in HexDirections)
            {
                var neighborPos = position + direction;
                if (IsValidPosition(neighborPos))
                {
                    var neighbor = board[neighborPos.x, neighborPos.y];
                    if (neighbor.IsOccupiedBy(PlayerType.AI))
                    {
                        aiConnections++;
                    }
                }
            }

            // 连接越多，价值越高
            score += aiConnections * weights.connectionWeight;

            return score;
        }

        #endregion

        #region 优化和辅助方法

        private List<Vector2Int> GetCandidatePositions(List<Vector2Int> availableMoves)
        {
            if (!weights.useFrontierOptimization || !isInitialized)
            {
                // 如果不使用前沿优化，返回所有可用位置（但限制数量）
                return availableMoves.Take(weights.maxCandidatesCheck).ToList();
            }

            // 使用前沿优化：优先检查前沿格子
            var candidates = new HashSet<Vector2Int>();

            // 添加AI前沿格子
            foreach (var pos in aiFrontier)
            {
                if (availableMoves.Contains(pos))
                {
                    candidates.Add(pos);
                }
            }

            // 添加人类前沿格子（用于阻断）
            foreach (var pos in humanFrontier)
            {
                if (availableMoves.Contains(pos))
                {
                    candidates.Add(pos);
                }
            }

            // 如果前沿格子不够，添加其他可用位置
            if (candidates.Count < weights.maxCandidatesCheck)
            {
                foreach (var pos in availableMoves)
                {
                    candidates.Add(pos);
                    if (candidates.Count >= weights.maxCandidatesCheck)
                        break;
                }
            }

            return candidates.ToList();
        }

        private GameState CreateTempGameState(HexCellState[,] board)
        {
            var tempGameState = new GameState(config);

            // 复制棋盘状态
            for (int i = 0; i < config.boardRows; i++)
            {
                for (int j = 0; j < config.boardCols; j++)
                {
                    var sourceCell = board[i, j];
                    var targetCell = tempGameState.Board[i, j];

                    if (sourceCell.isOccupied && sourceCell.occupiedBy >= 0)
                    {
                        PlayerType player = (PlayerType)sourceCell.occupiedBy;
                        targetCell.SetOccupied(player);
                    }
                }
            }

            return tempGameState;
        }

        private bool IsValidPosition(Vector2Int position)
        {
            return position.x >= 0 && position.x < config.boardRows &&
                   position.y >= 0 && position.y < config.boardCols;
        }

        #endregion

        #region IConfigurable 接口

        public void UpdateConfig(GameConfig newConfig)
        {
            config = newConfig;
            weights.immediateWinWeight = config.winScore;
            weights.immediateBlockWeight = config.blockScore;

            // 重新初始化前沿
            InitializeFrontiers();
        }

        #endregion

        #region 公共接口方法

        /// <summary>
        /// 手动更新前沿格子（当棋盘发生变化时调用）
        /// </summary>
        public void UpdateFrontiers(GameState gameState, Move lastMove)
        {
            if (!weights.useFrontierOptimization)
                return;

            if (lastMove != null)
            {
                // 移除已占据的位置
                aiFrontier.Remove(lastMove.Position);
                humanFrontier.Remove(lastMove.Position);

                // 添加新的前沿格子
                UpdateFrontierForCell(gameState.Board, lastMove.Position, lastMove.Player);
            }
            else
            {
                // 完全重建前沿
                RebuildFrontiers(gameState.Board);
            }
        }

        /// <summary>
        /// 获取当前前沿统计信息
        /// </summary>
        public string GetFrontierStats()
        {
            return $"前沿统计:\n" +
                   $"- AI前沿格子: {aiFrontier.Count}\n" +
                   $"- 人类前沿格子: {humanFrontier.Count}\n" +
                   $"- 前沿优化: {(weights.useFrontierOptimization ? "启用" : "禁用")}";
        }

        /// <summary>
        /// 设置威胁检测权重
        /// </summary>
        public void SetThreatWeights(ThreatWeights newWeights)
        {
            weights = newWeights ?? weights;
        }

        #endregion

        #region 调试

        private void LogDebug(string message)
        {
            if (config.enableDebugMode)
            {
                Debug.Log($"[ThreatDetectionStrategy] {message}");
            }
        }

        #endregion
    }
}