using osu.Game.Online.API;
using System.Text.Json;

namespace LazerRelaxLeaderboard
{
    public static class Utils
    {
        public static readonly string[] AllowedMods = new[]
        {
            "HD", "DT", "HR", "NC",
            "HT", "DC", "EZ", "FL",
            "SD", "PF", "CL", "MR",
            "TC", "BL", "SO", "NF",
            "TD", "RX"
        };

        public static readonly (string Setting, string Description)[] AllowedModSettings = new[]
        {
            ("speed_change", "Rate adjust"),
            ("adjust_pitch", "Adjust pitch"),
            ("reflection", "Mirror direction"),
            ("restart", "Restart on fail")
        };

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
            for (var i = startIndex; i < initialArray.Length; i++)
            {
                combinations.Add(pair.Append(initialArray[i]).ToArray());
                combinations.AddRange(CreateCombinations(i + 1, pair.Append(initialArray[i]).ToArray(), initialArray));
            }

            return combinations;
        }

        public static bool CheckAllowedMods(APIMod[] mods)
        {
            return mods.All(m => AllowedMods.Contains(m.Acronym));
        }

        public static bool CheckAllowedModSettings(APIMod[] mods)
        {
            return mods.All(m =>
                       m.Settings.Count == 0 || m.Settings.Keys.All(s => AllowedModSettings.Any(a => a.Setting == s)));
        }

        public static int MonthDifference(DateTime date1, DateTime date2)
        {
            return Math.Abs(((date1.Year - date2.Year) * 12) + date1.Month - date2.Month);
        }
    }
}
