using UnityEngine;
using HexGame.Core;

public class HexCell : MonoBehaviour
{
    public int x, y;
    public bool isOccupied = false;
    public int occupiedBy = -1; // -1: 未占据, 0: 玩家, 1: AI

    private SpriteRenderer spriteRenderer; // 用于更改格子的 Sprite

    // 暴露两个 Sprite 变量，以便在 Unity 编辑器中手动赋值
    public Sprite playerSprite; // Player 精灵
    public Sprite aiSprite;     // AI 精灵

    // HexCellState 用于保存逻辑状态
    public HexCellState cellState;

    void Start()
    {
        // 获取该格子的 SpriteRenderer 组件
        spriteRenderer = GetComponent<SpriteRenderer>();

        // 初始化逻辑状态
        cellState = new HexCellState(x, y, isOccupied, occupiedBy);
    }

    public void Initialize(int x, int y)
    {
        this.x = x;
        this.y = y;
        this.isOccupied = false;
        this.occupiedBy = -1;

        // 初始化 HexCellState
        cellState = new HexCellState(x, y);
    }

    public void SetOccupied(int player)
    {
        isOccupied = true;
        occupiedBy = player;

        // 同步逻辑状态
        cellState.isOccupied = true;
        cellState.occupiedBy = player;

        // 更改 Sprite
        ChangeSprite(player);
    }

    // 更改 Sprite 的方法
    private void ChangeSprite(int player)
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (spriteRenderer != null)
        {
            if (player == 0)
            {
                spriteRenderer.sprite = playerSprite; // 玩家占据时设置 Sprite
            }
            else if (player == 1)
            {
                spriteRenderer.sprite = aiSprite; // AI 占据时设置 Sprite
            }
        }
    }
}
