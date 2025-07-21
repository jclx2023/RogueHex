using System.Collections.Generic;
using UnityEngine;
using HexGame.Core;

namespace HexGame.Gameplay
{
    /// <summary>
    /// ʤ������� - ר�Ÿ�������Ϸʤ������
    /// ��һְ��ֻ������ͨ�Լ���ʤ���ж������漰������Ϸ�߼�
    /// </summary>
    public class WinConditionChecker : MonoBehaviour, IWinConditionChecker
    {
        [Header("��������")]
        [SerializeField] private bool enableDebugLog = false;
        [SerializeField] private bool visualizeWinningPath = true;

        private GameConfig config;

        // �����ε�6�����ڷ���
        private static readonly Vector2Int[] HexDirections = new Vector2Int[]
        {
            new Vector2Int(-1, 0),  // ��
            new Vector2Int(-1, 1),  // ����  
            new Vector2Int(0, -1),  // ��
            new Vector2Int(0, 1),   // ��
            new Vector2Int(1, -1),  // ����
            new Vector2Int(1, 0)    // ��
        };

        private void Awake()
        {
            // ���Դ�GameManager��ȡ����
            var gameManager = FindObjectOfType<GameManager>();
            if (gameManager != null)
            {
                config = gameManager.GetGameConfig();
            }
        }

        #region IWinConditionChecker�ӿ�ʵ��

        /// <summary>
        /// ��鵱ǰ��Ϸ״̬��������Ϸ���
        /// </summary>
        public GameResult CheckGameResult(GameState gameState)
        {
            if (gameState == null)
            {
                LogError("GameState is null");
                return GameResult.GameContinues();
            }

            // �����������Ƿ��ʤ��������ͨ��
            var humanResult = CheckPlayerWin(gameState.Board, PlayerType.Human);
            if (humanResult.hasWon)
            {
                LogDebug($"Human player wins! Path length: {humanResult.winningPath.Count}");
                return GameResult.PlayerWins(PlayerType.Human, humanResult.winningPath);
            }

            // ���AI�Ƿ��ʤ��������ͨ��
            var aiResult = CheckPlayerWin(gameState.Board, PlayerType.AI);
            if (aiResult.hasWon)
            {
                LogDebug($"AI player wins! Path length: {aiResult.winningPath.Count}");
                return GameResult.PlayerWins(PlayerType.AI, aiResult.winningPath);
            }

            // ����Ƿ�ƽ�֣��������˵����˻�ʤ��
            if (IsBoardFull(gameState.Board))
            {
                LogDebug("Game ends in a draw - board is full");
                return GameResult.Draw();
            }

            // ��Ϸ����
            return GameResult.GameContinues();
        }

        /// <summary>
        /// ���ټ��ָ������Ƿ��Ѿ���ʤ������AIģ�⣩ - PlayerType�汾
        /// </summary>
        public bool HasPlayerWon(HexCellState[,] board, PlayerType player)
        {
            return CheckPlayerWin(board, player).hasWon;
        }

        /// <summary>
        /// ���ټ��ָ������Ƿ��Ѿ���ʤ������AIģ�⣩ - int�汾�����ݾɴ��룩
        /// </summary>
        public bool HasPlayerWon(HexCellState[,] board, int player)
        {
            if (player < 0 || player > 1) return false;
            PlayerType playerType = (PlayerType)player;
            return CheckPlayerWin(board, playerType).hasWon;
        }

        #endregion

        #region ����ʤ������߼�

        /// <summary>
        /// ���ָ������Ƿ��ʤ
        /// </summary>
        private (bool hasWon, List<Vector2Int> winningPath) CheckPlayerWin(HexCellState[,] board, PlayerType player)
        {
            int rows = board.GetLength(0);
            int cols = board.GetLength(1);
            bool[,] visited = new bool[rows, cols];

            // ��ȡ��ʼ�ߵ����и���
            var startingCells = GetStartingCells(board, player);

            // ��ÿ����ʼ���ӿ�ʼDFS����
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
        /// ��ȡָ����ҵ���ʼ�߸���
        /// </summary>
        private List<HexCellState> GetStartingCells(HexCellState[,] board, PlayerType player)
        {
            var startingCells = new List<HexCellState>();
            int rows = board.GetLength(0);
            int cols = board.GetLength(1);

            if (player == PlayerType.Human)
            {
                // ������ң���߽絽�ұ߽磬����߽翪ʼ
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
                // AI��ң��ϱ߽絽�±߽磬���ϱ߽翪ʼ
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
        /// �������������Ѱ����ͨ·��
        /// </summary>
        private bool DFS(HexCellState[,] board, HexCellState currentCell, PlayerType player,
                        bool[,] visited, List<Vector2Int> path)
        {
            int rows = board.GetLength(0);
            int cols = board.GetLength(1);

            // ��ǵ�ǰ����Ϊ�ѷ���
            visited[currentCell.x, currentCell.y] = true;
            path.Add(new Vector2Int(currentCell.x, currentCell.y));

            // ����Ƿ񵽴�Ŀ��߽�
            if (HasReachedTargetBoundary(currentCell, player, rows, cols))
            {
                LogDebug($"Player {player} reached target boundary at ({currentCell.x}, {currentCell.y})");
                return true;
            }

            // �����������ڸ���
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

            // ���ݣ��������·����ͨ����·�����Ƴ���ǰ����
            path.RemoveAt(path.Count - 1);
            return false;
        }

        /// <summary>
        /// ����Ƿ񵽴�Ŀ��߽�
        /// </summary>
        private bool HasReachedTargetBoundary(HexCellState cell, PlayerType player, int rows, int cols)
        {
            if (player == PlayerType.Human)
            {
                // ���������Ҫ�����ұ߽�
                return cell.y == cols - 1;
            }
            else // PlayerType.AI
            {
                // AI��Ҫ�����±߽�
                return cell.x == rows - 1;
            }
        }

        /// <summary>
        /// ��ȡָ�����ӵ�������Ч���ڸ���
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

                // ���߽�
                if (newX >= 0 && newX < rows && newY >= 0 && newY < cols)
                {
                    neighbors.Add(board[newX, newY]);
                }
            }

            return neighbors;
        }

        #endregion

        #region ��������

        /// <summary>
        /// ��������Ƿ�����
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
        /// ��������ռ����
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
        /// ��ȡָ����ҵ�������ͨ����
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
        /// ̽����ͨ����
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

        #region �����ӿڷ���

        /// <summary>
        /// ����Ƿ����Ǳ�ڵĻ�ʤ��в
        /// </summary>
        public bool HasWinningThreat(HexCellState[,] board, PlayerType player, out Vector2Int threatPosition)
        {
            threatPosition = Vector2Int.zero;
            int rows = board.GetLength(0);
            int cols = board.GetLength(1);

            // �������п�λ�������ø���ҵ������Ƿ���������ʤ
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    if (!board[i, j].isOccupied)
                    {
                        // ��ʱ��������
                        board[i, j].SetOccupied(player);

                        // ����Ƿ��ʤ
                        bool isWinning = HasPlayerWon(board, player);

                        // ������ʱ����
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
        /// ��ȡ��Ϸ��������
        /// </summary>
        public string GetGameProgressInfo(GameState gameState)
        {
            if (gameState == null) return "No game state";

            var occupancyRate = GetBoardOccupancyRate(gameState.Board);
            var humanRegions = GetPlayerConnectedRegions(gameState.Board, PlayerType.Human);
            var aiRegions = GetPlayerConnectedRegions(gameState.Board, PlayerType.AI);

            return $"������Ϣ:\n" +
                   $"- ����ռ����: {occupancyRate * 100:F1}%\n" +
                   $"- ���ƶ���: {gameState.TotalMoves}\n" +
                   $"- ������ͨ����: {humanRegions.Count}\n" +
                   $"- AI��ͨ����: {aiRegions.Count}\n" +
                   $"- ��Ϸʱ��: {gameState.GameDuration:mm\\:ss}";
        }

        #endregion

        #region ���Ժ���־

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
        /// ���ӻ���ʤ·���������ã�
        /// </summary>
        public void VisualizeWinningPath(List<Vector2Int> path)
        {
            if (!visualizeWinningPath || path == null || path.Count == 0)
                return;

            LogDebug($"��ʤ·��: {string.Join(" -> ", path)}");

            // ���������Ӿ���Ŀ��ӻ��߼��������������
            foreach (var position in path)
            {
                LogDebug($"��ʤ·���ڵ�: ({position.x}, {position.y})");
            }
        }

        #endregion
    }
}