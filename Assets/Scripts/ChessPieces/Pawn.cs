using System.Collections.Generic;
using UnityEngine;

public class Pawn : ChessPieces
{
    public override List<Vector2Int> GetAvailableMoves(ref ChessPieces[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> r = new List<Vector2Int>();

        int direction = (team == 0) ? 1 : -1;

        if(currentY + direction < 0 || currentY + direction >= tileCountY)
        {
            return r;
        }

        //1マス前
        if(board[currentX, currentY + direction] == null)
        {
            r.Add(new Vector2Int(currentX, currentY + direction));
        }


        //2マス前
        if(board[currentX, currentY + direction] == null)
        {   
            //white team
            if(team == 0 && currentY == 1 && board[currentX, currentY + (direction * 2)] == null)
            {
                r.Add(new Vector2Int(currentX, currentY + (direction * 2)));
            }

            //black team
            if(team == 1 && currentY == 6 && board[currentX, currentY + (direction * 2)] == null)
            {
                r.Add(new Vector2Int(currentX, currentY + (direction * 2)));
            }


        }

         //kill move
        if(currentX != tileCountX - 1)
        {
            if(board[currentX + 1, currentY + direction] != null && board[currentX + 1, currentY + direction].team != team)
            {
                r.Add(new Vector2Int(currentX + 1, currentY + direction));
            }
        }

        if(currentX != 0)
        {
            if(board[currentX - 1, currentY + direction] != null && board[currentX - 1, currentY + direction].team != team)
            {
                r.Add(new Vector2Int(currentX - 1, currentY + direction));
            }  
        }

        //1マス下がる
        if (canMoveBackWord)
        {
            int backY = currentY - direction;
            if(backY >= 0 && backY < tileCountY)
            {
                if(board[currentX, backY] == null)
                {
                    r.Add(new Vector2Int(currentX, backY));
                }
            }
        }
    
        return r;
    }
}
