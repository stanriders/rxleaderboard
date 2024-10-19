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
    }
}
