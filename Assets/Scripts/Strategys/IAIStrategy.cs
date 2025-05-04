// IAIStrategy.cs
using UnityEngine;

/// <summary>
/// 定义 AI 策略接口：
/// 兼容原有 GetBestMove，同时扩展支持新的综合评分系统。
/// </summary>
public interface IAIStrategy
{
    /// <summary>
    /// 原有接口：根据当前棋盘状态返回最佳移动的 HexCell。
    /// 传统策略仍可用此接口单独选点。
    /// </summary>
    /// <param name="board">当前棋盘二维数组</param>
    /// <param name="simulations">模拟次数或其他参数（不同策略可使用或忽略）</param>
    /// <returns>选择的 HexCell，若无合适移动则返回 null</returns>
    HexCell GetBestMove(HexCell[,] board, int simulations);

    /// <summary>
    /// 新增接口：对一个未占据的格子进行评分（用于综合打分系统）。
    /// 默认返回 0 分，子类可按需重写。
    /// </summary>
    /// <param name="board">当前棋盘二维数组</param>
    /// <param name="cell">待评估的 HexCell</param>
    /// <returns>该格子的评分（默认 0）</returns>
    float EvaluateCell(HexCell[,] board, HexCell cell)
    {
        return 0f; // 默认实现
    }

    /// <summary>
    /// 新增接口：检测是否存在紧急必下的位置（如立即胜利或必须防守）。
    /// 默认返回 null，子类可按需重写。
    /// </summary>
    /// <param name="board">当前棋盘二维数组</param>
    /// <returns>若存在需要优先落子的格子，返回 HexCell；否则返回 null</returns>
    HexCell GetImmediateMove(HexCell[,] board)
    {
        return null; // 默认实现
    }
}
