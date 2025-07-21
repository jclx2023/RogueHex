using System.Collections.Generic;
using UnityEngine;
using HexGame.Core;

namespace HexGame.Gameplay
{
    /// <summary>
    /// 胜负检测器 - 专门负责检测游戏胜负条件
    /// 单一职责：只处理连通性检测和胜负判定，不涉及其他游戏逻辑
    /// </summary>
    public class WinConditionChecker : MonoBehaviour, IWinConditionChecker
    {
        [Header("调试设置")]
        [SerializeField] private bool enableDebugLog = false;
        [SerializeField] private bool visualizeWinningPath = true;

        private GameConfig config;

        // 六边形的6个相邻方向
        private static readonly Vector2Int[] HexDirections = new Vector2Int[]
        {
            new Vector2Int(-1, 0),  // 上
            new Vector2Int(-1, 1),  // 右上  
            new Vector2Int(0, -1),  // 左
            new Vector2Int(0, 1),   // 右
            new Vector2Int(1, -1),  // 左下
            new Vector2Int(1, 0)    // 下
        };

        private void Awake()
        {
            // 尝试从GameManager获取配置
            var gameManager = FindObjectOfType<GameManager>();
            if (gameManager != null)
            {
                config = gameManager.GetGameConfig();
            }
        }

        #region IWinConditionChecker接口实现

        /// <summary>
        /// 检查当前游戏状态，返回游戏结果
        /// </summary>
        public GameResult CheckGameResult(GameState gameState)
        {
            if (gameState == null)
            {
                LogError("GameState is null");
                return GameResult.GameContinues();
            }

            // 检查人类玩家是否获胜（左右连通）
            var humanResult = CheckPlayerWin(gameState.Board, PlayerType.Human);
            if (humanResult.hasWon)
            {
                LogDebug($"Human player wins! Path length: {humanResult.winningPath.Count}");
                return GameResult.PlayerWins(PlayerType.Human, humanResult.winningPath);
            }

            // 检查AI是否获胜（上下连通）
            var aiResult = CheckPlayerWin(gameState.Board, PlayerType.AI);
            if (aiResult.hasWon)
            {
                LogDebug($"AI player wins! Path length: {aiResult.winningPath.Count}");
                return GameResult.PlayerWins(PlayerType.AI, aiResult.winningPath);
            }

            // 检查是否平局（棋盘满了但无人获胜）
            if (IsBoardFull(gameState.Board))
            {
                LogDebug("Game ends in a draw - board is full");
                return GameResult.Draw();
            }

            // 游戏继续
            return GameResult.GameContinues();
        }

        /// <summary>
        /// 快速检查指定玩家是否已经获胜（用于AI模拟） - PlayerType版本
        /// </summary>
        public bool HasPlayerWon(HexCellState[,] board, PlayerType player)
        {
            return CheckPlayerWin(board, player).hasWon;
        }

        /// <summary>
        /// 快速检查指定玩家是否已经获胜（用于AI模拟） - int版本（兼容旧代码）
        /// </summary>
        public bool HasPlayerWon(HexCellState[,] board, int player)
        {
            if (player < 0 || player > 1) return false;
            PlayerType playerType = (PlayerType)player;
            return CheckPlayerWin(board, playerType).hasWon;
        }

        #endregion

        #region 核心胜负检测逻辑

        /// <summary>
        /// 检查指定玩家是否获胜
        /// </summary>
        private (bool hasWon, List<Vector2Int> winningPath) CheckPlayerWin(HexCellState[,] board, PlayerType player)
        {
            int rows = board.GetLength(0);
            int cols = board.GetLength(1);
            bool[,] visited = new bool[rows, cols];

            // 获取起始边的所有格子
            var startingCells = GetStartingCells(board, player);

            // 从每个起始格子开始DFS搜索
            foreach (var startCell in startingCells)
            {
                if (!visited[startCell.x, startCell.y])
                {
                    var path = new List<Vector2Int>();
                    if (DFS(board, startCell, player, visited, path))
                    {
                        return (true, path);
                    }
                }
            }

            return (false, new List<Vector2Int>());
        }

        /// <summary>
        /// 获取指定玩家的起始边格子
        /// </summary>
        private List<HexCellState> GetStartingCells(HexCellState[,] board, PlayerType player)
        {
            var startingCells = new List<HexCellState>();
            int rows = board.GetLength(0);
            int cols = board.GetLength(1);

            if (player == PlayerType.Human)
            {
                // 人类玩家：左边界到右边界，从左边界开始
                for (int i = 0; i < rows; i++)
                {
                    var cell = board[i, 0];
                    if (cell.IsOccupiedBy(player))
                    {
                        startingCells.Add(cell);
                    }
                }
            }
            else // PlayerType.AI
            {
                // AI玩家：上边界到下边界，从上边界开始
                for (int j = 0; j < cols; j++)
                {
                    var cell = board[0, j];
                    if (cell.IsOccupiedBy(player))
                    {
                        startingCells.Add(cell);
                    }
                }
            }

            return startingCells;
        }

        /// <summary>
        /// 深度优先搜索，寻找连通路径
        /// </summary>
        private bool DFS(HexCellState[,] board, HexCellState currentCell, PlayerType player,
                        bool[,] visited, List<Vector2Int> path)
        {
            int rows = board.GetLength(0);
            int cols = board.GetLength(1);

            // 标记当前格子为已访问
            visited[currentCell.x, currentCell.y] = true;
            path.Add(new Vector2Int(currentCell.x, currentCell.y));

            // 检查是否到达目标边界
            if (HasReachedTargetBoundary(currentCell, player, rows, cols))
            {
                LogDebug($"Player {player} reached target boundary at ({currentCell.x}, {currentCell.y})");
                return true;
            }

            // 遍历所有相邻格子
            foreach (var neighbor in GetNeighbors(board, currentCell))
            {
                if (!visited[neighbor.x, neighbor.y] && neighbor.IsOccupiedBy(player))
                {
                    if (DFS(board, neighbor, player, visited, path))
                    {
                        return true;
                    }
                }
            }

            // 回溯：如果这条路径不通，从路径中移除当前格子
            path.RemoveAt(path.Count - 1);
            return false;
        }

        /// <summary>
        /// 检查是否到达目标边界
        /// </summary>
        private bool HasReachedTargetBoundary(HexCellState cell, PlayerType player, int rows, int cols)
        {
            if (player == PlayerType.Human)
            {
                // 人类玩家需要到达右边界
                return cell.y == cols - 1;
            }
            else // PlayerType.AI
            {
                // AI需要到达下边界
                return cell.x == rows - 1;
            }
        }

        /// <summary>
        /// 获取指定格子的所有有效相邻格子
        /// </summary>
        private List<HexCellState> GetNeighbors(HexCellState[,] board, HexCellState cell)
        {
            var neighbors = new List<HexCellState>();
            int rows = board.GetLength(0);
            int cols = board.GetLength(1);

            foreach (var direction in HexDirections)
            {
                int newX = cell.x + direction.x;
                int newY = cell.y + direction.y;

                // 检查边界
                if (newX >= 0 && newX < rows && newY >= 0 && newY < cols)
                {
                    neighbors.Add(board[newX, newY]);
                }
            }

            return neighbors;
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 检查棋盘是否已满
        /// </summary>
        private bool IsBoardFull(HexCellState[,] board)
        {
            int rows = board.GetLength(0);
            int cols = board.GetLength(1);

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    if (!board[i, j].isOccupied)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// 计算棋盘占据率
        /// </summary>
        public float GetBoardOccupancyRate(HexCellState[,] board)
        {
            int rows = board.GetLength(0);
            int cols = board.GetLength(1);
            int occupiedCount = 0;
            int totalCells = rows * cols;

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    if (board[i, j].isOccupied)
                    {
                        occupiedCount++;
                    }
                }
            }

            return (float)occupiedCount / totalCells;
        }

        /// <summary>
        /// 获取指定玩家的所有连通区域
        /// </summary>
        public List<List<Vector2Int>> GetPlayerConnectedRegions(HexCellState[,] board, PlayerType player)
        {
            int rows = board.GetLength(0);
            int cols = board.GetLength(1);
            bool[,] visited = new bool[rows, cols];
            var regions = new List<List<Vector2Int>>();

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    var cell = board[i, j];
                    if (!visited[i, j] && cell.IsOccupiedBy(player))
                    {
                        var region = new List<Vector2Int>();
                        ExploreRegion(board, cell, player, visited, region);
                        if (region.Count > 0)
                        {
                            regions.Add(region);
                        }
                    }
                }
            }

            return regions;
        }

        /// <summary>
        /// 探索连通区域
        /// </summary>
        private void ExploreRegion(HexCellState[,] board, HexCellState startCell, PlayerType player,
                                  bool[,] visited, List<Vector2Int> region)
        {
            var stack = new Stack<HexCellState>();
            stack.Push(startCell);

            while (stack.Count > 0)
            {
                var current = stack.Pop();

                if (visited[current.x, current.y])
                    continue;

                visited[current.x, current.y] = true;
                region.Add(new Vector2Int(current.x, current.y));

                foreach (var neighbor in GetNeighbors(board, current))
                {
                    if (!visited[neighbor.x, neighbor.y] && neighbor.IsOccupiedBy(player))
                    {
                        stack.Push(neighbor);
                    }
                }
            }
        }

        #endregion

        #region 公共接口方法

        /// <summary>
        /// 检查是否存在潜在的获胜威胁
        /// </summary>
        public bool HasWinningThreat(HexCellState[,] board, PlayerType player, out Vector2Int threatPosition)
        {
            threatPosition = Vector2Int.zero;
            int rows = board.GetLength(0);
            int cols = board.GetLength(1);

            // 遍历所有空位，检查放置该玩家的棋子是否能立即获胜
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    if (!board[i, j].isOccupied)
                    {
                        // 临时放置棋子
                        board[i, j].SetOccupied(player);

                        // 检查是否获胜
                        bool isWinning = HasPlayerWon(board, player);

                        // 撤销临时放置
                        board[i, j].Clear();

                        if (isWinning)
                        {
                            threatPosition = new Vector2Int(i, j);
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 获取游戏进度评估
        /// </summary>
        public string GetGameProgressInfo(GameState gameState)
        {
            if (gameState == null) return "No game state";

            var occupancyRate = GetBoardOccupancyRate(gameState.Board);
            var humanRegions = GetPlayerConnectedRegions(gameState.Board, PlayerType.Human);
            var aiRegions = GetPlayerConnectedRegions(gameState.Board, PlayerType.AI);

            return $"进度信息:\n" +
                   $"- 棋盘占据率: {occupancyRate * 100:F1}%\n" +
                   $"- 总移动数: {gameState.TotalMoves}\n" +
                   $"- 人类连通区域: {humanRegions.Count}\n" +
                   $"- AI连通区域: {aiRegions.Count}\n" +
                   $"- 游戏时长: {gameState.GameDuration:mm\\:ss}";
        }

        #endregion

        #region 调试和日志

        private void LogDebug(string message)
        {
            if (enableDebugLog)
            {
                Debug.Log($"[WinConditionChecker] {message}");
            }
        }

        private void LogError(string message)
        {
            Debug.LogError($"[WinConditionChecker] {message}");
        }

        /// <summary>
        /// 可视化获胜路径（调试用）
        /// </summary>
        public void VisualizeWinningPath(List<Vector2Int> path)
        {
            if (!visualizeWinningPath || path == null || path.Count == 0)
                return;

            LogDebug($"获胜路径: {string.Join(" -> ", path)}");

            // 这里可以添加具体的可视化逻辑，比如高亮格子
            foreach (var position in path)
            {
                LogDebug($"获胜路径节点: ({position.x}, {position.y})");
            }
        }

        #endregion
    }
}