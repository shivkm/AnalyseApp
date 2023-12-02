using AnalyseApp.models;
using AnalyseApp.Models;

namespace AnalyseApp.Interfaces;

public interface IMachineLearning
{
    SoccerGameData PrepareDataBy(List<Matches> matches);
}