using UnityEngine;
using HexGame.Core;

namespace HexGame.Rendering
{
    /// <summary>
    /// 六边形格子 - 棋盘上的单个格子组件
    /// 负责格子的视觉表现、交互检测和状态同步
    /// </summary>
    public class HexCell : MonoBehaviour
    {
        [Header("格子状态")]
        public int x, y;
        public bool isOccupied = false;
        public int occupiedBy = -1; // -1: 未占据, 0: 玩家, 1: AI
        
        [Header("视觉资源")]
        public Sprite playerSprite; // Player 精灵
        public Sprite aiSprite;     // AI 精灵
        public Sprite emptySprite;  // 空格子精灵
        
        [Header("交互设置")]
        [SerializeField] private bool isInteractable = true;
        [SerializeField] private LayerMask interactionLayer = 1;
        
        // 组件引用
        private SpriteRenderer spriteRenderer;
        private Collider2D cellCollider;
        
        // 状态同步
        public HexCellState cellState { get; private set; }
        
        // 视觉状态
        private Color originalColor;
        private Vector3 originalScale;
        
        #region Unity生命周期
        
        private void Awake()
        {
            InitializeComponents();
        }
        
        private void Start()
        {
            InitializeVisualState();
        }
        
        #endregion
        
        #region 初始化
        
        private void InitializeComponents()
        {
            // 获取SpriteRenderer组件
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }
            
            // 获取或添加Collider2D
            cellCollider = GetComponent<Collider2D>();
            if (cellCollider == null)
            {
                // 添加CircleCollider2D作为默认碰撞器
                var circleCollider = gameObject.AddComponent<CircleCollider2D>();
                circleCollider.radius = 0.5f;
                cellCollider = circleCollider;
            }
            
            // 设置图层
            gameObject.layer = Mathf.RoundToInt(Mathf.Log(interactionLayer.value, 2));
        }
        
        private void InitializeVisualState()
        {
            // 保存原始视觉状态
            if (spriteRenderer != null)
            {
                originalColor = spriteRenderer.color;
            }
            originalScale = transform.localScale;
            
            // 初始化为空格子状态
            UpdateVisualState();
        }
        
        #endregion
        
        #region 公共接口
        
        /// <summary>
        /// 初始化格子坐标和状态
        /// </summary>
        public void Initialize(int x, int y)
        {
            this.x = x;
            this.y = y;
            this.isOccupied = false;
            this.occupiedBy = -1;
            
            // 初始化逻辑状态
            cellState = new HexCellState(x, y, isOccupied, occupiedBy);
            
            // 更新视觉状态
            UpdateVisualState();
        }
        
        /// <summary>
        /// 设置格子被占据（int版本 - 兼容原有代码）
        /// </summary>
        public void SetOccupied(int player)
        {
            if (player < -1 || player > 1)
            {
                Debug.LogWarning($"Invalid player value: {player}. Expected -1, 0, or 1.");
                return;
            }
            
            isOccupied = player >= 0;
            occupiedBy = player;
            
            // 同步逻辑状态
            if (cellState != null)
            {
                if (isOccupied)
                {
                    cellState.SetOccupied(player);
                }
                else
                {
                    cellState.Clear();
                }
            }
            
            // 更新视觉表现
            UpdateVisualState();
        }
        
        /// <summary>
        /// 设置格子被占据（PlayerType版本 - 新系统）
        /// </summary>
        public void SetOccupied(PlayerType player)
        {
            SetOccupied((int)player);
        }
        
        /// <summary>
        /// 清空格子
        /// </summary>
        public void Clear()
        {
            SetOccupied(-1);
        }
        
        /// <summary>
        /// 检查是否被指定玩家占据
        /// </summary>
        public bool IsOccupiedBy(int player)
        {
            return isOccupied && occupiedBy == player;
        }
        
        /// <summary>
        /// 检查是否被指定玩家占据（PlayerType版本）
        /// </summary>
        public bool IsOccupiedBy(PlayerType player)
        {
            return IsOccupiedBy((int)player);
        }
        
        /// <summary>
        /// 获取格子位置
        /// </summary>
        public Vector2Int GetPosition()
        {
            return new Vector2Int(x, y);
        }
        
        /// <summary>
        /// 获取占据者的PlayerType
        /// </summary>
        public PlayerType? GetOccupiedByPlayerType()
        {
            if (!isOccupied || occupiedBy < 0)
                return null;
            
            return (PlayerType)occupiedBy;
        }
        
        #endregion
        
        #region 视觉更新
        
        /// <summary>
        /// 更新格子的视觉状态
        /// </summary>
        private void UpdateVisualState()
        {
            if (spriteRenderer == null) return;
            
            if (isOccupied)
            {
                // 设置占据状态的精灵和颜色
                if (occupiedBy == 0) // 人类玩家
                {
                    spriteRenderer.sprite = playerSprite ?? emptySprite;
                }
                else if (occupiedBy == 1) // AI玩家
                {
                    spriteRenderer.sprite = aiSprite ?? emptySprite;
                }
                
                // 可以在这里设置不同的颜色
                // spriteRenderer.color = occupiedBy == 0 ? Color.blue : Color.red;
            }
            else
            {
                // 设置空格子状态
                spriteRenderer.sprite = emptySprite;
                spriteRenderer.color = originalColor;
            }
        }
        
        /// <summary>
        /// 设置格子颜色
        /// </summary>
        public void SetColor(Color color)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = color;
            }
        }
        
        /// <summary>
        /// 恢复原始颜色
        /// </summary>
        public void RestoreOriginalColor()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = originalColor;
            }
        }
        
        /// <summary>
        /// 设置高亮状态
        /// </summary>
        public void SetHighlighted(bool highlighted, Color? highlightColor = null)
        {
            if (spriteRenderer != null)
            {
                if (highlighted)
                {
                    Color color = highlightColor ?? Color.yellow;
                    spriteRenderer.color = Color.Lerp(originalColor, color, 0.5f);
                }
                else
                {
                    RestoreOriginalColor();
                }
            }
        }
        
        #endregion
        
        #region 动画支持
        
        /// <summary>
        /// 播放放置动画
        /// </summary>
        public void PlayPlaceAnimation(float duration = 0.3f)
        {
            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(PlayPlaceAnimationCoroutine(duration));
            }
        }
        
        private System.Collections.IEnumerator PlayPlaceAnimationCoroutine(float duration)
        {
            Vector3 targetScale = originalScale * 1.2f;
            float elapsedTime = 0f;
            
            // 放大阶段
            while (elapsedTime < duration * 0.5f)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / (duration * 0.5f);
                transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
                yield return null;
            }
            
            // 缩小阶段
            elapsedTime = 0f;
            while (elapsedTime < duration * 0.5f)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / (duration * 0.5f);
                transform.localScale = Vector3.Lerp(targetScale, originalScale, t);
                yield return null;
            }
            
            // 确保最终缩放正确
            transform.localScale = originalScale;
        }
        
        /// <summary>
        /// 重置变换到原始状态
        /// </summary>
        public void ResetTransform()
        {
            transform.localScale = originalScale;
        }
        
        #endregion
        
        #region 交互支持
        
        /// <summary>
        /// 设置交互状态
        /// </summary>
        public void SetInteractable(bool interactable)
        {
            isInteractable = interactable;
            if (cellCollider != null)
            {
                cellCollider.enabled = interactable;
            }
        }
        
        /// <summary>
        /// 检查是否可交互
        /// </summary>
        public bool IsInteractable()
        {
            return isInteractable && !isOccupied;
        }
        
        #endregion
        
        #region 状态同步
        
        /// <summary>
        /// 从HexCellState同步状态
        /// </summary>
        public void SyncFromCellState(HexCellState state)
        {
            if (state == null) return;
            
            x = state.x;
            y = state.y;
            isOccupied = state.isOccupied;
            occupiedBy = state.occupiedBy;
            
            cellState = state.Clone();
            UpdateVisualState();
        }
        
        /// <summary>
        /// 将当前状态同步到HexCellState
        /// </summary>
        public void SyncToCellState()
        {
            if (cellState == null)
            {
                cellState = new HexCellState(x, y, isOccupied, occupiedBy);
            }
            else
            {
                cellState.x = x;
                cellState.y = y;
                cellState.isOccupied = isOccupied;
                cellState.occupiedBy = occupiedBy;
            }
        }
        
        /// <summary>
        /// 获取当前的HexCellState
        /// </summary>
        public HexCellState GetCellState()
        {
            SyncToCellState();
            return cellState.Clone();
        }
        
        #endregion
        
        #region 调试和验证
        
        /// <summary>
        /// 验证格子状态的一致性
        /// </summary>
        public bool ValidateState()
        {
            if (cellState == null) return false;
            
            return cellState.x == x && 
                   cellState.y == y && 
                   cellState.isOccupied == isOccupied && 
                   cellState.occupiedBy == occupiedBy;
        }
        
        /// <summary>
        /// 获取格子的详细信息
        /// </summary>
        public string GetDetailedInfo()
        {
            return $"HexCell[{x},{y}]: Occupied={isOccupied}, OccupiedBy={occupiedBy}, " +
                   $"Interactable={isInteractable}, StateValid={ValidateState()}";
        }
        
        /// <summary>
        /// 在Scene视图中显示调试信息
        /// </summary>
        private void OnDrawGizmos()
        {
            // 绘制格子边界
            Gizmos.color = isOccupied ? Color.red : Color.green;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
            
            #if UNITY_EDITOR
            // 显示坐标
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.7f, $"({x},{y})");
            #endif
        }
        
        #endregion
        
        #region 事件回调
        
        /// <summary>
        /// 鼠标点击事件（用于测试）
        /// </summary>
        private void OnMouseDown()
        {
            if (IsInteractable())
            {
                Debug.Log($"HexCell ({x}, {y}) clicked!");
                // 这里可以触发点击事件，但实际的输入处理应该由InputHandler负责
            }
        }
        
        /// <summary>
        /// 鼠标进入事件
        /// </summary>
        private void OnMouseEnter()
        {
            if (IsInteractable())
            {
                SetHighlighted(true, Color.white);
            }
        }
        
        /// <summary>
        /// 鼠标离开事件
        /// </summary>
        private void OnMouseExit()
        {
            if (IsInteractable())
            {
                SetHighlighted(false);
            }
        }
        
        #endregion
        
        #region 兼容性方法
        
        /// <summary>
        /// 旧版本兼容：更改Sprite的方法
        /// </summary>
        private void ChangeSprite(int player)
        {
            // 这个方法保留是为了兼容旧代码，实际功能已整合到UpdateVisualState中
            UpdateVisualState();
        }
        
        #endregion
    }
}