using System;
using System.Collections.Generic;
using UnityEngine;
using HexGame.Rendering;

namespace HexGame.Core
{
    #region ö�ٶ���

    /// <summary>
    /// �������
    /// </summary>
    public enum PlayerType
    {
        Human = 0,  // �������
        AI = 1      // AI���
    }

    /// <summary>
    /// ��Ϸ�׶�
    /// </summary>
    public enum GamePhase
    {
        Initializing,   // ��ʼ����
        Playing,        // ��Ϸ������
        Paused,         // ��ͣ
        GameOver,       // ��Ϸ����
        Resetting       // ������
    }

    #endregion

    #region �������ݽṹ

    /// <summary>
    /// �ƶ����ݽṹ
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
    /// �ƶ���֤���
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
    /// ��Ϸ���
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
    /// �����θ���״̬�������߼����㣩
    /// </summary>
    [System.Serializable]
    public class HexCellState
    {
        public int x, y;
        public bool isOccupied;
        public int occupiedBy; // -1: δռ��, 0: ���, 1: AI

        public HexCellState(int x, int y, bool isOccupied = false, int occupiedBy = -1)
        {
            this.x = x;
            this.y = y;
            this.isOccupied = isOccupied;
            this.occupiedBy = occupiedBy;
        }

        public Vector2Int Position => new Vector2Int(x, y);

        /// <summary>
        /// ���ø��ӱ�ָ�����ռ��
        /// </summary>
        public void SetOccupied(PlayerType player)
        {
            isOccupied = true;
            occupiedBy = (int)player;
        }

        /// <summary>
        /// ���ø��ӱ�ָ�����ռ�ݣ�ʹ��int������
        /// </summary>
        public void SetOccupied(int player)
        {
            isOccupied = true;
            occupiedBy = player;
        }

        /// <summary>
        /// ��ո���
        /// </summary>
        public void Clear()
        {
            isOccupied = false;
            occupiedBy = -1;
        }

        /// <summary>
        /// ����Ƿ�ָ�����ռ��
        /// </summary>
        public bool IsOccupiedBy(PlayerType player)
        {
            return isOccupied && occupiedBy == (int)player;
        }

        /// <summary>
        /// ����Ƿ�ָ�����ռ�ݣ�ʹ��int������
        /// </summary>
        public bool IsOccupiedBy(int player)
        {
            return isOccupied && occupiedBy == player;
        }

        /// <summary>
        /// ��ȡռ���ߵ�PlayerType������еĻ���
        /// </summary>
        public PlayerType? GetOccupiedByPlayerType()
        {
            if (!isOccupied || occupiedBy < 0)
                return null;

            return (PlayerType)occupiedBy;
        }

        /// <summary>
        /// �������Ƿ�Ϊ��
        /// </summary>
        public bool IsEmpty => !isOccupied;

        /// <summary>
        /// �������Ƿ���Чռ�ݣ�ռ����occupiedBy >= 0��
        /// </summary>
        public bool IsValidOccupied => isOccupied && occupiedBy >= 0;

        /// <summary>
        /// �������
        /// </summary>
        public HexCellState Clone()
        {
            return new HexCellState(x, y, isOccupied, occupiedBy);
        }

        /// <summary>
        /// ��HexCellת��ΪHexCellState
        /// </summary>
        public static HexCellState FromHexCell(HexCell hexCell)
        {
            if (hexCell == null)
                return null;

            return new HexCellState(hexCell.x, hexCell.y, hexCell.isOccupied, hexCell.occupiedBy);
        }

        /// <summary>
        /// ����ת��HexCell����ΪHexCellState����
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
        /// ��֤ռ��״̬��һ����
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
        /// �޸���һ�µ�״̬
        /// </summary>
        public void FixState()
        {
            if (isOccupied && occupiedBy < 0)
            {
                // ������Ϊռ�ݵ�occupiedBy��Ч����ո���
                Clear();
            }
            else if (!isOccupied && occupiedBy >= 0)
            {
                // ������Ϊδռ�ݵ���ռ���ߣ����ռ����
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
        /// �����ã���ȡ��ϸ״̬��Ϣ
        /// </summary>
        public string GetDetailedInfo()
        {
            return $"HexCellState[{x},{y}]: Occupied={isOccupied}, OccupiedBy={occupiedBy}, Valid={IsStateValid()}";
        }

        /// <summary>
        /// �Ƚ�����HexCellState�Ƿ���ͬһλ��
        /// </summary>
        public bool IsSamePosition(HexCellState other)
        {
            return other != null && x == other.x && y == other.y;
        }

        /// <summary>
        /// �Ƚ�����HexCellState��ռ��״̬�Ƿ���ͬ
        /// </summary>
        public bool HasSameOccupation(HexCellState other)
        {
            return other != null && isOccupied == other.isOccupied && occupiedBy == other.occupiedBy;
        }
    }

    #endregion

    #region ��Ϸ״̬����

    /// <summary>
    /// ��Ϸ״̬������ - ������Ϸ������״̬����
    /// </summary>
    public class GameState
    {
        // ��������
        private readonly GameConfig config;

        // ��ǰ״̬
        public GamePhase CurrentPhase { get; private set; }
        public PlayerType CurrentPlayer { get; private set; }
        public HexCellState[,] Board { get; private set; }
        public List<Move> MoveHistory { get; private set; }

        // ��Ϸͳ��
        public int TotalMoves => MoveHistory.Count;
        public DateTime GameStartTime { get; private set; }
        public TimeSpan GameDuration => DateTime.Now - GameStartTime;

        // ״̬����¼�
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

        #region ״̬��ʼ��������

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
            // ��������
            for (int i = 0; i < config.boardRows; i++)
            {
                for (int j = 0; j < config.boardCols; j++)
                {
                    Board[i, j].Clear();
                }
            }

            // ������Ϸ״̬
            MoveHistory = new List<Move>();
            CurrentPhase = GamePhase.Initializing;
            CurrentPlayer = PlayerType.Human;
            GameStartTime = DateTime.Now;

            OnBoardChanged?.Invoke();
        }

        #endregion

        #region ״̬�޸�

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

        #region ״̬��ѯ

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

        #region ���̸��ƺ�ģ��

        /// <summary>
        /// ��������״̬�����������AIģ��
        /// </summary>
        public HexCellState[,] CloneBoardState()
        {
            var clone = new HexCellState[config.boardRows, config.boardCols];
            for (int i = 0; i < config.boardRows; i++)
            {
                for (int j = 0; j < config.boardCols; j++)
                {
                    var original = Board[i, j];
                    clone[i, j] = original.Clone(); // ʹ��Clone����ȷ�����
                }
            }
            return clone;
        }

        /// <summary>
        /// �ڿ�¡��������ģ���ƶ�����Ӱ����ʵ��Ϸ״̬��
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

        #region ���Ժ���Ϣ

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