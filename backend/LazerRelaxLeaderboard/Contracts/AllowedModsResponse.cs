namespace LazerRelaxLeaderboard.Contracts
{
    public class AllowedModsResponse
    {
        public required string[] Mods { get; set; }
        public required string[] ModSettings { get; set; }
    }
}
