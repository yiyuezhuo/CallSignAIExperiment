using System.Collections.Generic;
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

    public void SyncWithStack(List<AbstractViewer> viewers)
    {
        stackPieceChooser.Clear();

        foreach(var viewer in viewers)
        {
            var sprite = viewer.GetSprite();

            var item = itemTemplate.CloneTree();
            var icon = item.Q<VisualElement>("Icon");
            icon.style.backgroundImage = new(sprite);

            stackPieceChooser.Add(item);

            if(viewer is PieceViewer pieceViewer)
            {
                var piece = pieceViewer.currentPiece;

                item.AddManipulator(new Clickable(() => {
                    Debug.Log($"Item clicked: {piece.name}");

                    GameManager.Instance.OnPieceClicked(piece);
                }));
            }
            if(viewer is DamageTokenViewer damageTokenViewer)
            {
                Debug.Log($"Damage Token is clicked: {damageTokenViewer}");
            }
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