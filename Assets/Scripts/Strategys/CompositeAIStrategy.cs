// CompositeAIStrategy.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class CompositeAIStrategy : IAIStrategy
{
    private List<(IAIStrategy strategy, float weight)> strategies;

    public CompositeAIStrategy()
    {
        strategies = new List<(IAIStrategy, float)>();
    }

    public void AddStrategy(IAIStrategy strategy, float weight)
    {
        strategies.Add((strategy, weight));
    }

    /// <summary>
    /// 返回内部所有子策略及其权重
    /// </summary>
    public List<(IAIStrategy strategy, float weight)> GetStrategies()
    {
        return strategies;
    }

    /// <summary>
    /// 新版：综合决策逻辑
    /// 先检查是否有紧急落子；
    /// 如果没有，综合各子策略的评分后选择得分最高的格子。
    /// </summary>
    public HexCell GetBestMove(HexCell[,] board, int simulations)
    {
        // 1. 检查是否有任何策略返回需要立即落子的 HexCell
        foreach (var (strategy, weight) in strategies)
        {
            HexCell immediateMove = strategy.GetImmediateMove(board);
            if (immediateMove != null)
            {
                Debug.Log($"Immediate move selected by {strategy.GetType().Name} at ({immediateMove.x}, {immediateMove.y})");
                return immediateMove;
            }
        }

        // 2. 如果没有紧急落子，则进行综合评分
        Dictionary<HexCell, float> scoreMap = new Dictionary<HexCell, float>();
        int rows = board.GetLength(0);
        int cols = board.GetLength(1);

        // 遍历所有未占据的格子
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                HexCell cell = board[i, j];
                if (!cell.isOccupied)
                {
                    float totalScore = 0f;
                    foreach (var (strategy, weight) in strategies)
                    {
                        totalScore += weight * strategy.EvaluateCell(board, cell);
                    }
                    scoreMap[cell] = totalScore;
                }
            }
        }

        if (scoreMap.Count == 0)
        {
            Debug.LogWarning("No available moves found!");
            return null;
        }

        // 3. 选择评分最高的格子
        HexCell bestMove = scoreMap.OrderByDescending(kv => kv.Value).First().Key;
        Debug.Log($"Composite strategy selects move at ({bestMove.x}, {bestMove.y}) with score {scoreMap[bestMove]:F2}");
        return bestMove;
    }
}
