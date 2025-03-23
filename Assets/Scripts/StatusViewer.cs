using UnityEngine;
using UnityEngine.UIElements;
using CallSignLib;
using Unity.VisualScripting;

public class StatusViewer : MonoBehaviour
{
    public ListView engagementRecordListView;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        var doc = GetComponent<UIDocument>();
        var root = doc.rootVisualElement;
        root.dataSource = GameManager.Instance;

        var aiRunAndNextPhaseButton = root.Q<Button>("AIRunAndNextPhaseButton");
        aiRunAndNextPhaseButton.clicked += OnAIRunAndNextPhaseButtonClicked;

        var endEditAndNextPhaseButton = root.Q<Button>("EndEditAndNextPhaseButton");
        endEditAndNextPhaseButton.clicked += OnEndEditAndNextPhaseButton;

        var showCurrentActionButton = root.Q<Button>("ShowCurrentActionButton");
        showCurrentActionButton.clicked += OnWhowCurrentActionButton;

        var showCurrentStateButton = root.Q<Button>("ShowCurrentStateButton");
        showCurrentStateButton.clicked += OnShowCurrentStateButton;

        var editMoveButton = root.Q<Button>("EditMoveButton");
        editMoveButton.clicked += OnEditMoveButtonClicked;

        engagementRecordListView = root.Q<ListView>("EngagementRecordListView");
        engagementRecordListView.SetBinding("itemsSource", new DataBinding(){dataSourcePath=new Unity.Properties.PropertyPath("gameState.engagementDeclares")});
    }

    public void OnEditMoveButtonClicked()
    {
        Debug.Log("OnEditMoveButtonClicked");

        var gmr = GameManager.Instance;
        if(gmr.state == GameManager.State.Idle)
        {
            gmr.state = GameManager.State.EditMoveBegin;
        }
    }

    public void OnShowCurrentStateButton()
    {
        Debug.Log("ShowCurrentStateButton");

        // Debug.Log(GameManager.Instance.gameState.ToString());
        Debug.Log(GameManager.Instance.gameState.ToXML());
    }

    public void OnWhowCurrentActionButton()
    {
        Debug.Log("OnWhowCurrentActionButton");

        var gameState = GameManager.Instance.gameState;
        if(gameState.IsNeedAction())
        {
            var actions = gameState.GetActions();
            Debug.Log(string.Join(",", actions));
        }
    }

    public void OnEndEditAndNextPhaseButton()
    {
        Debug.Log("OnEndEditAndNextPhaseButton");

        var gmr = GameManager.Instance;

        gmr.gameState.NextPhase();

        // skip evading phase
        while(gmr.gameState.currentPhase == GameState.Phase.EvadingDeclare)
        {
            gmr.gameState.NextPhase();
        }

        gmr.UpdateStackLocations();
    }

    public void OnAIRunAndNextPhaseButtonClicked()
    {
        Debug.Log("OnNextPhaseButtonClicked");

        var gmr = GameManager.Instance;

        if(gmr.gameState.IsNeedAction())
        {
            gmr.randomAgent.Run(gmr.gameState);
        }
        else
        {
            gmr.gameState.NextPhase();
        }

        // skip evading phase
        while(gmr.gameState.currentPhase == GameState.Phase.EvadingDeclare)
        {
            gmr.gameState.NextPhase();
        }

        gmr.UpdateStackLocations();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
