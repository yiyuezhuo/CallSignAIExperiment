using UnityEngine;
using UnityEngine.EventSystems;
using Unity.Properties;
using CallSignLib;
using System.Collections.Generic;
// using UnityEngine.AI;
using TMPro;
using System.Linq;
using Unity.VisualScripting;

public class GameManager : MonoBehaviour
{
    public Grid grid;

    public GameState gameState = GameState.Setup();
    public int currentX;
    public int currentY;

    [DoNotSerialize]
    public Piece currentPiece = null;

    public Transform redNotDeployedTransform;
    public Transform redDestroyedTransform;
    public Transform blueNotDeployedTransform;
    public Transform blueDestroyedTransform;
    public Transform pieceViewersTransform;

    public Transform labelsTransform;
    public GameObject labelPrefab;

    public GameObject piecePrefab;

    public bool showLabels;

    public RandomAgent randomAgent = new();

    public enum StackType
    {
        Map,
        RedNotDeployed,
        RedRegenerated,
        BlueNotDeployed,
        BlueRegenerated
    }

    public Dictionary<Piece, PieceViewer> pieceToViewer = new();


    public void InitialSetup()
    {
        foreach(var piece in gameState.pieces)
        {
            var pieceViewer = Instantiate(piecePrefab, pieceViewersTransform).GetComponent<PieceViewer>();
            pieceViewer.currentPiece = piece;
            pieceViewer.SyncTexture();
            pieceToViewer[piece] = pieceViewer;
            pieceViewer.name = $"{piece.name} ({piece.id})";
        }

        UpdateStackLocations();

        foreach(((var x, var y), var hex) in GameState.grid.hexMap)
        {
            var worldPos = GameXYToWorldPos(x, y);
            var newLabel = Instantiate(labelPrefab, worldPos, Quaternion.Euler(0, 0, 0), labelsTransform);
            var text = newLabel.GetComponent<TMP_Text>();
            text.text = $"({x}, {y})";
        }
    }

    public Dictionary<(StackType, int, int), List<Piece>> CollectStackKeyToPieces()
    {
        // (StackType, x, y) => Piece
        var stackKeyToPieces = new Dictionary<(StackType, int, int), List<Piece>>();
        foreach(var piece in gameState.pieces)
        {
            // var pieceViewer = pieceToViewer[piece];
            var stackType = (piece.mapState, piece.side) switch
            {
                (MapState.NotDeployed, Side.Red) => StackType.RedNotDeployed,
                (MapState.NotDeployed, Side.Blue) => StackType.BlueNotDeployed,
                (MapState.Destroyed, Side.Red) => StackType.RedRegenerated,
                (MapState.Destroyed, Side.Blue) => StackType.BlueRegenerated,
                _ => StackType.Map
            };
            var x = -1;
            var y = -1;
            if(stackType == StackType.Map)
            {
                x = piece.x;
                y = piece.y;
            }
            var stackKey = (stackType, x, y);
            if(!stackKeyToPieces.TryGetValue(stackKey,  out var stack))
            {
                stackKeyToPieces[stackKey] = stack = new();
            }
            stack.Add(piece);
        }
        return stackKeyToPieces;
    }

    public void UpdateStackLocations()
    {
        var stackKeyToPieces = CollectStackKeyToPieces();
        
        foreach(((var stackType, var gameX, var gameY), var stack) in stackKeyToPieces)
        {
            var n=0;
            foreach(var piece in stack)
            {
                var pieceViewer = pieceToViewer[piece];

                var worldPos = stackType switch
                {
                    StackType.RedNotDeployed => redNotDeployedTransform.position,
                    StackType.RedRegenerated => redDestroyedTransform.position,
                    StackType.BlueNotDeployed => blueNotDeployedTransform.position,
                    StackType.BlueRegenerated => blueDestroyedTransform.position,
                    _ => GameXYToWorldPos(piece.x, piece.y)
                };

                pieceViewer.SetStackOffset(n, worldPos);
                n++;
            }
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        InitialSetup();

        GameState.logged += (sender, message) => Debug.Log(message);

        currentPiece = null;
    }

    // Update is called once per frame
    void Update()
    {
        if(!EventSystem.current.IsPointerOverGameObject())
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3Int gridPos = grid.WorldToCell(mousePos);
            var gameXY = GridPosToGameXY(gridPos);

            currentX = gameXY.x;
            currentY = gameXY.y;

            if (Input.GetMouseButtonDown(0))
            {
                // Handle left-click
                Debug.Log($"mousePos={mousePos}, gridPos={gridPos}, gameXY={gameXY}");

                // var ray = Camera.main.ScreenPointToRay(mousePos);
                Vector2 mousePos2 = mousePos;
                var hit = Physics2D.Raycast(mousePos2, Vector2.zero);
                if(hit.collider != null)
                {
                    var pieceViewer = hit.collider.GetComponent<PieceViewer>();
                    if(pieceViewer != null)
                    {
                        // clicked on piece
                        Debug.Log($"Clicked on Piece: {pieceViewer}");

                        currentPiece = pieceViewer.currentPiece;

                        var stackKeyToPieces = CollectStackKeyToPieces();
                        (var selectedStackKey, var selectedStack) = stackKeyToPieces.First(p => p.Value.Contains(pieceViewer.currentPiece));

                        OnStackClicked(selectedStack);
                    }
                }
                // Handle Hex Clicked? (Supprress unit/stack selection sometimes)
            }

            if(Input.GetKeyDown(KeyCode.Escape))
            {
                currentPiece = null;
            }
        }

        labelsTransform.gameObject.SetActive(showLabels);
    }

    public void OnStackClicked(List<Piece> pieces)
    {
        Debug.Log(string.Format("Clicked on stack: {0}", string.Join(",", pieces.Select(p => p.name))));

        StackPieceChooser.Instance.SyncWithStack(pieces);
    }

    public void OnPieceClicked(Piece piece)
    {
        Debug.Log($"OnPieceClicked: {piece.name}");

        currentPiece = piece;
    }

    public Vector2Int GridPosToGameXY(Vector3Int gridPos)
    {
        return new Vector2Int(gridPos.y + 3, -gridPos.x + 2);
    }

    public Vector3Int GameXYToGridPos(Vector2Int gameXY)
    {
        return new Vector3Int(-gameXY.y + 2, gameXY.x - 3, 0);
    }

    public Vector3Int GameXYToGridPos(int gameX, int gameY)
    {
        return new Vector3Int(-gameY + 2, gameX - 3, 0);
    }

    public Vector3 GameXYToWorldPos(int gameX, int gameY)
    {
        return grid.CellToWorld(GameXYToGridPos(gameX, gameY));
    }


    public static GameManager _instance;
    public static GameManager Instance
    {
        get 
        {
            if(_instance == null)
            {
                _instance = FindFirstObjectByType<GameManager>();
            }
            return _instance;
        }
    }
    public void OnDestroy()
    {
        Debug.Log("OnDestroy");
        _instance = null;
    }
}
