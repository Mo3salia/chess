using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public enum SpecialMove1
{
    None=0,
    EnPassant,
    Castling,
    Promotion
}
public class Chessboard : MonoBehaviour
{
    //art 
    [Header("ART")]
    [SerializeField] private Material tileMaterial;
    [SerializeField] private float tileSize = 1f;
    [SerializeField] private float yOffset = 0.2f;
    [SerializeField] private float deathSize = 0.3f;
    [SerializeField] private float deathSpacing = 0.3f;
    [SerializeField] private Vector3 boardCenter = Vector3.zero;
    [SerializeField] private float dragOffset = 1.25f;
    [SerializeField] private GameObject victoryScreen;
    [SerializeField] private AudioSource movePieceSound;
    
    
    [Header("perfabs & materials")]
    [SerializeField] private GameObject[] prefabs;
    [SerializeField] private Material[] teamMaterials;

    // logic 
    private chessPiece[,] chessPieces;
    private chessPiece currentlyDragging;
    private List<Vector2Int> availableMoves = new List<Vector2Int>();
    private List<chessPiece> deadWhites = new List<chessPiece>();
    private List<chessPiece> deadBlacks = new List<chessPiece>();
    private List<chessPiece> AiBlack = new List<chessPiece>();
    private const int TILE_COUNT_X = 8;
    private const int TILE_COUNT_Y = 8;
    private GameObject[,] tiles;
    private Camera currentCamera;
    private Vector2Int currentHover;
    private Vector3 bounds;
    private bool isWhiteTurn;
    private List<Vector2Int[]> moveList = new List<Vector2Int[]>();
    private SpecialMove1 SpecialMove1;
    public bool QueenPromotion=false;
    public bool BishopPromotion = false;
    public bool knightPromotion = false;
    public bool RookPromotion = false;
    [SerializeField] private GameObject promotionMenu;

    //scene
    public string sceneName;
    public Scene currentScene;
    private void Awake()
    {
        isWhiteTurn = true;
        GenerateAlTiles(tileSize, TILE_COUNT_X, TILE_COUNT_Y);
        SpwanAllPieces();
        PositionAllPieces();
        // Create a temporary reference to the current scene.
         currentScene = SceneManager.GetActiveScene();

        // Retrieve the name of this scene.
         sceneName = currentScene.name;
    }
    private void Update()
    {
        //pause menu
        if (Input.GetButtonDown("Cancel"))
        {
            if (gamepaused == false)
            {
               
                gamepaused = true;
                Cursor.visible = true;
                pauseMenu.SetActive(true);
            }
            else
            {
                
                gamepaused = false;
                Cursor.visible = false;
                pauseMenu.SetActive(false);

            }
        }
        if (!currentCamera)
        {
            currentCamera = Camera.main;
            return;
        }
        
        RaycastHit info;
        Ray ray = currentCamera.ScreenPointToRay(Input.mousePosition);
        if (currentScene.name == "multiplayer")
        {
            if (!isPositionUIobject())
            {
                if (Physics.Raycast(ray, out info, 100, LayerMask.GetMask("Tile", "Hover", "Highlight")))
                {

                    Vector2Int hitPosition = LookTileIndex(info.transform.gameObject);

                    //if we're hovering a tile after not hovering any tiles 
                    if (currentHover == -Vector2Int.one)
                    {
                        currentHover = hitPosition;
                        tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
                    }
                    // if we were already hovering a tile , change the pervious one 
                    if (currentHover != hitPosition)
                    {
                        tiles[currentHover.x, currentHover.y].layer = (ContaninsValidMove(ref availableMoves, currentHover)) ?
                            LayerMask.NameToLayer("Highlight") : LayerMask.NameToLayer("Tile");
                        currentHover = hitPosition;
                        tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");

                    }
                    // if we press down on the mouse
                    if (Input.GetMouseButtonDown(0))
                    {
                        if (chessPieces[hitPosition.x, hitPosition.y] != null)
                        {
                            //is it your turn ?
                            if ((chessPieces[hitPosition.x, hitPosition.y].team == 0 && isWhiteTurn) || (chessPieces[hitPosition.x, hitPosition.y].team == 1 && !isWhiteTurn))
                            {
                                currentlyDragging = chessPieces[hitPosition.x, hitPosition.y];

                                // get a list of available moves 
                                availableMoves = currentlyDragging.GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);
                                // get a list of special moves
                                SpecialMove1 = currentlyDragging.GetSpecialMove1s(ref chessPieces, ref moveList, ref availableMoves);

                                preventCheck();
                                HighlightTiles();
                            }
                        }
                    }
                    // if we re releasing the mouse button 
                    if (Input.GetMouseButtonUp(0) && currentlyDragging != null)
                    {
                        Vector2Int previousPosition = new Vector2Int(currentlyDragging.currentX, currentlyDragging.currentY);
                        bool validMove = MoveTo(currentlyDragging, hitPosition.x, hitPosition.y);
                        if (!validMove)
                            currentlyDragging.SetPosition(GetTileCenter(previousPosition.x, previousPosition.y));

                        currentlyDragging = null;
                        RemoveHighlightTiles();

                    }
                }
                else
                {
                    if (currentHover != -Vector2Int.one)
                    {
                        tiles[currentHover.x, currentHover.y].layer = (ContaninsValidMove(ref availableMoves, currentHover)) ?
                            LayerMask.NameToLayer("Highlight") : LayerMask.NameToLayer("Tile");
                        currentHover = -Vector2Int.one;
                    }
                    if (currentlyDragging && Input.GetMouseButtonUp(0))
                    {
                        currentlyDragging.SetPosition(GetTileCenter(currentlyDragging.currentX, currentlyDragging.currentY));
                        currentlyDragging = null;
                        RemoveHighlightTiles();
                    }

                }
                // if we are dragging a piece 
                if (currentlyDragging)
                {
                    Plane horizotnalPlane = new Plane(Vector3.up, Vector3.up * yOffset);
                    float distance = 0.0f;
                    if (horizotnalPlane.Raycast(ray, out distance))
                        currentlyDragging.SetPosition(ray.GetPoint(distance) + Vector3.up * dragOffset);
                }
            }
        }
        else if (currentScene.name == "singleplayer")
        {
            if (isWhiteTurn)
            {
            if (!isPositionUIobject())
            {
                if (Physics.Raycast(ray, out info, 100, LayerMask.GetMask("Tile", "Hover", "Highlight")))
                {

                    Vector2Int hitPosition = LookTileIndex(info.transform.gameObject);

                    //if we're hovering a tile after not hovering any tiles 
                    if (currentHover == -Vector2Int.one)
                    {
                        currentHover = hitPosition;
                        tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
                    }
                    // if we were already hovering a tile , change the pervious one 
                    if (currentHover != hitPosition)
                    {
                        tiles[currentHover.x, currentHover.y].layer = (ContaninsValidMove(ref availableMoves, currentHover)) ?
                            LayerMask.NameToLayer("Highlight") : LayerMask.NameToLayer("Tile");
                        currentHover = hitPosition;
                        tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");

                    }
                    // if we press down on the mouse
                    if (Input.GetMouseButtonDown(0))
                    {
                        if (chessPieces[hitPosition.x, hitPosition.y] != null)
                        {
                            //is it your turn ?
                            if ((chessPieces[hitPosition.x, hitPosition.y].team == 0 && isWhiteTurn))
                            {
                                currentlyDragging = chessPieces[hitPosition.x, hitPosition.y];

                                // get a list of available moves 
                                availableMoves = currentlyDragging.GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);
                                // get a list of special moves
                                SpecialMove1 = currentlyDragging.GetSpecialMove1s(ref chessPieces, ref moveList, ref availableMoves);

                                preventCheck();
                                HighlightTiles();
                            }
                        }
                    }
                    // if we re releasing the mouse button 
                    if (Input.GetMouseButtonUp(0) && currentlyDragging != null)
                    {
                        Vector2Int previousPosition = new Vector2Int(currentlyDragging.currentX, currentlyDragging.currentY);
                        bool validMove = MoveTo(currentlyDragging, hitPosition.x, hitPosition.y);
                        if (!validMove)
                            currentlyDragging.SetPosition(GetTileCenter(previousPosition.x, previousPosition.y));

                        currentlyDragging = null;
                        RemoveHighlightTiles();

                    }
                }
                else
                {
                    if (currentHover != -Vector2Int.one)
                    {
                        tiles[currentHover.x, currentHover.y].layer = (ContaninsValidMove(ref availableMoves, currentHover)) ?
                            LayerMask.NameToLayer("Highlight") : LayerMask.NameToLayer("Tile");
                        currentHover = -Vector2Int.one;
                    }
                    if (currentlyDragging && Input.GetMouseButtonUp(0))
                    {
                        currentlyDragging.SetPosition(GetTileCenter(currentlyDragging.currentX, currentlyDragging.currentY));
                        currentlyDragging = null;
                        RemoveHighlightTiles();
                    }

                }
                // if we are dragging a piece 
                if (currentlyDragging)
                {
                    Plane horizotnalPlane = new Plane(Vector3.up, Vector3.up * yOffset);
                    float distance = 0.0f;
                    if (horizotnalPlane.Raycast(ray, out distance))
                        currentlyDragging.SetPosition(ray.GetPoint(distance) + Vector3.up * dragOffset);
                }
            }
            }
            else
            {
                for (int r = 0; r < 8; r++)
                {
                    for (int c = 0; c < 8; c++)
                    {
                        if (chessPieces[r, c] != null)
                        {
                            //is it your turn ?
                            if ( (chessPieces[r, c].team == 1 && !isWhiteTurn))
                            {
                                currentlyDragging = chessPieces[r, c];

                                // get a list of available moves 
                                availableMoves = currentlyDragging.GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);
                                // get a list of special moves
                                SpecialMove1 = currentlyDragging.GetSpecialMove1s(ref chessPieces, ref moveList, ref availableMoves);

                                preventCheck();
                                if (availableMoves.Count > 0)
                                {
                                    currentlyDragging.AvailableMovesAI = availableMoves;
                                    currentlyDragging.specialMovesAI = SpecialMove1;
                                    AiBlack.Add(currentlyDragging);
                                    
                                }
                                currentlyDragging = null;
                                
                            }
                        }
                    }
                }
                int randomNum1 = Random.Range(0, AiBlack.Count);
                availableMoves = AiBlack[randomNum1].AvailableMovesAI;
                SpecialMove1 = AiBlack[randomNum1].specialMovesAI;
                int randomNum2 = Random.Range(0,availableMoves.Count);
                bool validMove = MoveTo(AiBlack[randomNum1], availableMoves[randomNum2].x, availableMoves[randomNum2].y);
                if (!validMove)
                    AiBlack[randomNum1].SetPosition(GetTileCenter(AiBlack[randomNum1].currentX, AiBlack[randomNum1].currentY));
                AiBlack.Clear();
                RemoveHighlightTiles();

            }

        }

    }

    // generate the board 
    private void GenerateAlTiles(float tileSize, int tILE_COUNT_X, int tILE_COUNT_Y)
    {
        yOffset += transform.position.y;
        bounds = new Vector3((tILE_COUNT_X / 2) * tileSize, 0, (tILE_COUNT_X / 2) * tileSize) + boardCenter;

        tiles = new GameObject[tILE_COUNT_X, tILE_COUNT_Y];
        for (int x = 0; x < tILE_COUNT_X; x++)
            for (int y = 0; y < tILE_COUNT_Y; y++)
                tiles[x, y] = GenerateSingleTile(tileSize, x, y);
    }

    //get the index of the tile i've hit
    private GameObject GenerateSingleTile(float tileSize, int x, int y)
    {
        GameObject tileObject = new GameObject($"X:{x}, Y:{y}");
        tileObject.transform.parent = transform;

        Mesh mesh = new Mesh();
        tileObject.AddComponent<MeshFilter>().mesh = mesh;
        tileObject.AddComponent<MeshRenderer>().material = tileMaterial;

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

    // swaping of the pieces 
    private void SpwanAllPieces()
    {
        chessPieces = new chessPiece[TILE_COUNT_X, TILE_COUNT_Y];
        int WhiteTeam = 0;
        int BlackTeam = 1;

        // white team 

        chessPieces[0, 0] = SpawnSinglePiece(ChessPieceType.Rook, WhiteTeam);
        chessPieces[1, 0] = SpawnSinglePiece(ChessPieceType.Knight, WhiteTeam);
        chessPieces[2, 0] = SpawnSinglePiece(ChessPieceType.Bishop, WhiteTeam);
        chessPieces[3, 0] = SpawnSinglePiece(ChessPieceType.Queen, WhiteTeam);
        chessPieces[4, 0] = SpawnSinglePiece(ChessPieceType.King, WhiteTeam);
        chessPieces[5, 0] = SpawnSinglePiece(ChessPieceType.Bishop, WhiteTeam);
        chessPieces[6, 0] = SpawnSinglePiece(ChessPieceType.Knight, WhiteTeam);
        chessPieces[7, 0] = SpawnSinglePiece(ChessPieceType.Rook, WhiteTeam);
        for (int i = 0; i < TILE_COUNT_X; i++)
            chessPieces[i, 1] = SpawnSinglePiece(ChessPieceType.Pawn, WhiteTeam);

        // Black team 

        chessPieces[0, 7] = SpawnSinglePiece(ChessPieceType.Rook, BlackTeam);
        chessPieces[1, 7] = SpawnSinglePiece(ChessPieceType.Knight, BlackTeam);
        chessPieces[2, 7] = SpawnSinglePiece(ChessPieceType.Bishop, BlackTeam);
        chessPieces[3, 7] = SpawnSinglePiece(ChessPieceType.Queen, BlackTeam);
        chessPieces[4, 7] = SpawnSinglePiece(ChessPieceType.King, BlackTeam);
        chessPieces[5, 7] = SpawnSinglePiece(ChessPieceType.Bishop, BlackTeam);
        chessPieces[6, 7] = SpawnSinglePiece(ChessPieceType.Knight, BlackTeam);
        chessPieces[7, 7] = SpawnSinglePiece(ChessPieceType.Rook, BlackTeam);
        for (int i = 0; i < TILE_COUNT_X; i++)
            chessPieces[i, 6] = SpawnSinglePiece(ChessPieceType.Pawn, BlackTeam);


    }


    private chessPiece SpawnSinglePiece(ChessPieceType type, int team)
    {
        chessPiece cp = Instantiate(prefabs[(int)type - 1], transform).GetComponent<chessPiece>();

        cp.type = type;
        cp.team = team;
        cp.GetComponent<MeshRenderer>().material = teamMaterials[team];
        return cp;

    }

    //positioning
    private void PositionAllPieces()
    {
        for (int x = 0; x < TILE_COUNT_X; x++)
            for (int y = 0; y < TILE_COUNT_Y; y++)
                if (chessPieces[x, y] != null)
                    PositionSinglePiece(x, y, true);
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

    //highlight tiles 
    private void HighlightTiles()
    {
        for (int i = 0; i < availableMoves.Count; i++)
            tiles[availableMoves[i].x, availableMoves[i].y].layer = LayerMask.NameToLayer("Highlight");
    }
    private void RemoveHighlightTiles()
    {
        for (int i = 0; i < availableMoves.Count; i++)
            tiles[availableMoves[i].x, availableMoves[i].y].layer = LayerMask.NameToLayer("Tile");

        availableMoves.Clear();
    }


    //checkMate

    private void checkMate(int team)
    {
        DisplayVicory(team);

    }

    //special moves
    private void ProcessSpecialMove1()
    {
        if (SpecialMove1 == SpecialMove1.EnPassant)
        {
            var newMove = moveList[moveList.Count - 1];
            chessPiece myPawn = chessPieces[newMove[1].x, newMove[1].y];
            var targetPawnPosition = moveList[moveList.Count - 2];
            chessPiece enmyPawn = chessPieces[targetPawnPosition[1].x, targetPawnPosition[1].y];

            if (myPawn.currentX == enmyPawn.currentX)
            {
                if (myPawn.currentY == enmyPawn.currentY - 1 || myPawn.currentY == enmyPawn.currentY + 1)
                {
                    if (enmyPawn.team == 0)
                    {
                        deadWhites.Add(enmyPawn);
                        enmyPawn.SetScale(Vector3.one * deathSize);
                        enmyPawn.SetPosition(new Vector3(8.25f * tileSize, yOffset, -2 * tileSize) - bounds
                            + new Vector3(tileSize / 2, 0, tileSize / 2)
                            + (Vector3.forward * deathSpacing) * deadWhites.Count);
                    }
                    else
                    {
                        deadBlacks.Add(enmyPawn);
                        enmyPawn.SetScale(Vector3.one * deathSize);
                        enmyPawn.SetPosition(new Vector3(-1.25f * tileSize, yOffset, 9 * tileSize) - bounds
                            + new Vector3(tileSize / 2, 0, tileSize / 2)
                            + (Vector3.back * deathSpacing) * deadBlacks.Count);

                    }
                    chessPieces[enmyPawn.currentX, enmyPawn.currentY] = null;
                }
            }

        }

        if (SpecialMove1 == SpecialMove1.Promotion)
        {
            var lastMove = moveList[moveList.Count - 1];
            chessPiece tragetPawn = chessPieces[lastMove[1].x, lastMove[1].y];

            if (tragetPawn.type == ChessPieceType.Pawn)
            {
                if ((currentScene.name == "singleplayer" && isWhiteTurn))
                {
                    if (tragetPawn.team == 1 && lastMove[1].y == 0) //black side
                    {
                        chessPiece newPiece = SpawnSinglePiece(ChessPieceType.Queen, 1);
                        newPiece.transform.position = chessPieces[lastMove[1].x, lastMove[1].y].transform.position;
                        Destroy(chessPieces[lastMove[1].x, lastMove[1].y].gameObject);
                        chessPieces[lastMove[1].x, lastMove[1].y] = newPiece;
                        PositionSinglePiece(lastMove[1].x, lastMove[1].y);
                    }
                    if (CheckForCheckmate())
                        checkMate(tragetPawn.team);
                    cameraposition(tragetPawn.team);
                }
                else
                {
                    promotionMenu.SetActive(true);
                    Time.timeScale = 0;
                }
            }


        }

        if (SpecialMove1 == SpecialMove1.Castling)
        {
            Vector2Int[] lastMove = moveList[moveList.Count - 1];
            //left rook
            if (lastMove[1].x == 2)
            {
                if (lastMove[1].y == 0) // white side
                {
                    chessPiece rook = chessPieces[0, 0];
                    chessPieces[3, 0] = rook;
                    PositionSinglePiece(3, 0);
                    chessPieces[0, 0] = null;
                }
                else if (lastMove[1].y == 7) // black side
                {
                    chessPiece rook = chessPieces[0, 7];
                    chessPieces[3, 7] = rook;
                    PositionSinglePiece(3, 7);
                    chessPieces[0, 7] = null;

                }
            }
            //right rook
            else if (lastMove[1].x == 6)
            {
                if (lastMove[1].y == 0) // white side
                {
                    chessPiece rook = chessPieces[7, 0];
                    chessPieces[5, 0] = rook;
                    PositionSinglePiece(5, 0);
                    chessPieces[7, 0] = null;
                }
                else if (lastMove[1].y == 7) // black side
                {
                    chessPiece rook = chessPieces[7, 7];
                    chessPieces[5, 7] = rook;
                    PositionSinglePiece(5, 7);
                    chessPieces[7, 7] = null;

                }

            }

        }

    }

    private void preventCheck()
    {
        chessPiece targetKing = null;

        for (int x = 0; x < TILE_COUNT_X; x++)
            for (int y = 0; y < TILE_COUNT_Y; y++)
                if (chessPieces[x, y] != null)
                    if (chessPieces[x, y].type == ChessPieceType.King)
                        if (chessPieces[x, y].team == currentlyDragging.team)
                            targetKing = chessPieces[x, y];
        // reduce the moves that will make king wirhout protection
        SimulateMoveSinglePiece(currentlyDragging, ref availableMoves, targetKing);

    }
    private void SimulateMoveSinglePiece(chessPiece cp, ref List<Vector2Int> moves, chessPiece targetKing)
    {
        // save the currnt values , to reset after the function call
        int actualX = cp.currentX;
        int actualY = cp.currentY;
        List<Vector2Int> moveToRemove = new List<Vector2Int>();

        //goin through all the moves, simualte them and check if we're in check
        for (int i = 0; i < moves.Count; i++)
        {
            int simX = moves[i].x;
            int simY = moves[i].y;
            Vector2Int kingPositionThisSim = new Vector2Int(targetKing.currentX, targetKing.currentY);
            //did we simulate the king's move
            if (cp.type == ChessPieceType.King)
                kingPositionThisSim = new Vector2Int(simX, simY);

            //copy the [,] and not a ref
            chessPiece[,] simulation = new chessPiece[TILE_COUNT_X, TILE_COUNT_Y];
            List<chessPiece> simAttackingPieces = new List<chessPiece>();
            for (int x = 0; x < TILE_COUNT_X; x++)
                for (int y = 0; y < TILE_COUNT_Y; y++)
                {
                    if (chessPieces[x, y] != null)
                    {
                        simulation[x, y] = chessPieces[x, y];
                        if (simulation[x, y].team != cp.team)
                            simAttackingPieces.Add(simulation[x, y]);
                    }
                }
            // simulate that move
            simulation[actualX, actualY] = null;
            cp.currentX = simX;
            cp.currentY = simY;
            simulation[simX, simY] = cp;

            //did one of the piece got taken down during our simulation 
            var deadpiece = simAttackingPieces.Find(c => c.currentX == simX && c.currentY == simY);
            if (deadpiece != null)
                simAttackingPieces.Remove(deadpiece);

            //get all the simulated attacking pieces moves 
            List<Vector2Int> simMoves = new List<Vector2Int>();
            for (int a = 0; a < simAttackingPieces.Count; a++)
            {
                var piecemoves = simAttackingPieces[a].GetAvailableMoves(ref simulation, TILE_COUNT_X, TILE_COUNT_Y);
                for (int pm = 0; pm < piecemoves.Count; pm++)
                    simMoves.Add(piecemoves[pm]);
            }

            //if the king in trouble ?so ,remove the move
            if (ContaninsValidMove(ref simMoves, kingPositionThisSim))
                moveToRemove.Add(moves[i]);

            //restore the actual cp data
            cp.currentX = actualX;
            cp.currentY = actualY;

        }
        //remove from the currnt available move list 
        for (int i = 0; i < moveToRemove.Count; i++)
            moves.Remove(moveToRemove[i]);

    }
    private bool CheckForCheckmate()
    {
        var lastMove = moveList[moveList.Count - 1];
        int targetTeam = (chessPieces[lastMove[1].x, lastMove[1].y].team == 0) ? 1 : 0;

        List<chessPiece> attackingPieces = new List<chessPiece>();
        List<chessPiece> defendingPieces = new List<chessPiece>();
        chessPiece targetKing = null;
        for (int x = 0; x < TILE_COUNT_X; x++)
            for (int y = 0; y < TILE_COUNT_Y; y++)
                if (chessPieces[x, y] != null)
                {
                    if (chessPieces[x, y].team == targetTeam)
                    {
                        defendingPieces.Add(chessPieces[x, y]);
                        if (chessPieces[x, y].type == ChessPieceType.King)
                            targetKing = chessPieces[x, y];
                    }
                    else
                    {
                        attackingPieces.Add(chessPieces[x, y]);
                    }


                }

        // is the king attacked right now ?

        List<Vector2Int> currentAvailableMoves = new List<Vector2Int>();
        for (int i = 0; i < attackingPieces.Count; i++)
        {
            var piecemoves = attackingPieces[i].GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);
            for (int pm = 0; pm < piecemoves.Count; pm++)
                currentAvailableMoves.Add(piecemoves[pm]);
        }
        // are we in check right now ?
        if (ContaninsValidMove(ref currentAvailableMoves, new Vector2Int(targetKing.currentX, targetKing.currentY)))
        {
            // king is under attack , can we move something to help him ?
            for (int i = 0; i < defendingPieces.Count; i++)
            {
                List<Vector2Int> defendingMoves = defendingPieces[i].GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);
                SimulateMoveSinglePiece(defendingPieces[i], ref defendingMoves, targetKing);

                if (defendingMoves.Count != 0)
                    return false;
            }

            return true; //checkmate exit
        }

        return false;
    }


    // Operations
    private void cameraposition(int team)
    {
        if (currentScene.name == "multiplayer")
        {
            if (team == 1)
                Camera.main.transform.SetPositionAndRotation(new Vector3(-0.2f, 4.27f, -3.44f), Quaternion.Euler(57.936f, 0, 0));
            else
                Camera.main.transform.SetPositionAndRotation(new Vector3(0.2f, 4.27f, 3.44f), Quaternion.Euler(57.936f, 180, 0));
        }
        
    }

    private bool ContaninsValidMove(ref List<Vector2Int> moves, Vector2Int pos)
    {
        for (int i = 0; i < moves.Count; i++)
            if (moves[i].x == pos.x && moves[i].y == pos.y)
                return true;

        return false;


    }
    private Vector2Int LookTileIndex (GameObject hitInfo)
    {
          for(int x = 0;x <TILE_COUNT_X;x++)
            for (int y = 0;y <TILE_COUNT_Y;y++)
                if (tiles[x,y] == hitInfo)
                    return new Vector2Int (x,y);

        return -Vector2Int.one; //-1,-1 invalide


    }
    private bool MoveTo(chessPiece cp, int x, int y)
    {
        if (!ContaninsValidMove(ref availableMoves, new Vector2Int(x, y)))
            return false;

        Vector2Int previousPosition = new Vector2Int (cp.currentX,cp.currentY);

        //is there another piece on the traget position ?
        if(chessPieces [x,y] != null)
        {
            chessPiece ocp = chessPieces[x, y];
            if (cp.team == ocp.team)
                return false;

            // if its in enemy team 

            if(ocp.team == 0)
            {
                if (ocp.type == ChessPieceType.King)
                    checkMate(1);
                deadWhites.Add(ocp);
                ocp.SetScale(Vector3.one * deathSize);
                ocp.SetPosition(new Vector3(8.25f*tileSize,yOffset,-2*tileSize)-bounds 
                    +new Vector3 (tileSize/2,0,tileSize/2) 
                    +(Vector3.forward *deathSpacing)*deadWhites.Count);
                movePieceSound.Play();
                cameraposition(ocp.team);

            }
            else
            {
                if (ocp.type == ChessPieceType.King)
                    checkMate(0);
                deadBlacks.Add(ocp);
                ocp.SetScale(Vector3.one * deathSize);
                ocp.SetPosition(new Vector3(-1.25f * tileSize, yOffset, 9 * tileSize) - bounds
                    + new Vector3(tileSize / 2, 0, tileSize / 2)
                    + (Vector3.back * deathSpacing) * deadBlacks.Count);
                movePieceSound.Play();
                cameraposition(ocp.team);
            }
            
        }

        chessPieces [x,y] =cp;
        chessPieces[previousPosition.x, previousPosition.y] = null;

        PositionSinglePiece(x, y);

        isWhiteTurn = !isWhiteTurn;
        moveList.Add(new Vector2Int[] { previousPosition, new Vector2Int(x, y) });
        ProcessSpecialMove1();
        if (CheckForCheckmate())
            checkMate(cp.team);
        cameraposition(cp.team);
        movePieceSound.Play();
        return true;
    }
    //UI
    private bool isPositionUIobject()
    {

        PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
        eventDataCurrentPosition.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
        return results.Count > 0;
    }

    private void DisplayVicory(int winningTeam)
    {
        victoryScreen.SetActive(true);
        victoryScreen.transform.GetChild(winningTeam).gameObject.SetActive(true);
    }
    public void OnResetButton()
    {
        //UI
        pauseMenu.SetActive(false);
        gamepaused = false;
        victoryScreen.SetActive(false);
        victoryScreen.transform.GetChild(0).gameObject.SetActive(false);
        victoryScreen.transform.GetChild(1).gameObject.SetActive(false);

        //fields reset
        currentlyDragging = null;
        availableMoves.Clear();
        moveList.Clear();
        cameraposition(1);

        //clean Up
        for (int x = 0; x < TILE_COUNT_X; x++)
            for (int y = 0; y < TILE_COUNT_Y; y++)
            {
                if (chessPieces[x, y] != null)
                    Destroy(chessPieces[x, y].gameObject);
                chessPieces[x, y] = null;
            }

        for (int i = 0; i < deadWhites.Count; i++)
            Destroy(deadWhites[i].gameObject);
        for (int i = 0; i < deadBlacks.Count; i++)
            Destroy(deadBlacks[i].gameObject);

        deadWhites.Clear();
        deadBlacks.Clear();

        SpwanAllPieces();
        PositionAllPieces();
        isWhiteTurn = true;

    }
    //pause menu
    public bool gamepaused = false;
    public GameObject pauseMenu;
    public void UnpauseGame()
    {
        gamepaused = false;
        pauseMenu.SetActive(false);

    }

    public void QuitLevel()
    {
        SceneManager.LoadScene(1);
    }
    //promotion menu
    public void queen ()
    {
        var lastMove = moveList[moveList.Count - 1];
        chessPiece tragetPawn = chessPieces[lastMove[1].x, lastMove[1].y];
        if (tragetPawn.team == 0 && lastMove[1].y == 7) //white side
        {
            chessPiece newPiece = SpawnSinglePiece(ChessPieceType.Queen, 0);
            newPiece.transform.position = chessPieces[lastMove[1].x, lastMove[1].y].transform.position;
            Destroy(chessPieces[lastMove[1].x, lastMove[1].y].gameObject);
            chessPieces[lastMove[1].x, lastMove[1].y] = newPiece;
            PositionSinglePiece(lastMove[1].x, lastMove[1].y);
            
        }
        else if (tragetPawn.team == 1 && lastMove[1].y == 0) //black side
        {
            chessPiece newPiece = SpawnSinglePiece(ChessPieceType.Queen, 1);
            newPiece.transform.position = chessPieces[lastMove[1].x, lastMove[1].y].transform.position;
            Destroy(chessPieces[lastMove[1].x, lastMove[1].y].gameObject);
            chessPieces[lastMove[1].x, lastMove[1].y] = newPiece;
            PositionSinglePiece(lastMove[1].x, lastMove[1].y);
        }
        if (CheckForCheckmate())
            checkMate(tragetPawn.team);
        cameraposition(tragetPawn.team);
        promotionMenu.SetActive(false);
        Time.timeScale = 1;
    }
    public void bishop()
    {
        var lastMove = moveList[moveList.Count - 1];
        chessPiece tragetPawn = chessPieces[lastMove[1].x, lastMove[1].y];
        if (tragetPawn.team == 0 && lastMove[1].y == 7) //white side
        {
            chessPiece newPiece = SpawnSinglePiece(ChessPieceType.Bishop, 0);
            newPiece.transform.position = chessPieces[lastMove[1].x, lastMove[1].y].transform.position;
            Destroy(chessPieces[lastMove[1].x, lastMove[1].y].gameObject);
            chessPieces[lastMove[1].x, lastMove[1].y] = newPiece;
            PositionSinglePiece(lastMove[1].x, lastMove[1].y);
        }
        else if (tragetPawn.team == 1 && lastMove[1].y == 0) //black side
        {
            chessPiece newPiece = SpawnSinglePiece(ChessPieceType.Bishop, 1);
            newPiece.transform.position = chessPieces[lastMove[1].x, lastMove[1].y].transform.position;
            Destroy(chessPieces[lastMove[1].x, lastMove[1].y].gameObject);
            chessPieces[lastMove[1].x, lastMove[1].y] = newPiece;
            PositionSinglePiece(lastMove[1].x, lastMove[1].y);
        }
        if (CheckForCheckmate())
            checkMate(tragetPawn.team);
        cameraposition(tragetPawn.team);
        promotionMenu.SetActive(false);
        Time.timeScale = 1;
    }
    public void knight()
    {
        var lastMove = moveList[moveList.Count - 1];
        chessPiece tragetPawn = chessPieces[lastMove[1].x, lastMove[1].y];
        if (tragetPawn.team == 0 && lastMove[1].y == 7) //white side
        {
            chessPiece newPiece = SpawnSinglePiece(ChessPieceType.Knight, 0);
            newPiece.transform.position = chessPieces[lastMove[1].x, lastMove[1].y].transform.position;
            Destroy(chessPieces[lastMove[1].x, lastMove[1].y].gameObject);
            chessPieces[lastMove[1].x, lastMove[1].y] = newPiece;
            PositionSinglePiece(lastMove[1].x, lastMove[1].y);
        }
        else if (tragetPawn.team == 1 && lastMove[1].y == 0) //black side
        {
            chessPiece newPiece = SpawnSinglePiece(ChessPieceType.Knight, 1);
            newPiece.transform.position = chessPieces[lastMove[1].x, lastMove[1].y].transform.position;
            Destroy(chessPieces[lastMove[1].x, lastMove[1].y].gameObject);
            chessPieces[lastMove[1].x, lastMove[1].y] = newPiece;
            PositionSinglePiece(lastMove[1].x, lastMove[1].y);
        }
        if (CheckForCheckmate())
            checkMate(tragetPawn.team);
        cameraposition(tragetPawn.team);
        promotionMenu.SetActive(false);
        Time.timeScale = 1;
    }
    public void rook()
    {
        var lastMove = moveList[moveList.Count - 1];
        chessPiece tragetPawn = chessPieces[lastMove[1].x, lastMove[1].y];
        if (tragetPawn.team == 0 && lastMove[1].y == 7) //white side
        {
            chessPiece newPiece = SpawnSinglePiece(ChessPieceType.Rook, 0);
            newPiece.transform.position = chessPieces[lastMove[1].x, lastMove[1].y].transform.position;
            Destroy(chessPieces[lastMove[1].x, lastMove[1].y].gameObject);
            chessPieces[lastMove[1].x, lastMove[1].y] = newPiece;
            PositionSinglePiece(lastMove[1].x, lastMove[1].y);
        }
        else if (tragetPawn.team == 1 && lastMove[1].y == 0) //black side
        {
            chessPiece newPiece = SpawnSinglePiece(ChessPieceType.Rook, 1);
            newPiece.transform.position = chessPieces[lastMove[1].x, lastMove[1].y].transform.position;
            Destroy(chessPieces[lastMove[1].x, lastMove[1].y].gameObject);
            chessPieces[lastMove[1].x, lastMove[1].y] = newPiece;
            PositionSinglePiece(lastMove[1].x, lastMove[1].y);
        }
        if (CheckForCheckmate())
            checkMate(tragetPawn.team);
        cameraposition(tragetPawn.team);
        promotionMenu.SetActive(false);
        Time.timeScale = 1;
    }
}
