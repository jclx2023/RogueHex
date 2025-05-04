// DoubleThreatStrategy.cs
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ˫����в���ԣ������ÿ�����Ӻ����Ƿ��γ�˫����в���֣�
/// </summary>
public class DoubleThreatStrategy : IAIStrategy
{
    /// <summary>
    /// �����һ�����ӵ����ã���Ҫ��������Ӻ���´����ԡ�
    /// </summary>
    public HexCell LastPlayerMove { get; set; }

    public HexCell GetBestMove(HexCell[,] board, int simulations)
    {
        // �����Ҹ����ӣ������Ƿ��γ���˫����в
        if (LastPlayerMove != null)
        {
            HexCell blockingMove = DetectThreatFromLastMove(LastPlayerMove, board);
            if (blockingMove != null)
            {
                return blockingMove;
            }
        }
        // ��δ��⵽������в�����˻�ʹ�� MCTS ����
        return new MCTSStrategy().GetBestMove(board, simulations);
    }

    /// <summary>
    /// ����ָ�����������ӿ�ʼ�Ƿ��γ�˫����в���֣�����⵽�򷵻�������ӵ�λ�á�
    /// ��Ϊ������������������Ϊ��һö���ӻ���Ϊ�ڶ�ö���ӡ�
    /// </summary>
    private HexCell DetectThreatFromLastMove(HexCell lastMove, HexCell[,] board)
    {
        int rows = board.GetLength(0);
        int cols = board.GetLength(1);
        // Ԥ������Ҫ����ģʽ��������������֮������λ�ã�diff���Լ����λ�ã�block1 �� block2��
        var patterns = new List<(Vector2Int diff, Vector2Int block1, Vector2Int block2)>
        {
            (new Vector2Int(1, 1),   new Vector2Int(0, 1),   new Vector2Int(1, 0)),
            (new Vector2Int(-1,-1),  new Vector2Int(-1, 0),  new Vector2Int(0, -1)),
            (new Vector2Int(-1,2),   new Vector2Int(0, 1),   new Vector2Int(-1,1)),
            (new Vector2Int(2,-1),   new Vector2Int(1,-1),  new Vector2Int(1,0)),
            (new Vector2Int(1,-2),   new Vector2Int(0,-1),  new Vector2Int(1,-1)),
            (new Vector2Int(-2,1),   new Vector2Int(-1,0),  new Vector2Int(-1,1))
        };

        foreach (var pattern in patterns)
        {
            // ���1��lastMove ��Ϊ��һö����
            int partnerX = lastMove.x + pattern.diff.x;
            int partnerY = lastMove.y + pattern.diff.y;
            if (IsWithinBoard(partnerX, partnerY, rows, cols))
            {
                HexCell partnerCell = board[partnerX, partnerY];
                if (partnerCell.isOccupied && partnerCell.occupiedBy == 0)
                {
                    // ������λ�ã������ lastMove ������λ��
                    int blockX1 = lastMove.x + pattern.block1.x;
                    int blockY1 = lastMove.y + pattern.block1.y;
                    if (IsWithinBoard(blockX1, blockY1, rows, cols))
                    {
                        HexCell blockCell1 = board[blockX1, blockY1];
                        if (!blockCell1.isOccupied)
                        {
                            return blockCell1;
                        }
                    }
                    int blockX2 = lastMove.x + pattern.block2.x;
                    int blockY2 = lastMove.y + pattern.block2.y;
                    if (IsWithinBoard(blockX2, blockY2, rows, cols))
                    {
                        HexCell blockCell2 = board[blockX2, blockY2];
                        if (!blockCell2.isOccupied)
                        {
                            return blockCell2;
                        }
                    }
                }
            }

            // ���2��lastMove ��Ϊ�ڶ�ö����
            int firstX = lastMove.x - pattern.diff.x;
            int firstY = lastMove.y - pattern.diff.y;
            if (IsWithinBoard(firstX, firstY, rows, cols))
            {
                HexCell firstCell = board[firstX, firstY];
                if (firstCell.isOccupied && firstCell.occupiedBy == 0)
                {
                    // ������λ�ã������ firstCell ������λ��
                    int blockX1 = firstCell.x + pattern.block1.x;
                    int blockY1 = firstCell.y + pattern.block1.y;
                    if (IsWithinBoard(blockX1, blockY1, rows, cols))
                    {
                        HexCell blockCell1 = board[blockX1, blockY1];
                        if (!blockCell1.isOccupied)
                        {
                            return blockCell1;
                        }
                    }
                    int blockX2 = firstCell.x + pattern.block2.x;
                    int blockY2 = firstCell.y + pattern.block2.y;
                    if (IsWithinBoard(blockX2, blockY2, rows, cols))
                    {
                        HexCell blockCell2 = board[blockX2, blockY2];
                        if (!blockCell2.isOccupied)
                        {
                            return blockCell2;
                        }
                    }
                }
            }
        }
        return null;
    }

    /// <summary>
    /// �����������жϸ��������Ƿ�λ�����̷�Χ��
    /// </summary>
    private bool IsWithinBoard(int x, int y, int rows, int cols)
    {
        return x >= 0 && x < rows && y >= 0 && y < cols;
    }
}
