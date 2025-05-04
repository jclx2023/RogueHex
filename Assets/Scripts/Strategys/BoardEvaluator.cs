// BoardEvaluator.cs
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// �ֶ���ȷ������������� lastEnemyMove ��Χ��Ȧ���ӣ�����ͬ��Ҫ�Լӷ֡�
/// </summary>
public class BoardEvaluator
{
    private HexCell[,] board;
    private HexCell lastEnemyMove;
    private int rows;
    private int cols;

    // ÿ��ƫ�Ƽ�������������ⲿ����
    public List<(Vector2Int offset, float score)> offsetsWithScores = new List<(Vector2Int, float)>
    {
        (new Vector2Int(0, 1), 10f),
        (new Vector2Int(0, -1), 10f),
        (new Vector2Int(-1, 1), 10f),
        (new Vector2Int(1, -1), 10f),
        (new Vector2Int(-2, 1), 10f),
        (new Vector2Int(1, 1), 10f),
        (new Vector2Int(-1, -1), 10f),
        (new Vector2Int(2, -1), 10f),
        (new Vector2Int(-1, 0), 8f),
        (new Vector2Int(1, 0), 8f),
        (new Vector2Int(-2, 0), 5f),
        (new Vector2Int(-1, -2), 5f),
        (new Vector2Int(0, -2), 5f),
        (new Vector2Int(1, -2), 5f),
        (new Vector2Int(2, 0), 5f),
        (new Vector2Int(1, 2), 5f),
        (new Vector2Int(-1, 2), 10f),
        (new Vector2Int(2, -2), 10f)
    };

    public BoardEvaluator(HexCell[,] board, HexCell lastEnemyMove)
    {
        this.board = board;
        this.lastEnemyMove = lastEnemyMove;
        this.rows = board.GetLength(0);
        this.cols = board.GetLength(1);
    }

    public Dictionary<HexCell, float> EvaluateBoard()
    {
        Dictionary<HexCell, float> scoreMap = new Dictionary<HexCell, float>();

        if (lastEnemyMove == null)
            return scoreMap;

        foreach (var (offset, score) in offsetsWithScores)
        {
            int nx = lastEnemyMove.x + offset.x;
            int ny = lastEnemyMove.y + offset.y;
            if (IsInBounds(nx, ny))
            {
                HexCell targetCell = board[nx, ny];
                if (!targetCell.isOccupied)
                {
                    scoreMap[targetCell] = score;
                }
            }
        }

        return scoreMap;
    }

    private bool IsInBounds(int x, int y)
    {
        return x >= 0 && x < rows && y >= 0 && y < cols;
    }
}
