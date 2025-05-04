// HeuristicStrategy.cs
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 启发式策略：对所有可用落子位置计算一个综合评分。
/// 评分主要综合考虑以下因素：
/// 1. 玩家交互奖励：鼓励 AI 落子靠近玩家最后落子的位置（LastPlayerMove），
///    有助于及时阻断或抢占关键区域；
/// 2. 邻域连通性：候选落子周围己方棋子较多有利连通，对手棋子则表示潜在威胁；
/// 3. 棋盘中心控制：靠近棋盘中心通常具备更多延展性；
/// 4. 单重威胁阻断奖励：如果候选位置正好位于玩家两个棋子之间（水平或垂直，间隔为 2），
///    则视为可以阻断玩家即将形成三连，给予额外加分。
/// </summary>
public class HeuristicStrategy : IAIStrategy
{
    /// <summary>
    /// 玩家上一次落子的位置，由外部在玩家落子后设置。
    /// </summary>
    public HexCell LastPlayerMove { get; set; }

    /// <summary>
    /// 根据当前棋盘状态返回评分最高的落子位置。
    /// </summary>
    /// <param name="board">当前棋盘二维数组</param>
    /// <param name="simulations">本策略中未使用该参数，可忽略</param>
    /// <returns>评分最高的未占据 HexCell；若无可用位置则返回 null</returns>
    public HexCell GetBestMove(HexCell[,] board, int simulations)
    {
        List<HexCell> availableCells = new List<HexCell>();
        int rows = board.GetLength(0);
        int cols = board.GetLength(1);

        // 收集所有未被占据的格子
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
        // 对每个空位计算综合评分
        foreach (HexCell cell in availableCells)
        {
            float score = EvaluateCell(board, cell);
            // 加入微小随机噪声，避免决策完全确定
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
    /// 评估单个候选落子位置的分数，主要考虑：
    /// 1. 玩家交互奖励：候选位置与玩家最后落子距离越近，奖励越高；
    /// 2. 邻域连通性：候选位置周围己方棋子越多（+分），对手棋子越多（-分）；
    /// 3. 棋盘中心控制奖励：候选位置越靠近棋盘中心，奖励越高；
    /// 4. 单重威胁阻断奖励：如果候选位置正好位于玩家两个棋子之间（水平或垂直间隔 2），
    ///    则视为可以阻断玩家即将形成三连，给予额外奖励。
    /// </summary>
    private float EvaluateCell(HexCell[,] board, HexCell cell)
    {
        int rows = board.GetLength(0);
        int cols = board.GetLength(1);

        // 1. 玩家交互奖励：鼓励 AI 落子靠近玩家最后落子的位置
        float playerInteractionBonus = 0f;
        if (LastPlayerMove != null)
        {
            float distanceToPlayer = Vector2.Distance(new Vector2(cell.x, cell.y), new Vector2(LastPlayerMove.x, LastPlayerMove.y));
            float maxDistance = Mathf.Sqrt((rows - 1) * (rows - 1) + (cols - 1) * (cols - 1));
            playerInteractionBonus = 1f - (distanceToPlayer / maxDistance);
        }

        // 2. 邻域连通性：统计候选位置周围己方与对手棋子的数量
        List<HexCell> neighbors = GetNeighbors(board, cell);
        int friendlyCount = 0;
        int enemyCount = 0;
        foreach (HexCell neighbor in neighbors)
        {
            if (neighbor.isOccupied)
            {
                if (neighbor.occupiedBy == 1)  // 己方棋子
                    friendlyCount++;
                else if (neighbor.occupiedBy == 0)  // 对手棋子
                    enemyCount++;
            }
        }

        // 3. 棋盘中心控制奖励
        float centerX = (rows - 1) / 2f;
        float centerY = (cols - 1) / 2f;
        float distToCenter = Vector2.Distance(new Vector2(cell.x, cell.y), new Vector2(centerX, centerY));
        float maxCenterDistance = Vector2.Distance(Vector2.zero, new Vector2(centerX, centerY));
        float centerBonus = 1f - (distToCenter / maxCenterDistance);

        // 4. 单重威胁阻断奖励：如果候选位置正好在玩家两个棋子之间（水平或垂直间隔为 2），则给予额外奖励
        float threatBlockingBonus = 0f;
        // 水平检测：检查候选位置左侧和右侧是否分别有玩家棋子（间隔 1 个空位）
        if (cell.x - 1 >= 0 && cell.x + 1 < rows)
        {
            if (board[cell.x - 1, cell.y].isOccupied && board[cell.x - 1, cell.y].occupiedBy == 0 &&
                board[cell.x + 1, cell.y].isOccupied && board[cell.x + 1, cell.y].occupiedBy == 0)
            {
                threatBlockingBonus += 1.5f;
            }
        }
        // 垂直检测：检查候选位置上方和下方是否分别有玩家棋子
        if (cell.y - 1 >= 0 && cell.y + 1 < cols)
        {
            if (board[cell.x, cell.y - 1].isOccupied && board[cell.x, cell.y - 1].occupiedBy == 0 &&
                board[cell.x, cell.y + 1].isOccupied && board[cell.x, cell.y + 1].occupiedBy == 0)
            {
                threatBlockingBonus += 1.5f;
            }
        }

        // 结合各项因素，并赋予不同权重
        float weightInteraction = 3.0f;  // 玩家交互奖励权重
        float weightFriendly = 2.0f;     // 己方邻居奖励
        float weightEnemy = -1.0f;       // 对手邻居惩罚
        float weightCenter = 1.5f;       // 中心控制奖励
        float weightThreat = 2.0f;       // 单重威胁阻断奖励权重

        float score = weightInteraction * playerInteractionBonus +
                      weightFriendly * friendlyCount +
                      weightEnemy * enemyCount +
                      weightCenter * centerBonus +
                      weightThreat * threatBlockingBonus;

        return score;
    }

    /// <summary>
    /// 获取指定格子在棋盘中的相邻单元（按照 Hex 棋规则的六个方向）。
    /// </summary>
    private List<HexCell> GetNeighbors(HexCell[,] board, HexCell cell)
    {
        List<HexCell> neighbors = new List<HexCell>();
        int rows = board.GetLength(0);
        int cols = board.GetLength(1);
        int x = cell.x;
        int y = cell.y;
        // 六个方向：左上、右上、左下、右下、正左、正右（具体方向可根据棋盘排布调整）
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
