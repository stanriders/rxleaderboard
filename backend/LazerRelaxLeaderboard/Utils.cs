using osu.Game.Online.API;
using System.Text.Json;

namespace LazerRelaxLeaderboard
{
    public static class Utils
    {
        public static string ModToString(APIMod mod)
        {
            if (mod.Settings.ContainsKey("speed_change"))
            {
                var rateChange = mod.Settings.First(x => x.Key == "speed_change");
                var rateChangeValue = (JsonElement) rateChange.Value;

                return $"{mod.Acronym}x{rateChangeValue.GetDouble()}";
            }

            return mod.Acronym;
        }

        public static List<string[]> CreateCombinations(int startIndex, string[] pair, string[] initialArray)
        {
            var combinations = new List<string[]>();
            for (int i = startIndex; i < initialArray.Length; i++)
            {
                combinations.Add(pair.Append(initialArray[i]).ToArray());
                combinations.AddRange(CreateCombinations(i + 1, pair.Append(initialArray[i]).ToArray(), initialArray));
            }

            return combinations;
        }
    }
}
