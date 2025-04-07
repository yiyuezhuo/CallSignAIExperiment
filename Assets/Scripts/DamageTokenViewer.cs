using System.Linq;
using CallSignLib;
using UnityEngine;

public abstract class AbstractViewer : MonoBehaviour
{
    protected SpriteRenderer spriteRenderer;

    protected void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public abstract Vector3 GetWorldPos();
    public abstract Sprite GetSprite();

    public void SetStackOffset(int n, Vector3 worldPos)
    {
        spriteRenderer.sortingOrder = n;
        const float offset = 0.15f;
        transform.position = worldPos + new Vector3(n*offset, n*offset, 0);
    }
}

public class DamageTokenViewer : AbstractViewer
{
    public Side side;

    public override Vector3 GetWorldPos()
    {
        (var x, var y) =  GameManager.Instance.gameState.sideData.First(s => s.side == side).carrierCenter;
        return GameManager.Instance.GameXYToWorldPos(x, y);
    }

    public override Sprite GetSprite()
    {
        return Resources.Load<Sprite>("D1");
    }
}