public class HexCellState
{
    public int x, y;
    public bool isOccupied;
    public int occupiedBy; // -1: Î´Õ¼¾Ý, 0: Íæ¼Ò, 1: AI

    public HexCellState(int x, int y, bool isOccupied = false, int occupiedBy = -1)
    {
        this.x = x;
        this.y = y;
        this.isOccupied = isOccupied;
        this.occupiedBy = occupiedBy;
    }
}
