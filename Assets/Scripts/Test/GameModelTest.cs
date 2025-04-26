using UnityEngine;
using Unity.Sentis;
using System;
using System.Collections.Generic;
using System.Linq;
using CallSignLib;




public class GameModelTest : MonoBehaviour
{
    Tensor<float> m_Data;

    public Bundles bundles = new();

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

        public void Setup()
        {
            foreach(var bundle in GetIWorkerExtractables())
            {
                bundle.Setup();
            }
        }

        public void Calculate(Tensor input)
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

    // public ModelAsset c2MoveActionModelAsset;
    // Worker c2MoveActionWorker;
    // // public float[] c2MoveActionDefaultResults;
    // public C2MoveActionRawResult c2MoveActionResults;

    // List<string> c2MoveActionOutputNames = new List<string>()
    // {
    //     "pieceIdC2", "pieceId1", "code1", "pieceId2", "code2"
    // };

    void Start()
    {
        m_Data = new Tensor<float>(new TensorShape(1, 51));

        // dummy input
        for(int i=0; i<51; i++)
            m_Data[0, i] = 0.0f;

        bundles.Setup();
        bundles.Calculate(m_Data);

        // actionClassifierWorker = SetupWorker(actionClassifierModelAsset);
        // actionClassifierWorker.Schedule(m_Data);
        // Tensor<float> outputTensor = actionClassifierWorker.PeekOutput(0) as Tensor<float>;
        // actionClassifierResults = outputTensor.DownloadToArray();

        // c2MoveActionWorker = SetupWorker(c2MoveActionModelAsset);
        // c2MoveActionWorker.Schedule(m_Data);

        // // c2MoveActionDefaultResults = (c2MoveActionWorker.PeekOutput() as Tensor<float>).DownloadToArray();

        // // c2MoveActionResults = c2MoveActionOutputNames.Select(
        // //     name => (c2MoveActionWorker.PeekOutput(name) as Tensor<float>).DownloadToArray().ToList()
        // // ).ToList();
        // // c2MoveActionResults = Enumerable.Range(0, 5).Select(
        // //     i => (c2MoveActionWorker.PeekOutput(i) as Tensor<float>).DownloadToArray().ToList()
        // // ).ToList();
        // c2MoveActionResults = new C2MoveActionRawResult();
        // c2MoveActionResults.Extract(c2MoveActionWorker);
    }


    void OnDisable()
    {
        Debug.Log("GameModelTest OnDisable");

        // actionClassifierWorker.Dispose(); // Tell the GPU we're finished with memory the engine used.
        bundles.Dispose();
    }
}