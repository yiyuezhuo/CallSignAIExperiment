using UnityEngine;
using UnityEngine.UIElements;
using CallSignLib;
using System.Collections.Generic;

public class StatusViewer : MonoBehaviour
{
    public ListView engagementRecordListView;

    // public class EngagementRecordBinder
    // {
    //     VisualElement shooterIcon;
    //     VisualElement targetIcon;
    //     Clickable shooterManipulator;
    //     Clickable targetManipulator;

    //     public void BindItem(VisualElement element, int index)
    //     {
    //         if(element.userData != null)
    //             return;
            
    //         shooterIcon = element.Q<VisualElement>("ShooterIcon");
    //         targetIcon = element.Q<VisualElement>("TargetIcon");

    //         shooterManipulator = new Clickable(() => {
    //             Debug.Log($"Shooter clicked: {index}");
    //         });
    //         targetManipulator = new Clickable(() => {
    //             Debug.Log($"Target clicked: {index}");
    //         });

    //         shooterIcon.AddManipulator(shooterManipulator);
    //         targetIcon.AddManipulator(targetManipulator);

    //         element.userData = this;
    //     }

    //     public void UnbindItem(VisualElement element, int index)
    //     {
    //         if(element.userData != this)
    //         {
    //             shooterIcon.RemoveManipulator(shooterManipulator);
    //             targetIcon.RemoveManipulator(targetManipulator);

    //             element.userData = null;
    //         }
    //     }
    // }

    // public class EngagementRecordBinderFactory
    // {
    //     public void BindItem(VisualElement element, int index)
    //     {
    //         var binder = new EngagementRecordBinder();
    //         binder.BindItem(element, index);
    //     }
    // }

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

        var nextPhaseButton = root.Q<Button>("NextPhaseButton");
        nextPhaseButton.clicked += OnNextPhaseButtonClicked;

        var showCurrentActionButton = root.Q<Button>("ShowCurrentActionButton");
        showCurrentActionButton.clicked += OnWhowCurrentActionButton;

        var showCurrentStateButton = root.Q<Button>("ShowCurrentStateButton");
        showCurrentStateButton.clicked += OnShowCurrentStateButton;

        var editMoveButton = root.Q<Button>("EditMoveButton");
        editMoveButton.clicked += OnEditMoveButtonClicked;

        var commitButton = root.Q<Button>("CommitButton");
        commitButton.clicked += OnCommitButtonClicked;
        commitButton.dataSource = GameManager.Instance;

        engagementRecordListView = root.Q<ListView>("EngagementRecordListView");
        engagementRecordListView.SetBinding("itemsSource", new DataBinding(){dataSourcePath=new Unity.Properties.PropertyPath("gameState.engagementDeclares")});

        engagementRecordListView.itemsAdded -= EnsureSpeedRecordCreated;
        engagementRecordListView.itemsAdded += EnsureSpeedRecordCreated;

        engagementRecordListView.makeItem = () =>
        {
            var el = engagementRecordListView.itemTemplate.CloneTree();

            el.userData = -1; // current binded index

            var shooterIcon = el.Q<VisualElement>("ShooterIcon");
            var targetIcon = el.Q<VisualElement>("TargetIcon");

            var shooterManipulator = new Clickable(() => {
                Debug.Log($"Shooter clicked: {el.userData}");

                GameManager.Instance.state = GameManager.State.SelectShooter;
                GameManager.Instance.currentEditEngagementId = (int)el.userData;
            });
            var targetManipulator = new Clickable(() => {
                Debug.Log($"Target clicked: {el.userData}");

                GameManager.Instance.state = GameManager.State.SelectTarget;
                GameManager.Instance.currentEditEngagementId = (int)el.userData;
            });

            shooterIcon.AddManipulator(shooterManipulator);
            targetIcon.AddManipulator(targetManipulator);

            return el;
        };

        // var binder = new EngagementRecordBinder();

        // engagementRecordListView.bindItem += binder.BindItem;
        // engagementRecordListView.unbindItem += binder.UnbindItem;

        engagementRecordListView.bindItem = (VisualElement element, int index) =>
        {
            element.userData = index;
        };
    }


    void OnCommitButtonClicked()
    {
        Debug.Log("OnCommitButtonClicked");

        var gmr = GameManager.Instance;

        gmr.DoCommitUnit();
    }

    public void EnsureSpeedRecordCreated(IEnumerable<int> index)
    {
        foreach(var i in index)
        {
            var v = engagementRecordListView.itemsSource[i];
            if(v == null)
            {
                engagementRecordListView.itemsSource[i] = new EngagmentDeclare.EngagementRecord(){
                    shooterPieceId=-1,
                    targetPieceId=-1
                };
            }
        }
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

    void OnNextPhaseButtonClicked()
    {
        Debug.Log("OnNextPhaseButtonClicked");

        var gmr = GameManager.Instance;

        gmr.gameState.NextPhase();

        while(true)
        {
            if(gmr.gameState.currentPhase == GameState.Phase.EvadingDeclare)
            {
                gmr.gameState.NextPhase();
            }
            else if(!gmr.gameState.IsNeedAction())
            {
                gmr.gameState.NextPhase();
            }
            else if(gmr.gameState.currentSide != gmr.playingSide)
            {
                gmr.randomAgent.Run(gmr.gameState);
            }
            else
            {
                break;
            }
        }

        gmr.UpdateStackLocations();
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
