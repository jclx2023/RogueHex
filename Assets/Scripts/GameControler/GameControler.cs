// GameController.cs
using UnityEngine;
// ��� BoardManager ������ĳ�������ռ��У���ȷ���ڴ˴�ʹ����ȷ�� using ���
// ���磺using MyGame;

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
                    // ������ӣ����Ϊ 0����ұ�ţ�
                    hexCell.SetOccupied(0);
                    Debug.Log($"Player placed a piece at ({hexCell.x}, {hexCell.y})");

                    // ������ϲ�����������Ҫ֪������������ӵĲ���
                    foreach (var entry in aiPlayer.compositeStrategy.GetStrategies())
                    {
                        if (entry.strategy is DoubleThreatStrategy dts)
                        {
                            dts.LastPlayerMove = hexCell;
                        }
                    }

                    // ʹ�� GetBoard() ʱ���ܴ��ڶ����ԣ����� BoardManager ���Ƿ��ظ�����
                    // ���磬��� BoardManager ������ MyGame �����ռ��У���ʹ�ã�
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
    /// �ж���Ϸ�Ƿ����������һ��ң�0 Ϊ��ң�1 Ϊ AI���Ѿ���ͨ��Ӧ�߽磬����Ϸ������
    /// </summary>
    private bool IsGameOver(HexCellState[,] board)
    {
        return aiPlayer.HasPlayerWon(board, 0) || aiPlayer.HasPlayerWon(board, 1);
    }

    /// <summary>
    /// �� HexCell[,] ת��Ϊ HexCellState[,]��������Ϸ�������ʹ�á�
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
