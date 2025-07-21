using System.Collections.Generic;
using UnityEngine;

namespace HexGame.Core
{
    /// <summary>
    /// ��Ϸ���� - ʹ��ScriptableObjectʵ�ֿ����û�
    /// ���ڱ༭���д�����ͬ������Ԥ�裬֧������ʱ��̬����
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "HexGame/Game Config", order = 1)]
    public class GameConfig : ScriptableObject
    {
        [Header("== �������� ==")]
        [SerializeField, Range(5, 19)]
        private int _boardRows = 11;

        [SerializeField, Range(5, 19)]
        private int _boardCols = 11;

        [SerializeField, Range(1.0f, 3.0f)]
        private float _hexWidth = 1.9375f;

        [SerializeField, Range(0.5f, 2.0f)]
        private float _hexSpacing = 1.0f;

        [Header("== AI ���� ==")]
        [SerializeField, Range(10, 1000)]
        private int _mctsSimulations = 100;

        [SerializeField, Range(1000f, 50000f)]
        private float _winScore = 10000f;

        [SerializeField, Range(500f, 25000f)]
        private float _blockScore = 9000f;

        [SerializeField, Range(0.1f, 5.0f)]
        private float _aiThinkingDelay = 1.0f;

        [SerializeField]
        private AIMode _defaultAIMode = AIMode.Hybrid;

        [Header("== ����Ȩ������ ==")]
        [SerializeField]
        private List<OffsetScore> _evaluationWeights = new List<OffsetScore>();

        [Header("== ��Ϸ���� ==")]
        [SerializeField]
        private bool _allowUndoMove = false;

        [SerializeField]
        private bool _showPossibleMoves = true;

        [SerializeField]
        private bool _highlightLastMove = true;

        [Header("== �������� ==")]
        [SerializeField, Range(1, 10)]
        private int _maxAIThreads = 2;

        [SerializeField]
        private bool _useAsyncAI = true;

        [SerializeField]
        private bool _enableAICache = true;

        [Header("== �������� ==")]
        [SerializeField]
        private bool _enableDebugMode = false;

        [SerializeField]
        private bool _showAIThinking = false;

        [SerializeField]
        private bool _logMoveHistory = false;

        #region ���Է�����

        // ��������
        public int boardRows => _boardRows;
        public int boardCols => _boardCols;
        public float hexWidth => _hexWidth;
        public float hexSpacing => _hexSpacing;
        public float hexHeight => Mathf.Sqrt(3) / 2 * _hexWidth;
        public Vector2Int boardSize => new Vector2Int(_boardRows, _boardCols);

        // AI����
        public int mctsSimulations => _mctsSimulations;
        public float winScore => _winScore;
        public float blockScore => _blockScore;
        public float aiThinkingDelay => _aiThinkingDelay;
        public AIMode defaultAIMode => _defaultAIMode;

        // ����Ȩ��
        public List<OffsetScore> evaluationWeights => _evaluationWeights;

        // ��Ϸ����
        public bool allowUndoMove => _allowUndoMove;
        public bool showPossibleMoves => _showPossibleMoves;
        public bool highlightLastMove => _highlightLastMove;

        // ��������
        public int maxAIThreads => _maxAIThreads;
        public bool useAsyncAI => _useAsyncAI;
        public bool enableAICache => _enableAICache;

        // ��������
        public bool enableDebugMode => _enableDebugMode;
        public bool showAIThinking => _showAIThinking;
        public bool logMoveHistory => _logMoveHistory;

        #endregion

        #region Ĭ�����ó�ʼ��

        private void OnEnable()
        {
            if (_evaluationWeights == null || _evaluationWeights.Count == 0)
            {
                InitializeDefaultEvaluationWeights();
            }
        }

        private void InitializeDefaultEvaluationWeights()
        {
            _evaluationWeights = new List<OffsetScore>
            {
                // ��һȦ���ڸ��� - �߷�
                new OffsetScore(new Vector2Int(0, 1), 10f, "��"),
                new OffsetScore(new Vector2Int(0, -1), 10f, "��"),
                new OffsetScore(new Vector2Int(-1, 1), 10f, "����"),
                new OffsetScore(new Vector2Int(1, -1), 10f, "����"),
                new OffsetScore(new Vector2Int(-1, 0), 8f, "��"),
                new OffsetScore(new Vector2Int(1, 0), 8f, "��"),
                
                // �Խ�����Ҫλ�� - �߷�
                new OffsetScore(new Vector2Int(-2, 1), 10f, "Զ����"),
                new OffsetScore(new Vector2Int(1, 1), 10f, "����"),
                new OffsetScore(new Vector2Int(-1, -1), 10f, "����"),
                new OffsetScore(new Vector2Int(2, -1), 10f, "Զ����"),
                new OffsetScore(new Vector2Int(-1, 2), 10f, "Զ��"),
                new OffsetScore(new Vector2Int(2, -2), 10f, "Զ��"),
                
                // �ڶ�Ȧ���� - �еȷ���
                new OffsetScore(new Vector2Int(-2, 0), 5f, "Զ��"),
                new OffsetScore(new Vector2Int(2, 0), 5f, "Զ��"),
                new OffsetScore(new Vector2Int(0, -2), 5f, "Զ��"),
                new OffsetScore(new Vector2Int(0, 2), 5f, "Զ��"),
                new OffsetScore(new Vector2Int(-1, -2), 5f, "����Զ"),
                new OffsetScore(new Vector2Int(1, -2), 5f, "����Զ"),
                new OffsetScore(new Vector2Int(1, 2), 5f, "����Զ"),
                new OffsetScore(new Vector2Int(-2, 2), 3f, "Զ����"),
            };
        }

        #endregion

        #region ������֤

        private void OnValidate()
        {
            // ȷ�����̳ߴ�Ϊ�������Գ��ԣ�
            if (_boardRows % 2 == 0) _boardRows++;
            if (_boardCols % 2 == 0) _boardCols++;

            // ȷ���������ú���
            if (_blockScore >= _winScore)
                _blockScore = _winScore * 0.9f;

            // ����MCTSģ������Է�ֹ��������
            if (_mctsSimulations > 500)
                _mctsSimulations = 500;
        }

        #endregion

        #region ���ù��߷���

        /// <summary>
        /// ��ȡ�����������������е�λ��
        /// </summary>
        public Vector3 GetHexWorldPosition(Vector2Int hexCoord)
        {
            float xOffset = _hexWidth * 0.75f;
            float yOffset = hexHeight;
            float centerOffsetX = Mathf.Floor(_boardCols * xOffset) / 2f;

            float xPos = (hexCoord.y + hexCoord.x) * xOffset - centerOffsetX;
            float yPos = hexCoord.x * yOffset + (hexCoord.y * hexHeight / 2) - hexCoord.x * (hexHeight * 1.5f);

            return new Vector3(xPos, yPos, 0) * _hexSpacing;
        }

        /// <summary>
        /// ��������Ƿ������̷�Χ��
        /// </summary>
        public bool IsValidCoordinate(Vector2Int coord)
        {
            return coord.x >= 0 && coord.x < _boardRows &&
                   coord.y >= 0 && coord.y < _boardCols;
        }

        /// <summary>
        /// ��ȡָ��λ�õ�������������
        /// </summary>
        public List<Vector2Int> GetNeighborCoordinates(Vector2Int center)
        {
            var neighbors = new List<Vector2Int>();
            var directions = new Vector2Int[]
            {
                new Vector2Int(-1, 0),  // ��
                new Vector2Int(-1, 1),  // ����
                new Vector2Int(0, -1),  // ��
                new Vector2Int(0, 1),   // ��
                new Vector2Int(1, -1),  // ����
                new Vector2Int(1, 0)    // ��
            };

            foreach (var dir in directions)
            {
                var neighbor = center + dir;
                if (IsValidCoordinate(neighbor))
                {
                    neighbors.Add(neighbor);
                }
            }

            return neighbors;
        }

        /// <summary>
        /// �������
        /// </summary>
        public GameConfig Clone()
        {
            var clone = CreateInstance<GameConfig>();

            // ���������ֶ�
            clone._boardRows = this._boardRows;
            clone._boardCols = this._boardCols;
            clone._hexWidth = this._hexWidth;
            clone._hexSpacing = this._hexSpacing;
            clone._mctsSimulations = this._mctsSimulations;
            clone._winScore = this._winScore;
            clone._blockScore = this._blockScore;
            clone._aiThinkingDelay = this._aiThinkingDelay;
            clone._defaultAIMode = this._defaultAIMode;
            clone._allowUndoMove = this._allowUndoMove;
            clone._showPossibleMoves = this._showPossibleMoves;
            clone._highlightLastMove = this._highlightLastMove;
            clone._maxAIThreads = this._maxAIThreads;
            clone._useAsyncAI = this._useAsyncAI;
            clone._enableAICache = this._enableAICache;
            clone._enableDebugMode = this._enableDebugMode;
            clone._showAIThinking = this._showAIThinking;
            clone._logMoveHistory = this._logMoveHistory;

            // �������Ȩ��
            clone._evaluationWeights = new List<OffsetScore>();
            foreach (var weight in this._evaluationWeights)
            {
                clone._evaluationWeights.Add(new OffsetScore(weight.offset, weight.score, weight.description));
            }

            return clone;
        }

        #endregion

        #region Ԥ������

        [ContextMenu("Set Easy AI")]
        public void SetEasyAI()
        {
            _mctsSimulations = 50;
            _aiThinkingDelay = 0.5f;
            _defaultAIMode = AIMode.PositionalOnly;
        }

        [ContextMenu("Set Medium AI")]
        public void SetMediumAI()
        {
            _mctsSimulations = 100;
            _aiThinkingDelay = 1.0f;
            _defaultAIMode = AIMode.Hybrid;
        }

        [ContextMenu("Set Hard AI")]
        public void SetHardAI()
        {
            _mctsSimulations = 300;
            _aiThinkingDelay = 2.0f;
            _defaultAIMode = AIMode.MCTSFocused;
        }

        [ContextMenu("Reset to Default")]
        public void ResetToDefault()
        {
            _boardRows = 11;
            _boardCols = 11;
            _hexWidth = 1.9375f;
            _hexSpacing = 1.0f;
            _mctsSimulations = 100;
            _winScore = 10000f;
            _blockScore = 9000f;
            _aiThinkingDelay = 1.0f;
            _defaultAIMode = AIMode.Hybrid;
            InitializeDefaultEvaluationWeights();
        }

        #endregion
    }

    #region ������ݽṹ

    /// <summary>
    /// AIģʽö��
    /// </summary>
    public enum AIMode
    {
        PositionalOnly,     // ��ʹ��λ������
        MCTSFocused,        // ��Ҫʹ��MCTS
        Hybrid,             // ��ϲ���
        ThreatFocused       // רע��в���
    }

    /// <summary>
    /// ƫ��λ�ü����������
    /// </summary>
    [System.Serializable]
    public class OffsetScore
    {
        [SerializeField] public Vector2Int offset;
        [SerializeField, Range(0f, 20f)] public float score;
        [SerializeField] public string description;

        public OffsetScore(Vector2Int offset, float score, string description = "")
        {
            this.offset = offset;
            this.score = score;
            this.description = description;
        }
    }

    #endregion
}