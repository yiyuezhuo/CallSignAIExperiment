using UnityEngine;
using Unity.Sentis;

public class SentisTest : MonoBehaviour
{
    public Texture2D inputTexture;
    public ModelAsset modelAsset;

    Model runtimeModel;
    Worker worker;
    public float[] results;

    void Start()
    {
        Model sourceModel = ModelLoader.Load(modelAsset);

        FunctionalGraph graph = new FunctionalGraph();
        FunctionalTensor[] inputs = graph.AddInputs(sourceModel);
        FunctionalTensor[] outputs = Functional.Forward(sourceModel, inputs);
        FunctionalTensor softmax = Functional.Softmax(outputs[0]);

        runtimeModel = graph.Compile(softmax);

        using Tensor inputTensor = TextureConverter.ToTensor(inputTexture, width: 28, height: 28, channels: 1);

        worker = new Worker(runtimeModel, BackendType.GPUCompute); // switch to cpu?

        worker.Schedule(inputTensor);

        Tensor<float> outputTensor = worker.PeekOutput() as Tensor<float>;

        results = outputTensor.DownloadToArray();
    }

    void OnDisable()
    {
        worker.Dispose(); // Tell the GPU we're finished with memory the engine used.
    }
}