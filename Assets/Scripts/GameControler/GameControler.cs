// GameController.cs
using UnityEngine;
// 如果 BoardManager 存在于某个命名空间中，请确保在此处使用正确的 using 语句
// 例如：using MyGame;

public class GameController : MonoBehaviour
{
    private AIPlayer aiPlayer;
    private BoardManager boardManager;
    private bool isPlayerTurn = true;

    void Start()
    {
        aiPlayer = FindObjectOfType<AIPlayer>();
        boardManager = FindObjectOfType<BoardManager>();
        Debug.Log("GameController initialized. Waiting for player action.");
    }

    void Update()
    {
        if (!isPlayerTurn)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);
            if (hit.collider != null)
            {
                HexCell hexCell = hit.collider.GetComponent<HexCell>();
                if (hexCell != null && !hexCell.isOccupied)
                {
                    // 玩家落子，标记为 0（玩家编号）
                    hexCell.SetOccupied(0);
                    Debug.Log($"Player placed a piece at ({hexCell.x}, {hexCell.y})");

                    // 更新组合策略中所有需要知道玩家最新落子的策略
                    foreach (var entry in aiPlayer.compositeStrategy.GetStrategies())
                    {
                        if (entry.strategy is DoubleThreatStrategy dts)
                        {
                            dts.LastPlayerMove = hexCell;
                        }
                    }

                    // 使用 GetBoard() 时可能存在二义性，请检查 BoardManager 类是否重复定义
                    // 例如，如果 BoardManager 定义在 MyGame 命名空间中，请使用：
                    // HexCell[,] boardCells = ((MyGame.BoardManager)boardManager).GetBoard();
                    HexCell[,] boardCells = boardManager.GetBoard();

                    HexCellState[,] boardState = ConvertToHexCellState(boardCells);
                    if (IsGameOver(boardState))
                    {
                        Debug.Log("Player wins!");
                        return;
                    }

                    isPlayerTurn = false;
                    Invoke("AIMove", 1f);
                }
                else
                {
                    Debug.Log("Cell is already occupied or not valid.");
                }
            }
            else
            {
                Debug.Log("No cell hit detected.");
            }
        }
    }

    void AIMove()
    {
        aiPlayer.MakeMove();
        HexCell[,] boardCells = boardManager.GetBoard();
        HexCellState[,] boardState = ConvertToHexCellState(boardCells);
        if (IsGameOver(boardState))
        {
            Debug.Log("AI wins!");
        }
        else
        {
            isPlayerTurn = true;
            Debug.Log("Player's turn.");
        }
    }

    /// <summary>
    /// 判断游戏是否结束：若任一玩家（0 为玩家，1 为 AI）已经连通对应边界，则游戏结束。
    /// </summary>
    private bool IsGameOver(HexCellState[,] board)
    {
        return aiPlayer.HasPlayerWon(board, 0) || aiPlayer.HasPlayerWon(board, 1);
    }

    /// <summary>
    /// 将 HexCell[,] 转换为 HexCellState[,]，便于游戏结束检测使用。
    /// </summary>
    private HexCellState[,] ConvertToHexCellState(HexCell[,] board)
    {
        int rows = board.GetLength(0);
        int cols = board.GetLength(1);
        HexCellState[,] boardState = new HexCellState[rows, cols];

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                HexCell cell = board[i, j];
                boardState[i, j] = new HexCellState(cell.x, cell.y, cell.isOccupied, cell.occupiedBy);
            }
        }
        return boardState;
    }
}
