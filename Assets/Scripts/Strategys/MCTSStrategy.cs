// MCTSStrategy.cs
using UnityEngine;
using System.Collections.Generic;

public class MCTSStrategy : IAIStrategy
{
    /// <summary>
    /// ���ؿ���ģ����������ڹ���ʱ���ⲿ����
    /// </summary>
    public int simulations = 100;

    /// <summary>
    /// MCTS ����һ����ʤ/�ط��Ľ������ӣ�ʼ�շ��� null
    /// </summary>
    public HexCell GetImmediateMove(HexCell[,] board)
    {
        return null;
    }

    /// <summary>
    /// �Ե����ո�������֣����ж�����ģ�⣬ͳ�� AI����� 1����ʤ��
    /// </summary>
    public float EvaluateCell(HexCell[,] board, HexCell cell)
    {
        int winCount = 0;
        for (int k = 0; k < simulations; k++)
        {
            // ��¡����״̬�� HexCellState ����
            var boardCopy = CloneBoard(board);
            // �� clone ��ģ������
            var simMove = boardCopy[cell.x, cell.y];
            PlayRandomGame(boardCopy, simMove, 1);
            // ��� AI��1���Ƿ��ʤ
            if (HasPlayerWon(boardCopy, 1))
                winCount++;
        }
        // ʤ����Ϊ�÷�
        return (float)winCount / simulations;
    }

    #region ���� ������ MCTS �ĸ������� ���� 

    private HexCellState[,] CloneBoard(HexCell[,] original)
    {
        int rows = original.GetLength(0);
        int cols = original.GetLength(1);
        var copy = new HexCellState[rows, cols];
        for (int i = 0; i < rows; i++)
            for (int j = 0; j < cols; j++)
            {
                var c = original[i, j];
                copy[i, j] = new HexCellState(c.x, c.y, c.isOccupied, c.occupiedBy);
            }
        return copy;
    }

    private void PlayRandomGame(HexCellState[,] board, HexCellState move, int player)
    {
        // Ӧ�ó�ʼ����
        board[move.x, move.y].isOccupied = true;
        board[move.x, move.y].occupiedBy = player;

        int current = (player == 0) ? 1 : 0;
        // �����ȫֱ����Ϸ����
        while (!IsGameOver(board))
        {
            var avail = GetAvailableMoves(board);
            if (avail.Count == 0) break;
            var nxt = avail[Random.Range(0, avail.Count)];
            nxt.isOccupied = true;
            nxt.occupiedBy = current;
            current = (current == 0) ? 1 : 0;
        }
    }

    private bool HasPlayerWon(HexCellState[,] board, int player)
    {
        int rows = board.GetLength(0);
        int cols = board.GetLength(1);
        bool[,] seen = new bool[rows, cols];
        var starts = GetStartingCells(board, player);
        foreach (var s in starts)
            if (DFS(board, s, player, seen))
                return true;
        return false;
    }

    private bool IsGameOver(HexCellState[,] board)
    {
        return HasPlayerWon(board, 0) || HasPlayerWon(board, 1);
    }

    private List<HexCellState> GetAvailableMoves(HexCellState[,] board)
    {
        var list = new List<HexCellState>();
        int rows = board.GetLength(0);
        int cols = board.GetLength(1);
        for (int i = 0; i < rows; i++)
            for (int j = 0; j < cols; j++)
                if (!board[i, j].isOccupied)
                    list.Add(board[i, j]);
        return list;
    }

    private List<HexCellState> GetStartingCells(HexCellState[,] board, int player)
    {
        var list = new List<HexCellState>();
        int rows = board.GetLength(0), cols = board.GetLength(1);
        if (player == 0)
        {
            // �����
            for (int i = 0; i < rows; i++)
                if (board[i, 0].occupiedBy == player)
                    list.Add(board[i, 0]);
        }
        else
        {
            // �ϡ���
            for (int j = 0; j < cols; j++)
                if (board[0, j].occupiedBy == player)
                    list.Add(board[0, j]);
        }
        return list;
    }

    private bool DFS(HexCellState[,] board, HexCellState cell, int player, bool[,] visited)
    {
        int rows = board.GetLength(0), cols = board.GetLength(1);
        if ((player == 0 && cell.y == cols - 1) ||
            (player == 1 && cell.x == rows - 1))
            return true;

        visited[cell.x, cell.y] = true;
        foreach (var n in GetNeighbors(board, cell))
        {
            if (!visited[n.x, n.y] && n.occupiedBy == player)
                if (DFS(board, n, player, visited))
                    return true;
        }
        return false;
    }

    private List<HexCellState> GetNeighbors(HexCellState[,] board, HexCellState cell)
    {
        var list = new List<HexCellState>();
        int[,] dirs = new int[,] { { -1, 0 }, { -1, 1 }, { 0, -1 }, { 0, 1 }, { 1, -1 }, { 1, 0 } };
        int rows = board.GetLength(0), cols = board.GetLength(1);
        for (int i = 0; i < 6; i++)
        {
            int nx = cell.x + dirs[i, 0], ny = cell.y + dirs[i, 1];
            if (nx >= 0 && nx < rows && ny >= 0 && ny < cols)
                list.Add(board[nx, ny]);
        }
        return list;
    }

    #endregion
}
