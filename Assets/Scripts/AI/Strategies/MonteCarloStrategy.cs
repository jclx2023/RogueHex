using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using HexGame.Core;

namespace HexGame.AI
{
    /// <summary>
    /// ���ؿ��޲��� - �������ģ���AI���߲���
    /// �ع���ԭ�е� MCTSStrategy���ṩ����Ч�Ϳ����õ�MCTSʵ��
    /// </summary>
    public class MonteCarloStrategy : IAIStrategy, IConfigurable
    {
        private GameConfig config;
        private IWinConditionChecker winConditionChecker;

        [System.Serializable]
        public class MCTSSettings
        {
            [Header("ģ�����")]
            [Range(10, 2000)]
            public int simulationsPerMove = 100;         // ÿ����ѡλ�õ�ģ�����

            [Range(1, 50)]
            public int maxCandidates = 10;               // ����ѡλ������

            [Header("ģ���Ż�")]
            public bool useSmartSimulation = true;       // �Ƿ�ʹ������ģ��
            public bool enableEarlyTermination = true;   // �Ƿ�������ǰ��ֹ

            [Range(0.1f, 0.9f)]
            public float earlyTerminationThreshold = 0.8f; // ��ǰ��ֹ��ֵ

            [Header("����Կ���")]
            [Range(0f, 1f)]
            public float explorationRate = 0.3f;        // ̽���ʣ����ѡ��vs����ѡ��

            public bool useProgressiveWidening = true;   // �Ƿ�ʹ�ý���ʽ��չ

            [Header("��������")]
            public bool enableParallelSimulation = false; // �Ƿ����ò���ģ��
            public int maxSimulationDepth = 100;         // ���ģ�����
        }

        private MCTSSettings settings;
        private System.Random randomGenerator;

        // ����ͳ��
        private int totalSimulations = 0;
        private float totalSimulationTime = 0f;
        private System.Diagnostics.Stopwatch simulationTimer;

        public MonteCarloStrategy(GameConfig config, IWinConditionChecker winConditionChecker)
        {
            this.config = config ?? throw new System.ArgumentNullException(nameof(config));
            this.winConditionChecker = winConditionChecker ?? throw new System.ArgumentNullException(nameof(winConditionChecker));

            InitializeSettings();
            randomGenerator = new System.Random();
            simulationTimer = new System.Diagnostics.Stopwatch();
        }

        #region ��ʼ��

        private void InitializeSettings()
        {
            settings = new MCTSSettings
            {
                simulationsPerMove = config.mctsSimulations,
                maxCandidates = 10,
                useSmartSimulation = true,
                enableEarlyTermination = true,
                earlyTerminationThreshold = 0.8f,
                explorationRate = 0.3f,
                useProgressiveWidening = true,
                enableParallelSimulation = false,
                maxSimulationDepth = 100
            };
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

            simulationTimer.Restart();

            try
            {
                // ��ȡ��ѡλ�ã������������������ܣ�
                var candidates = GetTopCandidates(gameState, availableMoves);

                LogDebug($"MCTS���� {candidates.Count} ����ѡλ�ã�ÿ��λ�� {settings.simulationsPerMove} ��ģ��");

                // ����ÿ����ѡλ��
                var evaluationResults = new List<(Vector2Int position, float winRate, int simulations)>();

                foreach (var candidate in candidates)
                {
                    float winRate = EvaluatePosition(gameState, candidate);
                    int actualSimulations = settings.simulationsPerMove; // ��������ǰ��ֹ������

                    evaluationResults.Add((candidate, winRate, actualSimulations));

                    // ������ּ���ʤ����������ǰ��ֹ������ֹͣ��������
                    if (settings.enableEarlyTermination && winRate >= settings.earlyTerminationThreshold)
                    {
                        LogDebug($"���ָ�ʤ��λ�� ({candidate.x},{candidate.y})��{winRate:P1}����ǰ��ֹ����");
                        break;
                    }
                }

                // ѡ�����λ��
                var bestResult = evaluationResults.OrderByDescending(x => x.winRate).First();

                LogDebug($"MCTSѡ��λ�� ({bestResult.position.x},{bestResult.position.y})��" +
                        $"ʤ��: {bestResult.winRate:P1}��ģ�����: {bestResult.simulations}");

                return new Move(bestResult.position, PlayerType.AI);
            }
            finally
            {
                simulationTimer.Stop();
                RecordPerformanceStats();
            }
        }

        public float EvaluatePosition(GameState gameState, Vector2Int position)
        {
            int winCount = 0;
            int totalSims = settings.simulationsPerMove;

            for (int i = 0; i < totalSims; i++)
            {
                // ��¡����״̬
                var boardClone = gameState.CloneBoardState();

                // Ӧ�ó�ʼ�ƶ�
                var initialMove = new Move(position, PlayerType.AI);
                ApplyMoveToBoard(boardClone, initialMove);

                // �������ģ��
                PlayerType? winner = SimulateRandomGame(boardClone, PlayerType.Human); // ��һ���ֵ�����

                if (winner == PlayerType.AI)
                {
                    winCount++;
                }

                // ��ǰ��ֹ���
                if (settings.enableEarlyTermination && i >= 20) // ����ģ��20�κ�ſ�����ǰ��ֹ
                {
                    float currentWinRate = (float)winCount / (i + 1);

                    // ���ʤ���Ѿ��ܸ߻�ܵͣ�������ǰ��ֹ
                    if (currentWinRate >= settings.earlyTerminationThreshold ||
                        currentWinRate <= (1f - settings.earlyTerminationThreshold))
                    {
                        totalSims = i + 1; // ����ʵ��ģ�����
                        break;
                    }
                }
            }

            float winRate = totalSims > 0 ? (float)winCount / totalSims : 0f;
            totalSimulations += totalSims;

            return winRate;
        }

        #endregion

        #region ����MCTS�㷨

        /// <summary>
        /// ģ�������Ϸֱ������
        /// </summary>
        private PlayerType? SimulateRandomGame(HexCellState[,] boardState, PlayerType currentPlayer)
        {
            int moveCount = 0;
            int maxMoves = settings.maxSimulationDepth;

            while (moveCount < maxMoves)
            {
                // �����Ϸ�Ƿ����
                if (winConditionChecker.HasPlayerWon(boardState, PlayerType.AI))
                    return PlayerType.AI;
                if (winConditionChecker.HasPlayerWon(boardState, PlayerType.Human))
                    return PlayerType.Human;

                // ��ȡ�����ƶ�
                var availableMoves = GetAvailableMovesFromBoard(boardState);
                if (availableMoves.Count == 0)
                    break; // ƽ��

                // ѡ����һ���ƶ�
                Vector2Int nextMove = SelectNextMoveInSimulation(boardState, availableMoves, currentPlayer);

                // Ӧ���ƶ�
                var move = new Move(nextMove, currentPlayer);
                ApplyMoveToBoard(boardState, move);

                // �л����
                currentPlayer = currentPlayer == PlayerType.AI ? PlayerType.Human : PlayerType.AI;
                moveCount++;
            }

            return null; // ƽ�ֻ�ﵽ������
        }

        /// <summary>
        /// ��ģ����ѡ����һ���ƶ�
        /// </summary>
        private Vector2Int SelectNextMoveInSimulation(HexCellState[,] boardState, List<Vector2Int> availableMoves, PlayerType player)
        {
            if (settings.useSmartSimulation)
            {
                // ����ģ�⣺�������Ժ�����ʽѡ��
                return SelectSmartMove(boardState, availableMoves, player);
            }
            else
            {
                // �����ѡ��
                return availableMoves[randomGenerator.Next(availableMoves.Count)];
            }
        }

        /// <summary>
        /// �����ƶ�ѡ��ƽ������ԺͲ�����
        /// </summary>
        private Vector2Int SelectSmartMove(HexCellState[,] boardState, List<Vector2Int> availableMoves, PlayerType player)
        {
            // ����̽���ʾ��������ѡ���ǲ���ѡ��
            if (randomGenerator.NextDouble() < settings.explorationRate)
            {
                // ���̽��
                return availableMoves[randomGenerator.Next(availableMoves.Count)];
            }
            else
            {
                // ����ѡ������ѡ�����Ӷȸߵ�λ��
                return SelectStrategicMove(boardState, availableMoves, player);
            }
        }

        /// <summary>
        /// ս�����ƶ�ѡ��
        /// </summary>
        private Vector2Int SelectStrategicMove(HexCellState[,] boardState, List<Vector2Int> availableMoves, PlayerType player)
        {
            var scoredMoves = new List<(Vector2Int position, float score)>();

            foreach (var move in availableMoves)
            {
                float score = EvaluateMoveInSimulation(boardState, move, player);
                scoredMoves.Add((move, score));
            }

            // ѡ�������ߵ��ƶ���������һЩ�����
            var sortedMoves = scoredMoves.OrderByDescending(x => x.score).ToList();

            // ��ǰ�������ѡ�������ѡ��
            int topChoices = Mathf.Min(3, sortedMoves.Count);
            int selectedIndex = randomGenerator.Next(topChoices);

            return sortedMoves[selectedIndex].position;
        }

        /// <summary>
        /// ��ģ���������ƶ��ļ�ֵ
        /// </summary>
        private float EvaluateMoveInSimulation(HexCellState[,] boardState, Vector2Int position, PlayerType player)
        {
            float score = 0f;

            // 1. ���Ӽ�ֵ���뼺�����ӵ�������
            int connections = CountPlayerConnections(boardState, position, player);
            score += connections * 2f;

            // 2. �߽��ֵ������Ŀ��߽�ļ�ֵ
            float boundaryValue = EvaluateBoundaryDistance(position, player);
            score += boundaryValue;

            // 3. ���ļ�ֵ���ʶ�ƫ������λ��
            float centerValue = EvaluateCenterDistance(position);
            score += centerValue * 0.5f;

            return score;
        }

        #endregion

        #region ������������

        private int CountPlayerConnections(HexCellState[,] boardState, Vector2Int position, PlayerType player)
        {
            int connections = 0;
            var directions = new Vector2Int[]
            {
                new Vector2Int(-1, 0), new Vector2Int(-1, 1),
                new Vector2Int(0, -1), new Vector2Int(0, 1),
                new Vector2Int(1, -1), new Vector2Int(1, 0)
            };

            foreach (var dir in directions)
            {
                var neighborPos = position + dir;
                if (IsValidPosition(neighborPos))
                {
                    var neighbor = boardState[neighborPos.x, neighborPos.y];
                    if (neighbor.IsOccupiedBy(player))
                    {
                        connections++;
                    }
                }
            }

            return connections;
        }

        private float EvaluateBoundaryDistance(Vector2Int position, PlayerType player)
        {
            if (player == PlayerType.Human)
            {
                // ������ң�������ͨ���������ұ߽�Խ��Խ��
                float leftDistance = position.y;
                float rightDistance = config.boardCols - 1 - position.y;
                return (config.boardCols - Mathf.Min(leftDistance, rightDistance)) / (float)config.boardCols;
            }
            else
            {
                // AI��ң�������ͨ���������±߽�Խ��Խ��
                float topDistance = position.x;
                float bottomDistance = config.boardRows - 1 - position.x;
                return (config.boardRows - Mathf.Min(topDistance, bottomDistance)) / (float)config.boardRows;
            }
        }

        private float EvaluateCenterDistance(Vector2Int position)
        {
            var center = new Vector2(config.boardRows / 2f, config.boardCols / 2f);
            float maxDistance = Mathf.Max(config.boardRows, config.boardCols) / 2f;
            float distance = Vector2.Distance(position, center);
            return (maxDistance - distance) / maxDistance;
        }

        private List<Vector2Int> GetAvailableMovesFromBoard(HexCellState[,] boardState)
        {
            var availableMoves = new List<Vector2Int>();
            int rows = boardState.GetLength(0);
            int cols = boardState.GetLength(1);

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    if (!boardState[i, j].isOccupied)
                    {
                        availableMoves.Add(new Vector2Int(i, j));
                    }
                }
            }

            return availableMoves;
        }

        private List<Vector2Int> GetTopCandidates(GameState gameState, List<Vector2Int> availableMoves)
        {
            if (availableMoves.Count <= settings.maxCandidates)
            {
                return availableMoves;
            }

            // ʹ�ü򵥵�����ʽ����ѡ���ѡλ��
            var candidates = new List<(Vector2Int position, float priority)>();

            foreach (var move in availableMoves)
            {
                float priority = CalculateMovePriority(gameState, move);
                candidates.Add((move, priority));
            }

            // �������ȼ���ߵĺ�ѡ
            return candidates
                .OrderByDescending(x => x.priority)
                .Take(settings.maxCandidates)
                .Select(x => x.position)
                .ToList();
        }

        private float CalculateMovePriority(GameState gameState, Vector2Int position)
        {
            float priority = 0f;
            var board = gameState.Board;

            // 1. ���Ӽ�ֵ
            int aiConnections = CountPlayerConnections(board, position, PlayerType.AI);
            int humanConnections = CountPlayerConnections(board, position, PlayerType.Human);
            priority += aiConnections * 3f + humanConnections * 2f; // ��϶���Ҳ�м�ֵ

            // 2. �߽��ֵ
            priority += EvaluateBoundaryDistance(position, PlayerType.AI) * 2f;

            // 3. ����ƫ��
            priority += EvaluateCenterDistance(position);

            return priority;
        }

        private bool IsValidPosition(Vector2Int position)
        {
            return position.x >= 0 && position.x < config.boardRows &&
                   position.y >= 0 && position.y < config.boardCols;
        }

        #endregion

        #region ���ú�����

        public void UpdateConfig(GameConfig newConfig)
        {
            config = newConfig;
            settings.simulationsPerMove = config.mctsSimulations;
        }

        public void SetMCTSSettings(MCTSSettings newSettings)
        {
            settings = newSettings ?? settings;
        }

        public MCTSSettings GetCurrentSettings()
        {
            return settings;
        }

        private void RecordPerformanceStats()
        {
            totalSimulationTime += simulationTimer.ElapsedMilliseconds;
        }

        public string GetPerformanceStats()
        {
            if (totalSimulations == 0)
                return "����MCTSͳ������";

            float avgSimTime = totalSimulationTime / totalSimulations * 1000f; // ת��Ϊ΢��

            return $"MCTS����ͳ��:\n" +
                   $"- ��ģ�����: {totalSimulations}\n" +
                   $"- ƽ��ÿ��ģ��ʱ��: {avgSimTime:F2}��s\n" +
                   $"- ÿ��ģ����: {settings.simulationsPerMove}\n" +
                   $"- ����ģ��: {(settings.useSmartSimulation ? "����" : "����")}\n" +
                   $"- ��ǰ��ֹ: {(settings.enableEarlyTermination ? "����" : "����")}";
        }

        public void ResetStats()
        {
            totalSimulations = 0;
            totalSimulationTime = 0f;
        }

        #endregion

        #region ����

        private void LogDebug(string message)
        {
            if (config.enableDebugMode)
            {
                Debug.Log($"[MonteCarloStrategy] {message}");
            }
        }

        #endregion
    }
}