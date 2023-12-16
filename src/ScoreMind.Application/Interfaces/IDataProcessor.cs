using System.Text.RegularExpressions;
using AnalyseApp.Models;

namespace ScoreMind.Interfaces;

public interface IDataProcessor
{
    TeamPerformance CalculateTeamPerformanceBy(IEnumerable<Match> matches, string teamName);
}