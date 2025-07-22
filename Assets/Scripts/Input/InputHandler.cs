using System;
using UnityEngine;
using HexGame.Core;
using HexGame.Rendering;
using UnityEngine.InputSystem;

namespace HexGame.Input
{
    /// <summary>
    /// 输入处理器 - 负责处理玩家输入并转换为游戏坐标
    /// 支持鼠标点击、触屏输入和键盘快捷键
    /// </summary>
    public class InputHandler : MonoBehaviour, IInputHandler
    {
        [Header("输入设置")]
        [SerializeField] private LayerMask hexCellLayerMask = -1;
        [SerializeField] private bool enableKeyboardShortcuts = true;
        
        [Header("调试设置")]
        [SerializeField] private bool enableDebugLog = false;
        [SerializeField] private bool showClickIndicator = true;
        
        // 输入事件
        public event Action<Vector2Int> OnCellClicked;
        public event Action OnResetRequested;
        public event Action OnPauseRequested;
        
        // 组件依赖
        private Camera mainCamera;
        private GameManager gameManager;
        
        // 输入状态
        private bool isInputEnabled = true;
        private Vector2Int? lastClickedCell;
        
        #region Unity生命周期
        
        private void Awake()
        {
            InitializeComponents();
        }
        
        private void Start()
        {
            LogDebug("InputHandler initialized");
        }
        
        private void Update()
        {
            if (!isInputEnabled) return;
            
            HandleMouseInput();
            HandleKeyboardInput();
        }
        
        #endregion
        
        #region 初始化
        
        private void InitializeComponents()
        {
            // 获取主摄像机
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = FindObjectOfType<Camera>();
                LogWarning("Main Camera not found, using first available camera");
            }
            
            // 获取GameManager引用
            gameManager = FindObjectOfType<GameManager>();
            if (gameManager == null)
            {
                LogError("GameManager not found!");
            }
        }
        
        #endregion
        
        #region 鼠标输入处理
        
        private void HandleMouseInput()
        {
            if (Mouse.current.leftButton.wasPressedThisFrame) // 左键点击
            {
                Vector3 mousePosition = Mouse.current.position.ReadValue();
                ProcessClickInput(mousePosition);
            }
        }
        
        private void ProcessClickInput(Vector3 screenPosition)
        {
            Vector2Int? cellPosition = GetCellFromScreenPosition(screenPosition);
            
            if (cellPosition.HasValue)
            {
                LogDebug($"Cell clicked: ({cellPosition.Value.x}, {cellPosition.Value.y})");
                
                lastClickedCell = cellPosition.Value;
                OnCellClicked?.Invoke(cellPosition.Value);
                
                if (showClickIndicator)
                {
                    ShowClickIndicator(cellPosition.Value);
                }
            }
            else
            {
                LogDebug("Click missed - no valid cell found");
            }
        }
        
        #endregion
        
        #region 键盘输入处理
        
        private void HandleKeyboardInput()
        {
            if (!enableKeyboardShortcuts) return;
            
            // ESC键 - 暂停/取消暂停
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                OnPauseRequested?.Invoke();
                LogDebug("Pause requested via ESC key");
            }
            
            // R键 - 重置游戏
            if (Keyboard.current.rKey.wasPressedThisFrame)
            {
                OnResetRequested?.Invoke();
                LogDebug("Reset requested via R key");
            }
            
            // Space键 - 暂停/恢复
            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                OnPauseRequested?.Invoke();
                LogDebug("Pause toggled via Space key");
            }
        }
        
        #endregion
        
        #region 坐标转换
        
        /// <summary>
        /// 将屏幕坐标转换为棋盘格子坐标
        /// </summary>
        private Vector2Int? GetCellFromScreenPosition(Vector3 screenPosition)
        {
            if (mainCamera == null) return null;
            
            // 屏幕坐标转世界坐标
            Vector3 worldPosition = mainCamera.ScreenToWorldPoint(screenPosition);
            worldPosition.z = 0; // 确保Z轴为0
            
            // 使用射线检测获取点击的格子
            Vector2Int? cellFromRaycast = GetCellFromRaycast(worldPosition);
            if (cellFromRaycast.HasValue)
            {
                return cellFromRaycast.Value;
            }
            
            // 如果射线检测失败，尝试使用几何计算
            return GetCellFromWorldPosition(worldPosition);
        }
        
        /// <summary>
        /// 使用射线检测获取格子坐标
        /// </summary>
        private Vector2Int? GetCellFromRaycast(Vector3 worldPosition)
        {
            RaycastHit2D hit = Physics2D.Raycast(worldPosition, Vector2.zero, Mathf.Infinity, hexCellLayerMask);
            
            if (hit.collider != null)
            {
                HexCell hexCell = hit.collider.GetComponent<HexCell>();
                if (hexCell != null)
                {
                    return new Vector2Int(hexCell.x, hexCell.y);
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// 使用几何计算从世界坐标获取格子坐标（备用方法）
        /// </summary>
        private Vector2Int? GetCellFromWorldPosition(Vector3 worldPosition)
        {
            if (gameManager == null) return null;
            
            var config = gameManager.GetGameConfig();
            if (config == null) return null;
            
            // 这里需要实现六边形坐标的逆向计算
            // 暂时使用简化的网格计算，后续可以用BoardGeometry完善
            
            float hexWidth = config.hexWidth;
            float hexHeight = config.hexHeight;
            float xOffset = hexWidth * 0.75f;
            
            // 简化的坐标转换（需要根据实际的六边形布局调整）
            int estimatedX = Mathf.RoundToInt(worldPosition.y / hexHeight);
            int estimatedY = Mathf.RoundToInt((worldPosition.x + estimatedX * xOffset * 0.5f) / xOffset);
            
            // 边界检查
            if (estimatedX >= 0 && estimatedX < config.boardRows && 
                estimatedY >= 0 && estimatedY < config.boardCols)
            {
                return new Vector2Int(estimatedX, estimatedY);
            }
            
            return null;
        }
        
        #endregion
        
        #region 视觉反馈
        
        /// <summary>
        /// 显示点击指示器
        /// </summary>
        private void ShowClickIndicator(Vector2Int cellPosition)
        {
            // 这里可以添加点击特效，如粒子效果、高亮等
            // 暂时使用调试日志
            LogDebug($"Click indicator shown at cell ({cellPosition.x}, {cellPosition.y})");
        }
        
        #endregion
        
        #region 公共接口
        
        /// <summary>
        /// 启用/禁用输入
        /// </summary>
        public void SetInputEnabled(bool enabled)
        {
            isInputEnabled = enabled;
            LogDebug($"Input {(enabled ? "enabled" : "disabled")}");
        }
        
        /// <summary>
        /// 获取最后点击的格子
        /// </summary>
        public Vector2Int? GetLastClickedCell()
        {
            return lastClickedCell;
        }
        
        /// <summary>
        /// 清除最后点击的格子记录
        /// </summary>
        public void ClearLastClickedCell()
        {
            lastClickedCell = null;
        }
        
        /// <summary>
        /// 设置调试模式
        /// </summary>
        public void SetDebugMode(bool enabled)
        {
            enableDebugLog = enabled;
        }
        
        /// <summary>
        /// 手动触发格子点击事件（用于测试）
        /// </summary>
        public void SimulateClickAt(Vector2Int cellPosition)
        {
            lastClickedCell = cellPosition;
            OnCellClicked?.Invoke(cellPosition);
            LogDebug($"Simulated click at ({cellPosition.x}, {cellPosition.y})");
        }
        
        #endregion
        
        #region 事件订阅管理
        
        private void OnEnable()
        {
            SubscribeToGameEvents();
        }
        
        private void OnDisable()
        {
            UnsubscribeFromGameEvents();
        }
        
        private void SubscribeToGameEvents()
        {
            if (gameManager != null)
            {
                GameManager.OnPhaseChanged += HandleGamePhaseChanged;
            }
        }
        
        private void UnsubscribeFromGameEvents()
        {
            GameManager.OnPhaseChanged -= HandleGamePhaseChanged;
        }
        
        private void HandleGamePhaseChanged(GamePhase newPhase)
        {
            // 根据游戏阶段调整输入状态
            switch (newPhase)
            {
                case GamePhase.Playing:
                    SetInputEnabled(true);
                    break;
                    
                case GamePhase.GameOver:
                case GamePhase.Paused:
                    SetInputEnabled(false);
                    break;
                    
                default:
                    SetInputEnabled(true);
                    break;
            }
        }
        
        #endregion
        
        #region 调试和日志
        
        private void LogDebug(string message)
        {
            if (enableDebugLog)
            {
                Debug.Log($"[InputHandler] {message}");
            }
        }
        
        private void LogWarning(string message)
        {
            Debug.LogWarning($"[InputHandler] {message}");
        }
        
        private void LogError(string message)
        {
            Debug.LogError($"[InputHandler] {message}");
        }
        
        #endregion
    }
}