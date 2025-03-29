using CallSignLib;
using Unity.VisualScripting;
using UnityEngine;

public class PieceViewer: AbstractViewer
{
    public Piece currentPiece;

    void Start()
    {
        
    }

    public static string GetTextureName(Piece currentPiece)
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

    // public static Sprite GetSprite(Piece currentPiece)
    // {
    //     return Resources.Load<Sprite>(GetTextureName(currentPiece));
    // }

    public override Sprite GetSprite() => Resources.Load<Sprite>(GetTextureName(currentPiece));

    public void SyncTexture()
    {
        spriteRenderer.sprite = GetSprite();
    }

    public override Vector3 GetWorldPos()
    {
        return GameManager.Instance.GameXYToWorldPos(currentPiece.x, currentPiece.y);
    }

    void Update()
    {
        
    }

}