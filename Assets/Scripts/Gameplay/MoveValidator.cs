using UnityEngine;

namespace HexGame.Core
{
    /// <summary>
    /// 移动验证器 - 负责验证玩家移动的合法性
    /// 单一职责：只处理移动验证逻辑，不涉及游戏状态管理
    /// </summary>
    public class MoveValidator
    {
        private readonly GameConfig config;

        public MoveValidator(GameConfig config)
        {
            this.config = config ?? throw new System.ArgumentNullException(nameof(config));
        }

        #region 移动验证

        /// <summary>
        /// 验证移动是否合法，如果合法则返回成功结果
        /// </summary>
        public MoveResult ValidateAndApplyMove(GameState gameState, Move move)
        {
            // 基础验证
            var basicValidation = ValidateBasicMove(gameState, move);
            if (!basicValidation.IsValid)
            {
                return basicValidation;
            }

            // 游戏状态验证
            var stateValidation = ValidateGameState(gameState);
            if (!stateValidation.IsValid)
            {
                return stateValidation;
            }

            // 玩家回合验证
            var turnValidation = ValidatePlayerTurn(gameState, move);
            if (!turnValidation.IsValid)
            {
                return turnValidation;
            }

            // 所有验证通过
            return MoveResult.Valid(move);
        }

        /// <summary>
        /// 快速验证移动是否合法（不考虑游戏状态和回合）
        /// 主要用于AI模拟时的快速检查
        /// </summary>
        public bool IsValidMoveQuick(HexCellState[,] board, Vector2Int position)
        {
            // 检查坐标范围
            if (!IsValidPosition(position))
                return false;

            // 检查格子是否已被占据
            return !board[position.x, position.y].isOccupied;
        }

        #endregion

        #region 基础验证方法

        /// <summary>
        /// 验证移动的基础信息
        /// </summary>
        private MoveResult ValidateBasicMove(GameState gameState, Move move)
        {
            if (move == null)
                return MoveResult.Invalid("移动不能为空");

            if (!IsValidPosition(move.Position))
                return MoveResult.Invalid($"位置 ({move.Position.x}, {move.Position.y}) 超出棋盘范围");

            var cell = gameState.GetCell(move.Position);
            if (cell == null)
                return MoveResult.Invalid($"无法获取位置 ({move.Position.x}, {move.Position.y}) 的格子信息");

            if (cell.isOccupied)
                return MoveResult.Invalid($"位置 ({move.Position.x}, {move.Position.y}) 已被占据");

            return MoveResult.Valid(move);
        }

        /// <summary>
        /// 验证游戏状态是否允许移动
        /// </summary>
        private MoveResult ValidateGameState(GameState gameState)
        {
            if (gameState == null)
                return MoveResult.Invalid("游戏状态为空");

            if (gameState.CurrentPhase != GamePhase.Playing)
                return MoveResult.Invalid($"当前游戏阶段 ({gameState.CurrentPhase}) 不允许移动");

            return MoveResult.Valid(null);
        }

        /// <summary>
        /// 验证是否轮到该玩家移动
        /// </summary>
        private MoveResult ValidatePlayerTurn(GameState gameState, Move move)
        {
            if (gameState.CurrentPlayer != move.Player)
                return MoveResult.Invalid($"当前不是 {move.Player} 的回合，应该是 {gameState.CurrentPlayer} 的回合");

            return MoveResult.Valid(move);
        }

        /// <summary>
        /// 检查位置是否在棋盘范围内
        /// </summary>
        private bool IsValidPosition(Vector2Int position)
        {
            return position.x >= 0 && position.x < config.boardRows &&
                   position.y >= 0 && position.y < config.boardCols;
        }

        #endregion

        #region 高级验证（可选功能）

        /// <summary>
        /// 验证移动是否会导致立即失败（用于AI决策辅助）
        /// </summary>
        public bool WouldMoveResultInImmediateLoss(GameState gameState, Move move, IWinConditionChecker winChecker)
        {
            if (winChecker == null) return false;

            // 克隆游戏状态
            var clonedBoard = gameState.CloneBoardState();

            // 应用移动
            GameState.ApplyMoveToBoard(clonedBoard, move);

            // 检查对手是否会在下一步获胜
            var opponent = move.Player == PlayerType.Human ? PlayerType.AI : PlayerType.Human;

            // 遍历所有可能的对手移动
            for (int i = 0; i < config.boardRows; i++)
            {
                for (int j = 0; j < config.boardCols; j++)
                {
                    if (!clonedBoard[i, j].isOccupied)
                    {
                        var opponentMove = new Move(i, j, opponent);
                        var testBoard = CloneBoard(clonedBoard);
                        GameState.ApplyMoveToBoard(testBoard, opponentMove);

                        var tempGameState = CreateTempGameState(testBoard);
                        var result = winChecker.CheckGameResult(tempGameState);

                        if (result.IsGameOver && result.Winner == opponent)
                        {
                            return true; // 对手会在下一步获胜
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 检查移动是否会立即获胜
        /// </summary>
        public bool WouldMoveResultInWin(GameState gameState, Move move, IWinConditionChecker winChecker)
        {
            if (winChecker == null) return false;

            // 克隆游戏状态并应用移动
            var clonedBoard = gameState.CloneBoardState();
            GameState.ApplyMoveToBoard(clonedBoard, move);

            // 创建临时游戏状态进行检查
            var tempGameState = CreateTempGameState(clonedBoard);
            var result = winChecker.CheckGameResult(tempGameState);

            return result.IsGameOver && result.Winner == move.Player;
        }

        /// <summary>
        /// 获取位置的战略价值评分（用于移动排序）
        /// </summary>
        public float GetPositionStrategicValue(Vector2Int position, PlayerType player)
        {
            float value = 0f;

            // 中心位置价值更高
            var center = new Vector2(config.boardRows / 2f, config.boardCols / 2f);
            var distanceFromCenter = Vector2.Distance(position, center);
            var maxDistance = Mathf.Max(config.boardRows, config.boardCols) / 2f;
            var centerValue = (maxDistance - distanceFromCenter) / maxDistance * 2f;
            value += centerValue;

            // 边缘位置的连通价值
            if (player == PlayerType.Human) // 人类玩家需要左右连通
            {
                if (position.y == 0 || position.y == config.boardCols - 1)
                    value += 3f; // 边缘格子价值更高
            }
            else // AI需要上下连通
            {
                if (position.x == 0 || position.x == config.boardRows - 1)
                    value += 3f;
            }

            return value;
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 克隆棋盘状态
        /// </summary>
        private HexCellState[,] CloneBoard(HexCellState[,] original)
        {
            int rows = original.GetLength(0);
            int cols = original.GetLength(1);
            var clone = new HexCellState[rows, cols];

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    var cell = original[i, j];
                    clone[i, j] = new HexCellState(cell.x, cell.y, cell.isOccupied, cell.occupiedBy);
                }
            }

            return clone;
        }

        /// <summary>
        /// 创建临时游戏状态用于测试
        /// </summary>
        private GameState CreateTempGameState(HexCellState[,] board)
        {
            var tempGameState = new GameState(config);

            // 复制棋盘状态
            for (int i = 0; i < config.boardRows; i++)
            {
                for (int j = 0; j < config.boardCols; j++)
                {
                    var sourceCell = board[i, j];
                    var targetCell = tempGameState.Board[i, j];

                    if (sourceCell.isOccupied && sourceCell.occupiedBy >= 0)
                    {
                        PlayerType player = (PlayerType)sourceCell.occupiedBy;
                        targetCell.SetOccupied(player);
                    }
                }
            }

            return tempGameState;
        }

        #endregion

        #region 调试和统计

        /// <summary>
        /// 获取当前棋盘上所有合法移动
        /// </summary>
        public System.Collections.Generic.List<Vector2Int> GetAllValidMoves(GameState gameState)
        {
            var validMoves = new System.Collections.Generic.List<Vector2Int>();

            for (int i = 0; i < config.boardRows; i++)
            {
                for (int j = 0; j < config.boardCols; j++)
                {
                    var position = new Vector2Int(i, j);
                    if (gameState.IsPositionEmpty(position))
                    {
                        validMoves.Add(position);
                    }
                }
            }

            return validMoves;
        }
    }
}

        #endregion