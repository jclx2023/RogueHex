// AIPlayer.cs
using UnityEngine;
using System.Collections.Generic;

public class AIPlayer : MonoBehaviour
{
    private BoardManager boardManager;

    // 上次玩家落子，用于 BoardEvaluator
    private HexCell lastPlayerMove;

    // 极大化分数：一步胜利／阻断
    private const float WIN_SCORE = 10000f;
    private const float BLOCK_SCORE = 9000f;

    void Start()
    {
        boardManager = FindObjectOfType<BoardManager>();
    }

    /// <summary>
    /// 在玩家落子后必须调用，用于记录 lastPlayerMove
    /// </summary>
    public void OnPlayerMove(HexCell playerCell)
    {
        lastPlayerMove = playerCell;
        Debug.Log($"[AIPlayer] OnPlayerMove ← ({playerCell.x},{playerCell.y})");
    }


    /// <summary>
    /// AI 全盘打分决策：扫描所有空格，打分选最高分落子
    /// </summary>
    public void MakeMove()
    {
        HexCell[,] board = boardManager.GetBoard();
        int rows = board.GetLength(0), cols = board.GetLength(1);

        // 收集所有可落子格
        var available = new List<HexCell>();
        for (int i = 0; i < rows; i++)
            for (int j = 0; j < cols; j++)
                if (!board[i, j].isOccupied)
                    available.Add(board[i, j]);

        if (available.Count == 0)
        {
            Debug.LogWarning("AI: no available moves.");
            return;
        }

        // 1）上次玩家落子周边得分
        var evaluator = new BoardEvaluator(board, lastPlayerMove);
        var evalMap = evaluator.EvaluateBoard();

        HexCell bestCell = null;
        float bestScore = float.MinValue;

        // 对每个空格打分
        foreach (var cell in available)
        {
            float score = 0f;

            // —— 一步必胜检测 ——
            var simWin = CloneBoardState(board);
            ApplyMove(simWin, cell, player: 1);
            if (HasPlayerWon(simWin, player: 1))
            {
                score += WIN_SCORE;
            }
            else
            {
                // —— 一步必防检测 ——
                var simBlock = CloneBoardState(board);
                ApplyMove(simBlock, cell, player: 0);
                if (HasPlayerWon(simBlock, player: 0))
                    score += BLOCK_SCORE;
            }

            // —— 上次玩家落子周边得分 ——
            if (evalMap.TryGetValue(cell, out float s))
                score += s;

            // 选择最高分
            if (score > bestScore)
            {
                bestScore = score;
                bestCell = cell;
            }
        }

        // 最终落子
        if (bestCell != null)
        {
            bestCell.SetOccupied(1);
            Debug.Log($"AI places at ({bestCell.x},{bestCell.y}) score={bestScore:F1}");
            Debug.Log($"lastPlayerMove = {lastPlayerMove?.x},{lastPlayerMove?.y}, evalMap count = {evalMap.Count}");
            foreach (var kv in evalMap)
                Debug.Log($"  neighbor {kv.Key.x},{kv.Key.y} => {kv.Value}");

        }
    }

    #region —— 模拟 & 克隆 全盘状态 ——
    private HexCellState[,] CloneBoardState(HexCell[,] original)
    {
        int r = original.GetLength(0), c = original.GetLength(1);
        var copy = new HexCellState[r, c];
        for (int i = 0; i < r; i++)
            for (int j = 0; j < c; j++)
            {
                var src = original[i, j];
                copy[i, j] = new HexCellState(src.x, src.y, src.isOccupied, src.occupiedBy);
            }
        return copy;
    }

    private void ApplyMove(HexCellState[,] state, HexCell cell, int player)
    {
        state[cell.x, cell.y].isOccupied = true;
        state[cell.x, cell.y].occupiedBy = player;
    }
    #endregion

    #region —— 胜利检测：DFS连通 ——
    public bool HasPlayerWon(HexCellState[,] boardState, int player)
    {
        int r = boardState.GetLength(0), c = boardState.GetLength(1);
        bool[,] visited = new bool[r, c];
        var starts = GetStartingCells(boardState, player);
        foreach (var sc in starts)
            if (DFS(boardState, sc, player, visited))
                return true;
        return false;
    }

    private List<HexCellState> GetStartingCells(HexCellState[,] boardState, int player)
    {
        int r = boardState.GetLength(0), c = boardState.GetLength(1);
        var list = new List<HexCellState>();
        if (player == 0)
        {
            // 玩家 0 左→右
            for (int i = 0; i < r; i++)
                if (boardState[i, 0].occupiedBy == player)
                    list.Add(boardState[i, 0]);
        }
        else
        {
            // 玩家 1 上→下
            for (int j = 0; j < c; j++)
                if (boardState[0, j].occupiedBy == player)
                    list.Add(boardState[0, j]);
        }
        return list;
    }

    private bool DFS(HexCellState[,] boardState, HexCellState cell, int player, bool[,] visited)
    {
        int r = boardState.GetLength(0), c = boardState.GetLength(1);
        // 到达对面边即胜
        if ((player == 0 && cell.y == c - 1) ||
            (player == 1 && cell.x == r - 1))
            return true;

        visited[cell.x, cell.y] = true;
        foreach (var n in GetNeighbors(boardState, cell))
        {
            if (!visited[n.x, n.y] && n.occupiedBy == player)
                if (DFS(boardState, n, player, visited))
                    return true;
        }
        return false;
    }

    private List<HexCellState> GetNeighbors(HexCellState[,] boardState, HexCellState cell)
    {
        int[,] dirs = new int[,]
        {
            {-1, 0}, {-1, 1},
            { 0,-1}, { 0, 1},
            { 1,-1}, { 1, 0}
        };
        var list = new List<HexCellState>();
        int r = boardState.GetLength(0), c = boardState.GetLength(1);
        for (int i = 0; i < 6; i++)
        {
            int nx = cell.x + dirs[i, 0], ny = cell.y + dirs[i, 1];
            if (nx >= 0 && nx < r && ny >= 0 && ny < c)
                list.Add(boardState[nx, ny]);
        }
        return list;
    }
    #endregion
}
