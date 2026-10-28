using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

public enum ChessPieceType
{
    None = 0,
    Pawn = 1,
    Rook = 2,
    Knight = 3,
    Bishop = 4,
    Queen = 5,
    King = 6
}

public class ChessPieces : MonoBehaviour
{
    public int team;
    public int currentX;
    public int currentY;
    public ChessPieceType type;

    private Vector3 desiredPosition;
    private Vector3 desiredScale = new Vector3(1.5f, 1.5f, 1.5f);

    [Header("Enhancements")]
    public bool canMoveBackWord = false; //ポーン用強化：後ろにも下がれる
    public bool hasLongJump = false ; //ナイト用強化：長距離ジャンプ
    public bool hasGhostStep = false; //ルーク用強化：味方を一体まですり抜けられる
    public bool hasShield = false; //キング用強化：攻撃を一度防いで反撃
    public bool hasKnightMove = false; //クイーン用強化：ナイトの動きを追加


    private void Start()
    {

        Vector3 currentEuler = transform.eulerAngles;
        transform.rotation = Quaternion.Euler((team == 0) ? new Vector3(currentEuler.x, currentEuler.y, 0) : new Vector3(currentEuler.x, currentEuler.y, 180));
    }

    private void Update()
    {
        transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * 10);
        transform.localScale = Vector3.Lerp(transform.localScale, desiredScale, Time.deltaTime * 10);
    }

    public virtual List<Vector2Int> GetAvailableMoves(ref ChessPieces[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> r = new List<Vector2Int>();

        r.Add(new Vector2Int(3, 3));
        r.Add(new Vector2Int(3, 4));
        r.Add(new Vector2Int(4, 3));
        r.Add(new Vector2Int(4, 4));

        return r;
    }

    public virtual void SetPosition(Vector3 position, bool force = false)
    {
        desiredPosition = position;
        if (force)
        {
            transform.position = desiredPosition;
        }
    }

    public virtual void SetScale(Vector3 scale, bool force = false)
    {
        desiredScale = scale;
        if (force)
        {
            transform.localScale = desiredScale;
        }
    }
}
