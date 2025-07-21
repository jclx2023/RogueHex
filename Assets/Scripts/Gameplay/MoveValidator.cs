using UnityEngine;

namespace HexGame.Core
{
    /// <summary>
    /// �ƶ���֤�� - ������֤����ƶ��ĺϷ���
    /// ��һְ��ֻ�����ƶ���֤�߼������漰��Ϸ״̬����
    /// </summary>
    public class MoveValidator
    {
        private readonly GameConfig config;

        public MoveValidator(GameConfig config)
        {
            this.config = config ?? throw new System.ArgumentNullException(nameof(config));
        }

        #region �ƶ���֤

        /// <summary>
        /// ��֤�ƶ��Ƿ�Ϸ�������Ϸ��򷵻سɹ����
        /// </summary>
        public MoveResult ValidateAndApplyMove(GameState gameState, Move move)
        {
            // ������֤
            var basicValidation = ValidateBasicMove(gameState, move);
            if (!basicValidation.IsValid)
            {
                return basicValidation;
            }

            // ��Ϸ״̬��֤
            var stateValidation = ValidateGameState(gameState);
            if (!stateValidation.IsValid)
            {
                return stateValidation;
            }

            // ��һغ���֤
            var turnValidation = ValidatePlayerTurn(gameState, move);
            if (!turnValidation.IsValid)
            {
                return turnValidation;
            }

            // ������֤ͨ��
            return MoveResult.Valid(move);
        }

        /// <summary>
        /// ������֤�ƶ��Ƿ�Ϸ�����������Ϸ״̬�ͻغϣ�
        /// ��Ҫ����AIģ��ʱ�Ŀ��ټ��
        /// </summary>
        public bool IsValidMoveQuick(HexCellState[,] board, Vector2Int position)
        {
            // ������귶Χ
            if (!IsValidPosition(position))
                return false;

            // �������Ƿ��ѱ�ռ��
            return !board[position.x, position.y].isOccupied;
        }

        #endregion

        #region ������֤����

        /// <summary>
        /// ��֤�ƶ��Ļ�����Ϣ
        /// </summary>
        private MoveResult ValidateBasicMove(GameState gameState, Move move)
        {
            if (move == null)
                return MoveResult.Invalid("�ƶ�����Ϊ��");

            if (!IsValidPosition(move.Position))
                return MoveResult.Invalid($"λ�� ({move.Position.x}, {move.Position.y}) �������̷�Χ");

            var cell = gameState.GetCell(move.Position);
            if (cell == null)
                return MoveResult.Invalid($"�޷���ȡλ�� ({move.Position.x}, {move.Position.y}) �ĸ�����Ϣ");

            if (cell.isOccupied)
                return MoveResult.Invalid($"λ�� ({move.Position.x}, {move.Position.y}) �ѱ�ռ��");

            return MoveResult.Valid(move);
        }

        /// <summary>
        /// ��֤��Ϸ״̬�Ƿ������ƶ�
        /// </summary>
        private MoveResult ValidateGameState(GameState gameState)
        {
            if (gameState == null)
                return MoveResult.Invalid("��Ϸ״̬Ϊ��");

            if (gameState.CurrentPhase != GamePhase.Playing)
                return MoveResult.Invalid($"��ǰ��Ϸ�׶� ({gameState.CurrentPhase}) �������ƶ�");

            return MoveResult.Valid(null);
        }

        /// <summary>
        /// ��֤�Ƿ��ֵ�������ƶ�
        /// </summary>
        private MoveResult ValidatePlayerTurn(GameState gameState, Move move)
        {
            if (gameState.CurrentPlayer != move.Player)
                return MoveResult.Invalid($"��ǰ���� {move.Player} �Ļغϣ�Ӧ���� {gameState.CurrentPlayer} �Ļغ�");

            return MoveResult.Valid(move);
        }

        /// <summary>
        /// ���λ���Ƿ������̷�Χ��
        /// </summary>
        private bool IsValidPosition(Vector2Int position)
        {
            return position.x >= 0 && position.x < config.boardRows &&
                   position.y >= 0 && position.y < config.boardCols;
        }

        #endregion

        #region �߼���֤����ѡ���ܣ�

        /// <summary>
        /// ��֤�ƶ��Ƿ�ᵼ������ʧ�ܣ�����AI���߸�����
        /// </summary>
        public bool WouldMoveResultInImmediateLoss(GameState gameState, Move move, IWinConditionChecker winChecker)
        {
            if (winChecker == null) return false;

            // ��¡��Ϸ״̬
            var clonedBoard = gameState.CloneBoardState();

            // Ӧ���ƶ�
            GameState.ApplyMoveToBoard(clonedBoard, move);

            // �������Ƿ������һ����ʤ
            var opponent = move.Player == PlayerType.Human ? PlayerType.AI : PlayerType.Human;

            // �������п��ܵĶ����ƶ�
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
                            return true; // ���ֻ�����һ����ʤ
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// ����ƶ��Ƿ��������ʤ
        /// </summary>
        public bool WouldMoveResultInWin(GameState gameState, Move move, IWinConditionChecker winChecker)
        {
            if (winChecker == null) return false;

            // ��¡��Ϸ״̬��Ӧ���ƶ�
            var clonedBoard = gameState.CloneBoardState();
            GameState.ApplyMoveToBoard(clonedBoard, move);

            // ������ʱ��Ϸ״̬���м��
            var tempGameState = CreateTempGameState(clonedBoard);
            var result = winChecker.CheckGameResult(tempGameState);

            return result.IsGameOver && result.Winner == move.Player;
        }

        /// <summary>
        /// ��ȡλ�õ�ս�Լ�ֵ���֣������ƶ�����
        /// </summary>
        public float GetPositionStrategicValue(Vector2Int position, PlayerType player)
        {
            float value = 0f;

            // ����λ�ü�ֵ����
            var center = new Vector2(config.boardRows / 2f, config.boardCols / 2f);
            var distanceFromCenter = Vector2.Distance(position, center);
            var maxDistance = Mathf.Max(config.boardRows, config.boardCols) / 2f;
            var centerValue = (maxDistance - distanceFromCenter) / maxDistance * 2f;
            value += centerValue;

            // ��Եλ�õ���ͨ��ֵ
            if (player == PlayerType.Human) // ���������Ҫ������ͨ
            {
                if (position.y == 0 || position.y == config.boardCols - 1)
                    value += 3f; // ��Ե���Ӽ�ֵ����
            }
            else // AI��Ҫ������ͨ
            {
                if (position.x == 0 || position.x == config.boardRows - 1)
                    value += 3f;
            }

            return value;
        }

        #endregion

        #region ��������

        /// <summary>
        /// ��¡����״̬
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
        /// ������ʱ��Ϸ״̬���ڲ���
        /// </summary>
        private GameState CreateTempGameState(HexCellState[,] board)
        {
            var tempGameState = new GameState(config);

            // ��������״̬
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

        #region ���Ժ�ͳ��

        /// <summary>
        /// ��ȡ��ǰ���������кϷ��ƶ�
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