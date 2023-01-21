using AnalyseApp.Extensions;
using AnalyseApp.models;

namespace AnalyseApp.Services;
internal class Node
{
    public string Decision;
    public Node Yes;
    public Node No;
}

internal class DecisionTree
{
    private Node _root;

    public DecisionTree()
    {
        _root = new Node();
    }

    public void Train(List<GameData> data, string homeTeam, string awayTeam, string league)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));
        var currentMatches = data.GetLeagueSeasonBy(2022, 2023, league)
            .Where(ii => ii.HomeTeam == homeTeam || ii.AwayTeam == awayTeam ||
                         ii.HomeTeam == awayTeam || ii.AwayTeam == homeTeam)
            .ToList();

        _root = BuildTree(currentMatches);
    }

    private Node BuildTree(IList<GameData> data)
    {
        // base case: return a leaf node if all the labels are the same
        if (data.Select(x => x.FTR).Distinct().Count() == 1)
        {
            return new Node { Decision = data[0].FTR };
        }

        // choose the best feature to split the data
        int bestFeature = ChooseBestFeature(data);

        // create a new internal node
        var node = new Node { Decision = bestFeature.ToString() };

        // split the data into two groups
        var yesData = data.Where(x => x.HomeTeam == "yes").ToList();
        var noData = data.Where(x => x.HomeTeam == "no").ToList();

        // recursively build the yes and no branches
        node.Yes = BuildTree(yesData);
        node.No = BuildTree(noData);

        return node;
    }
    private int ChooseBestFeature(IList<GameData> data)
    {
        var features = new[] { "HomeTeam", "AwayTeam", "FTHG", "FTAG", "HTHG", "HTAG" };
        var featureGains = new Dictionary<string, double>();
        var entropy = CalculateEntropy(data);
   
        //calculate the information gain for each feature
        foreach (var feature in features)
        {
            double gain = 0;
            var featureValues = data.Select(x => x.GetType().GetProperty(feature)?.GetValue(x, null)).Distinct();
            foreach (var featureValue in featureValues)
            {
                var subset = data.Where(x => x.GetType().GetProperty(feature).GetValue(x, null).Equals(featureValue));
                var subsetEntropy = CalculateEntropy(subset);
                gain += subsetEntropy * subset.Count() / data.Count();
            }
            featureGains.Add(feature, entropy - gain);
        }
        // return the feature with the highest information gain
        return features.ToList().IndexOf(featureGains.OrderByDescending(x => x.Value).First().Key);
    }

    private double CalculateEntropy(IEnumerable<GameData> data)
    {
        var outcomes = data.Select(x => x.FTR).Distinct();
        double entropy = 0;
        foreach (var outcome in outcomes)
        {
            var probability = data.Count(x => x.FTR == outcome) / (double)data.Count();
            entropy += -probability * Math.Log(probability, 2);
        }
        return entropy;
    }

    public string Predict(string[] features)
    {
        var node = _root;
        while (node is { Yes: { }, No: { } })
        {
            node = features[int.Parse(node.Decision)] != "yes" ? node.No : node.Yes;
        }
        return node.Decision;
    }
}
