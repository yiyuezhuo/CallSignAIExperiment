using System.Collections.Generic;
using Mono.Cecil.Cil;
using UnityEngine;
using UnityEngine.UIElements;
using CallSignLib;


public class StackPieceChooser: MonoBehaviour
{
    VisualElement root;
    VisualElement stackPieceChooser;
    public VisualTreeAsset itemTemplate;

    void Awake()
    {
        var doc = GetComponent<UIDocument>();
        root = doc.rootVisualElement;

        stackPieceChooser = root.Q<VisualElement>("StackPieceChooser");
        SyncWithStack(new());
    }

    void Start()
    {
        var pieceDetailPanel = root.Q<VisualElement>("PieceDetailPanel");
        pieceDetailPanel.dataSource = GameManager.Instance;
    }

    public void SyncWithStack(List<Piece> pieces)
    {
        stackPieceChooser.Clear();

        foreach(var piece in pieces)
        {
            var sprite = PieceViewer.GetSprite(piece);

            var item = itemTemplate.CloneTree();
            var icon = item.Q<VisualElement>("Icon");
            icon.style.backgroundImage = new(sprite);

            item.AddManipulator(new Clickable(() => {
                Debug.Log($"Item clicked: {piece.name}");

                GameManager.Instance.OnPieceClicked(piece);
            }));

            stackPieceChooser.Add(item);
        }
    }

    public static StackPieceChooser _instance;
    public static StackPieceChooser Instance
    {
        get 
        {
            if(_instance == null)
            {
                _instance = FindFirstObjectByType<StackPieceChooser>();
            }
            return _instance;
        }
    }
    public void OnDestroy()
    {
        _instance = null;
    }
}