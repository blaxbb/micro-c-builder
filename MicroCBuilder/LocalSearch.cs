using MicroCLib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FuzzySharp.SimilarityRatio;
using FuzzySharp.SimilarityRatio.Scorer.Composite;
using FuzzySharp;
using FuzzySharp.SimilarityRatio.Scorer.StrategySensitive;

namespace MicroCBuilder
{
    public static class LocalSearch
    {
        static Dictionary<string, int> IdFilterScores = new Dictionary<string, int>();

        public static void Init()
        {

        }

        public static void ReplaceItems(List<Item> items)
        {
            IdFilterScores.Clear();
        }

        public static IEnumerable<Item> Search(string query, List<Item> items)
        {
            var split = query.Split(' ');
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            var ret = items.Select(item =>
            {
                if(item.SKU == query)
                {
                    return (item, score: 0);
                }
                var brandScore = Process.ExtractOne(item.Brand, split, scorer: ScorerCache.Get<WeightedRatioScorer>()).Score;
                var nameScore = Fuzz.WeightedRatio(item.Name, query, FuzzySharp.PreProcess.PreprocessMode.Full);

                var importantSpecs = ImportantSpecs(item);
                int maxSpecScore = 0;
                foreach (var spec in importantSpecs)
                {
                    if (item.Specs.ContainsKey(spec))
                    {
                        //var specScore = Fuzz.Ratio(i.Specs[spec], FilterText, FuzzySharp.PreProcess.PreprocessMode.Full);
                        var specScore = Process.ExtractOne(item.Specs[spec], split, scorer: ScorerCache.Get<DefaultRatioScorer>()).Score;
                        if (specScore > maxSpecScore)
                        {
                            maxSpecScore = specScore;
                        }
                    }
                }

                //customize weights of scoring if necessary
                var maxScore = new int[] {
                        (int)(brandScore   * 1),
                        (int)(nameScore    * 1),
                        (int)(maxSpecScore * 1)
                    }.Sum();

                //for debugging purposes
                //item.Stock = maxScore.ToString();

                if (IdFilterScores.ContainsKey(item.ID))
                {
                    IdFilterScores[item.ID] = maxScore;
                }
                else
                {
                    IdFilterScores.Add(item.ID, maxScore);
                }
                return (item, score: maxScore);
            }).Where(result => result.score > 50).OrderByDescending(result => result.score).Select(result => result.item);
            sw.Stop();
            //System.Diagnostics.Debug.WriteLine($"ELAPSED {sw.Elapsed.TotalMilliseconds} ms");
            return ret;
        }

        static IEnumerable<string> ImportantSpecs(Item i)
        {
            switch (i.ComponentType)
            {
                case BuildComponent.ComponentType.CPU:
                    yield return "Processor";
                    break;
                case BuildComponent.ComponentType.Motherboard:
                    yield return "Socket Type";
                    yield return "North Bridge";
                    break;
                case BuildComponent.ComponentType.Case:
                    yield return "Max Motherboard Size";
                    break;
                case BuildComponent.ComponentType.PowerSupply:
                    yield return "Wattage";
                    break;
                case BuildComponent.ComponentType.GPU:
                    yield return "GPU Chipset";
                    break;
            }
        }
    }
}
