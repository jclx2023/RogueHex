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
    /// �����ڲ������Ӳ��Լ���Ȩ��
    /// </summary>
    public List<(IAIStrategy strategy, float weight)> GetStrategies()
    {
        return strategies;
    }

    /// <summary>
    /// �°棺�ۺϾ����߼�
    /// �ȼ���Ƿ��н������ӣ�
    /// ���û�У��ۺϸ��Ӳ��Ե����ֺ�ѡ��÷���ߵĸ��ӡ�
    /// </summary>
    public HexCell GetBestMove(HexCell[,] board, int simulations)
    {
        // 1. ����Ƿ����κβ��Է�����Ҫ�������ӵ� HexCell
        foreach (var (strategy, weight) in strategies)
        {
            HexCell immediateMove = strategy.GetImmediateMove(board);
            if (immediateMove != null)
            {
                Debug.Log($"Immediate move selected by {strategy.GetType().Name} at ({immediateMove.x}, {immediateMove.y})");
                return immediateMove;
            }
        }

        // 2. ���û�н������ӣ�������ۺ�����
        Dictionary<HexCell, float> scoreMap = new Dictionary<HexCell, float>();
        int rows = board.GetLength(0);
        int cols = board.GetLength(1);

        // ��������δռ�ݵĸ���
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

        // 3. ѡ��������ߵĸ���
        HexCell bestMove = scoreMap.OrderByDescending(kv => kv.Value).First().Key;
        Debug.Log($"Composite strategy selects move at ({bestMove.x}, {bestMove.y}) with score {scoreMap[bestMove]:F2}");
        return bestMove;
    }
}
