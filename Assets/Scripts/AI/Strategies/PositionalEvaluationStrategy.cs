using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using HexGame.Core;

namespace HexGame.AI
{
    /// <summary>
    /// λ���������� - ��������ʽ���������λ������ϵͳ
    /// ����ԭ�е������߼���ͬʱ�ṩ����չ��Ȩ�ص��ڻ���
    /// </summary>
    public class PositionalEvaluationStrategy : IAIStrategy, IConfigurable
    {
        private GameConfig config;
        private IWinConditionChecker winConditionChecker;

        // ����Ȩ������ (��ͨ�������ļ�����)
        [System.Serializable]
        public class EvaluationWeights
        {
            [Header("������вȨ��")]
            public float winMoveWeight = 10000f;      // һ����ʤ
            public float blockMoveWeight = 9000f;     // һ���ط�

            [Header("λ������Ȩ��")]
            public float lastMoveEvalWeight = 1.0f;   // ���ڵз��ϴ����ӵ�����Ȩ��
            public float noiseWeight = 0.1f;          // �������Ȩ��

            [Header("AIǿ�ȵ���")]
            [Range(0f, 1f)]
            public float aiStrength = 1.0f;           // AIǿ�� (0-1, Ӱ�����׼ȷ��)
            public bool enableRandomness = true;      // �Ƿ����������

            [Header("��������")]
            public bool enableDetailedLog = false;    // �Ƿ���ʾ��ϸ������־
        }

        private EvaluationWeights weights;
        private LastMoveEvaluator lastMoveEvaluator;
        private System.Random randomGenerator;

        public PositionalEvaluationStrategy(GameConfig config, IWinConditionChecker winConditionChecker = null)
        {
            this.config = config ?? throw new System.ArgumentNullException(nameof(config));
            this.winConditionChecker = winConditionChecker;

            InitializeWeights();
            InitializeComponents();

            randomGenerator = new System.Random();
        }

        #region ��ʼ��

        private void InitializeWeights()
        {
            weights = new EvaluationWeights
            {
                winMoveWeight = config.winScore,
                blockMoveWeight = config.blockScore,
                lastMoveEvalWeight = 1.0f,
                noiseWeight = 0.1f,
                aiStrength = 1.0f,
                enableRandomness = true,
                enableDetailedLog = config.enableDebugMode
            };
        }

        private void InitializeComponents()
        {
            lastMoveEvaluator = new LastMoveEvaluator(config);
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

            Vector2Int bestPosition = Vector2Int.zero;
            float bestScore = float.MinValue;
            var scoreDetails = new List<(Vector2Int pos, float score, string details)>();

            foreach (var position in availableMoves)
            {
                float score = EvaluatePosition(gameState, position);
                scoreDetails.Add((position, score, GetScoreBreakdown(gameState, position)));

                if (score > bestScore)
                {
                    bestScore = score;
                    bestPosition = position;
                }
            }

            // ������־
            if (weights.enableDetailedLog)
            {
                LogDetailedScores(scoreDetails);
            }

            // Ӧ��AIǿ�Ⱥ������
            bestPosition = ApplyAIStrengthAndRandomness(scoreDetails, bestPosition);

            return new Move(bestPosition, PlayerType.AI);
        }

        public float EvaluatePosition(GameState gameState, Vector2Int position)
        {
            float totalScore = 0f;
            var scoreBreakdown = new Dictionary<string, float>();

            // 1. ��в������� (������ȼ�)
            float threatScore = EvaluateThreatScore(gameState, position);
            totalScore += threatScore;
            scoreBreakdown["��в"] = threatScore;

            // ����ǹؼ���в��ֱ�ӷ��ظ߷�
            if (threatScore >= weights.blockMoveWeight)
            {
                return totalScore;
            }

            // 2. ���ڵз��ϴ����ӵ�λ������
            float lastMoveScore = EvaluateLastMoveScore(gameState, position);
            totalScore += lastMoveScore * weights.lastMoveEvalWeight;
            scoreBreakdown["�ϴ�����"] = lastMoveScore;

            // 3. ���������� (���ڵ���AIǿ��)
            if (weights.enableRandomness)
            {
                float noise = GetRandomNoise();
                totalScore += noise;
                scoreBreakdown["����"] = noise;
            }

            return totalScore;
        }

        #endregion

        #region ���������߼�

        /// <summary>
        /// ��в���֣����һ����ʤ��һ���ط�
        /// </summary>
        private float EvaluateThreatScore(GameState gameState, Vector2Int position)
        {
            if (winConditionChecker == null)
                return 0f;

            // ��¡����״̬����ģ��
            var boardClone = gameState.CloneBoardState();

            // ���һ����ʤ
            var aiMove = new Move(position, PlayerType.AI);
            ApplyMoveToBoard(boardClone, aiMove);

            if (winConditionChecker.HasPlayerWon(boardClone, PlayerType.AI))
            {
                return weights.winMoveWeight;
            }

            // �ָ�����״̬�����һ���ط�
            boardClone = gameState.CloneBoardState();
            var humanMove = new Move(position, PlayerType.Human);
            ApplyMoveToBoard(boardClone, humanMove);

            if (winConditionChecker.HasPlayerWon(boardClone, PlayerType.Human))
            {
                return weights.blockMoveWeight;
            }

            return 0f;
        }

        /// <summary>
        /// ���ڵз��ϴ����ӵ����� (����ԭ���߼�)
        /// </summary>
        private float EvaluateLastMoveScore(GameState gameState, Vector2Int position)
        {
            var lastPlayerMove = gameState.GetLastMoveByPlayer(PlayerType.Human);
            if (lastPlayerMove == null)
                return 0f;

            return lastMoveEvaluator.EvaluatePosition(gameState.Board, position, lastPlayerMove.Position);
        }

        /// <summary>
        /// �����������
        /// </summary>
        private float GetRandomNoise()
        {
            // ����AIǿ�ȵ���������С
            float noiseRange = weights.noiseWeight * (1f - weights.aiStrength);
            return (float)(randomGenerator.NextDouble() * 2 - 1) * noiseRange;
        }

        #endregion

        #region AIǿ�Ⱥ�����Դ���

        /// <summary>
        /// ����AIǿ�Ⱥ�����Ե�������ѡ��
        /// </summary>
        private Vector2Int ApplyAIStrengthAndRandomness(List<(Vector2Int pos, float score, string details)> scoreDetails,
                                                       Vector2Int originalBest)
        {
            // ���AIǿ��Ϊ��ߣ�ֱ�ӷ������ѡ��
            if (weights.aiStrength >= 1.0f || !weights.enableRandomness)
            {
                return originalBest;
            }

            // ����AIǿ�Ⱦ����Ƿ�ż��ѡ����Ž�
            var sortedMoves = scoreDetails.OrderByDescending(x => x.score).ToList();

            // ����ѡ��ǰ������ѡ�ĸ���
            int candidateCount = Mathf.Max(1, Mathf.RoundToInt(sortedMoves.Count * (1f - weights.aiStrength) + 1));
            candidateCount = Mathf.Min(candidateCount, sortedMoves.Count);

            // ��ǰN����ѡ�����ѡ��
            int selectedIndex = randomGenerator.Next(candidateCount);
            return sortedMoves[selectedIndex].pos;
        }

        #endregion

        #region ���ú͵���

        public void UpdateConfig(GameConfig newConfig)
        {
            config = newConfig;
            weights.winMoveWeight = config.winScore;
            weights.blockMoveWeight = config.blockScore;
            weights.enableDetailedLog = config.enableDebugMode;

            lastMoveEvaluator?.UpdateConfig(newConfig);
        }

        /// <summary>
        /// ����AIǿ�� (0=���, 1=��ǿ)
        /// </summary>
        public void SetAIStrength(float strength)
        {
            weights.aiStrength = Mathf.Clamp01(strength);
        }

        /// <summary>
        /// ��������Ȩ��
        /// </summary>
        public void SetEvaluationWeights(EvaluationWeights newWeights)
        {
            weights = newWeights ?? weights;
        }

        /// <summary>
        /// ��ȡ��ǰȨ������
        /// </summary>
        public EvaluationWeights GetCurrentWeights()
        {
            return weights;
        }

        /// <summary>
        /// ��ȡλ�����ֵ���ϸ�ֽ�
        /// </summary>
        private string GetScoreBreakdown(GameState gameState, Vector2Int position)
        {
            if (!weights.enableDetailedLog)
                return "";

            var breakdown = new System.Text.StringBuilder();

            float threatScore = EvaluateThreatScore(gameState, position);
            if (threatScore > 0)
            {
                if (threatScore >= weights.winMoveWeight)
                    breakdown.AppendLine($"  ��ʤ��: {threatScore}");
                else if (threatScore >= weights.blockMoveWeight)
                    breakdown.AppendLine($"  �ط���: {threatScore}");
            }

            float lastMoveScore = EvaluateLastMoveScore(gameState, position);
            if (lastMoveScore > 0)
            {
                breakdown.AppendLine($"  λ�÷�: {lastMoveScore:F1}");
            }

            return breakdown.ToString();
        }

        private void LogDetailedScores(List<(Vector2Int pos, float score, string details)> scoreDetails)
        {
            var sortedScores = scoreDetails.OrderByDescending(x => x.score).Take(5);

            Debug.Log("=== AIλ���������� (ǰ5��) ===");
            foreach (var item in sortedScores)
            {
                Debug.Log($"λ��({item.pos.x},{item.pos.y}): {item.score:F1}��\n{item.details}");
            }
        }

        #endregion
    }

    /// <summary>
    /// ���ڵз��ϴ����ӵ������� (����ԭ���߼�)
    /// </summary>
    public class LastMoveEvaluator
    {
        private GameConfig config;
        private List<OffsetScore> offsetScores;

        public LastMoveEvaluator(GameConfig config)
        {
            this.config = config;
            InitializeOffsetScores();
        }

        private void InitializeOffsetScores()
        {
            // ʹ�������ļ��е�����Ȩ�أ����û����ʹ��Ĭ��ֵ
            if (config.evaluationWeights != null && config.evaluationWeights.Count > 0)
            {
                offsetScores = new List<OffsetScore>(config.evaluationWeights);
            }
            else
            {
                // Ĭ�ϵ�18��ƫ��λ������ (����ԭ���߼�)
                offsetScores = new List<OffsetScore>
                {
                    new OffsetScore(new Vector2Int(0, 1), 10f, "��"),
                    new OffsetScore(new Vector2Int(0, -1), 10f, "��"),
                    new OffsetScore(new Vector2Int(-1, 1), 10f, "����"),
                    new OffsetScore(new Vector2Int(1, -1), 10f, "����"),
                    new OffsetScore(new Vector2Int(-2, 1), 10f, "Զ����"),
                    new OffsetScore(new Vector2Int(1, 1), 10f, "����"),
                    new OffsetScore(new Vector2Int(-1, -1), 10f, "����"),
                    new OffsetScore(new Vector2Int(2, -1), 10f, "Զ����"),
                    new OffsetScore(new Vector2Int(-1, 0), 8f, "��"),
                    new OffsetScore(new Vector2Int(1, 0), 8f, "��"),
                    new OffsetScore(new Vector2Int(-2, 0), 5f, "Զ��"),
                    new OffsetScore(new Vector2Int(-1, -2), 5f, "����Զ"),
                    new OffsetScore(new Vector2Int(0, -2), 5f, "Զ��"),
                    new OffsetScore(new Vector2Int(1, -2), 5f, "����Զ"),
                    new OffsetScore(new Vector2Int(2, 0), 5f, "Զ��"),
                    new OffsetScore(new Vector2Int(1, 2), 5f, "����Զ"),
                    new OffsetScore(new Vector2Int(-1, 2), 10f, "Զ��"),
                    new OffsetScore(new Vector2Int(2, -2), 10f, "Զ��")
                };
            }
        }

        public float EvaluatePosition(HexCellState[,] board, Vector2Int position, Vector2Int lastEnemyMove)
        {
            float score = 0f;

            foreach (var offsetScore in offsetScores)
            {
                Vector2Int targetPos = lastEnemyMove + offsetScore.offset;

                // ���Ŀ��λ���Ƿ����Ҫ������λ��
                if (targetPos == position)
                {
                    // ȷ����λ�������̷�Χ����δ��ռ��
                    if (IsValidPosition(targetPos) && !board[targetPos.x, targetPos.y].isOccupied)
                    {
                        score += offsetScore.score;
                    }
                }
            }

            return score;
        }

        public void UpdateConfig(GameConfig newConfig)
        {
            config = newConfig;
            InitializeOffsetScores();
        }

        private bool IsValidPosition(Vector2Int position)
        {
            return position.x >= 0 && position.x < config.boardRows &&
                   position.y >= 0 && position.y < config.boardCols;
        }
    }
}