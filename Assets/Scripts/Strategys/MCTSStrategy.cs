// MCTSStrategy.cs
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 使用蒙特卡罗树搜索策略选择最佳落子位置。
/// 该策略会对每个未占据的单元进行多次随机游戏模拟，统计 AI 获胜的比率，
/// 最后选择获胜率最高的移动。
/// </summary>
public class MCTSStrategy : IAIStrategy
{
    public HexCell GetBestMove(HexCell[,] board, int simulations)
    {
        HexCell bestMove = null;
        float bestWinRatio = -1f;

        int rows = board.GetLength(0);
        int cols = board.GetLength(1);

        // 遍历棋盘上所有未占据的格子
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                HexCell cell = board[i, j];
                if (!cell.isOccupied)
                {
                    int winCount = 0;
                    // 对当前落子位置进行多次模拟
                    for (int k = 0; k < simulations; k++)
                    {
                        // 克隆当前棋盘状态（将 HexCell 转换为 HexCellState 数组）
                        HexCellState[,] boardCopy = CloneBoard(board);
                        // 获取当前待模拟的移动对应的状态
                        HexCellState simulatedMove = boardCopy[cell.x, cell.y];
                        // 模拟随机游戏，AI 为玩家 1
                        PlayRandomGame(boardCopy, simulatedMove, 1);
                        // 模拟结束后检查 AI 是否获胜
                        if (HasPlayerWon(boardCopy, 1))
                        {
                            winCount++;
                        }
                    }
                    float winRatio = (float)winCount / simulations;
                    if (winRatio > bestWinRatio)
                    {
                        bestWinRatio = winRatio;
                        bestMove = cell;
                    }
                }
            }
        }
        return bestMove;
    }

    /// <summary>
    /// 克隆棋盘状态，将 HexCell[,] 转换为 HexCellState[,]，便于模拟过程中不影响原始数据。
    /// </summary>
    private HexCellState[,] CloneBoard(HexCell[,] original)
    {
        int rows = original.GetLength(0);
        int cols = original.GetLength(1);
        HexCellState[,] boardCopy = new HexCellState[rows, cols];

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                HexCell cell = original[i, j];
                boardCopy[i, j] = new HexCellState(cell.x, cell.y, cell.isOccupied, cell.occupiedBy);
            }
        }
        return boardCopy;
    }

    /// <summary>
    /// 模拟一局随机游戏，从指定移动开始，双方交替落子直到游戏结束。
    /// </summary>
    /// <param name="board">当前棋盘状态</param>
    /// <param name="move">起始移动</param>
    /// <param name="player">当前玩家（对起始移动而言）</param>
    private void PlayRandomGame(HexCellState[,] board, HexCellState move, int player)
    {
        // 应用初始移动
        board[move.x, move.y].isOccupied = true;
        board[move.x, move.y].occupiedBy = player;

        // 切换到另一玩家
        int currentPlayer = SwitchPlayer(player);

        // 随机落子直到游戏结束
        while (!IsGameOver(board))
        {
            List<HexCellState> availableMoves = GetAvailableMoves(board);
            if (availableMoves.Count == 0)
                break;
            // 随机选择一个可用位置
            HexCellState randomMove = availableMoves[Random.Range(0, availableMoves.Count)];
            randomMove.isOccupied = true;
            randomMove.occupiedBy = currentPlayer;
            currentPlayer = SwitchPlayer(currentPlayer);
        }
    }

    /// <summary>
    /// 检查指定玩家是否在当前棋盘状态下获胜。
    /// 对于玩家 0，胜利条件为连接左侧到右侧；
    /// 对于玩家 1，胜利条件为连接上侧到下侧。
    /// </summary>
    private bool HasPlayerWon(HexCellState[,] board, int player)
    {
        int rows = board.GetLength(0);
        int cols = board.GetLength(1);
        bool[,] visited = new bool[rows, cols];
        List<HexCellState> startCells = GetStartingCells(board, player);

        foreach (HexCellState startCell in startCells)
        {
            if (DFS(board, startCell, player, visited))
                return true;
        }
        return false;
    }

    /// <summary>
    /// 判断棋局是否结束，即任一玩家获胜。
    /// </summary>
    private bool IsGameOver(HexCellState[,] board)
    {
        return HasPlayerWon(board, 0) || HasPlayerWon(board, 1);
    }

    /// <summary>
    /// 切换玩家（假定只有玩家 0 和 1）。
    /// </summary>
    private int SwitchPlayer(int currentPlayer)
    {
        return (currentPlayer == 0) ? 1 : 0;
    }

    /// <summary>
    /// 获取棋盘中所有未被占据的单元。
    /// </summary>
    private List<HexCellState> GetAvailableMoves(HexCellState[,] board)
    {
        List<HexCellState> availableMoves = new List<HexCellState>();
        int rows = board.GetLength(0);
        int cols = board.GetLength(1);
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                if (!board[i, j].isOccupied)
                {
                    availableMoves.Add(board[i, j]);
                }
            }
        }
        return availableMoves;
    }

    /// <summary>
    /// 根据 Hex 棋规则，获取指定玩家的起始边界单元，用于 DFS 搜索判断是否获胜。
    /// 玩家 0 从左侧开始，目标为右侧；玩家 1 从上侧开始，目标为下侧。
    /// </summary>
    private List<HexCellState> GetStartingCells(HexCellState[,] board, int player)
    {
        List<HexCellState> startCells = new List<HexCellState>();
        int rows = board.GetLength(0);
        int cols = board.GetLength(1);
        if (player == 0)
        {
            // 玩家 0：检查每一行最左侧的单元
            for (int i = 0; i < rows; i++)
            {
                if (board[i, 0].occupiedBy == player)
                    startCells.Add(board[i, 0]);
            }
        }
        else if (player == 1)
        {
            // 玩家 1：检查每一列最上侧的单元
            for (int j = 0; j < cols; j++)
            {
                if (board[0, j].occupiedBy == player)
                    startCells.Add(board[0, j]);
            }
        }
        return startCells;
    }

    /// <summary>
    /// 使用深度优先搜索检测玩家是否通过连接起始边界和目标边界而获胜。
    /// </summary>
    private bool DFS(HexCellState[,] board, HexCellState cell, int player, bool[,] visited)
    {
        int rows = board.GetLength(0);
        int cols = board.GetLength(1);
        int x = cell.x;
        int y = cell.y;

        // 玩家 0 的胜利条件：到达右侧边界
        if (player == 0 && y == cols - 1)
            return true;
        // 玩家 1 的胜利条件：到达下侧边界
        if (player == 1 && x == rows - 1)
            return true;

        visited[x, y] = true;
        List<HexCellState> neighbors = GetNeighbors(board, cell);
        foreach (HexCellState neighbor in neighbors)
        {
            if (!visited[neighbor.x, neighbor.y] && neighbor.occupiedBy == player)
            {
                if (DFS(board, neighbor, player, visited))
                    return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 获取当前单元格在棋盘中的相邻单元（按照 Hex 棋的六个方向）。
    /// </summary>
    private List<HexCellState> GetNeighbors(HexCellState[,] board, HexCellState cell)
    {
        List<HexCellState> neighbors = new List<HexCellState>();
        int rows = board.GetLength(0);
        int cols = board.GetLength(1);
        int x = cell.x;
        int y = cell.y;
        // 定义六个方向：{-1,0}, {-1,1}, {0,-1}, {0,1}, {1,-1}, {1,0}
        int[,] directions = new int[,] {
            {-1, 0}, {-1, 1}, {0, -1}, {0, 1}, {1, -1}, {1, 0}
        };

        for (int i = 0; i < 6; i++)
        {
            int newX = x + directions[i, 0];
            int newY = y + directions[i, 1];
            if (newX >= 0 && newX < rows && newY >= 0 && newY < cols)
            {
                neighbors.Add(board[newX, newY]);
            }
        }
        return neighbors;
    }
}
