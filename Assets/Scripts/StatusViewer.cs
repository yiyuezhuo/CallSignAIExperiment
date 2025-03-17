using UnityEngine;
using UnityEngine.UIElements;
using CallSignLib;

public class StatusViewer : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        var doc = GetComponent<UIDocument>();
        var root = doc.rootVisualElement;
        root.dataSource = GameManager.Instance;

        var nextPhaseButton = root.Q<Button>("NextPhaseButton");
        nextPhaseButton.clicked += OnNextPhaseButtonClicked;

        var showCurrentActionButton = root.Q<Button>("ShowCurrentActionButton");
        showCurrentActionButton.clicked += OnWhowCurrentActionButton;

        var showCurrentStateButton = root.Q<Button>("ShowCurrentStateButton");
        showCurrentStateButton.clicked += OnShowCurrentStateButton;
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

    public void OnNextPhaseButtonClicked()
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
