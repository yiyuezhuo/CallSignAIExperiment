
using UnityEngine;
using Unity.Sentis;
using CallSignLib;

public class NNBaseline1: AbstractAgent
{
    public Model actionClassifierModel;
    public Model c2MoveActionModel;
    public Model deployActionModel;
    public Model moveActionModel;
    public Model regenerateActionModel;
    

    public override AbstractGameAction Policy(GameState state)
    {
        return null;
    }
}

public class NNBaseline1ScriptableObject: ScriptableObject
{
    public ModelAsset actionClassifier;
    public ModelAsset c2MoveAction;
    public ModelAsset deployAction;
    public ModelAsset moveAction;
    public ModelAsset regenerateAction;

    public NNBaseline1 Make()
    {
        return new()
        {
            actionClassifierModel = ModelLoader.Load(actionClassifier),
            c2MoveActionModel = ModelLoader.Load(c2MoveAction),
            deployActionModel = ModelLoader.Load(deployAction),
            moveActionModel = ModelLoader.Load(moveAction),
            regenerateActionModel = ModelLoader.Load(regenerateAction)
        };
    }    

    
}