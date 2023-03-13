using Accord.Math;
using Accord.Neuro;
using Accord.Neuro.Learning;
using PredictionTool.Models;

namespace PredictionTool.Services;

public class NeuralNetwork
{
    private readonly List<Game> _historicalData;

    public NeuralNetwork(List<Game> historicalData)
    {
        _historicalData = historicalData;
    }

    public void TrainAndTest()
    {
        // Create neural network
        var neuralNetwork = new ActivationNetwork(
            new SigmoidFunction(2.0),
            4, // input layer (6 nodes)
            10, // hidden layer (10 nodes)
            1); // output layer (1 node)

        // Create the learning algorithm
        var learningAlgo = new LevenbergMarquardtLearning(neuralNetwork);

        // Normalize input data
        var input = _historicalData.Select(g => new double[]
        {
            (double)g.FullTimeHomeScore,
            (double)g.FullTimeAwayScore,
            (double)g.HalftimeHomeScore,
            (double)g.HalftimeAwayScore
        }).ToArray();

        // Normalize output data
        var output = _historicalData.Select(g => new double[]
        {
            (double)(g.FullTimeHomeScore > g.FullTimeAwayScore ? 1 : 0)
        }).ToArray();

        // Train neural network
        double error = double.MaxValue;
        int epoch = 0;
        while (error > 0.05 && epoch < 100)
        {
            error = learningAlgo.RunEpoch(input, output);
            Console.WriteLine($"Epoch: {epoch} Error: {error}");
            epoch++;
        }

        // Test neural network
        double[] testInput = { 2 , 2, 1, 1 };
        var predictedOutput = neuralNetwork.Compute(testInput);

        // Display predicted output
        Console.WriteLine("\nPredictions:");
        for (int i = 0; i < predictedOutput.Length; i++)
        {
            Console.WriteLine($"Input: {testInput[i]} Output: {predictedOutput[i]}");
        }
    }
    
    public void TrainAndTestScores()
    {
        // Create neural network
        var neuralNetwork = new ActivationNetwork(
            new SigmoidFunction(2.0),
            4, // input layer (4 nodes)
            10, // hidden layer (10 nodes)
            2); // output layer (2 nodes)

        // Create the learning algorithm
        var learningAlgo = new LevenbergMarquardtLearning(neuralNetwork);

        // Normalize input data
        var input = _historicalData.Select(g => new double[]
        {
            (double)g.FullTimeHomeScore,
            (double)g.FullTimeAwayScore,
            (double)g.HalftimeHomeScore,
            (double)g.HalftimeAwayScore
        }).ToArray();

        // Normalize output data
        var output = _historicalData.Select(g => new double[]
        {
            (double)(g.FullTimeHomeScore >= 1 ? 1 : 0), // 1 if home team scored at least one goal, 0 otherwise
            (double)(g.FullTimeHomeScore >= 2 ? 1 : 0)  // 1 if home team scored at least two goals, 0 otherwise
        }).ToArray();

        // Train neural network
        double error = double.MaxValue;
        int epoch = 0;
        while (error > 0.05 && epoch < 100)
        {
            error = learningAlgo.RunEpoch(input, output);
            Console.WriteLine($"Epoch: {epoch} Error: {error}");
            epoch++;
        }

        // Test neural network
        double[] testInput = { 1 , 1, 0,0 };
        var predictedOutput = neuralNetwork.Compute(testInput);

        // Display predicted output
        Console.WriteLine("\nPredictions:");
        Console.WriteLine($"Input: {testInput[0]}, {testInput[1]}, {testInput[2]}, {testInput[3]}");
        Console.WriteLine($"Probability of scoring at least one goal: {predictedOutput[0]}");
        Console.WriteLine($"Probability of scoring at least two goals: {predictedOutput[1]}");
    }

}