using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/NNBaseline1AgentScriptableObject", order = 1)]
public class NNBaseline1AgentScriptableObject: ScriptableObject
{
    public NNBaseline1Agent agent = new(); // Used to specify ModelAsset
}