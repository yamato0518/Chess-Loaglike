using System.Collections.Generic;
using UnityEngine;

public class Knight : ChessPieces
{
    public override List<Vector2Int> GetAvailableMoves(ref ChessPieces[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> r = new List<Vector2Int>();

        // ナイトが移動できる8通りの相対座標 (X, Y)
        int[] dx = { 1, 2, -1, -2, 1, 2, -1, -2 };
        int[] dy = { 2, 1, 2, 1, -2, -1, -2, -1 };

        for (int i = 0; i < 8; i++)
        {
            // 盤面の範囲内（0 <= x < 幅 かつ 0 <= y < 高さ）か判定
            EvaluateMove(currentX + dx[i], currentY + dy[i], ref board, tileCountX, tileCountY, ref r);
        }

        if (hasLongJump)
        {
            int[] longDx = { 1, 3, -1, -3, 1, 3, -1, -3 };
            int[] longDy = { 3, 1, 3, 1, -3, -1, -3, -1 };

            for (int i = 0; i < 8; i++)
            {
                EvaluateMove(currentX + longDx[i], currentY + longDy[i], ref board, tileCountX, tileCountY, ref r);
            }
        }

        return r;
    }

    private void EvaluateMove(int x, int y, ref ChessPieces[,] board, int tileCountX, int tileCountY, ref List<Vector2Int> r)
    {
        if (x >= 0 && x < tileCountX && y >= 0 && y < tileCountY)
        {
            // 空きマスか、敵駒なら移動可能
            if (board[x, y] == null || board[x, y].team != team)
            {
                r.Add(new Vector2Int(x, y));
            }
        }
    }
}
