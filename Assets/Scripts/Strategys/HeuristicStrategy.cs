// HeuristicStrategy.cs
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ����ʽ���ԣ������п�������λ�ü���һ���ۺ����֡�
/// ������Ҫ�ۺϿ����������أ�
/// 1. ��ҽ������������� AI ���ӿ������������ӵ�λ�ã�LastPlayerMove����
///    �����ڼ�ʱ��ϻ���ռ�ؼ�����
/// 2. ������ͨ�ԣ���ѡ������Χ�������ӽ϶�������ͨ�������������ʾǱ����в��
/// 3. �������Ŀ��ƣ�������������ͨ���߱�������չ�ԣ�
/// 4. ������в��Ͻ����������ѡλ������λ�������������֮�䣨ˮƽ��ֱ�����Ϊ 2����
///    ����Ϊ���������Ҽ����γ��������������ӷ֡�
/// </summary>
public class HeuristicStrategy : IAIStrategy
{
    /// <summary>
    /// �����һ�����ӵ�λ�ã����ⲿ��������Ӻ����á�
    /// </summary>
    public HexCell LastPlayerMove { get; set; }

    /// <summary>
    /// ���ݵ�ǰ����״̬����������ߵ�����λ�á�
    /// </summary>
    /// <param name="board">��ǰ���̶�ά����</param>
    /// <param name="simulations">��������δʹ�øò������ɺ���</param>
    /// <returns>������ߵ�δռ�� HexCell�����޿���λ���򷵻� null</returns>
    public HexCell GetBestMove(HexCell[,] board, int simulations)
    {
        List<HexCell> availableCells = new List<HexCell>();
        int rows = board.GetLength(0);
        int cols = board.GetLength(1);

        // �ռ�����δ��ռ�ݵĸ���
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                if (!board[i, j].isOccupied)
                {
                    availableCells.Add(board[i, j]);
                }
            }
        }
        if (availableCells.Count == 0)
            return null;

        HexCell bestCell = null;
        float bestScore = float.MinValue;
        // ��ÿ����λ�����ۺ�����
        foreach (HexCell cell in availableCells)
        {
            float score = EvaluateCell(board, cell);
            // ����΢С������������������ȫȷ��
            score += Random.Range(0f, 0.1f);
            if (score > bestScore)
            {
                bestScore = score;
                bestCell = cell;
            }
        }
        return bestCell;
    }

    /// <summary>
    /// ����������ѡ����λ�õķ�������Ҫ���ǣ�
    /// 1. ��ҽ�����������ѡλ�������������Ӿ���Խ��������Խ�ߣ�
    /// 2. ������ͨ�ԣ���ѡλ����Χ��������Խ�ࣨ+�֣�����������Խ�ࣨ-�֣���
    /// 3. �������Ŀ��ƽ�������ѡλ��Խ�����������ģ�����Խ�ߣ�
    /// 4. ������в��Ͻ����������ѡλ������λ�������������֮�䣨ˮƽ��ֱ��� 2����
    ///    ����Ϊ���������Ҽ����γ�������������⽱����
    /// </summary>
    private float EvaluateCell(HexCell[,] board, HexCell cell)
    {
        int rows = board.GetLength(0);
        int cols = board.GetLength(1);

        // 1. ��ҽ������������� AI ���ӿ������������ӵ�λ��
        float playerInteractionBonus = 0f;
        if (LastPlayerMove != null)
        {
            float distanceToPlayer = Vector2.Distance(new Vector2(cell.x, cell.y), new Vector2(LastPlayerMove.x, LastPlayerMove.y));
            float maxDistance = Mathf.Sqrt((rows - 1) * (rows - 1) + (cols - 1) * (cols - 1));
            playerInteractionBonus = 1f - (distanceToPlayer / maxDistance);
        }

        // 2. ������ͨ�ԣ�ͳ�ƺ�ѡλ����Χ������������ӵ�����
        List<HexCell> neighbors = GetNeighbors(board, cell);
        int friendlyCount = 0;
        int enemyCount = 0;
        foreach (HexCell neighbor in neighbors)
        {
            if (neighbor.isOccupied)
            {
                if (neighbor.occupiedBy == 1)  // ��������
                    friendlyCount++;
                else if (neighbor.occupiedBy == 0)  // ��������
                    enemyCount++;
            }
        }

        // 3. �������Ŀ��ƽ���
        float centerX = (rows - 1) / 2f;
        float centerY = (cols - 1) / 2f;
        float distToCenter = Vector2.Distance(new Vector2(cell.x, cell.y), new Vector2(centerX, centerY));
        float maxCenterDistance = Vector2.Distance(Vector2.zero, new Vector2(centerX, centerY));
        float centerBonus = 1f - (distToCenter / maxCenterDistance);

        // 4. ������в��Ͻ����������ѡλ�������������������֮�䣨ˮƽ��ֱ���Ϊ 2�����������⽱��
        float threatBlockingBonus = 0f;
        // ˮƽ��⣺����ѡλ�������Ҳ��Ƿ�ֱ���������ӣ���� 1 ����λ��
        if (cell.x - 1 >= 0 && cell.x + 1 < rows)
        {
            if (board[cell.x - 1, cell.y].isOccupied && board[cell.x - 1, cell.y].occupiedBy == 0 &&
                board[cell.x + 1, cell.y].isOccupied && board[cell.x + 1, cell.y].occupiedBy == 0)
            {
                threatBlockingBonus += 1.5f;
            }
        }
        // ��ֱ��⣺����ѡλ���Ϸ����·��Ƿ�ֱ����������
        if (cell.y - 1 >= 0 && cell.y + 1 < cols)
        {
            if (board[cell.x, cell.y - 1].isOccupied && board[cell.x, cell.y - 1].occupiedBy == 0 &&
                board[cell.x, cell.y + 1].isOccupied && board[cell.x, cell.y + 1].occupiedBy == 0)
            {
                threatBlockingBonus += 1.5f;
            }
        }

        // ��ϸ������أ������費ͬȨ��
        float weightInteraction = 3.0f;  // ��ҽ�������Ȩ��
        float weightFriendly = 2.0f;     // �����ھӽ���
        float weightEnemy = -1.0f;       // �����ھӳͷ�
        float weightCenter = 1.5f;       // ���Ŀ��ƽ���
        float weightThreat = 2.0f;       // ������в��Ͻ���Ȩ��

        float score = weightInteraction * playerInteractionBonus +
                      weightFriendly * friendlyCount +
                      weightEnemy * enemyCount +
                      weightCenter * centerBonus +
                      weightThreat * threatBlockingBonus;

        return score;
    }

    /// <summary>
    /// ��ȡָ�������������е����ڵ�Ԫ������ Hex �������������򣩡�
    /// </summary>
    private List<HexCell> GetNeighbors(HexCell[,] board, HexCell cell)
    {
        List<HexCell> neighbors = new List<HexCell>();
        int rows = board.GetLength(0);
        int cols = board.GetLength(1);
        int x = cell.x;
        int y = cell.y;
        // �����������ϡ����ϡ����¡����¡��������ң����巽��ɸ��������Ų�������
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
}
