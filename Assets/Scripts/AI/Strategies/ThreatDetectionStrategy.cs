using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using HexGame.Core;

namespace HexGame.AI
{
    /// <summary>
    /// ��в������ - ר�ż�������в�ͻ���
    /// �ع���ԭ�е� FrontierThreatStrategy���ṩ����Ч����в���
    /// </summary>
    public class ThreatDetectionStrategy : IAIStrategy, IConfigurable
    {
        private GameConfig config;
        private IWinConditionChecker winConditionChecker;

        // ǰ�ظ��ӹ��� - ֻ�����ռ������Χ�Ŀո����Ч��
        private HashSet<Vector2Int> aiFrontier;
        private HashSet<Vector2Int> humanFrontier;

        // �����η���
        private static readonly Vector2Int[] HexDirections = new Vector2Int[]
        {
            new Vector2Int(-1, 0),  // ��
            new Vector2Int(-1, 1),  // ����
            new Vector2Int(0, -1),  // ��
            new Vector2Int(0, 1),   // ��
            new Vector2Int(1, -1),  // ����
            new Vector2Int(1, 0)    // ��
        };

        [System.Serializable]
        public class ThreatWeights
        {
            [Header("��в���Ȩ��")]
            public float immediateWinWeight = 10000f;     // һ����ʤ
            public float immediateBlockWeight = 9000f;    // һ���ط�
            public float twoStepThreatWeight = 500f;      // ������в
            public float connectionWeight = 100f;         // ���Ӽ�ֵ

            [Header("������")]
            public int maxThreatDepth = 2;                // �����в������
            public bool enableMultiStepThreat = true;     // �Ƿ����öಽ��в���

            [Header("�����Ż�")]
            public bool useFrontierOptimization = true;   // �Ƿ�ʹ��ǰ���Ż�
            public int maxCandidatesCheck = 20;           // ����ѡ�������
        }

        private ThreatWeights weights;
        private bool isInitialized = false;

        public ThreatDetectionStrategy(GameConfig config, IWinConditionChecker winConditionChecker)
        {
            this.config = config ?? throw new System.ArgumentNullException(nameof(config));
            this.winConditionChecker = winConditionChecker ?? throw new System.ArgumentNullException(nameof(winConditionChecker));

            InitializeWeights();
            InitializeFrontiers();
        }

        #region ��ʼ��

        private void InitializeWeights()
        {
            weights = new ThreatWeights
            {
                immediateWinWeight = config.winScore,
                immediateBlockWeight = config.blockScore,
                twoStepThreatWeight = 500f,
                connectionWeight = 100f,
                maxThreatDepth = 2,
                enableMultiStepThreat = true,
                useFrontierOptimization = true,
                maxCandidatesCheck = 20
            };
        }

        private void InitializeFrontiers()
        {
            aiFrontier = new HashSet<Vector2Int>();
            humanFrontier = new HashSet<Vector2Int>();
            isInitialized = false;
        }

        /// <summary>
        /// ���ݵ�ǰ����״̬�ؽ�ǰ�ظ��Ӽ���
        /// </summary>
        private void RebuildFrontiers(HexCellState[,] board)
        {
            if (!weights.useFrontierOptimization)
                return;

            aiFrontier.Clear();
            humanFrontier.Clear();

            int rows = board.GetLength(0);
            int cols = board.GetLength(1);

            // ����������ռ���ӣ������ھӿո�����Ӧ��ǰ��
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    var cell = board[i, j];
                    if (cell.isOccupied && cell.occupiedBy >= 0)
                    {
                        PlayerType player = (PlayerType)cell.occupiedBy;
                        UpdateFrontierForCell(board, new Vector2Int(i, j), player);
                    }
                }
            }

            isInitialized = true;
        }

        private void UpdateFrontierForCell(HexCellState[,] board, Vector2Int position, PlayerType player)
        {
            var targetFrontier = player == PlayerType.AI ? aiFrontier : humanFrontier;

            foreach (var direction in HexDirections)
            {
                var neighborPos = position + direction;
                if (IsValidPosition(neighborPos) && !board[neighborPos.x, neighborPos.y].isOccupied)
                {
                    targetFrontier.Add(neighborPos);
                }
            }
        }

        #endregion

        #region ���̲�����������

        /// <summary>
        /// �ڿ�¡��������Ӧ���ƶ�
        /// </summary>
        private void ApplyMoveToBoard(HexCellState[,] board, Move move)
        {
            if (board != null && move != null)
            {
                var cell = board[move.Position.x, move.Position.y];
                cell.SetOccupied(move.Player);
            }
        }

        #endregion

        #region IAIStrategy �ӿ�ʵ��

        public Move GetBestMove(GameState gameState, List<Vector2Int> availableMoves)
        {
            if (availableMoves == null || availableMoves.Count == 0)
                return null;

            var board = gameState.Board;

            // ȷ��ǰ���ѳ�ʼ��
            if (!isInitialized)
            {
                RebuildFrontiers(board);
            }

            // 1. ���һ����ʤ
            var winningMove = GetWinningMove(gameState, PlayerType.AI);
            if (winningMove != null)
            {
                LogDebug("����һ����ʤ!");
                return winningMove;
            }

            // 2. ���һ���ط�
            var blockingMove = GetBlockingMove(gameState, PlayerType.Human);
            if (blockingMove != null)
            {
                LogDebug("����һ���ط�!");
                return blockingMove;
            }

            // 3. ���û�н�����в�����������вֵ��λ��
            Vector2Int bestPosition = Vector2Int.zero;
            float bestScore = float.MinValue;

            // ʹ��ǰ���Ż���ֻ���ǰ�ظ���
            var candidatePositions = GetCandidatePositions(availableMoves);

            foreach (var position in candidatePositions)
            {
                float score = EvaluatePosition(gameState, position);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestPosition = position;
                }
            }

            if (bestScore > float.MinValue)
            {
                LogDebug($"��в���ѡ��λ�� ({bestPosition.x},{bestPosition.y})����вֵ: {bestScore:F1}");
                return new Move(bestPosition, PlayerType.AI);
            }

            return null; // ���������Դ���
        }

        public float EvaluatePosition(GameState gameState, Vector2Int position)
        {
            float totalScore = 0f;

            // 1. һ����в���
            float immediateScore = EvaluateImmediateThreat(gameState, position);
            totalScore += immediateScore;

            // ����ǹؼ���в��ֱ�ӷ���
            if (immediateScore >= weights.immediateBlockWeight)
            {
                return immediateScore;
            }

            // 2. �ಽ��в��� (�������)
            if (weights.enableMultiStepThreat)
            {
                float multiStepScore = EvaluateMultiStepThreat(gameState, position);
                totalScore += multiStepScore;
            }

            // 3. ���Ӽ�ֵ����
            float connectionScore = EvaluateConnectionValue(gameState, position);
            totalScore += connectionScore;

            return totalScore;
        }

        #endregion

        #region ��в�����ķ���

        /// <summary>
        /// ��ȡAI��һ����ʤ�ƶ�
        /// </summary>
        public Move GetWinningMove(GameState gameState, PlayerType player)
        {
            var candidatePositions = GetCandidatePositions(gameState.GetEmptyPositions());

            foreach (var position in candidatePositions)
            {
                if (IsWinningMove(gameState, position, player))
                {
                    return new Move(position, player);
                }
            }

            return null;
        }

        /// <summary>
        /// ��ȡ��ֹ���ֻ�ʤ���ƶ�
        /// </summary>
        public Move GetBlockingMove(GameState gameState, PlayerType opponentPlayer)
        {
            var candidatePositions = GetCandidatePositions(gameState.GetEmptyPositions());

            foreach (var position in candidatePositions)
            {
                if (IsWinningMove(gameState, position, opponentPlayer))
                {
                    return new Move(position, PlayerType.AI); // AIռ�����λ������ֹ����
                }
            }

            return null;
        }

        /// <summary>
        /// �����ָ��λ�������Ƿ�����ָ����һ�ʤ
        /// </summary>
        private bool IsWinningMove(GameState gameState, Vector2Int position, PlayerType player)
        {
            // ��¡����״̬
            var boardClone = gameState.CloneBoardState();

            // ģ������
            var move = new Move(position, player);
            ApplyMoveToBoard(boardClone, move);

            // ����Ƿ��ʤ
            return winConditionChecker.HasPlayerWon(boardClone, player);
        }

        /// <summary>
        /// ����һ����в
        /// </summary>
        private float EvaluateImmediateThreat(GameState gameState, Vector2Int position)
        {
            // ���AIһ����ʤ
            if (IsWinningMove(gameState, position, PlayerType.AI))
            {
                return weights.immediateWinWeight;
            }

            // �����ֹ����һ����ʤ
            if (IsWinningMove(gameState, position, PlayerType.Human))
            {
                return weights.immediateBlockWeight;
            }

            return 0f;
        }

        /// <summary>
        /// �����ಽ��в
        /// </summary>
        private float EvaluateMultiStepThreat(GameState gameState, Vector2Int position)
        {
            float score = 0f;

            // ���������в��AI�ڴ�λ�����Ӻ���һ���Ƿ��б�ʤ����
            var boardClone = gameState.CloneBoardState();
            var aiMove = new Move(position, PlayerType.AI);
            GameState.ApplyMoveToBoard(boardClone, aiMove);

            // ������ʱ��Ϸ״̬
            var tempGameState = CreateTempGameState(boardClone);
            var nextMovePositions = tempGameState.GetEmptyPositions().Take(10); // ���Ƽ������

            int aiWinningMoves = 0;
            foreach (var nextPos in nextMovePositions)
            {
                if (IsWinningMove(tempGameState, nextPos, PlayerType.AI))
                {
                    aiWinningMoves++;
                }
            }

            // ����ж��������ʤ���ᣬ����һ��ǿ��λ��
            if (aiWinningMoves > 0)
            {
                score += weights.twoStepThreatWeight * aiWinningMoves;
            }

            return score;
        }

        /// <summary>
        /// �������Ӽ�ֵ
        /// </summary>
        private float EvaluateConnectionValue(GameState gameState, Vector2Int position)
        {
            float score = 0f;
            var board = gameState.Board;

            // �����뼺�����ӵ�������
            int aiConnections = 0;
            foreach (var direction in HexDirections)
            {
                var neighborPos = position + direction;
                if (IsValidPosition(neighborPos))
                {
                    var neighbor = board[neighborPos.x, neighborPos.y];
                    if (neighbor.IsOccupiedBy(PlayerType.AI))
                    {
                        aiConnections++;
                    }
                }
            }

            // ����Խ�࣬��ֵԽ��
            score += aiConnections * weights.connectionWeight;

            return score;
        }

        #endregion

        #region �Ż��͸�������

        private List<Vector2Int> GetCandidatePositions(List<Vector2Int> availableMoves)
        {
            if (!weights.useFrontierOptimization || !isInitialized)
            {
                // �����ʹ��ǰ���Ż����������п���λ�ã�������������
                return availableMoves.Take(weights.maxCandidatesCheck).ToList();
            }

            // ʹ��ǰ���Ż������ȼ��ǰ�ظ���
            var candidates = new HashSet<Vector2Int>();

            // ���AIǰ�ظ���
            foreach (var pos in aiFrontier)
            {
                if (availableMoves.Contains(pos))
                {
                    candidates.Add(pos);
                }
            }

            // �������ǰ�ظ��ӣ�������ϣ�
            foreach (var pos in humanFrontier)
            {
                if (availableMoves.Contains(pos))
                {
                    candidates.Add(pos);
                }
            }

            // ���ǰ�ظ��Ӳ����������������λ��
            if (candidates.Count < weights.maxCandidatesCheck)
            {
                foreach (var pos in availableMoves)
                {
                    candidates.Add(pos);
                    if (candidates.Count >= weights.maxCandidatesCheck)
                        break;
                }
            }

            return candidates.ToList();
        }

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

        private bool IsValidPosition(Vector2Int position)
        {
            return position.x >= 0 && position.x < config.boardRows &&
                   position.y >= 0 && position.y < config.boardCols;
        }

        #endregion

        #region IConfigurable �ӿ�

        public void UpdateConfig(GameConfig newConfig)
        {
            config = newConfig;
            weights.immediateWinWeight = config.winScore;
            weights.immediateBlockWeight = config.blockScore;

            // ���³�ʼ��ǰ��
            InitializeFrontiers();
        }

        #endregion

        #region �����ӿڷ���

        /// <summary>
        /// �ֶ�����ǰ�ظ��ӣ������̷����仯ʱ���ã�
        /// </summary>
        public void UpdateFrontiers(GameState gameState, Move lastMove)
        {
            if (!weights.useFrontierOptimization)
                return;

            if (lastMove != null)
            {
                // �Ƴ���ռ�ݵ�λ��
                aiFrontier.Remove(lastMove.Position);
                humanFrontier.Remove(lastMove.Position);

                // ����µ�ǰ�ظ���
                UpdateFrontierForCell(gameState.Board, lastMove.Position, lastMove.Player);
            }
            else
            {
                // ��ȫ�ؽ�ǰ��
                RebuildFrontiers(gameState.Board);
            }
        }

        /// <summary>
        /// ��ȡ��ǰǰ��ͳ����Ϣ
        /// </summary>
        public string GetFrontierStats()
        {
            return $"ǰ��ͳ��:\n" +
                   $"- AIǰ�ظ���: {aiFrontier.Count}\n" +
                   $"- ����ǰ�ظ���: {humanFrontier.Count}\n" +
                   $"- ǰ���Ż�: {(weights.useFrontierOptimization ? "����" : "����")}";
        }

        /// <summary>
        /// ������в���Ȩ��
        /// </summary>
        public void SetThreatWeights(ThreatWeights newWeights)
        {
            weights = newWeights ?? weights;
        }

        #endregion

        #region ����

        private void LogDebug(string message)
        {
            if (config.enableDebugMode)
            {
                Debug.Log($"[ThreatDetectionStrategy] {message}");
            }
        }

        #endregion
    }
}