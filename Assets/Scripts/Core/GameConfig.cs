using System.Collections.Generic;
using UnityEngine;

namespace HexGame.Core
{
    /// <summary>
    /// 游戏配置 - 使用ScriptableObject实现可配置化
    /// 可在编辑器中创建不同的配置预设，支持运行时动态调整
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "HexGame/Game Config", order = 1)]
    public class GameConfig : ScriptableObject
    {
        [Header("== 棋盘设置 ==")]
        [SerializeField, Range(5, 19)]
        private int _boardRows = 11;

        [SerializeField, Range(5, 19)]
        private int _boardCols = 11;

        [SerializeField, Range(1.0f, 3.0f)]
        private float _hexWidth = 1.9375f;

        [SerializeField, Range(0.5f, 2.0f)]
        private float _hexSpacing = 1.0f;

        [Header("== AI 设置 ==")]
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

        [Header("== 评分权重配置 ==")]
        [SerializeField]
        private List<OffsetScore> _evaluationWeights = new List<OffsetScore>();

        [Header("== 游戏规则 ==")]
        [SerializeField]
        private bool _allowUndoMove = false;

        [SerializeField]
        private bool _showPossibleMoves = true;

        [SerializeField]
        private bool _highlightLastMove = true;

        [Header("== 性能设置 ==")]
        [SerializeField, Range(1, 10)]
        private int _maxAIThreads = 2;

        [SerializeField]
        private bool _useAsyncAI = true;

        [SerializeField]
        private bool _enableAICache = true;

        [Header("== 调试设置 ==")]
        [SerializeField]
        private bool _enableDebugMode = false;

        [SerializeField]
        private bool _showAIThinking = false;

        [SerializeField]
        private bool _logMoveHistory = false;

        #region 属性访问器

        // 棋盘属性
        public int boardRows => _boardRows;
        public int boardCols => _boardCols;
        public float hexWidth => _hexWidth;
        public float hexSpacing => _hexSpacing;
        public float hexHeight => Mathf.Sqrt(3) / 2 * _hexWidth;
        public Vector2Int boardSize => new Vector2Int(_boardRows, _boardCols);

        // AI属性
        public int mctsSimulations => _mctsSimulations;
        public float winScore => _winScore;
        public float blockScore => _blockScore;
        public float aiThinkingDelay => _aiThinkingDelay;
        public AIMode defaultAIMode => _defaultAIMode;

        // 评分权重
        public List<OffsetScore> evaluationWeights => _evaluationWeights;

        // 游戏规则
        public bool allowUndoMove => _allowUndoMove;
        public bool showPossibleMoves => _showPossibleMoves;
        public bool highlightLastMove => _highlightLastMove;

        // 性能设置
        public int maxAIThreads => _maxAIThreads;
        public bool useAsyncAI => _useAsyncAI;
        public bool enableAICache => _enableAICache;

        // 调试设置
        public bool enableDebugMode => _enableDebugMode;
        public bool showAIThinking => _showAIThinking;
        public bool logMoveHistory => _logMoveHistory;

        #endregion

        #region 默认配置初始化

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
                // 第一圈相邻格子 - 高分
                new OffsetScore(new Vector2Int(0, 1), 10f, "右"),
                new OffsetScore(new Vector2Int(0, -1), 10f, "左"),
                new OffsetScore(new Vector2Int(-1, 1), 10f, "右上"),
                new OffsetScore(new Vector2Int(1, -1), 10f, "左下"),
                new OffsetScore(new Vector2Int(-1, 0), 8f, "上"),
                new OffsetScore(new Vector2Int(1, 0), 8f, "下"),
                
                // 对角线重要位置 - 高分
                new OffsetScore(new Vector2Int(-2, 1), 10f, "远右上"),
                new OffsetScore(new Vector2Int(1, 1), 10f, "右下"),
                new OffsetScore(new Vector2Int(-1, -1), 10f, "左上"),
                new OffsetScore(new Vector2Int(2, -1), 10f, "远左下"),
                new OffsetScore(new Vector2Int(-1, 2), 10f, "远右"),
                new OffsetScore(new Vector2Int(2, -2), 10f, "远左"),
                
                // 第二圈格子 - 中等分数
                new OffsetScore(new Vector2Int(-2, 0), 5f, "远上"),
                new OffsetScore(new Vector2Int(2, 0), 5f, "远下"),
                new OffsetScore(new Vector2Int(0, -2), 5f, "远左"),
                new OffsetScore(new Vector2Int(0, 2), 5f, "远右"),
                new OffsetScore(new Vector2Int(-1, -2), 5f, "左上远"),
                new OffsetScore(new Vector2Int(1, -2), 5f, "左下远"),
                new OffsetScore(new Vector2Int(1, 2), 5f, "右下远"),
                new OffsetScore(new Vector2Int(-2, 2), 3f, "远右上"),
            };
        }

        #endregion

        #region 配置验证

        private void OnValidate()
        {
            // 确保棋盘尺寸为奇数（对称性）
            if (_boardRows % 2 == 0) _boardRows++;
            if (_boardCols % 2 == 0) _boardCols++;

            // 确保分数设置合理
            if (_blockScore >= _winScore)
                _blockScore = _winScore * 0.9f;

            // 限制MCTS模拟次数以防止性能问题
            if (_mctsSimulations > 500)
                _mctsSimulations = 500;
        }

        #endregion

        #region 配置工具方法

        /// <summary>
        /// 获取六边形在世界坐标中的位置
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
        /// 检查坐标是否在棋盘范围内
        /// </summary>
        public bool IsValidCoordinate(Vector2Int coord)
        {
            return coord.x >= 0 && coord.x < _boardRows &&
                   coord.y >= 0 && coord.y < _boardCols;
        }

        /// <summary>
        /// 获取指定位置的所有相邻坐标
        /// </summary>
        public List<Vector2Int> GetNeighborCoordinates(Vector2Int center)
        {
            var neighbors = new List<Vector2Int>();
            var directions = new Vector2Int[]
            {
                new Vector2Int(-1, 0),  // 上
                new Vector2Int(-1, 1),  // 右上
                new Vector2Int(0, -1),  // 左
                new Vector2Int(0, 1),   // 右
                new Vector2Int(1, -1),  // 左下
                new Vector2Int(1, 0)    // 下
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
        /// 创建深拷贝
        /// </summary>
        public GameConfig Clone()
        {
            var clone = CreateInstance<GameConfig>();

            // 复制所有字段
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

            // 深拷贝评分权重
            clone._evaluationWeights = new List<OffsetScore>();
            foreach (var weight in this._evaluationWeights)
            {
                clone._evaluationWeights.Add(new OffsetScore(weight.offset, weight.score, weight.description));
            }

            return clone;
        }

        #endregion

        #region 预设配置

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

    #region 相关数据结构

    /// <summary>
    /// AI模式枚举
    /// </summary>
    public enum AIMode
    {
        PositionalOnly,     // 仅使用位置评估
        MCTSFocused,        // 主要使用MCTS
        Hybrid,             // 混合策略
        ThreatFocused       // 专注威胁检测
    }

    /// <summary>
    /// 偏移位置及其分数配置
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