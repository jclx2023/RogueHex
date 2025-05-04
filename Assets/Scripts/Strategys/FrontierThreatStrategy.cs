using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ֻ���ڡ�ǰ�ؿո񡱼��һ����ʤ���ط��Ĳ��ԡ�
/// ��� CompositeAIStrategy ʹ�ã�����Ϊ������ȼ��Ľ������Ӳ��ԡ�
/// </summary>
public class FrontierThreatStrategy : IAIStrategy
{
    private HexCell[,] board;
    private int rows, cols;

    // �����������ϡ����ϡ����¡����¡���������
    private static readonly Vector2Int[] neighborDirs = new Vector2Int[]
    {
        new Vector2Int(-1, 0),  new Vector2Int(-1, 1),
        new Vector2Int(0, -1),  new Vector2Int(0, 1),
        new Vector2Int(1, -1),  new Vector2Int(1, 0)
    };

    // �ֱ��¼ AI ����ҵ� frontier �ո�
    private HashSet<HexCell> aiFrontier = new HashSet<HexCell>();
    private HashSet<HexCell> playerFrontier = new HashSet<HexCell>();

    public FrontierThreatStrategy(HexCell[,] board)
    {
        this.board = board;
        rows = board.GetLength(0);
        cols = board.GetLength(1);
        // ��ʼ�������������ӵ����ڿո�ӽ� frontier
        for (int i = 0; i < rows; i++)
            for (int j = 0; j < cols; j++)
                if (board[i, j].isOccupied)
                    UpdateFrontier(board[i, j]);
    }

    // IAIStrategy �ӿڣ���ʹ�ô˷���
    //public HexCell GetBestMove(HexCell[,] board, int simulations) => null;

    // IAIStrategy �ӿڣ��������Ӽ��
    public HexCell GetImmediateMove(HexCell[,] board)
    {
        // 1) AI һ��Ӯ��
        var win = FindWinningMove(1, aiFrontier);
        if (win != null) return win;

        // 2) ������һ��Ӯ��
        var block = FindWinningMove(0, playerFrontier);
        if (block != null) return block;

        return null;
    }

    /// <summary>
    /// �ⲿ��ÿ���������Ӻ����� AI ������ң�����Ҫ���ô˷�����
    ///  - �� placed �Ӷ�Ӧ frontier ���Ƴ�  
    ///  - Ȼ�������Χ���� frontier �ո���ӽ���  
    /// </summary>
    public void OnPlaced(HexCell placed)
    {
        // �ѱ�ռ�ĸ��Ӳ����� frontier
        aiFrontier.Remove(placed);
        playerFrontier.Remove(placed);

        // �ŵ���Ӧһ���� frontier �����߼�
        UpdateFrontier(placed);
    }

    // ��һ����������Χ�����пո���ӵ�����Ӧһ���� frontier ��
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

    // �� frontier ��ģ��һ�����ӣ���� player �Ƿ�����ͨ����
    private HexCell FindWinningMove(int player, HashSet<HexCell> frontier)
    {
        foreach (var cell in frontier)
        {
            // ��ʱ��¡��ǰ״̬
            var state = CloneBoard(board);
            state[cell.x, cell.y].isOccupied = true;
            state[cell.x, cell.y].occupiedBy = player;

            if (HasPlayerWon(state, player))
                return cell;
        }
        return null;
    }

    // ��¡�� HexCellState ���飬�����ƻ�ԭʼ board
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

    // ��������ж� player �Ƿ�����ͨ���ߣ����� MCTSStrategy ���߼���
    private bool HasPlayerWon(HexCellState[,] state, int player)
    {
        bool[,] seen = new bool[rows, cols];
        var stack = new Stack<HexCellState>();

        // ��ʼ�ߣ�player=0 ����ң�player=1 �ϡ���
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

            // ����Ŀ��߼�ʤ
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
