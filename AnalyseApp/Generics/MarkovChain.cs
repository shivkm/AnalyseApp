namespace AnalyseApp.Generics;
using System;
using System.Collections.Generic;
using System.Linq;

public class MarkovChain<T> where T : notnull
{
    private readonly Dictionary<T, Dictionary<T, int>> _transitionMatrix = new ();
    private readonly List<T> _states;
    
    public void Train(T currentState, T nextState)
    {
        if (!_transitionMatrix.ContainsKey(currentState))
        {
            _transitionMatrix[currentState] = new Dictionary<T, int>();
        }

        if (!_transitionMatrix[currentState].ContainsKey(nextState))
        {
            _transitionMatrix[currentState][nextState] = 0;
        }

        _transitionMatrix[currentState][nextState]++;
    }

    public T Predict(T currentState)
    {
        if (!_transitionMatrix.ContainsKey(currentState))
        {
            throw new ArgumentException($"The current state '{currentState}' does not exist in the transition matrix.");
        }

        var nextStates = _transitionMatrix[currentState];
        var totalTransitions = nextStates.Values.Sum();

        var randomValue = new Random().Next(0, totalTransitions);
        var currentSum = 0;

        foreach (var nextState in nextStates)
        {
            currentSum += nextState.Value;
            if (currentSum > randomValue)
            {
                return nextState.Key;
            }
        }

        return default;
    }
}