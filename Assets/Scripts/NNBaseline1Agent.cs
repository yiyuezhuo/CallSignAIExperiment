
using UnityEngine;
using Unity.Sentis;
using System;
using System.Collections.Generic;
using System.Linq;
using CallSignLib;
using Unity.VisualScripting;

public static class SentisUtils
{
    public static List<(int, int)> evenOffset = new(){(0,-1), (1,-1), (1,0), (0,1), (-1,0), (-1,-1)};
    public static List<(int, int)> oddOffset = new(){(0,-1), (1,0), (1,1), (0,1), (-1,1), (-1,0)};

    public static (int, int) DecodeToXY(int code, int currentX, int currentY)
    {
        var isEven = currentX % 2 == 0;
        var offsets = isEven ? evenOffset : oddOffset;
        (var dx, var dy) = offsets[code];
        return (currentX + dx, currentY + dy);
    }

    public static float[] GetFloatArray(Worker worker, int i)
    {
        // return (worker.PeekOutput(i) as Tensor<float>).DownloadToArray();
        // return (worker.PeekOutput(i) as Tensor<float>).ReadbackAndClone().DownloadToArray();
        return (worker.PeekOutput(i) as Tensor<float>).DownloadToArray();
    }

    public static float[] Softmax(float[] input)
    {
        // Prevent numerical overflow by subtracting the maximum value
        float max = input.Max();
        
        // Calculate exponential values and their sum
        float sum = 0f;
        float[] expValues = new float[input.Length];
        
        for (int i = 0; i < input.Length; i++)
        {
            expValues[i] = (float)Math.Exp(input[i] - max);
            sum += expValues[i];
        }
        
        // Normalize by dividing each value by the sum
        float[] output = new float[input.Length];
        for (int i = 0; i < output.Length; i++)
        {
            output[i] = expValues[i] / sum;
        }
        
        return output;
    }

    public static int ArgMax(this float[] array)
    {
        if (array == null || array.Length == 0)
            return -1;

        int maxIndex = 0;
        float maxValue = array[0];

        for (int i = 1; i < array.Length; i++)
        {
            if (array[i] > maxValue)
            {
                maxValue = array[i];
                maxIndex = i;
            }
        }

        return maxIndex;
    }
}

public interface IWorkerExtractable
{
    void Extract(Worker worker);
}

public interface IToActionable
{
    AbstractGameAction ToAction(GameState state);
}

[Serializable]
public class C2MoveActionRawResult: IWorkerExtractable, IToActionable
{
    public float[] pieceIdC2Logit;
    public float[] pieceId1Logit;
    public float[] code1Logit;
    public float[] pieceId2Logit;
    public float[] code2Logit;

    public override string ToString()
    {
        return $"C2MoveActionRawResult(pieceIdC2Logit={Utils.ToStr(pieceIdC2Logit)}, pieceId1Logit={Utils.ToStr(pieceId1Logit)}, code1Logit={Utils.ToStr(code1Logit)}, pieceId2Logit={Utils.ToStr(pieceId2Logit)}, code2Logit={Utils.ToStr(code2Logit)})";
    }

    public void Extract(Worker worker)
    {
        pieceIdC2Logit = SentisUtils.GetFloatArray(worker, 0);
        pieceId1Logit = SentisUtils.GetFloatArray(worker, 1);
        code1Logit = SentisUtils.GetFloatArray(worker, 2);
        pieceId2Logit = SentisUtils.GetFloatArray(worker, 3);
        code2Logit = SentisUtils.GetFloatArray(worker, 4);
    }

    public C2MoveAction ToAction(GameState state)
    {
        var pieceIdC2 = pieceIdC2Logit.ArgMax();
        var pieceId1 = pieceId1Logit.ArgMax();
        var code1 = code1Logit.ArgMax();
        var pieceId2 = pieceId2Logit.ArgMax();
        var code2 = code2Logit.ArgMax();

        var piece1 = state.pieces[pieceId1];
        var piece2 = state.pieces[pieceId2];
        (var toX1, var toY1) = SentisUtils.DecodeToXY(code1, piece1.x, piece1.y);
        (var toX2, var toY2) = SentisUtils.DecodeToXY(code2, piece2.x, piece2.y);

        return new()
        {
            pieceidC2=pieceIdC2,
            pieceId1=pieceId1,
            toX1=toX1,
            toY1=toY1,
            pieceId2=pieceId2,
            toX2=toX2,
            toY2=toY2
        };
    }

    AbstractGameAction IToActionable.ToAction(GameState state) => ToAction(state);
}

[Serializable]
public abstract class MLP2HeadRawResult
{
    public float[] pieceIdLogit; 
    public float[] codeLogit;

    public override string ToString()
    {
        return $"{GetType().Name}(pieceIdLogit={Utils.ToStr(pieceIdLogit)}, codeLogit={Utils.ToStr(codeLogit)})";
    }

    public void Extract(Worker worker)
    {
        pieceIdLogit = SentisUtils.GetFloatArray(worker, 0);
        codeLogit = SentisUtils.GetFloatArray(worker, 1);
    }

    public (int, int, int) ToActionRelativeToPiece(GameState state)
    {
        var pieceId = pieceIdLogit.ArgMax();
        var code = codeLogit.ArgMax();

        var piece = state.pieces[pieceId];
        (var toX, var toY) = SentisUtils.DecodeToXY(code, piece.x, piece.y);

        return (pieceId, toX, toY);
    }

    public (int, int, int) ToActionRelativeToCarrier(GameState state)
    {
        var pieceId = pieceIdLogit.ArgMax();
        var code = codeLogit.ArgMax();

        var piece = state.pieces[pieceId];
        var sideData = state.sideData.First(s => s.side == piece.side);
        (var toX, var toY) = SentisUtils.DecodeToXY(code, sideData.carrierCenter.Item1, sideData.carrierCenter.Item2);

        return (pieceId, toX, toY);
    }

    // public abstract AbstractGameAction ToAction(GameState state);
}

[Serializable]
public class DeployActionRawResult: MLP2HeadRawResult, IWorkerExtractable, IToActionable
{
    public DeployAction ToAction(GameState state)
    {
        (var pieceId, var toX, var toY) = ToActionRelativeToCarrier(state);
        return new()
        {
            pieceId=pieceId,
            toX=toX,
            toY=toY
        };
    }

    AbstractGameAction IToActionable.ToAction(GameState state) => ToAction(state);
}

[Serializable]
public class RegenerateActionRawResult: MLP2HeadRawResult, IWorkerExtractable, IToActionable
{
    public RegenerateAction ToAction(GameState state)
    {
        (var pieceId, var toX, var toY) = ToActionRelativeToCarrier(state);
        return new()
        {
            pieceId=pieceId,
            toX=toX,
            toY=toY
        };
    }

    AbstractGameAction IToActionable.ToAction(GameState state) => ToAction(state);
}

[Serializable]
public class MoveActionRawResult: MLP2HeadRawResult, IWorkerExtractable, IToActionable
{
    public MoveAction ToAction(GameState state)
    {
        (var pieceId, var toX, var toY) = ToActionRelativeToPiece(state);
        return new()
        {
            pieceId=pieceId,
            toX=toX,
            toY=toY
        };
    }

    AbstractGameAction IToActionable.ToAction(GameState state) => ToAction(state);
}

[Serializable]
public class ActionTypeRawResult: IWorkerExtractable
{
    public float[] actionTypeLogit;
    public override string ToString()
    {
        return $"ActionTypeRawResult(actionTypeLogit={Utils.ToStr(actionTypeLogit)})";
    }

    public void Extract(Worker worker)
    {
        actionTypeLogit = SentisUtils.GetFloatArray(worker, 0);
    }
}

[Serializable]
public class NullActionRawResult: IWorkerExtractable, IToActionable
{
    public void Extract(Worker worker)
    {
    }

    AbstractGameAction IToActionable.ToAction(GameState state) => new NullAction();
}

[Serializable]
public class NNBaseline1Agent: AbstractAgent
{
    public Bundles bundles = new();

    public AbstractAgent fallbackAgent = new BaselineAgent4(); // Used in Engagement Phase

    // public ModelAsset actionClassifierModelAsset;
    // Worker actionClassifierWorker;
    // public float[] actionClassifierResults;

    public interface IBundle
    {
        void Setup();
        void Calculate(Tensor input);
        void Dispose();
    }

    [Serializable]
    public class Bundle<T>: IBundle where T: IWorkerExtractable
    {
        public ModelAsset modelAsset;
        Worker worker;
        public T rawResult;

        public void Setup()
        {
            Model sourceModel = ModelLoader.Load(modelAsset);

            FunctionalGraph graph = new FunctionalGraph();
            FunctionalTensor[] inputs = graph.AddInputs(sourceModel);
            FunctionalTensor[] outputs = Functional.Forward(sourceModel, inputs);

            var runtimeModel = graph.Compile(outputs);

            // FunctionalTensor softmax = Functional.Softmax(outputs[0]);

            // var runtimeModel = graph.Compile(softmax);

            worker = new Worker(runtimeModel, BackendType.GPUCompute); // switch to cpu?
            // worker = new Worker(runtimeModel, BackendType.CPU);
        }

        public void Calculate(Tensor input)
        {
            worker.Schedule(input);
            rawResult.Extract(worker);
        }

        public void Dispose()
        {
            worker.Dispose();
        }
    }

    [Serializable]
    public class Bundles
    {
        public Tensor<float> input;

        public Bundle<ActionTypeRawResult> actionType = new();
        public Bundle<C2MoveActionRawResult> c2MoveAction = new();
        public Bundle<DeployActionRawResult> deployAction = new();
        public Bundle<RegenerateActionRawResult> regenerateAction = new();
        public Bundle<MoveActionRawResult> moveAction = new();

        public List<IBundle> GetIWorkerExtractables()
        {
            return new List<IBundle>()
            {
                actionType,
                c2MoveAction,
                deployAction,
                regenerateAction,
                moveAction
            };
        }

        public void SetInput(float[] _input)
        {
            input?.Dispose();

            input = new Tensor<float>(new TensorShape(1, _input.Length));

            for(var i=0; i<_input.Length; i++)
                input[0, i] = _input[i];
            // input.Upload(_input); // will freeze for some reason
        }

        public void Setup()
        {
            input = new Tensor<float>(new TensorShape(1, 51));

            foreach(var bundle in GetIWorkerExtractables())
            {
                bundle.Setup();
            }
        }

        public void Calculate()
        {
            foreach(var bundle in GetIWorkerExtractables())
            {
                bundle.Calculate(input);
            }
        }
        
        public void Dispose()
        {
            foreach(var bundle in GetIWorkerExtractables())
            {
                bundle.Dispose();
            }
        }
    }

    public void Setup()
    {
        bundles.Setup();
    }

    public float[] EncodePiece(Piece piece)
    {
        return new float[]
        {
            piece.mapState == MapState.NotDeployed ? 1f : 0,
            piece.mapState == MapState.Destroyed ? 1f : 0,
            piece.x / 5,
            piece.y / 4
        };
    }

    public float[] EncodeToNNInput(GameState state)
    {
        var pieceStates = state.pieces.Select(p => EncodePiece(p));
        var misc = new float[]
        {
            state.currentSide == Side.Blue ? 1f : 0,
            state.sideData[0].carrierDamage,
            state.sideData[1].carrierDamage
        };
        List<float> ret = new List<float>();
        foreach(var pieceState in pieceStates)
        {
            ret.AddRange(pieceState);
        }
        ret.AddRange(misc);
        return ret.ToArray();
    }

    // def extract_type_index(action: AbstractAction):
    //     idx = 0
    //     match action:
    //         case DeployAction():
    //             idx = 0
    //         case EngagementDeclare():
    //             idx = 1
    //         case C2MoveAction():
    //             idx = 2
    //         case RegenerateAction():
    //             idx = 3
    //         case MoveAction():
    //             idx = 4
    //         case NullAction():
    //             idx = 5
    //      return idx
    

    public override AbstractGameAction Policy(GameState state)
    {
        if(state.currentPhase !=  GameState.Phase.Action)
            return fallbackAgent.Policy(state);

        var stateEncoded = EncodeToNNInput(state);
        bundles.SetInput(stateEncoded);
        bundles.Calculate();
        var actionTypeLogit = bundles.actionType.rawResult.actionTypeLogit;

        Debug.Log(bundles.actionType.rawResult);

        var actionTypeIdx = actionTypeLogit.ArgMax();
        // AbstractGameAction ret = actionTypeIdx switch
        // {
        //     0 => bundles.deployAction.rawResult.ToAction(state),
        //     1 => null, // Engagement phase is not modeled in NNBaseline1
        //     2 => bundles.c2MoveAction.rawResult.ToAction(state),
        //     3 => bundles.regenerateAction.rawResult.ToAction(state),
        //     4 => bundles.moveAction.rawResult.ToAction(state),
        //     5 => new NullAction(),
        //     _ => null
        // };
        IToActionable rawResult = actionTypeIdx switch
        {
            0 => bundles.deployAction.rawResult,
            1 => null, // Engagement phase is not modeled in NNBaseline1
            2 => bundles.c2MoveAction.rawResult,
            3 => bundles.regenerateAction.rawResult,
            4 => bundles.moveAction.rawResult,
            5 => new NullActionRawResult(),
            _ => null
        };

        Debug.Log($"rawResult={rawResult}");
        var actionProposed = rawResult.ToAction(state);
        var retAction = actionProposed;

        if(!actionProposed.IsValid(state))
        {
            Debug.LogWarning($"Invalid action proposed: fallback to null action: {actionProposed}");
            retAction = new NullAction();
        }

        return retAction;
    }
}