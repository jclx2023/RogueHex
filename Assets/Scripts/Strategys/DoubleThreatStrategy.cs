// DoubleThreatStrategy.cs
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 双重威胁策略：在玩家每次落子后检查是否形成双重威胁布局，
/// </summary>
public class DoubleThreatStrategy : IAIStrategy
{
    /// <summary>
    /// 玩家上一次落子的引用，需要在玩家落子后更新此属性。
    /// </summary>
    public HexCell LastPlayerMove { get; set; }

    public HexCell GetBestMove(HexCell[,] board, int simulations)
    {
        // 如果玩家刚落子，则检测是否形成了双重威胁
        if (LastPlayerMove != null)
        {
            HexCell blockingMove = DetectThreatFromLastMove(LastPlayerMove, board);
            if (blockingMove != null)
            {
                return blockingMove;
            }
        }
        // 若未检测到明显威胁，则退化使用 MCTS 策略
        return new MCTSStrategy().GetBestMove(board, simulations);
    }

    /// <summary>
    /// 检测从指定玩家最后落子开始是否形成双重威胁布局，若检测到则返回阻断落子的位置。
    /// 分为两种情况：玩家落子作为第一枚棋子或作为第二枚棋子。
    /// </summary>
    private HexCell DetectThreatFromLastMove(HexCell lastMove, HexCell[,] board)
    {
        int rows = board.GetLength(0);
        int cols = board.GetLength(1);
        // 预定义需要检测的模式，包括两个棋子之间的相对位置（diff）以及阻断位置（block1 和 block2）
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
            // 情况1：lastMove 作为第一枚棋子
            int partnerX = lastMove.x + pattern.diff.x;
            int partnerY = lastMove.y + pattern.diff.y;
            if (IsWithinBoard(partnerX, partnerY, rows, cols))
            {
                HexCell partnerCell = board[partnerX, partnerY];
                if (partnerCell.isOccupied && partnerCell.occupiedBy == 0)
                {
                    // 检查阻断位置：相对于 lastMove 的两个位置
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

            // 情况2：lastMove 作为第二枚棋子
            int firstX = lastMove.x - pattern.diff.x;
            int firstY = lastMove.y - pattern.diff.y;
            if (IsWithinBoard(firstX, firstY, rows, cols))
            {
                HexCell firstCell = board[firstX, firstY];
                if (firstCell.isOccupied && firstCell.occupiedBy == 0)
                {
                    // 检查阻断位置：相对于 firstCell 的两个位置
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
    /// 辅助函数：判断给定坐标是否位于棋盘范围内
    /// </summary>
    private bool IsWithinBoard(int x, int y, int rows, int cols)
    {
        return x >= 0 && x < rows && y >= 0 && y < cols;
    }
}
