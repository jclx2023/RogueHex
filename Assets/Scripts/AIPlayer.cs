// AIPlayer.cs
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// AIPlayer ͨ�� CompositeAIStrategy ��϶��ֲ������������ӣ�ͬʱ��������Ϸ���������߼���
/// </summary>
public class AIPlayer : MonoBehaviour
{
    private BoardManager boardManager;

    // ��ǰʹ�õ���ϲ��ԣ�������ʵ���� IAIStrategy �Ĳ��԰�Ȩ�������һ��
    public CompositeAIStrategy compositeStrategy { get; private set; }

    // ģ����������� MCTS �Ȳ��ԣ����ɸ�����Ҫ����
    public int MTCLMoves = 100;

    void Start()
    {
        boardManager = FindObjectOfType<BoardManager>();

        // ��ʼ����ϲ��ԣ�����Ӹ����Ӳ��Լ���Ȩ��
        compositeStrategy = new CompositeAIStrategy();
        compositeStrategy.AddStrategy(new MCTSStrategy(), 1.4f);
        compositeStrategy.AddStrategy(new DoubleThreatStrategy(), 2.5f);
        compositeStrategy.AddStrategy(new HeuristicStrategy(), 2.0f);
        // ������Ҫ����������� RandomStrategy ����������
    }

    /// <summary>
    /// AI �������Ӿ��ߣ�����ѡ�е����ӱ��Ϊ AI��ռ���߱�� 1����
    /// </summary>
    public void MakeMove()
    {
        HexCell[,] board = boardManager.GetBoard();
        HexCell bestMove = compositeStrategy.GetBestMove(board, MTCLMoves);
        if (bestMove != null)
        {
            bestMove.SetOccupied(1); // 1 ��ʾ AI ����
            Debug.Log($"AI chooses to place at ({bestMove.x}, {bestMove.y}) using composite strategy.");
        }
        else
        {
            Debug.Log("AI did not find a valid move.");
        }
    }

    #region ��Ϸ��������߼�

    /// <summary>
    /// �ж�ָ������Ƿ��Ѿ���ʤ������ Hex ����ͨ�Թ��򣩡�
    /// ������� 0��ʤ������Ϊ����������Ҳࣻ������� 1��ʤ������Ϊ���ϲ������²ࡣ
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
    /// ������ұ�Ż�ȡ��ʼ�߽�����̵�Ԫ��
    /// ��� 0 ����࿪ʼ����� 1 ���ϲ࿪ʼ��
    /// </summary>
    public List<HexCellState> GetStartingCells(HexCellState[,] board, int player)
    {
        List<HexCellState> startCells = new List<HexCellState>();
        int rows = board.GetLength(0);
        int cols = board.GetLength(1);
        if (player == 0)
        {
            // ��� 0�����ÿһ�������ĵ�Ԫ
            for (int i = 0; i < rows; i++)
            {
                if (board[i, 0].occupiedBy == player)
                    startCells.Add(board[i, 0]);
            }
        }
        else if (player == 1)
        {
            // ��� 1�����ÿһ�����ϲ�ĵ�Ԫ
            for (int j = 0; j < cols; j++)
            {
                if (board[0, j].occupiedBy == player)
                    startCells.Add(board[0, j]);
            }
        }
        return startCells;
    }

    /// <summary>
    /// ʹ���������������DFS���������Ƿ�ͨ����ͨ��ʼ�߽��Ŀ��߽����ʤ��
    /// </summary>
    private bool DFS(HexCellState[,] board, HexCellState cell, int player, bool[,] visited)
    {
        int rows = board.GetLength(0);
        int cols = board.GetLength(1);
        int x = cell.x;
        int y = cell.y;

        // ��� 0 ��ʤ�������������Ҳ�߽磻��� 1 ��ʤ�������������²�߽�
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
    /// ��ȡ��ǰ���̵�Ԫ�� Hex ���е����ڵ�Ԫ���������򣩡�
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
