namespace LazerRelaxLeaderboard.Services
{
    // why does asp net STILL not have an api key middleware/policy/service/WHATEVER like really
    public class KeyAuthService : IKeyAuthService
    {
        private readonly IConfiguration _configuration;

        public KeyAuthService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public bool Authorize(HttpContext context)
        {
            if (context.Request.Headers.ContainsKey("X-Auth-Key"))
            {
                var key = _configuration["Key"]!;
                if (key == context.Request.Headers["X-Auth-Key"]!.ToString())
                {
                    return true;
                }
            }

            return false;
        }
    }
}
