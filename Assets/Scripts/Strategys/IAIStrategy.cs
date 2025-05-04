using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public interface IAIStrategy
{
    /// <summary>
    /// 对一个未占据的格子进行评分（必由子类实现）。
    /// </summary>
    float EvaluateCell(HexCell[,] board, HexCell cell);

    /// <summary>
    /// 检测是否存在一步必下的紧急落子（如一步必赢或一步必防），
    /// 若有则返回该格子；否则返回 null。（子类可实现）
    /// </summary>
    HexCell GetImmediateMove(HexCell[,] board);

    /// <summary>
    /// 默认实现：先调用 GetImmediateMove，如果有结果就直接返回；
    /// 否则对所有空格调用 EvaluateCell，选出得分最高的格子。
    /// </summary>
    HexCell GetBestMove(HexCell[,] board, int simulations = 0)
    {
        // 1. 紧急必下
        var urgent = GetImmediateMove(board);
        if (urgent != null) return urgent;

        // 2. 常规评分
        int rows = board.GetLength(0), cols = board.GetLength(1);
        HexCell best = null;
        float bestScore = float.MinValue;

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                var cell = board[i, j];
                if (cell.isOccupied) continue;

                float score = EvaluateCell(board, cell);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = cell;
                }
            }
        }

        return best;
    }
}
