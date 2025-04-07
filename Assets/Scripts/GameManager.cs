using UnityEngine;
using UnityEngine.EventSystems;
using Unity.Properties;
using CallSignLib;
using System.Collections.Generic;
using System;
using TMPro;
using System.Linq;


public class GameManager : MonoBehaviour
{
    public Grid grid;

    public GameState gameState = GameState.Setup();
    public int currentX;
    public int currentY;

    // [NonSerialized]
    public Piece currentPiece = null;

    public Transform redNotDeployedTransform;
    public Transform redDestroyedTransform;
    public Transform blueNotDeployedTransform;
    public Transform blueDestroyedTransform;

    public Transform pieceViewersTransform;

    public Transform labelsTransform;
    public GameObject labelPrefab;

    public GameObject piecePrefab;
    public GameObject damageTokenPrefab;

    public bool showLabels;

    public AbstractAgent currentAgent;
    public List<AbstractAgent> agents = new()
    {
        new RandomAgent(),
        new BaselineAgent(),
        new BaselineAgent2(),
        new BaselineAgent3(),
        new BaselineAgent4()
    };

    public enum StackType
    {
        Map,
        RedNotDeployed,
        RedRegenerated,
        BlueNotDeployed,
        BlueRegenerated
    }

    public enum State
    {
        Idle, // Select a unit and use edit command
        EditMoveBegin, // Select a hex to move to.
        SelectShooter,
        SelectTarget
    }

    public State state;

    // public Dictionary<Piece, PieceViewer> pieceToViewer = new();
    public Dictionary<int, PieceViewer> pieceIdToViewer = new();
    public Dictionary<Side, DamageTokenViewer> sideToDamageTokenViewer = new();

    public LayerMask pieceLayer;
    public LayerMask mapLayer;

    public class RefAreaRecord
    {
        public MapState mapState;
        public Side side;
        public Transform transform;
        public StackType stackType;
    }

    List<RefAreaRecord> refAreaRecords;

    public int currentEditEngagementId;
    public Side playingSide;

    void Awake()
    {
        pieceLayer = LayerMask.GetMask("Piece"); // Can't be set in the declaration
        mapLayer = LayerMask.GetMask("Map");

        currentAgent = agents[^1]; // Baseline Agent

        refAreaRecords = new()
        {
            new RefAreaRecord()
            {
                mapState = MapState.NotDeployed,
                side = Side.Blue,
                transform = blueNotDeployedTransform,
                stackType = StackType.BlueNotDeployed
            },
            new RefAreaRecord()
            {
                mapState = MapState.NotDeployed,
                side = Side.Red,
                transform = redNotDeployedTransform,
                stackType = StackType.RedNotDeployed
            },
            new RefAreaRecord()
            {
                mapState = MapState.Destroyed,
                side = Side.Blue,
                transform = blueDestroyedTransform,
                stackType = StackType.BlueRegenerated
            },
            new RefAreaRecord()
            {
                mapState = MapState.Destroyed,
                side = Side.Red,
                transform = redDestroyedTransform,
                stackType = StackType.RedRegenerated
            }
        };
    }

    // 


    public void InitialSetup()
    {
        foreach(var piece in gameState.pieces) // Piece State => Viewer
        {
            var pieceViewer = Instantiate(piecePrefab, pieceViewersTransform).GetComponent<PieceViewer>();
            pieceViewer.currentPieceId = piece.id;
            pieceViewer.SyncTexture();
            // pieceToViewer[piece] = pieceViewer;
            pieceIdToViewer[piece.id] = pieceViewer;
            pieceViewer.name = $"{piece.name} ({piece.id})";
        }

        foreach(var sideData in gameState.sideData) // Side State => Viewer
        {
            // sideToDamageToken[sideData.side] = new DamageToken();
            var damageTokenViewer = Instantiate(damageTokenPrefab, pieceViewersTransform).GetComponent<DamageTokenViewer>();
            damageTokenViewer.side = sideData.side;
            sideToDamageTokenViewer[sideData.side] = damageTokenViewer;
            damageTokenViewer.name = $"{sideData.side} Damage Token";
            damageTokenViewer.gameObject.SetActive(false);
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

    public Dictionary<(StackType, int, int), List<AbstractViewer>> CollectStackKeyToPieces()
    {
        // (StackType, x, y) => Piece
        var stackKeyToPieces = new Dictionary<(StackType, int, int), List<AbstractViewer>>(); // TODO: Use OneOf?
        // Collect Pieces
        foreach(var piece in gameState.pieces)
        {
            // var stackType = (piece.mapState, piece.side) switch
            // {
            //     (MapState.NotDeployed, Side.Red) => StackType.RedNotDeployed,
            //     (MapState.NotDeployed, Side.Blue) => StackType.BlueNotDeployed,
            //     (MapState.Destroyed, Side.Red) => StackType.RedRegenerated,
            //     (MapState.Destroyed, Side.Blue) => StackType.BlueRegenerated,
            //     _ => StackType.Map
            // };

            var refRecord = refAreaRecords.FirstOrDefault(r => r.mapState == piece.mapState && r.side == piece.side);
            var stackType = refRecord != null ? refRecord.stackType : StackType.Map; 

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
            // stack.Add(pieceToViewer[piece]);
            stack.Add(pieceIdToViewer[piece.id]);
        }

        // Collect Virtualized damaged tokens
        foreach(var sideData in gameState.sideData)
        {
            var damageTokenViewer = sideToDamageTokenViewer[sideData.side];
            if(sideData.carrierDamage > 0)
            {
                damageTokenViewer.gameObject.SetActive(true);

                (var x, var y) = sideData.carrierCenter;
                var stackKey = (StackType.Map, x, y);
                if(!stackKeyToPieces.TryGetValue(stackKey,  out var stack))
                {
                    stackKeyToPieces[stackKey] = stack = new();
                }
                stack.Add(damageTokenViewer);
            }
            else
            {
                damageTokenViewer.gameObject.SetActive(false);
            }
        }
        return stackKeyToPieces;
    }

    public void DoCommitUnit()
    {
        if(!(state == State.SelectShooter || state == State.SelectTarget))
        {
            return;
        }

        var decs = gameState.engagementDeclares;
        var recId = currentEditEngagementId;

        if(recId >= 0 && recId < decs.Count && currentPiece != null)
        {
            var valueId = currentPiece.id;
            if(state == State.SelectShooter)
            {
                decs[recId].shooterPieceId = valueId;
            }
            else if(state == State.SelectTarget)
            {
                decs[recId].targetPieceId = valueId;
            }
        }
        
        state = State.Idle;
    }

    public void UpdateStackLocations()
    {
        var stackKeyToPieces = CollectStackKeyToPieces();
        
        foreach(((var stackType, var gameX, var gameY), var stack) in stackKeyToPieces)
        {
            var n=0;
            foreach(var viewer in stack)
            {
                var refRecord = refAreaRecords.FirstOrDefault(r => r.stackType == stackType);
                var worldPos = refRecord != null ? refRecord.transform.position : viewer.GetWorldPos();

                viewer.SetStackOffset(n, worldPos);
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

        // DebugSetup();
    }

    void DebugSetup()
    {
        foreach(var piece in gameState.pieces)
        {
            piece.mapState = MapState.OnMap;
            (piece.x, piece.y) = gameState.sideData[0].carrierCenter;
        }

        UpdateStackLocations();
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            currentPiece = null;
            state = State.Idle;
        }
        if(Input.GetKeyDown(KeyCode.C))
        {
            DoCommitUnit();
        }

        if(!EventSystem.current.IsPointerOverGameObject())
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3Int gridPos = grid.WorldToCell(mousePos);
            var gameXY = GridPosToGameXY(gridPos);

            currentX = gameXY.x;
            currentY = gameXY.y;

            // if(state == State.Idle)
            if(state != State.EditMoveBegin)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    // Handle left-click
                    // Debug.Log($"Idle: mousePos={mousePos}, gridPos={gridPos}, gameXY={gameXY}");

                    // var ray = Camera.main.ScreenPointToRay(mousePos);
                    Vector2 mousePos2 = mousePos;
                    var hit = Physics2D.Raycast(mousePos2, Vector2.zero, Mathf.Infinity, pieceLayer);
                    if(hit.collider != null)
                    {
                        var pieceViewer = hit.collider.GetComponent<PieceViewer>();
                        if(pieceViewer != null)
                        {
                            // clicked on piece
                            Debug.Log($"Clicked on Piece: {pieceViewer}");

                            currentPiece = pieceViewer.currentPiece;

                            var stackKeyToPieces = CollectStackKeyToPieces();
                            (var selectedStackKey, var selectedStack) = stackKeyToPieces.First(p => p.Value.Contains(pieceViewer));

                            OnStackClicked(selectedStack);
                        }
                    }
                    // Handle Hex Clicked? (Supprress unit/stack selection sometimes)
                }

                if(Input.GetKeyDown(KeyCode.M) && currentPiece != null)
                {
                    state = State.EditMoveBegin;
                }
            }
            else if(state == State.EditMoveBegin)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    Vector2 mousePos2 = mousePos;
                    var hit = Physics2D.Raycast(mousePos2, Vector2.zero, Mathf.Infinity, mapLayer);
                    if(hit.collider != null) // ref area
                    {
                        Debug.Log($"ref area={hit.collider}");
                        if(currentPiece != null)
                        {
                            //
                            // hit.collider.transform switch
                            // {
                            //     redNotDeployedTransform => (MapState.NotDeployed, Side.Red),
                            //     blueNotDeployedTransform => (MapState.NotDeployed, Side.Blue),
                            //     redRegeneratedTransform => (MapState.Destroyed, Side.Red),
                            //     blueRegeneratedTransform =>(MapState.Destroyed, Side.Blue),
                            // };
                            var refRecord = refAreaRecords.FirstOrDefault(r => r.transform == hit.collider.transform);
                            if(currentPiece.side == refRecord.side)
                            {
                                currentPiece.mapState = refRecord.mapState;
                            }
                            else
                            {
                                Debug.Log("It's not a valid move target");
                            }
                        }
                    }
                    else // hex
                    {
                        Debug.Log($"EditMoveBegin: mousePos={mousePos}, gridPos={gridPos}, gameXY={gameXY}");
                        
                        if(currentPiece != null)
                        {
                            currentPiece.mapState = MapState.OnMap;
                            currentPiece.x = gameXY.x;
                            currentPiece.y = gameXY.y;
                        }
                    }

                    state = State.Idle;
                    UpdateStackLocations();
                }
            }
        }

        // Sync
        labelsTransform.gameObject.SetActive(showLabels);
    }

    public void OnStackClicked(List<AbstractViewer> pieces)
    {
        Debug.Log(string.Format("Clicked on stack: {0}", string.Join(",", pieces.Select(p => p))));

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

    // [CreateProperty]

    // public currentGameState

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
