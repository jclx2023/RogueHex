using UnityEngine;

public class BoardManager : MonoBehaviour
{
    public GameObject hexCellPrefab; // 在 Inspector 中链接 Hex Cell Prefab
    public int numRows = 11; // 行数
    public int numCols = 11; // 列数
    private HexCell[,] board;

    void Start()
    {
        GenerateOffsetBoard();
    }

    public HexCell[,] GetBoard()
    {
        return board;
    }

    // 生成偏移排列的棋盘的方法
    private void GenerateOffsetBoard()
    {
        board = new HexCell[numRows, numCols];
        float hexWidth = 1.9375f; // 假设六边形的宽度
        float hexHeight = Mathf.Sqrt(3) / 2 * hexWidth; // 高度根据六边形几何计算
        float xOffset = hexWidth * 0.75f; // 每个单元格的水平偏移
        float yOffset = hexHeight; // 每行的垂直偏移
        float centerOffsetX = (Mathf.Floor(numCols * xOffset)); // 水平居中偏移量

        for (int row = 0; row < numRows; row++)
        {
            for (int col = 0; col < numCols; col++)
            {
                float xPos = (col + row) * xOffset - centerOffsetX;
                float yPos = row * yOffset + (col * hexHeight / 2) - row * (hexHeight * 1.5f);
                Vector3 position = new Vector3(xPos, yPos, 0);

                GameObject hexCellObj = Instantiate(hexCellPrefab, position, Quaternion.identity, this.transform);
                hexCellObj.name = $"Cell({row}|{col})";
                HexCell hexCell = hexCellObj.GetComponent<HexCell>();

                if (hexCell != null)
                {
                    hexCell.Initialize(row, col);
                    board[row, col] = hexCell;
                }
            }
        }
    }
}
