using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public interface IAIStrategy
{
    /// <summary>
    /// ��һ��δռ�ݵĸ��ӽ������֣���������ʵ�֣���
    /// </summary>
    float EvaluateCell(HexCell[,] board, HexCell cell);

    /// <summary>
    /// ����Ƿ����һ�����µĽ������ӣ���һ����Ӯ��һ���ط�����
    /// �����򷵻ظø��ӣ����򷵻� null���������ʵ�֣�
    /// </summary>
    HexCell GetImmediateMove(HexCell[,] board);

    /// <summary>
    /// Ĭ��ʵ�֣��ȵ��� GetImmediateMove������н����ֱ�ӷ��أ�
    /// ��������пո���� EvaluateCell��ѡ���÷���ߵĸ��ӡ�
    /// </summary>
    HexCell GetBestMove(HexCell[,] board, int simulations = 0)
    {
        // 1. ��������
        var urgent = GetImmediateMove(board);
        if (urgent != null) return urgent;

        // 2. ��������
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
