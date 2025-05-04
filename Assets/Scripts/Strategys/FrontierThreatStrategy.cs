using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 只基于“前沿空格”检测一步必胜／必防的策略。
/// 配合 CompositeAIStrategy 使用，将作为最高优先级的紧急落子策略。
/// </summary>
public class FrontierThreatStrategy : IAIStrategy
{
    private HexCell[,] board;
    private int rows, cols;

    // 六个方向：左上、右上、左下、右下、正左、正右
    private static readonly Vector2Int[] neighborDirs = new Vector2Int[]
    {
        new Vector2Int(-1, 0),  new Vector2Int(-1, 1),
        new Vector2Int(0, -1),  new Vector2Int(0, 1),
        new Vector2Int(1, -1),  new Vector2Int(1, 0)
    };

    // 分别记录 AI 和玩家的 frontier 空格
    private HashSet<HexCell> aiFrontier = new HashSet<HexCell>();
    private HashSet<HexCell> playerFrontier = new HashSet<HexCell>();

    public FrontierThreatStrategy(HexCell[,] board)
    {
        this.board = board;
        rows = board.GetLength(0);
        cols = board.GetLength(1);
        // 初始化：把已有棋子的相邻空格加进 frontier
        for (int i = 0; i < rows; i++)
            for (int j = 0; j < cols; j++)
                if (board[i, j].isOccupied)
                    UpdateFrontier(board[i, j]);
    }

    // IAIStrategy 接口：不使用此方法
    //public HexCell GetBestMove(HexCell[,] board, int simulations) => null;

    // IAIStrategy 接口：紧急落子检测
    public HexCell GetImmediateMove(HexCell[,] board)
    {
        // 1) AI 一步赢？
        var win = FindWinningMove(1, aiFrontier);
        if (win != null) return win;

        // 2) 阻断玩家一步赢？
        var block = FindWinningMove(0, playerFrontier);
        if (block != null) return block;

        return null;
    }

    /// <summary>
    /// 外部在每次真正落子后（无论 AI 还是玩家），都要调用此方法：
    ///  - 将 placed 从对应 frontier 中移除  
    ///  - 然后把它周围的新 frontier 空格添加进来  
    /// </summary>
    public void OnPlaced(HexCell placed)
    {
        // 已被占的格子不再是 frontier
        aiFrontier.Remove(placed);
        playerFrontier.Remove(placed);

        // 放到对应一方的 frontier 更新逻辑
        UpdateFrontier(placed);
    }

    // 把一个新落子周围的所有空格，添加到它对应一方的 frontier 中
    private void UpdateFrontier(HexCell placed)
    {
        var targetSet = placed.occupiedBy == 1 ? aiFrontier : playerFrontier;
        foreach (var d in neighborDirs)
        {
            int nx = placed.x + d.x, ny = placed.y + d.y;
            if (nx >= 0 && nx < rows && ny >= 0 && ny < cols)
            {
                var c = board[nx, ny];
                if (!c.isOccupied)
                    targetSet.Add(c);
            }
        }
    }

    // 在 frontier 中模拟一步落子，检查 player 是否能连通两边
    private HexCell FindWinningMove(int player, HashSet<HexCell> frontier)
    {
        foreach (var cell in frontier)
        {
            // 临时克隆当前状态
            var state = CloneBoard(board);
            state[cell.x, cell.y].isOccupied = true;
            state[cell.x, cell.y].occupiedBy = player;

            if (HasPlayerWon(state, player))
                return cell;
        }
        return null;
    }

    // 克隆到 HexCellState 数组，避免破坏原始 board
    private HexCellState[,] CloneBoard(HexCell[,] original)
    {
        var copy = new HexCellState[rows, cols];
        for (int i = 0; i < rows; i++)
            for (int j = 0; j < cols; j++)
                copy[i, j] = new HexCellState(
                    original[i, j].x,
                    original[i, j].y,
                    original[i, j].isOccupied,
                    original[i, j].occupiedBy
                );
        return copy;
    }

    // 深度优先判断 player 是否已连通两边（复用 MCTSStrategy 的逻辑）
    private bool HasPlayerWon(HexCellState[,] state, int player)
    {
        bool[,] seen = new bool[rows, cols];
        var stack = new Stack<HexCellState>();

        // 起始边：player=0 左→右，player=1 上→下
        if (player == 0)
        {
            for (int i = 0; i < rows; i++)
                if (state[i, 0].isOccupied && state[i, 0].occupiedBy == player)
                    stack.Push(state[i, 0]);
        }
        else
        {
            for (int j = 0; j < cols; j++)
                if (state[0, j].isOccupied && state[0, j].occupiedBy == player)
                    stack.Push(state[0, j]);
        }

        while (stack.Count > 0)
        {
            var cell = stack.Pop();
            if (seen[cell.x, cell.y]) continue;
            seen[cell.x, cell.y] = true;

            // 到达目标边即胜
            if ((player == 0 && cell.y == cols - 1) ||
                (player == 1 && cell.x == rows - 1))
                return true;

            foreach (var d in neighborDirs)
            {
                int nx = cell.x + d.x, ny = cell.y + d.y;
                if (nx >= 0 && nx < rows && ny >= 0 && ny < cols)
                {
                    var nxt = state[nx, ny];
                    if (!seen[nx, ny] && nxt.isOccupied && nxt.occupiedBy == player)
                        stack.Push(nxt);
                }
            }
        }

        return false;
    }
    public float EvaluateCell(HexCell[,] board, HexCell cell)
    {
        return 0f;
    }
}
