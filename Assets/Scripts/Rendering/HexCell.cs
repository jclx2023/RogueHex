using UnityEngine;
using HexGame.Core;

public class HexCell : MonoBehaviour
{
    public int x, y;
    public bool isOccupied = false;
    public int occupiedBy = -1; // -1: δռ��, 0: ���, 1: AI

    private SpriteRenderer spriteRenderer; // ���ڸ��ĸ��ӵ� Sprite

    // ��¶���� Sprite �������Ա��� Unity �༭�����ֶ���ֵ
    public Sprite playerSprite; // Player ����
    public Sprite aiSprite;     // AI ����

    // HexCellState ���ڱ����߼�״̬
    public HexCellState cellState;

    void Start()
    {
        // ��ȡ�ø��ӵ� SpriteRenderer ���
        spriteRenderer = GetComponent<SpriteRenderer>();

        // ��ʼ���߼�״̬
        cellState = new HexCellState(x, y, isOccupied, occupiedBy);
    }

    public void Initialize(int x, int y)
    {
        this.x = x;
        this.y = y;
        this.isOccupied = false;
        this.occupiedBy = -1;

        // ��ʼ�� HexCellState
        cellState = new HexCellState(x, y);
    }

    public void SetOccupied(int player)
    {
        isOccupied = true;
        occupiedBy = player;

        // ͬ���߼�״̬
        cellState.isOccupied = true;
        cellState.occupiedBy = player;

        // ���� Sprite
        ChangeSprite(player);
    }

    // ���� Sprite �ķ���
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
                spriteRenderer.sprite = playerSprite; // ���ռ��ʱ���� Sprite
            }
            else if (player == 1)
            {
                spriteRenderer.sprite = aiSprite; // AI ռ��ʱ���� Sprite
            }
        }
    }
}
