// IAIStrategy.cs
using UnityEngine;

/// <summary>
/// ���� AI ���Խӿڣ�
/// ����ԭ�� GetBestMove��ͬʱ��չ֧���µ��ۺ�����ϵͳ��
/// </summary>
public interface IAIStrategy
{
    /// <summary>
    /// ԭ�нӿڣ����ݵ�ǰ����״̬��������ƶ��� HexCell��
    /// ��ͳ�����Կ��ô˽ӿڵ���ѡ�㡣
    /// </summary>
    /// <param name="board">��ǰ���̶�ά����</param>
    /// <param name="simulations">ģ�������������������ͬ���Կ�ʹ�û���ԣ�</param>
    /// <returns>ѡ��� HexCell�����޺����ƶ��򷵻� null</returns>
    HexCell GetBestMove(HexCell[,] board, int simulations);

    /// <summary>
    /// �����ӿڣ���һ��δռ�ݵĸ��ӽ������֣������ۺϴ��ϵͳ����
    /// Ĭ�Ϸ��� 0 �֣�����ɰ�����д��
    /// </summary>
    /// <param name="board">��ǰ���̶�ά����</param>
    /// <param name="cell">�������� HexCell</param>
    /// <returns>�ø��ӵ����֣�Ĭ�� 0��</returns>
    float EvaluateCell(HexCell[,] board, HexCell cell)
    {
        return 0f; // Ĭ��ʵ��
    }

    /// <summary>
    /// �����ӿڣ�����Ƿ���ڽ������µ�λ�ã�������ʤ���������أ���
    /// Ĭ�Ϸ��� null������ɰ�����д��
    /// </summary>
    /// <param name="board">��ǰ���̶�ά����</param>
    /// <returns>��������Ҫ�������ӵĸ��ӣ����� HexCell�����򷵻� null</returns>
    HexCell GetImmediateMove(HexCell[,] board)
    {
        return null; // Ĭ��ʵ��
    }
}
