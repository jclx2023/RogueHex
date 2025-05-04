// AIPlayer.cs
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// AIPlayer 通过 CompositeAIStrategy 组合多种策略来决策落子，同时还包含游戏结束检测的逻辑。
/// </summary>
public class AIPlayer : MonoBehaviour
{
    private BoardManager boardManager;

    // 当前使用的组合策略，将各个实现了 IAIStrategy 的策略按权重组合在一起
    public CompositeAIStrategy compositeStrategy { get; private set; }

    // 模拟次数（用于 MCTS 等策略），可根据需要调整
    public int MTCLMoves = 100;

    void Start()
    {
        boardManager = FindObjectOfType<BoardManager>();

        // 初始化组合策略，并添加各个子策略及其权重
        compositeStrategy = new CompositeAIStrategy();
        compositeStrategy.AddStrategy(new MCTSStrategy(), 1.4f);
        compositeStrategy.AddStrategy(new DoubleThreatStrategy(), 2.5f);
        compositeStrategy.AddStrategy(new HeuristicStrategy(), 2.0f);
        // 如有需要，还可以添加 RandomStrategy 或其他策略
    }

    /// <summary>
    /// AI 进行落子决策，并将选中的落子标记为 AI（占据者编号 1）。
    /// </summary>
    public void MakeMove()
    {
        HexCell[,] board = boardManager.GetBoard();
        HexCell bestMove = compositeStrategy.GetBestMove(board, MTCLMoves);
        if (bestMove != null)
        {
            bestMove.SetOccupied(1); // 1 表示 AI 落子
            Debug.Log($"AI chooses to place at ({bestMove.x}, {bestMove.y}) using composite strategy.");
        }
        else
        {
            Debug.Log("AI did not find a valid move.");
        }
    }

    #region 游戏结束检测逻辑

    /// <summary>
    /// 判断指定玩家是否已经获胜（根据 Hex 棋连通性规则）。
    /// 对于玩家 0，胜利条件为从左侧连到右侧；对于玩家 1，胜利条件为从上侧连到下侧。
    /// </summary>
    public bool HasPlayerWon(HexCellState[,] board, int player)
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
    /// 根据玩家编号获取起始边界的棋盘单元：
    /// 玩家 0 从左侧开始；玩家 1 从上侧开始。
    /// </summary>
    public List<HexCellState> GetStartingCells(HexCellState[,] board, int player)
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
    /// 使用深度优先搜索（DFS）检测玩家是否通过连通起始边界和目标边界而获胜。
    /// </summary>
    private bool DFS(HexCellState[,] board, HexCellState cell, int player, bool[,] visited)
    {
        int rows = board.GetLength(0);
        int cols = board.GetLength(1);
        int x = cell.x;
        int y = cell.y;

        // 玩家 0 的胜利条件：到达右侧边界；玩家 1 的胜利条件：到达下侧边界
        if ((player == 0 && y == cols - 1) || (player == 1 && x == rows - 1))
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
    /// 获取当前棋盘单元在 Hex 棋中的相邻单元（六个方向）。
    /// </summary>
    private List<HexCellState> GetNeighbors(HexCellState[,] board, HexCellState cell)
    {
        List<HexCellState> neighbors = new List<HexCellState>();
        int rows = board.GetLength(0);
        int cols = board.GetLength(1);
        int x = cell.x;
        int y = cell.y;
        int[,] directions = new int[,]
        {
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

    #endregion
}
