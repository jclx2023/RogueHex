using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HexGame.Core;

namespace HexGame.Rendering
{
    /// <summary>
    /// 棋盘渲染器 - 负责棋盘的视觉表现和动画效果
    /// 管理所有HexCell的视觉更新、动画播放和特效显示
    /// </summary>
    public class BoardRenderer : MonoBehaviour, IBoardRenderer
    {
        [Header("棋盘生成设置")]
        [SerializeField] private GameObject hexCellPrefab;
        [SerializeField] private Transform boardParent;
        
        [Header("视觉设置")]
        [SerializeField] private Sprite playerSprite;
        [SerializeField] private Sprite aiSprite;
        [SerializeField] private Sprite emptySprite;
        
        [Header("颜色设置")]
        [SerializeField] private Color playerColor = Color.blue;
        [SerializeField] private Color aiColor = Color.red;
        [SerializeField] private Color emptyColor = Color.white;
        [SerializeField] private Color highlightColor = Color.yellow;
        [SerializeField] private Color winningPathColor = Color.green;
        
        [Header("动画设置")]
        [SerializeField] private float moveAnimationDuration = 0.3f;
        [SerializeField] private AnimationCurve moveAnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private bool enablePlaceAnimation = true;
        [SerializeField] private bool enableHighlightAnimation = true;
        
        [Header("特效设置")]
        [SerializeField] private GameObject placeEffectPrefab;
        [SerializeField] private GameObject winEffectPrefab;
        [SerializeField] private bool enableParticleEffects = true;
        
        [Header("调试设置")]
        [SerializeField] private bool enableDebugLog = false;
        [SerializeField] private bool showCoordinates = false;
        
        // 组件依赖
        private GameConfig config;
        private GameManager gameManager;
        
        // 棋盘状态
        private HexCell[,] hexCells;
        private Dictionary<Vector2Int, HexCell> cellLookup;
        private List<HexCell> highlightedCells;
        
        // 动画管理
        private Dictionary<Vector2Int, Coroutine> runningAnimations;
        
        #region Unity生命周期
        
        private void Awake()
        {
            InitializeComponents();
            InitializeCollections();
        }
        
        private void Start()
        {
            InitializeBoard();
            SubscribeToEvents();
            LogDebug("BoardRenderer initialized");
        }
        
        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }
        
        #endregion
        
        #region 初始化
        
        private void InitializeComponents()
        {
            // 获取GameManager和配置
            gameManager = FindObjectOfType<GameManager>();
            if (gameManager != null)
            {
                config = gameManager.GetGameConfig();
            }
            
            if (config == null)
            {
                LogError("GameConfig not found! Using default settings.");
                return;
            }
            
            // 设置棋盘父对象
            if (boardParent == null)
            {
                var boardObject = new GameObject("Board");
                boardObject.transform.SetParent(transform);
                boardParent = boardObject.transform;
            }
        }
        
        private void InitializeCollections()
        {
            cellLookup = new Dictionary<Vector2Int, HexCell>();
            highlightedCells = new List<HexCell>();
            runningAnimations = new Dictionary<Vector2Int, Coroutine>();
        }
        
        #endregion
        
        #region 棋盘生成和管理
        
        /// <summary>
        /// 初始化整个棋盘
        /// </summary>
        private void InitializeBoard()
        {
            if (config == null || hexCellPrefab == null)
            {
                LogError("Cannot initialize board: missing config or prefab");
                return;
            }
            
            GenerateBoard();
            LogDebug($"Board generated: {config.boardRows}x{config.boardCols}");
        }
        
        /// <summary>
        /// 生成棋盘格子
        /// </summary>
        private void GenerateBoard()
        {
            hexCells = new HexCell[config.boardRows, config.boardCols];
            
            for (int row = 0; row < config.boardRows; row++)
            {
                for (int col = 0; col < config.boardCols; col++)
                {
                    CreateHexCell(row, col);
                }
            }
        }
        
        /// <summary>
        /// 创建单个六边形格子
        /// </summary>
        private void CreateHexCell(int row, int col)
        {
            // 计算世界坐标位置
            Vector3 worldPosition = config.GetHexWorldPosition(new Vector2Int(row, col));
            
            // 实例化格子对象
            GameObject cellObject = Instantiate(hexCellPrefab, worldPosition, Quaternion.identity, boardParent);
            cellObject.name = $"HexCell({row},{col})";
            
            // 初始化HexCell组件
            HexCell hexCell = cellObject.GetComponent<HexCell>();
            if (hexCell != null)
            {
                hexCell.Initialize(row, col);
                
                // 设置默认精灵
                SetCellSprites(hexCell);
                
                // 添加到数组和查找表
                hexCells[row, col] = hexCell;
                cellLookup[new Vector2Int(row, col)] = hexCell;
                
                // 显示坐标（如果启用）
                if (showCoordinates)
                {
                    CreateCoordinateLabel(hexCell);
                }
            }
            else
            {
                LogError($"HexCell component not found on prefab for position ({row}, {col})");
            }
        }
        
        /// <summary>
        /// 设置格子的精灵资源
        /// </summary>
        private void SetCellSprites(HexCell hexCell)
        {
            if (hexCell != null)
            {
                hexCell.playerSprite = playerSprite;
                hexCell.aiSprite = aiSprite;
                
                // 设置初始精灵为空格子
                var spriteRenderer = hexCell.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null && emptySprite != null)
                {
                    spriteRenderer.sprite = emptySprite;
                    spriteRenderer.color = emptyColor;
                }
            }
        }
        
        /// <summary>
        /// 创建坐标标签（调试用）
        /// </summary>
        private void CreateCoordinateLabel(HexCell hexCell)
        {
            var labelObject = new GameObject($"Label({hexCell.x},{hexCell.y})");
            labelObject.transform.SetParent(hexCell.transform);
            labelObject.transform.localPosition = Vector3.zero;
            
            var textMesh = labelObject.AddComponent<TextMesh>();
            textMesh.text = $"{hexCell.x},{hexCell.y}";
            textMesh.fontSize = 12;
            textMesh.color = Color.black;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
        }
        
        #endregion
        
        #region IBoardRenderer接口实现
        
        /// <summary>
        /// 更新格子的视觉表现
        /// </summary>
        public void UpdateCellVisual(Vector2Int position, PlayerType player)
        {
            HexCell hexCell = GetHexCell(position);
            if (hexCell == null)
            {
                LogError($"Cannot update cell visual: cell not found at ({position.x}, {position.y})");
                return;
            }
            
            // 更新格子状态
            hexCell.SetOccupied((int)player);
            
            // 更新视觉表现
            var spriteRenderer = hexCell.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                // 设置精灵和颜色
                if (player == PlayerType.Human)
                {
                    spriteRenderer.sprite = playerSprite ?? emptySprite;
                    spriteRenderer.color = playerColor;
                }
                else if (player == PlayerType.AI)
                {
                    spriteRenderer.sprite = aiSprite ?? emptySprite;
                    spriteRenderer.color = aiColor;
                }
            }
            
            LogDebug($"Updated cell visual at ({position.x}, {position.y}) for player {player}");
        }
        
        /// <summary>
        /// 播放移动动画
        /// </summary>
        public void PlayMoveAnimation(Vector2Int position)
        {
            if (!enablePlaceAnimation) return;
            
            HexCell hexCell = GetHexCell(position);
            if (hexCell == null) return;
            
            // 停止该位置的现有动画
            StopAnimationAt(position);
            
            // 开始新的放置动画
            var animation = StartCoroutine(PlayPlaceAnimationCoroutine(hexCell));
            runningAnimations[position] = animation;
            
            // 播放特效
            if (enableParticleEffects && placeEffectPrefab != null)
            {
                PlayPlaceEffect(hexCell.transform.position);
            }
        }
        
        /// <summary>
        /// 高亮获胜路径
        /// </summary>
        public void HighlightWinningPath(List<Vector2Int> path)
        {
            if (path == null || path.Count == 0) return;
            
            // 清除之前的高亮
            ClearHighlights();
            
            // 高亮获胜路径上的每个格子
            foreach (var position in path)
            {
                HighlightCell(position, winningPathColor);
            }
            
            // 播放获胜特效
            if (enableParticleEffects && winEffectPrefab != null)
            {
                PlayWinEffect(path);
            }
            
            LogDebug($"Highlighted winning path with {path.Count} cells");
        }
        
        /// <summary>
        /// 清空棋盘
        /// </summary>
        public void ClearBoard()
        {
            if (hexCells == null) return;
            
            // 停止所有动画
            StopAllAnimations();
            
            // 清除所有高亮
            ClearHighlights();
            
            // 重置所有格子
            for (int row = 0; row < config.boardRows; row++)
            {
                for (int col = 0; col < config.boardCols; col++)
                {
                    var hexCell = hexCells[row, col];
                    if (hexCell != null)
                    {
                        ResetCell(hexCell);
                    }
                }
            }
            
            LogDebug("Board cleared");
        }
        
        #endregion
        
        #region 动画系统
        
        /// <summary>
        /// 播放放置动画协程
        /// </summary>
        private IEnumerator PlayPlaceAnimationCoroutine(HexCell hexCell)
        {
            var transform = hexCell.transform;
            var originalScale = transform.localScale;
            var targetScale = originalScale * 1.2f;
            
            float elapsedTime = 0f;
            
            // 放大阶段
            while (elapsedTime < moveAnimationDuration * 0.5f)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / (moveAnimationDuration * 0.5f);
                t = moveAnimationCurve.Evaluate(t);
                
                transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
                yield return null;
            }
            
            // 缩小阶段
            elapsedTime = 0f;
            while (elapsedTime < moveAnimationDuration * 0.5f)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / (moveAnimationDuration * 0.5f);
                t = moveAnimationCurve.Evaluate(t);
                
                transform.localScale = Vector3.Lerp(targetScale, originalScale, t);
                yield return null;
            }
            
            // 确保最终缩放正确
            transform.localScale = originalScale;
            
            // 从运行中的动画列表移除
            var position = new Vector2Int(hexCell.x, hexCell.y);
            if (runningAnimations.ContainsKey(position))
            {
                runningAnimations.Remove(position);
            }
        }
        
        /// <summary>
        /// 停止指定位置的动画
        /// </summary>
        private void StopAnimationAt(Vector2Int position)
        {
            if (runningAnimations.TryGetValue(position, out Coroutine animation))
            {
                if (animation != null)
                {
                    StopCoroutine(animation);
                }
                runningAnimations.Remove(position);
            }
        }
        
        /// <summary>
        /// 停止所有动画
        /// </summary>
        private void StopAllAnimations()
        {
            foreach (var animation in runningAnimations.Values)
            {
                if (animation != null)
                {
                    StopCoroutine(animation);
                }
            }
            runningAnimations.Clear();
        }
        
        #endregion
        
        #region 高亮系统
        
        /// <summary>
        /// 高亮指定格子
        /// </summary>
        private void HighlightCell(Vector2Int position, Color highlightColor)
        {
            HexCell hexCell = GetHexCell(position);
            if (hexCell == null) return;
            
            var spriteRenderer = hexCell.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                // 保存原始颜色（如果需要恢复）
                spriteRenderer.color = Color.Lerp(spriteRenderer.color, highlightColor, 0.5f);
                
                // 添加到高亮列表
                if (!highlightedCells.Contains(hexCell))
                {
                    highlightedCells.Add(hexCell);
                }
            }
        }
        
        /// <summary>
        /// 清除所有高亮
        /// </summary>
        private void ClearHighlights()
        {
            foreach (var hexCell in highlightedCells)
            {
                if (hexCell != null)
                {
                    RestoreCellColor(hexCell);
                }
            }
            highlightedCells.Clear();
        }
        
        /// <summary>
        /// 恢复格子原始颜色
        /// </summary>
        private void RestoreCellColor(HexCell hexCell)
        {
            var spriteRenderer = hexCell.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                if (hexCell.isOccupied)
                {
                    spriteRenderer.color = hexCell.occupiedBy == 0 ? playerColor : aiColor;
                }
                else
                {
                    spriteRenderer.color = emptyColor;
                }
            }
        }
        
        #endregion
        
        #region 特效系统
        
        /// <summary>
        /// 播放放置特效
        /// </summary>
        private void PlayPlaceEffect(Vector3 position)
        {
            if (placeEffectPrefab != null)
            {
                var effect = Instantiate(placeEffectPrefab, position, Quaternion.identity);
                
                // 自动销毁特效（如果没有自动销毁组件）
                Destroy(effect, 2f);
            }
        }
        
        /// <summary>
        /// 播放获胜特效
        /// </summary>
        private void PlayWinEffect(List<Vector2Int> winningPath)
        {
            if (winEffectPrefab == null || winningPath.Count == 0) return;
            
            // 在获胜路径的中心播放特效
            Vector3 centerPosition = Vector3.zero;
            foreach (var position in winningPath)
            {
                centerPosition += config.GetHexWorldPosition(position);
            }
            centerPosition /= winningPath.Count;
            
            var effect = Instantiate(winEffectPrefab, centerPosition, Quaternion.identity);
            Destroy(effect, 5f);
        }
        
        #endregion
        
        #region 辅助方法
        
        /// <summary>
        /// 获取指定位置的HexCell
        /// </summary>
        private HexCell GetHexCell(Vector2Int position)
        {
            if (cellLookup.TryGetValue(position, out HexCell hexCell))
            {
                return hexCell;
            }
            
            // 备用：从数组中获取
            if (hexCells != null && 
                position.x >= 0 && position.x < config.boardRows &&
                position.y >= 0 && position.y < config.boardCols)
            {
                return hexCells[position.x, position.y];
            }
            
            return null;
        }
        
        /// <summary>
        /// 重置格子到初始状态
        /// </summary>
        private void ResetCell(HexCell hexCell)
        {
            if (hexCell == null) return;
            
            // 重置逻辑状态
            hexCell.Initialize(hexCell.x, hexCell.y);
            
            // 重置视觉状态
            var spriteRenderer = hexCell.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = emptySprite;
                spriteRenderer.color = emptyColor;
            }
            
            // 重置变换
            hexCell.transform.localScale = Vector3.one;
        }
        
        #endregion
        
        #region 事件处理
        
        private void SubscribeToEvents()
        {
            if (gameManager != null)
            {
                GameManager.OnGameReset += HandleGameReset;
                GameManager.OnMoveExecuted += HandleMoveExecuted;
            }
        }
        
        private void UnsubscribeFromEvents()
        {
            GameManager.OnGameReset -= HandleGameReset;
            GameManager.OnMoveExecuted -= HandleMoveExecuted;
        }
        
        private void HandleGameReset()
        {
            ClearBoard();
        }
        
        private void HandleMoveExecuted(Move move)
        {
            // 这里可以添加额外的移动反馈，如音效等
            LogDebug($"Move executed: {move}");
        }
        
        #endregion
        
        #region 公共接口
        
        /// <summary>
        /// 手动生成棋盘（用于运行时重新生成）
        /// </summary>
        public void RegenerateBoard()
        {
            // 清理现有棋盘
            if (hexCells != null)
            {
                for (int row = 0; row < hexCells.GetLength(0); row++)
                {
                    for (int col = 0; col < hexCells.GetLength(1); col++)
                    {
                        if (hexCells[row, col] != null)
                        {
                            DestroyImmediate(hexCells[row, col].gameObject);
                        }
                    }
                }
            }
            
            cellLookup.Clear();
            
            // 重新生成
            InitializeBoard();
        }
        
        /// <summary>
        /// 设置动画启用状态
        /// </summary>
        public void SetAnimationEnabled(bool enabled)
        {
            enablePlaceAnimation = enabled;
        }
        
        /// <summary>
        /// 设置特效启用状态
        /// </summary>
        public void SetEffectsEnabled(bool enabled)
        {
            enableParticleEffects = enabled;
        }
        
        /// <summary>
        /// 获取棋盘中心位置
        /// </summary>
        public Vector3 GetBoardCenter()
        {
            if (config == null) return Vector3.zero;
            
            int centerRow = config.boardRows / 2;
            int centerCol = config.boardCols / 2;
            return config.GetHexWorldPosition(new Vector2Int(centerRow, centerCol));
        }
        
        /// <summary>
        /// 高亮可能的移动位置
        /// </summary>
        public void HighlightPossibleMoves(List<Vector2Int> positions)
        {
            if (positions == null) return;
            
            foreach (var position in positions)
            {
                HighlightCell(position, highlightColor);
            }
        }
        
        #endregion
        
        #region 调试和日志
        
        private void LogDebug(string message)
        {
            if (enableDebugLog)
            {
                Debug.Log($"[BoardRenderer] {message}");
            }
        }
        
        private void LogError(string message)
        {
            Debug.LogError($"[BoardRenderer] {message}");
        }
        
        #endregion
    }
}