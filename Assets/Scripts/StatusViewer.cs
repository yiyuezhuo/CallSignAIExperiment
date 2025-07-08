using UnityEngine;
using UnityEngine.UIElements;
using CallSignLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;
using System;

public class StatusViewer : MonoBehaviour
{
    public ListView engagementRecordListView;

    public int currentTotal;

    public int currentCompleted;

    public string currentResult;

    public ReplayGenerator.SetupMode currentSetupMode;
    public DropdownField agentDropdownField;

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

        var exportCurrentStateButton = root.Q<Button>("ExportCurrentStateButton");
        exportCurrentStateButton.clicked += OnExportCurrentStateButtonClicked;

        var importStateButton = root.Q<Button>("ImportStateButton");
        importStateButton.clicked += OnImportStateButtonClicked;

        var webglDebugButton = root.Q<Button>("WebGLDebugButton");
        webglDebugButton.clicked += () =>
        {
            SceneManager.LoadScene("CanvasSampleScene");
        };

        var replayDiv = root.Q<VisualElement>("ReplayDiv");
        replayDiv.dataSource = this;

        currentTotal = 10;

        var selfPlayAndCacheButton = root.Q<Button>("SelfPlayAndCacheButton");
        selfPlayAndCacheButton.clicked += OnSelfPlayAndCacheButtonClicked;

        var selfReplayProgressBar = root.Q<ProgressBar>("SelfReplayProgressBar");
       // selfReplayProgressBar.dataSource = this;

        var exportReplayButton = root.Q<Button>("ExportReplayButton");
        exportReplayButton.clicked += () =>
        {
            Debug.Log("exportReplayButton.clicked");
            if (currentResult != null)
            {
                // UnityUtils.SaveTextFile(currentResult, "replay", "xml");
                IOManager.Instance.SaveTextFile(currentResult, "replay", "xml");
            }
        };
        // exportReplayButton.dataSource = this;

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

        engagementRecordListView.bindItem = (VisualElement element, int index) =>
        {
            element.userData = index;
        };

        agentDropdownField = root.Q<DropdownField>("AgentDropdownField");
        OnAgentsOptionsChanged(GameManager.Instance, EventArgs.Empty);
        GameManager.Instance.onAgentOptionsChanged += OnAgentsOptionsChanged;
    }

    public void OnAgentsOptionsChanged(object sender, EventArgs e)
    {
        agentDropdownField.choices = GameManager.Instance.agents.Select(a => a.GetName()).ToList();
    }

    void OnSelfPlayAndCacheButtonClicked()
    {
        Debug.Log("OnSelfPlayAndExportButtonClicked");

        currentResult = null;
        // currentTotal = 10;
        currentCompleted = 0;

        var replayGenerator = new ReplayGenerator()
        {
            agent=GameManager.Instance.currentAgent,
            total=currentTotal,
            setupMode=currentSetupMode
        };
        replayGenerator.newReplayGenerated += (sender, args) =>
        {
            (var idx, var pairSeq) = args;
            currentCompleted = idx + 1;
            Debug.Log($"{idx}/{replayGenerator.total}: len={pairSeq.Count}");
        };
        replayGenerator.newPairGenerated += (sender, args) =>
        {
            (var i, var pair) = args;
            GameManager.Instance.gameState = pair.state;
            GameManager.Instance.UpdateStackLocations();
            Debug.Log($"Tick: {i}");
        };
        replayGenerator.completed += (sender, result) =>
        {
            currentResult = replayGenerator.ToXML();
            // var textData = replayGenerator.ToXML();
            // UnityUtils.SaveTextFile(textData, "replay", "xml");
        };
        // var replays = replayGenerator.Generate();
        StartCoroutine(replayGenerator.Generate());
    }

    void OnExportCurrentStateButtonClicked()
    {
        Debug.Log("OnExportCurrentStateButtonClicked");

        // UnityUtils.SaveTextFile(GameManager.Instance.gameState.ToXML(), "gameState", "xml");
        IOManager.Instance.SaveTextFile(GameManager.Instance.gameState.ToXML(), "gameState", "xml");
    }

    void OnImportStateButtonClicked()
    {
        var gmr = IOManager.Instance;

        gmr.textLoaded -= OnTextLoaded;
        gmr.textLoaded += OnTextLoaded;

        gmr.LoadTextFile("xml");
    }

    void OnTextLoaded(object sender, string s)
    {
        Debug.Log($"OnTextLoaded: s.Length={s.Length}");
        
        GameManager.Instance.gameState = GameState.FromXML(s);
        GameManager.Instance.UpdateStackLocations();
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
                engagementRecordListView.itemsSource[i] = new EngagementDeclare.EngagementRecord(){
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
                CurrentAgentRunGameState();
            }
            else
            {
                break;
            }
        }

        gmr.UpdateStackLocations();
    }

    public void CurrentAgentRunGameState()
    {
        var gmr = GameManager.Instance;
        
        // Temp hack for debug score
        // if(gmr.currentAgent is StateScoreBasedAgent socredAgent)
        // {
        //     var action = socredAgent.Policy(gmr.gameState);
        //     var prevScore = socredAgent.EstimateState(gmr.gameState);

        //     var newState = gmr.gameState.Clone();
        //     action.Execute(newState);
        //     var newScore = socredAgent.EstimateState(newState);

        //     Debug.LogWarning($"Score: {prevScore} => {newScore} ({action})");
        // }
        
        gmr.currentAgent.Run(gmr.gameState);
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
            // gmr.currentAgent.Run(gmr.gameState);
            CurrentAgentRunGameState();
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
