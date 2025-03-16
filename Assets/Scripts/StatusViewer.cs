using UnityEngine;
using UnityEngine.UIElements;

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

        gmr.UpdateStackLocations();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
