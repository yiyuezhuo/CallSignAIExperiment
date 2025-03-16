using CallSignLib;
using UnityEngine;

public class PieceViewer: MonoBehaviour
{
    public Piece currentPiece;

    SpriteRenderer spriteRenderer;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        
    }

    public string GetTextureName()
    {
        var sideSym = currentPiece.side switch
        {
            Side.Blue => "C",
            Side.Red => "R",
            _ => throw new System.Exception("Invalid side")
        };

        string typeSym;
        if(currentPiece.isTanker)
        {
            typeSym = "4";
        }
        else if(currentPiece.isJammer)
        {
            typeSym = "5";
        }
        else if(currentPiece.isC2)
        {
            typeSym = "6";
        }
        else if(currentPiece.antiShipRating > currentPiece.antiAirRating)
        {
            typeSym = "3";
        }
        else
        {
            typeSym = "1";
        }

        var sym = $"PieceTexture/{sideSym}{typeSym}";
        return sym;
    }

    public void SyncTexture()
    {
        spriteRenderer.sprite = Resources.Load<Sprite>(GetTextureName());
        
    }

    void Update()
    {
        
    }

    public void SetStackOffset(int n, Vector3 worldPos)
    {
        spriteRenderer.sortingOrder = n;
        const float offset = 0.15f;
        transform.position = worldPos + new Vector3(n*offset, n*offset, 0);
    }
}