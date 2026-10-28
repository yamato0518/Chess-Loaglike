using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.Animations;

public struct AIMove
{
    public ChessPieces piece;
    public Vector2Int targetPosition;
    public int score;

    public AIMove(ChessPieces piece, Vector2Int targetPosition, int score)
    {
        this.piece = piece;
        this.targetPosition = targetPosition;
        this.score = score;
    }
}

public class ChessAI
{
    //最善手を1つ選んで返す
    public static AIMove? GetBestMove(ChessPieces[,] board, int tileCountX, int tileCountY, int aiTeam)
    {
        List<AIMove> allValidMoves = new List<AIMove>();

        for (int x = 0; x < tileCountX; x++)
        {
            for (int y = 0; y < tileCountY; y++)
            {
                ChessPieces cp = board[x, y];
                if(cp != null && cp.team == aiTeam)
                {
                    List<Vector2Int> moves = cp.GetAvailableMoves(ref board, tileCountX, tileCountY);
                    foreach(Vector2Int move in moves)
                    {
                        int score = 0;
                        ChessPieces targetPiece = board[move.x, move.y];

                        if(targetPiece != null && targetPiece.team != aiTeam)
                        {
                            score = GetPieceValue(targetPiece.type);
                        }
                        allValidMoves.Add(new AIMove(cp, move, score));
                    }
                }
            }
        }

        if(allValidMoves.Count == 0) return null;

        int highestScore = -1;
        foreach(var move in allValidMoves)
        {
            if(move.score >= highestScore)
            {
                highestScore = move.score;
            }
        }
        List<AIMove> bestMoves = allValidMoves.FindAll(m => m.score == highestScore);
        return bestMoves[Random.Range(0, bestMoves.Count)];
    }

    //目標駒の優先度
    private static int GetPieceValue(ChessPieceType type)
    {
        switch (type)
        {
            case ChessPieceType.Pawn:
                return 100;
            case ChessPieceType.Knight:
                return 300;
            case ChessPieceType.Bishop:
                return 300;
            case ChessPieceType.Rook:
                return 500;
            case ChessPieceType.Queen:
                return 900;
            case ChessPieceType.King:
                return 1000;


            default: return 0;
        }
    }
}


