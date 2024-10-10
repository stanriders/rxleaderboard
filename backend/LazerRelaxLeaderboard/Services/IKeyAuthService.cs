namespace LazerRelaxLeaderboard.Services
{
    public interface IKeyAuthService
    {
        bool Authorize(HttpContext context);
    }
}
