// MCTSStrategy.cs
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ʹ�����ؿ�������������ѡ���������λ�á�
/// �ò��Ի��ÿ��δռ�ݵĵ�Ԫ���ж�������Ϸģ�⣬ͳ�� AI ��ʤ�ı��ʣ�
/// ���ѡ���ʤ����ߵ��ƶ���
/// </summary>
public class MCTSStrategy : IAIStrategy
{
    public HexCell GetBestMove(HexCell[,] board, int simulations)
    {
        HexCell bestMove = null;
        float bestWinRatio = -1f;

        int rows = board.GetLength(0);
        int cols = board.GetLength(1);

        // ��������������δռ�ݵĸ���
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                HexCell cell = board[i, j];
                if (!cell.isOccupied)
                {
                    int winCount = 0;
                    // �Ե�ǰ����λ�ý��ж��ģ��
                    for (int k = 0; k < simulations; k++)
                    {
                        // ��¡��ǰ����״̬���� HexCell ת��Ϊ HexCellState ���飩
                        HexCellState[,] boardCopy = CloneBoard(board);
                        // ��ȡ��ǰ��ģ����ƶ���Ӧ��״̬
                        HexCellState simulatedMove = boardCopy[cell.x, cell.y];
                        // ģ�������Ϸ��AI Ϊ��� 1
                        PlayRandomGame(boardCopy, simulatedMove, 1);
                        // ģ��������� AI �Ƿ��ʤ
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
    /// ��¡����״̬���� HexCell[,] ת��Ϊ HexCellState[,]������ģ������в�Ӱ��ԭʼ���ݡ�
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
    /// ģ��һ�������Ϸ����ָ���ƶ���ʼ��˫����������ֱ����Ϸ������
    /// </summary>
    /// <param name="board">��ǰ����״̬</param>
    /// <param name="move">��ʼ�ƶ�</param>
    /// <param name="player">��ǰ��ң�����ʼ�ƶ����ԣ�</param>
    private void PlayRandomGame(HexCellState[,] board, HexCellState move, int player)
    {
        // Ӧ�ó�ʼ�ƶ�
        board[move.x, move.y].isOccupied = true;
        board[move.x, move.y].occupiedBy = player;

        // �л�����һ���
        int currentPlayer = SwitchPlayer(player);

        // �������ֱ����Ϸ����
        while (!IsGameOver(board))
        {
            List<HexCellState> availableMoves = GetAvailableMoves(board);
            if (availableMoves.Count == 0)
                break;
            // ���ѡ��һ������λ��
            HexCellState randomMove = availableMoves[Random.Range(0, availableMoves.Count)];
            randomMove.isOccupied = true;
            randomMove.occupiedBy = currentPlayer;
            currentPlayer = SwitchPlayer(currentPlayer);
        }
    }

    /// <summary>
    /// ���ָ������Ƿ��ڵ�ǰ����״̬�»�ʤ��
    /// ������� 0��ʤ������Ϊ������ൽ�Ҳࣻ
    /// ������� 1��ʤ������Ϊ�����ϲൽ�²ࡣ
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
    /// �ж�����Ƿ����������һ��һ�ʤ��
    /// </summary>
    private bool IsGameOver(HexCellState[,] board)
    {
        return HasPlayerWon(board, 0) || HasPlayerWon(board, 1);
    }

    /// <summary>
    /// �л���ң��ٶ�ֻ����� 0 �� 1����
    /// </summary>
    private int SwitchPlayer(int currentPlayer)
    {
        return (currentPlayer == 0) ? 1 : 0;
    }

    /// <summary>
    /// ��ȡ����������δ��ռ�ݵĵ�Ԫ��
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
    /// ���� Hex ����򣬻�ȡָ����ҵ���ʼ�߽絥Ԫ������ DFS �����ж��Ƿ��ʤ��
    /// ��� 0 ����࿪ʼ��Ŀ��Ϊ�Ҳࣻ��� 1 ���ϲ࿪ʼ��Ŀ��Ϊ�²ࡣ
    /// </summary>
    private List<HexCellState> GetStartingCells(HexCellState[,] board, int player)
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
    /// ʹ��������������������Ƿ�ͨ��������ʼ�߽��Ŀ��߽����ʤ��
    /// </summary>
    private bool DFS(HexCellState[,] board, HexCellState cell, int player, bool[,] visited)
    {
        int rows = board.GetLength(0);
        int cols = board.GetLength(1);
        int x = cell.x;
        int y = cell.y;

        // ��� 0 ��ʤ�������������Ҳ�߽�
        if (player == 0 && y == cols - 1)
            return true;
        // ��� 1 ��ʤ�������������²�߽�
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
    /// ��ȡ��ǰ��Ԫ���������е����ڵ�Ԫ������ Hex ����������򣩡�
    /// </summary>
    private List<HexCellState> GetNeighbors(HexCellState[,] board, HexCellState cell)
    {
        List<HexCellState> neighbors = new List<HexCellState>();
        int rows = board.GetLength(0);
        int cols = board.GetLength(1);
        int x = cell.x;
        int y = cell.y;
        // ������������{-1,0}, {-1,1}, {0,-1}, {0,1}, {1,-1}, {1,0}
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
