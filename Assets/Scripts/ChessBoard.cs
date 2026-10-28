using System.Collections;
using System.Collections.Generic;
using NUnit.Framework.Constraints;
using Unity.Collections;
using UnityEditor;
using UnityEditor.MPE;
using UnityEngine;
using UnityEngine.InputSystem;

public enum UpgradeType
{
    PawnBackward,
    KnightLongJump,
    RookGhostStep,

    KingShield,
    QueenKnightMove,
    PawnAdvanceStart
}

public class ChessBoard : MonoBehaviour
{
    [Header("Art stuff")]
    [SerializeField] private Material tileMaterial;
    [SerializeField] private float tileSize = 1.0f;
    [SerializeField] private float yOffset = 0.5f;
    [SerializeField] private Vector3 boardCenter = Vector3.zero;
    [SerializeField] private float deathSize = 0.3f;
    [SerializeField] private float deathSpacing = 0.3f;
    [SerializeField] private float dragOffset = 1.5f;
    [SerializeField] private GameObject victoryScreen;

    [Header("Prefabs & Materials")]
    [SerializeField] private GameObject[] prefabs;
    [SerializeField] private Material[] teamMaterials;

    [Header("RewardPanels")]
    [SerializeField] private GameObject rewardScreen;
    [SerializeField] private TMPro.TextMeshProUGUI[] cardTitleText;
    [SerializeField] private TMPro.TextMeshProUGUI[] cardDescTexts;


    //変数
    private ChessPieces[,] chessPieces;
    private ChessPieces currentlyDragging;
    private List<Vector2Int> availableMoves = new List<Vector2Int>();
    private List<ChessPieces> deadWhites = new List<ChessPieces>();
    private List<ChessPieces> deadBlacks = new List<ChessPieces>();
    private const int TILE_COUNT_X = 8;
    private const int TILE_COUNT_Y = 8;
    private GameObject[,] tiles;
    private Camera currentCamera;
    private Vector2Int currentHover;
    private Vector3 bounds;
    private bool isWhiteTurn;

    //ステージ
    private int currentStage = 1;
    private const int MAX_STAGE = 3;

    //プレイヤーが所持している強化リスト
    private HashSet<UpgradeType> acquiredUpgrades = new HashSet<UpgradeType>();

    private void Awake()
    {
        isWhiteTurn = true;
        victoryScreen.SetActive(false);

        GenerateAllTiles(tileSize, TILE_COUNT_X, TILE_COUNT_Y);

        SpawnAllPieces();
        PositionAllPieces();
    }

    private void Update()
    {
        if (!isWhiteTurn) return;

        if (!currentCamera)
        {
            currentCamera = Camera.main;
            return;
        }

        if (Mouse.current == null) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();

        RaycastHit info;
        Ray ray = currentCamera.ScreenPointToRay(mousePos);
        if (Physics.Raycast(ray, out info, 100, LayerMask.GetMask("Tile", "Hover", "Highlight")))
        {
            //当たったタイルのインデックスを取得
            Vector2Int hitPosition = LookupTileIndex(info.transform.gameObject);

            //タイルにカーソルを合わせた後、タイルにカーソルを合わせた場合
            if (currentHover == -Vector2Int.one)
            {
                currentHover = hitPosition;
                tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
            }

            //既にタイルにカーソルを合わせている場合は、前のタイルを変更する
            if (currentHover != hitPosition)
            {
                tiles[currentHover.x, currentHover.y].layer = (ContainsValidMove(ref availableMoves, currentHover)) ? LayerMask.NameToLayer("Highlight") : LayerMask.NameToLayer("Tile");
                currentHover = hitPosition;
                tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
            }

            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                if (chessPieces[hitPosition.x, hitPosition.y] != null)
                {
                    if ((chessPieces[hitPosition.x, hitPosition.y].team == 0 && isWhiteTurn) || (chessPieces[hitPosition.x, hitPosition.y].team == 1 && !isWhiteTurn))
                    {
                        currentlyDragging = chessPieces[hitPosition.x, hitPosition.y];

                        //おける場所をハイライトする
                        availableMoves = currentlyDragging.GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);
                        HighlightTiles();
                    }
                }
            }

            if (currentlyDragging != null && Mouse.current.leftButton.wasReleasedThisFrame)
            {
                Vector2Int previousPosition = new Vector2Int(currentlyDragging.currentX, currentlyDragging.currentY);

                bool validMove = MoveTo(currentlyDragging, hitPosition.x, hitPosition.y);
                if (!validMove)
                {
                    currentlyDragging.SetPosition(GetTileCenter(previousPosition.x, previousPosition.y));
                    currentlyDragging = null;
                }

                currentlyDragging = null;
                RemoveHighlightTiles();
            }
        }
        else
        {
            if (currentHover != -Vector2Int.one)
            {
                tiles[currentHover.x, currentHover.y].layer = (ContainsValidMove(ref availableMoves, currentHover)) ? LayerMask.NameToLayer("Highlight") : LayerMask.NameToLayer("Tile");
                currentHover = -Vector2Int.one;
            }

            if (currentlyDragging && Mouse.current.leftButton.wasReleasedThisFrame)
            {
                currentlyDragging.SetPosition(GetTileCenter(currentlyDragging.currentX, currentlyDragging.currentY));
                currentlyDragging = null;
                RemoveHighlightTiles();
            }
        }

        //ドラッグしているとき
        if (currentlyDragging)
        {
            Plane horizontalPlane = new Plane(Vector3.up, Vector3.up * yOffset);
            float distance = 0.0f;
            if (horizontalPlane.Raycast(ray, out distance))
            {
                currentlyDragging.SetPosition(ray.GetPoint(distance) + Vector3.up * dragOffset);
            }
        }
    }



    //ゲーム盤の生成
    private void GenerateAllTiles(float tileSize, int tileCountX, int tileCountY)
    {
        yOffset += transform.position.y;
        bounds = new Vector3((tileCountX / 2) * tileSize, 0, (tileCountX / 2) * tileSize) + boardCenter;

        tiles = new GameObject[tileCountX, tileCountY];
        for (int x = 0; x < tileCountX; x++)
        {
            for (int y = 0; y < tileCountY; y++)
            {
                tiles[x, y] = GenerateSingleTile(tileSize, x, y);
            }
        }
    }

    private GameObject GenerateSingleTile(float tileSize, int x, int y)
    {
        GameObject tileObject = new GameObject(string.Format("X:{0}, Y:{1}", x, y));
        tileObject.transform.parent = transform;

        Mesh mesh = new Mesh();
        tileObject.AddComponent<MeshFilter>().mesh = mesh;
        tileObject.AddComponent<MeshRenderer>().material = tileMaterial; ;

        Vector3[] vertices = new Vector3[4];
        vertices[0] = new Vector3(x * tileSize, yOffset, y * tileSize) - bounds;
        vertices[1] = new Vector3(x * tileSize, yOffset, (y + 1) * tileSize) - bounds;
        vertices[2] = new Vector3((x + 1) * tileSize, yOffset, y * tileSize) - bounds;
        vertices[3] = new Vector3((x + 1) * tileSize, yOffset, (y + 1) * tileSize) - bounds;

        int[] tris = new int[] { 0, 1, 2, 1, 3, 2 };

        mesh.vertices = vertices;
        mesh.triangles = tris;

        mesh.RecalculateNormals();

        tileObject.layer = LayerMask.NameToLayer("Tile");
        tileObject.AddComponent<BoxCollider>();

        return tileObject;
    }

    //コマの生成
    private void SpawnAllPieces()
    {
        chessPieces = new ChessPieces[TILE_COUNT_X, TILE_COUNT_Y];

        int whiteTeam = 0, blackTeam = 1;

        // white team
        SpawnWhiteTeam(whiteTeam);

        //blackTeam
        SpawnBlackTeam(currentStage, blackTeam);
    }

    private void SpawnWhiteTeam(int team)
    {
        chessPieces[0, 0] = SpawnSinglePiece(ChessPieceType.Rook, team);
        chessPieces[1, 0] = SpawnSinglePiece(ChessPieceType.Knight, team);
        chessPieces[2, 0] = SpawnSinglePiece(ChessPieceType.Bishop, team);
        chessPieces[3, 0] = SpawnSinglePiece(ChessPieceType.Queen, team);
        chessPieces[4, 0] = SpawnSinglePiece(ChessPieceType.King, team);
        chessPieces[5, 0] = SpawnSinglePiece(ChessPieceType.Bishop, team);
        chessPieces[6, 0] = SpawnSinglePiece(ChessPieceType.Knight, team);
        chessPieces[7, 0] = SpawnSinglePiece(ChessPieceType.Rook, team);

        int pawnStartY = acquiredUpgrades.Contains(UpgradeType.PawnAdvanceStart) ? 2 : 1;

        for (int i = 0; i < TILE_COUNT_X; i++)
        {
            chessPieces[i, pawnStartY] = SpawnSinglePiece(ChessPieceType.Pawn, team);
        }

        ApplyUpgradesToWhitePieces();
    }

    private void SpawnBlackTeam(int stage, int team)
    {
        chessPieces[4, 7] = SpawnSinglePiece(ChessPieceType.King, team);

        switch (stage)
        {
            case 1:
                // chessPieces[0, 7] = SpawnSinglePiece(ChessPieceType.Rook, team);
                // chessPieces[1, 7] = SpawnSinglePiece(ChessPieceType.Knight, team);
                // chessPieces[2, 7] = SpawnSinglePiece(ChessPieceType.Bishop, team);
                // chessPieces[3, 7] = SpawnSinglePiece(ChessPieceType.Queen, team);
                // chessPieces[5, 7] = SpawnSinglePiece(ChessPieceType.Bishop, team);
                // chessPieces[6, 7] = SpawnSinglePiece(ChessPieceType.Knight, team);
                // chessPieces[7, 7] = SpawnSinglePiece(ChessPieceType.Rook, team);

                // for (int i = 0; i < TILE_COUNT_X; i++)
                // {
                //     chessPieces[i, 6] = SpawnSinglePiece(ChessPieceType.Pawn, team);
                // }
                break;

            case 2:
                // ナイトとルークが多めの前衛布陣
                chessPieces[0, 7] = SpawnSinglePiece(ChessPieceType.Rook, team);
                chessPieces[1, 7] = SpawnSinglePiece(ChessPieceType.Knight, team);
                chessPieces[2, 7] = SpawnSinglePiece(ChessPieceType.Knight, team); // ビショップをナイトに置換
                chessPieces[3, 7] = SpawnSinglePiece(ChessPieceType.Queen, team);
                chessPieces[5, 7] = SpawnSinglePiece(ChessPieceType.Knight, team);
                chessPieces[6, 7] = SpawnSinglePiece(ChessPieceType.Knight, team);
                chessPieces[7, 7] = SpawnSinglePiece(ChessPieceType.Rook, team);

                // ポーンの列のうち中央2体を最初から1歩前進
                for (int i = 0; i < TILE_COUNT_X; i++)
                {
                    int yPos = (i == 3 || i == 4) ? 5 : 6;
                    chessPieces[i, yPos] = SpawnSinglePiece(ChessPieceType.Pawn, team);
                }
                break;

            case 3:
                //クイーンが２体
                chessPieces[0, 7] = SpawnSinglePiece(ChessPieceType.Rook, team);
                chessPieces[1, 7] = SpawnSinglePiece(ChessPieceType.Queen, team); // ナイト枠をクイーンに！
                chessPieces[2, 7] = SpawnSinglePiece(ChessPieceType.Bishop, team);
                chessPieces[3, 7] = SpawnSinglePiece(ChessPieceType.Queen, team); // 本来のクイーン
                chessPieces[5, 7] = SpawnSinglePiece(ChessPieceType.Bishop, team);
                chessPieces[6, 7] = SpawnSinglePiece(ChessPieceType.Knight, team);
                chessPieces[7, 7] = SpawnSinglePiece(ChessPieceType.Rook, team);

                for (int i = 0; i < TILE_COUNT_X; i++)
                {
                    chessPieces[i, 6] = SpawnSinglePiece(ChessPieceType.Pawn, team);
                }
                break;
        }
    }

    private ChessPieces SpawnSinglePiece(ChessPieceType type, int team)
    {
        ChessPieces cp = Instantiate(prefabs[(int)type - 1], transform).GetComponent<ChessPieces>();

        cp.type = type;
        cp.team = team;
        cp.GetComponent<MeshRenderer>().material = teamMaterials[team];

        return cp;
    }

    //ポジショニング
    private void PositionAllPieces()
    {
        for (int x = 0; x < TILE_COUNT_X; x++)
        {
            for (int y = 0; y < TILE_COUNT_Y; y++)
            {
                if (chessPieces[x, y] != null)
                {
                    PositionSinglePiece(x, y, true);
                }
            }
        }
    }

    private void PositionSinglePiece(int x, int y, bool force = false)
    {
        chessPieces[x, y].currentX = x;
        chessPieces[x, y].currentY = y;
        chessPieces[x, y].SetPosition(GetTileCenter(x, y), force);
    }

    private Vector3 GetTileCenter(int x, int y)
    {
        return new Vector3(x * tileSize, yOffset, y * tileSize) - bounds + new Vector3(tileSize / 2, 0, tileSize / 2);
    }

    //タイルをハイライトする
    private void HighlightTiles()
    {
        for (int i = 0; i < availableMoves.Count; i++)
        {
            tiles[availableMoves[i].x, availableMoves[i].y].layer = LayerMask.NameToLayer("Highlight");
        }
    }

    private void RemoveHighlightTiles()
    {
        for (int i = 0; i < availableMoves.Count; i++)
        {
            tiles[availableMoves[i].x, availableMoves[i].y].layer = LayerMask.NameToLayer("Tile");

        }

        availableMoves.Clear();
    }

    //チェックメイト
    private void CheckMate(int team)
    {
        StopAllCoroutines();

        if (team == 0)
        {
            if (currentStage >= MAX_STAGE)
            {
                DisplayVictory(0);
            }
            else
            {
                ShowRewardUI();
            }
        }
        else
        {
            DisplayVictory(1);
        }
    }

    private void ShowRewardUI()
    {
        rewardScreen.SetActive(true);
        currentlyDragging = null;
        RemoveHighlightTiles();

        SetupRewardCardsForStage(currentStage);
    }

    private void SetupRewardCardsForStage(int stage)
    {
        if (stage == 1)
        {
            cardTitleText[0].text = "バックステップ";
            cardDescTexts[0].text = "ポーンが後ろに１マス下がれる";

            cardTitleText[1].text = "跳躍の蹄";
            cardDescTexts[1].text = "ナイトが３マス跳躍可能";

            cardTitleText[2].text = "ゴーストステップ";
            cardDescTexts[2].text = "ルークが味方１体をすり抜けて直進";
        }
        else if (stage == 2)
        {
            cardTitleText[0].text = "ラストスタンド";
            cardDescTexts[0].text = "キングが１度だけ敵の攻撃を耐えて反撃";

            cardTitleText[1].text = "クイーン覚醒";
            cardDescTexts[1].text = "クイーンがナイトの動きもできるようになる";

            cardTitleText[2].text = "前哨配備";
            cardDescTexts[2].text = "ポーン全員が１歩前進した状態で開始";
        }
    }

    private void DisplayVictory(int winningTeam)
    {
        victoryScreen.SetActive(true);
        victoryScreen.transform.GetChild(winningTeam).gameObject.SetActive(true);
    }

    public void OnRessetButton()
    {
        //盤面を初期化
        currentlyDragging = null;
        availableMoves = new List<Vector2Int>();

        //UI
        victoryScreen.transform.GetChild(0).gameObject.SetActive(false);
        victoryScreen.transform.GetChild(1).gameObject.SetActive(false);
        victoryScreen.SetActive(false);

        //Clean up
        for (int x = 0; x < TILE_COUNT_X; x++)
        {
            for (int y = 0; y < TILE_COUNT_Y; y++)
            {
                if (chessPieces[x, y] != null)
                {
                    Destroy(chessPieces[x, y].gameObject);
                }
                chessPieces[x, y] = null;
            }
        }

        for (int i = 0; i < deadWhites.Count; i++)
        {
            Destroy(deadWhites[i].gameObject);
        }
        for (int i = 0; i < deadBlacks.Count; i++)
        {
            Destroy(deadBlacks[i].gameObject);
        }
        deadWhites.Clear();
        deadBlacks.Clear();

        SpawnAllPieces();
        PositionAllPieces();
        isWhiteTurn = true;
    }

    public void OnExitButton()
    {
        Application.Quit();
    }

    //　オペレーション関数
    private bool ContainsValidMove(ref List<Vector2Int> moves, Vector2 pos)
    {
        for (int i = 0; i < moves.Count; i++)
        {
            if (moves[i].x == pos.x && moves[i].y == pos.y)
            {
                return true;
            }
        }
        return false;
    }

    private bool MoveTo(ChessPieces cp, int x, int y)
    {
        if (!ContainsValidMove(ref availableMoves, new Vector2(x, y)))
        {
            return false;
        }

        Vector2Int previousPosition = new Vector2Int(cp.currentX, cp.currentY);
        bool isGameOver = false;

        //他の駒があった場合
        if (chessPieces[x, y] != null)
        {
            ChessPieces ocp = chessPieces[x, y];

            if (cp.team == ocp.team)
            {
                return false;
            }

            //敵駒の場合
            if (ocp.team == 0)
            {
                if (ocp.type == ChessPieceType.King && ocp.hasShield)
                {
                    ocp.hasShield = false;

                    deadBlacks.Add(cp);
                    cp.SetScale(Vector3.one * deathSize);
                    cp.SetPosition(
                    new Vector3(8 * tileSize, yOffset, -1 * tileSize)
                    - bounds + new Vector3(tileSize / 2, 0, tileSize / 2)
                    + (Vector3.forward * deathSpacing) * deadWhites.Count);


                    isWhiteTurn = true;
                    return true;
                }

                if (ocp.type == ChessPieceType.King)
                {
                    CheckMate(1);
                }

                deadWhites.Add(ocp);
                ocp.SetScale(Vector3.one * deathSize);
                ocp.SetPosition(
                    new Vector3(8 * tileSize, yOffset, -1 * tileSize)
                    - bounds + new Vector3(tileSize / 2, 0, tileSize / 2)
                    + (Vector3.forward * deathSpacing) * deadWhites.Count);
            }
            else
            {
                if (ocp.type == ChessPieceType.King)
                {
                    CheckMate(0);
                    isGameOver = true;
                }

                deadBlacks.Add(ocp);
                ocp.SetScale(Vector3.one * deathSize);
                ocp.SetPosition(
                    new Vector3(-1 * tileSize, yOffset, 8 * tileSize)
                    - bounds + new Vector3(tileSize / 2, 0, tileSize / 2)
                    + (Vector3.back * deathSpacing) * deadBlacks.Count);
            }
        }


        chessPieces[x, y] = cp;
        chessPieces[previousPosition.x, previousPosition.y] = null;

        PositionSinglePiece(x, y);

        if (isGameOver)
        {
            return true;
        }


        isWhiteTurn = !isWhiteTurn;

        if (!isWhiteTurn)
        {
            StartCoroutine(MakeAIMoveCoroutine());
        }

        return true;
    }

    private Vector2Int LookupTileIndex(GameObject hitInfo)
    {
        for (int x = 0; x < TILE_COUNT_X; x++)
        {
            for (int y = 0; y < TILE_COUNT_Y; y++)
            {
                if (tiles[x, y] == hitInfo)
                {
                    return new Vector2Int(x, y);
                }
            }
        }

        return -Vector2Int.one; //無効
    }

    //敵処理
    private IEnumerator MakeAIMoveCoroutine()
    {
        yield return new WaitForSeconds(1.0f);

        if (victoryScreen.activeSelf) yield break;

        AIMove? bestMove = ChessAI.GetBestMove(chessPieces, TILE_COUNT_X, TILE_COUNT_Y, 1);
        if (bestMove.HasValue)
        {
            AIMove move = bestMove.Value;
            availableMoves = move.piece.GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);
            MoveTo(move.piece, move.targetPosition.x, move.targetPosition.y);
            availableMoves.Clear();
        }
        else
        {
            CheckMate(0);
        }
    }

    //次のステージへ
    public void AdvanceToNextStage()
    {
        StopAllCoroutines();
        rewardScreen.SetActive(false);

        currentStage++;

        ClearBoard();

        SpawnAllPieces();
        PositionAllPieces();

        isWhiteTurn = true;
        Debug.Log("次のステージへ");
    }

    //ボードをリセット
    private void ClearBoard()
    {
        currentlyDragging = null;
        RemoveHighlightTiles();

        for (int x = 0; x < TILE_COUNT_X; x++)
        {
            for (int y = 0; y < TILE_COUNT_Y; y++)
            {
                if (chessPieces[x, y] != null)
                {
                    Destroy(chessPieces[x, y].gameObject);
                    chessPieces[x, y] = null;
                }
            }
        }

        for (int i = 0; i < deadWhites.Count; i++)
        {
            if (deadWhites[i] != null) Destroy(deadWhites[i].gameObject);
        }

        for (int i = 0; i < deadBlacks.Count; i++)
        {
            if (deadBlacks[i] != null) Destroy(deadBlacks[i].gameObject);
        }

        deadWhites.Clear();
        deadBlacks.Clear();
    }

    //ボタンを押したときに強化を記録する
    public void SelectReward(UpgradeType upgrade)
    {
        acquiredUpgrades.Add(upgrade);
        AdvanceToNextStage();
    }

    //ボタン１
    public void OnClickRewardCard1()
    {
        if (currentStage == 1)
        {
            SelectReward(UpgradeType.PawnBackward);
        }
        else if (currentStage == 2)
        {
            SelectReward(UpgradeType.KingShield);
        }
    }

    //ボタン２
    public void OnClickRewardCard2()
    {
        if (currentStage == 1)
        {
            SelectReward(UpgradeType.KnightLongJump);
        }
        else if (currentStage == 2)
        {
            SelectReward(UpgradeType.QueenKnightMove);
        }
    }

    //ボタン３
    public void OnClickRewardCard3()
    {
        if (currentStage == 1)
        {
            SelectReward(UpgradeType.RookGhostStep);
        }
        else if (currentStage == 2)
        {
            SelectReward(UpgradeType.PawnAdvanceStart);
        }
    }

    //強化を白駒へと反映
    private void ApplyUpgradesToWhitePieces()
    {
        for (int x = 0; x < TILE_COUNT_X; x++)
        {
            for (int y = 0; y < TILE_COUNT_Y; y++)
            {
                ChessPieces piece = chessPieces[x, y];
                if (piece == null || piece.team != 0) continue;

                //ポーン強化
                if (piece.type == ChessPieceType.Pawn && acquiredUpgrades.Contains(UpgradeType.PawnBackward))
                {
                    piece.canMoveBackWord = true;
                }

                //ナイト強化
                if (piece.type == ChessPieceType.Knight && acquiredUpgrades.Contains(UpgradeType.KnightLongJump))
                {
                    piece.hasLongJump = true;
                }

                //ルーク強化
                if (piece.type == ChessPieceType.Rook && acquiredUpgrades.Contains(UpgradeType.RookGhostStep))
                {
                    piece.hasGhostStep = true;
                }

                //クイーン強化
                if (piece.type == ChessPieceType.Queen && acquiredUpgrades.Contains(UpgradeType.QueenKnightMove))
                {
                    piece.hasKnightMove = true;

                }

                //キング強化
                if (piece.type == ChessPieceType.King && acquiredUpgrades.Contains(UpgradeType.KingShield))
                {
                    piece.hasShield = true;
                }


            }
        }
    }
}
