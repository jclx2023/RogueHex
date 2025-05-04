using UnityEngine;

public class BoardManager : MonoBehaviour
{
    public GameObject hexCellPrefab; // �� Inspector ������ Hex Cell Prefab
    public int numRows = 11; // ����
    public int numCols = 11; // ����
    private HexCell[,] board;

    void Start()
    {
        GenerateOffsetBoard();
    }

    public HexCell[,] GetBoard()
    {
        return board;
    }

    // ����ƫ�����е����̵ķ���
    private void GenerateOffsetBoard()
    {
        board = new HexCell[numRows, numCols];
        float hexWidth = 1.9375f; // ���������εĿ��
        float hexHeight = Mathf.Sqrt(3) / 2 * hexWidth; // �߶ȸ��������μ��μ���
        float xOffset = hexWidth * 0.75f; // ÿ����Ԫ���ˮƽƫ��
        float yOffset = hexHeight; // ÿ�еĴ�ֱƫ��
        float centerOffsetX = (Mathf.Floor(numCols * xOffset)); // ˮƽ����ƫ����

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
