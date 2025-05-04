// BoardEvaluator.cs
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 手动精确定义分数：根据 lastEnemyMove 周围两圈格子，按不同重要性加分。
/// </summary>
public class BoardEvaluator
{
    private HexCell[,] board;
    private HexCell lastEnemyMove;
    private int rows;
    private int cols;

    // 每个偏移及其分数，方便外部调整
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
