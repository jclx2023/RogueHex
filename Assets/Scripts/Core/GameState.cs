using System;
using System.Collections.Generic;
using UnityEngine;
using HexGame.Rendering;

namespace HexGame.Core
{
    #region 枚举定义

    /// <summary>
    /// 玩家类型
    /// </summary>
    public enum PlayerType
    {
        Human = 0,  // 人类玩家
        AI = 1      // AI玩家
    }

    /// <summary>
    /// 游戏阶段
    /// </summary>
    public enum GamePhase
    {
        Initializing,   // 初始化中
        Playing,        // 游戏进行中
        Paused,         // 暂停
        GameOver,       // 游戏结束
        Resetting       // 重置中
    }

    #endregion

    #region 基础数据结构

    /// <summary>
    /// 移动数据结构
    /// </summary>
    [System.Serializable]
    public class Move
    {
        public Vector2Int Position { get; private set; }
        public PlayerType Player { get; private set; }
        public DateTime Timestamp { get; private set; }

        public Move(Vector2Int position, PlayerType player)
        {
            Position = position;
            Player = player;
            Timestamp = DateTime.Now;
        }

        public Move(int x, int y, PlayerType player) : this(new Vector2Int(x, y), player) { }

        public override string ToString()
        {
            return $"Move({Position.x}, {Position.y}) by {Player}";
        }

        public override bool Equals(object obj)
        {
            if (obj is Move other)
            {
                return Position == other.Position && Player == other.Player;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Position, Player);
        }
    }

    /// <summary>
    /// 移动验证结果
    /// </summary>
    public class MoveResult
    {
        public bool IsValid { get; private set; }
        public string ErrorMessage { get; private set; }
        public Move Move { get; private set; }

        private MoveResult(bool isValid, string errorMessage = null, Move move = null)
        {
            IsValid = isValid;
            ErrorMessage = errorMessage;
            Move = move;
        }

        public static MoveResult Valid(Move move)
        {
            return new MoveResult(true, null, move);
        }

        public static MoveResult Invalid(string errorMessage)
        {
            return new MoveResult(false, errorMessage);
        }
    }

    /// <summary>
    /// 游戏结果
    /// </summary>
    public class GameResult
    {
        public bool IsGameOver { get; private set; }
        public PlayerType? Winner { get; private set; }
        public List<Vector2Int> WinningPath { get; private set; }
        public string Description { get; private set; }

        private GameResult(bool isGameOver, PlayerType? winner, List<Vector2Int> winningPath, string description)
        {
            IsGameOver = isGameOver;
            Winner = winner;
            WinningPath = winningPath ?? new List<Vector2Int>();
            Description = description;
        }

        public static GameResult GameContinues()
        {
            return new GameResult(false, null, null, "Game continues");
        }

        public static GameResult PlayerWins(PlayerType winner, List<Vector2Int> winningPath)
        {
            return new GameResult(true, winner, winningPath, $"{winner} wins!");
        }

        public static GameResult Draw()
        {
            return new GameResult(true, null, null, "Game ends in a draw");
        }

        public bool IsDraw => IsGameOver && !Winner.HasValue;
        public bool HasWinner => IsGameOver && Winner.HasValue;
    }

    /// <summary>
    /// 六边形格子状态（用于逻辑计算）
    /// </summary>
    [System.Serializable]
    public class HexCellState
    {
        public int x, y;
        public bool isOccupied;
        public int occupiedBy; // -1: 未占据, 0: 玩家, 1: AI

        public HexCellState(int x, int y, bool isOccupied = false, int occupiedBy = -1)
        {
            this.x = x;
            this.y = y;
            this.isOccupied = isOccupied;
            this.occupiedBy = occupiedBy;
        }

        public Vector2Int Position => new Vector2Int(x, y);

        /// <summary>
        /// 设置格子被指定玩家占据
        /// </summary>
        public void SetOccupied(PlayerType player)
        {
            isOccupied = true;
            occupiedBy = (int)player;
        }

        /// <summary>
        /// 设置格子被指定玩家占据（使用int参数）
        /// </summary>
        public void SetOccupied(int player)
        {
            isOccupied = true;
            occupiedBy = player;
        }

        /// <summary>
        /// 清空格子
        /// </summary>
        public void Clear()
        {
            isOccupied = false;
            occupiedBy = -1;
        }

        /// <summary>
        /// 检查是否被指定玩家占据
        /// </summary>
        public bool IsOccupiedBy(PlayerType player)
        {
            return isOccupied && occupiedBy == (int)player;
        }

        /// <summary>
        /// 检查是否被指定玩家占据（使用int参数）
        /// </summary>
        public bool IsOccupiedBy(int player)
        {
            return isOccupied && occupiedBy == player;
        }

        /// <summary>
        /// 获取占据者的PlayerType（如果有的话）
        /// </summary>
        public PlayerType? GetOccupiedByPlayerType()
        {
            if (!isOccupied || occupiedBy < 0)
                return null;

            return (PlayerType)occupiedBy;
        }

        /// <summary>
        /// 检查格子是否为空
        /// </summary>
        public bool IsEmpty => !isOccupied;

        /// <summary>
        /// 检查格子是否有效占据（占据且occupiedBy >= 0）
        /// </summary>
        public bool IsValidOccupied => isOccupied && occupiedBy >= 0;

        /// <summary>
        /// 创建深拷贝
        /// </summary>
        public HexCellState Clone()
        {
            return new HexCellState(x, y, isOccupied, occupiedBy);
        }

        /// <summary>
        /// 从HexCell转换为HexCellState
        /// </summary>
        public static HexCellState FromHexCell(HexCell hexCell)
        {
            if (hexCell == null)
                return null;

            return new HexCellState(hexCell.x, hexCell.y, hexCell.isOccupied, hexCell.occupiedBy);
        }

        /// <summary>
        /// 批量转换HexCell数组为HexCellState数组
        /// </summary>
        public static HexCellState[,] FromHexCellArray(HexCell[,] hexCells)
        {
            if (hexCells == null)
                return null;

            int rows = hexCells.GetLength(0);
            int cols = hexCells.GetLength(1);
            var result = new HexCellState[rows, cols];

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    result[i, j] = FromHexCell(hexCells[i, j]);
                }
            }

            return result;
        }

        /// <summary>
        /// 验证占据状态的一致性
        /// </summary>
        public bool IsStateValid()
        {
            if (isOccupied)
            {
                return occupiedBy >= 0 && occupiedBy <= 1;
            }
            else
            {
                return occupiedBy == -1;
            }
        }

        /// <summary>
        /// 修复不一致的状态
        /// </summary>
        public void FixState()
        {
            if (isOccupied && occupiedBy < 0)
            {
                // 如果标记为占据但occupiedBy无效，清空格子
                Clear();
            }
            else if (!isOccupied && occupiedBy >= 0)
            {
                // 如果标记为未占据但有占据者，清除占据者
                occupiedBy = -1;
            }
        }

        public override string ToString()
        {
            if (isOccupied && occupiedBy >= 0)
            {
                string playerName = occupiedBy == 0 ? "Human" : "AI";
                return $"Cell({x}, {y}) - {playerName}";
            }
            else
            {
                return $"Cell({x}, {y}) - Empty";
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is HexCellState other)
            {
                return x == other.x && y == other.y &&
                       isOccupied == other.isOccupied &&
                       occupiedBy == other.occupiedBy;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return System.HashCode.Combine(x, y, isOccupied, occupiedBy);
        }

        /// <summary>
        /// 调试用：获取详细状态信息
        /// </summary>
        public string GetDetailedInfo()
        {
            return $"HexCellState[{x},{y}]: Occupied={isOccupied}, OccupiedBy={occupiedBy}, Valid={IsStateValid()}";
        }

        /// <summary>
        /// 比较两个HexCellState是否在同一位置
        /// </summary>
        public bool IsSamePosition(HexCellState other)
        {
            return other != null && x == other.x && y == other.y;
        }

        /// <summary>
        /// 比较两个HexCellState的占据状态是否相同
        /// </summary>
        public bool HasSameOccupation(HexCellState other)
        {
            return other != null && isOccupied == other.isOccupied && occupiedBy == other.occupiedBy;
        }
    }

    #endregion

    #region 游戏状态管理

    /// <summary>
    /// 游戏状态管理器 - 管理游戏的所有状态数据
    /// </summary>
    public class GameState
    {
        // 配置引用
        private readonly GameConfig config;

        // 当前状态
        public GamePhase CurrentPhase { get; private set; }
        public PlayerType CurrentPlayer { get; private set; }
        public HexCellState[,] Board { get; private set; }
        public List<Move> MoveHistory { get; private set; }

        // 游戏统计
        public int TotalMoves => MoveHistory.Count;
        public DateTime GameStartTime { get; private set; }
        public TimeSpan GameDuration => DateTime.Now - GameStartTime;

        // 状态变更事件
        public event Action<PlayerType> OnPlayerChanged;
        public event Action<GamePhase> OnPhaseChanged;
        public event Action<Move> OnMoveExecuted;
        public event Action OnBoardChanged;

        public GameState(GameConfig config)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            InitializeBoard();
            ResetGame();
        }

        #region 状态初始化和重置

        private void InitializeBoard()
        {
            Board = new HexCellState[config.boardRows, config.boardCols];
            for (int i = 0; i < config.boardRows; i++)
            {
                for (int j = 0; j < config.boardCols; j++)
                {
                    Board[i, j] = new HexCellState(i, j);
                }
            }
        }

        public void ResetGame()
        {
            // 重置棋盘
            for (int i = 0; i < config.boardRows; i++)
            {
                for (int j = 0; j < config.boardCols; j++)
                {
                    Board[i, j].Clear();
                }
            }

            // 重置游戏状态
            MoveHistory = new List<Move>();
            CurrentPhase = GamePhase.Initializing;
            CurrentPlayer = PlayerType.Human;
            GameStartTime = DateTime.Now;

            OnBoardChanged?.Invoke();
        }

        #endregion

        #region 状态修改

        public void SetPhase(GamePhase newPhase)
        {
            if (CurrentPhase != newPhase)
            {
                CurrentPhase = newPhase;
                OnPhaseChanged?.Invoke(newPhase);
            }
        }

        public void SetCurrentPlayer(PlayerType newPlayer)
        {
            if (CurrentPlayer != newPlayer)
            {
                CurrentPlayer = newPlayer;
                OnPlayerChanged?.Invoke(newPlayer);
            }
        }

        public void ExecuteMove(Move move)
        {
            if (move == null)
                throw new ArgumentNullException(nameof(move));

            var cell = Board[move.Position.x, move.Position.y];
            cell.SetOccupied(move.Player);

            MoveHistory.Add(move);

            OnMoveExecuted?.Invoke(move);
            OnBoardChanged?.Invoke();
        }

        #endregion

        #region 状态查询

        public bool IsPositionValid(Vector2Int position)
        {
            return position.x >= 0 && position.x < config.boardRows &&
                   position.y >= 0 && position.y < config.boardCols;
        }

        public bool IsPositionEmpty(Vector2Int position)
        {
            if (!IsPositionValid(position))
                return false;

            return !Board[position.x, position.y].isOccupied;
        }

        public HexCellState GetCell(Vector2Int position)
        {
            if (!IsPositionValid(position))
                return null;

            return Board[position.x, position.y];
        }

        public List<Vector2Int> GetEmptyPositions()
        {
            var emptyPositions = new List<Vector2Int>();
            for (int i = 0; i < config.boardRows; i++)
            {
                for (int j = 0; j < config.boardCols; j++)
                {
                    if (!Board[i, j].isOccupied)
                    {
                        emptyPositions.Add(new Vector2Int(i, j));
                    }
                }
            }
            return emptyPositions;
        }

        public Move GetLastMove()
        {
            return MoveHistory.Count > 0 ? MoveHistory[MoveHistory.Count - 1] : null;
        }

        public Move GetLastMoveByPlayer(PlayerType player)
        {
            for (int i = MoveHistory.Count - 1; i >= 0; i--)
            {
                if (MoveHistory[i].Player == player)
                    return MoveHistory[i];
            }
            return null;
        }

        public List<Move> GetMovesByPlayer(PlayerType player)
        {
            var moves = new List<Move>();
            foreach (var move in MoveHistory)
            {
                if (move.Player == player)
                    moves.Add(move);
            }
            return moves;
        }

        #endregion

        #region 棋盘复制和模拟

        /// <summary>
        /// 创建棋盘状态的深拷贝，用于AI模拟
        /// </summary>
        public HexCellState[,] CloneBoardState()
        {
            var clone = new HexCellState[config.boardRows, config.boardCols];
            for (int i = 0; i < config.boardRows; i++)
            {
                for (int j = 0; j < config.boardCols; j++)
                {
                    var original = Board[i, j];
                    clone[i, j] = original.Clone(); // 使用Clone方法确保深拷贝
                }
            }
            return clone;
        }

        /// <summary>
        /// 在克隆的棋盘上模拟移动（不影响真实游戏状态）
        /// </summary>
        public static void ApplyMoveToBoard(HexCellState[,] board, Move move)
        {
            if (board != null && move != null &&
                move.Position.x >= 0 && move.Position.x < board.GetLength(0) &&
                move.Position.y >= 0 && move.Position.y < board.GetLength(1))
            {
                var cell = board[move.Position.x, move.Position.y];
                cell.SetOccupied(move.Player);
            }
        }

        #endregion

        #region 调试和信息

        public string GetGameStateInfo()
        {
            return $"Phase: {CurrentPhase}, Player: {CurrentPlayer}, Moves: {TotalMoves}, Duration: {GameDuration:mm\\:ss}";
        }

        public void PrintBoard()
        {
            var boardStr = "Current Board State:\n";
            for (int i = 0; i < config.boardRows; i++)
            {
                for (int j = 0; j < config.boardCols; j++)
                {
                    var cell = Board[i, j];
                    if (cell.isOccupied && cell.occupiedBy >= 0)
                    {
                        boardStr += $"[{cell.occupiedBy}]";
                    }
                    else
                    {
                        boardStr += "[ ]";
                    }
                }
                boardStr += "\n";
            }
            Debug.Log(boardStr);
        }

        #endregion
    }

    #endregion
}