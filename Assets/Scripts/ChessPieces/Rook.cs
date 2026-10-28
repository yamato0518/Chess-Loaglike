using System.Collections.Generic;
using UnityEngine;

public class Rook : ChessPieces
{
    public override List<Vector2Int> GetAvailableMoves(ref ChessPieces[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> r = new List<Vector2Int>();

        CheckLine(0, -1, ref board, tileCountX, tileCountY, ref r);
        CheckLine(0, 1, ref board, tileCountX, tileCountY, ref r);
        CheckLine(1, 0, ref board, tileCountX, tileCountY, ref r);
        CheckLine(-1, 0, ref board, tileCountX, tileCountY, ref r);

        return r;
    }

    private void CheckLine(int dirX, int dirY, ref ChessPieces[,] board, int tileCountX, int tileCountY, ref List<Vector2Int> r)
    {
        int x = currentX + dirX;
        int y = currentY + dirY;
        int allyPassed = 0;

        while (x >= 0 && x < tileCountX && y >= 0 && y < tileCountY)
        {
            if (board[x, y] == null)
            {
                r.Add(new Vector2Int(x, y));
            }
            else
            {
                if (board[x, y].team == team)
                {
                    if (hasGhostStep && allyPassed == 0)
                    {
                        allyPassed++;
                        x += dirX;
                        y += dirY;
                        continue;
                    }
                    break;
                }
                else
                {
                    r.Add(new Vector2Int(x, y));
                    break;
                }
            }
            x += dirX;
            y += dirY;
        }
    }
}
