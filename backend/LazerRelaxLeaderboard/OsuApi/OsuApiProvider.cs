
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using LazerRelaxLeaderboard.Config;
using LazerRelaxLeaderboard.OsuApi.Interfaces;
using LazerRelaxLeaderboard.OsuApi.Models;
using Microsoft.Extensions.Options;

namespace LazerRelaxLeaderboard.OsuApi;

public class OsuApiProvider : IOsuApiProvider
{
    private const string osu_base = "https://osu.ppy.sh/";
    private const string api_scores_link = "api/v2/beatmaps/{0}/scores?mods[]=RX&mods[]={1}&mode=osu";
    private const string api_beatmap_link = "api/v2/beatmaps/{0}";
    private const string api_token_link = "oauth/token";

    private readonly HttpClient _httpClient;
    private readonly OsuApiConfig _config;
    private readonly ILogger<OsuApiProvider> _logger;

    private TokenResponse? _userlessToken;
    private DateTime? _userlessTokenExpiration;

    public OsuApiProvider(IOptions<OsuApiConfig> config, HttpClient httpClient, ILogger<OsuApiProvider> logger)
    {
        _config = config.Value;
        _httpClient = httpClient;
        _logger = logger;

        RefreshUserlessToken().Wait();
    }

    public async Task<BeatmapScores?> GetScores(int id, string[] mods)
    {
        await RefreshUserlessToken();

        var modsString = string.Join("&mods[]=", mods);

        var requestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri(osu_base + string.Format(api_scores_link, id, modsString)),
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", _userlessToken!.AccessToken)}
        };
        requestMessage.Headers.Add("x-api-version", "99999999");

        var response = await _httpClient.SendAsync(requestMessage);

        if (response is { IsSuccessStatusCode: false, StatusCode: HttpStatusCode.NotFound }) 
            return null;

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<BeatmapScores>();
    }

    public async Task<Beatmap?> GetBeatmap(int id)
    {
        await RefreshUserlessToken();
        
        var requestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri(osu_base + string.Format(api_beatmap_link, id)),
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", _userlessToken!.AccessToken) }
        };
        requestMessage.Headers.Add("x-api-version", "99999999");

        var response = await _httpClient.SendAsync(requestMessage);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<Beatmap>();
    }

    private async Task RefreshUserlessToken()
    {
        if (_userlessTokenExpiration > DateTime.UtcNow)
        {
            return;
        }

        var requestModel = new GetUserlessTokenRequest
        {
            ClientId = _config.ClientId,
            ClientSecret = _config.ClientSecret,
        };

        var requestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri(osu_base + api_token_link),
            Content = new StringContent(JsonSerializer.Serialize(requestModel), null, "application/json"),
            Headers = { Accept = { new MediaTypeWithQualityHeaderValue("application/json") } }
        };

        var response = await _httpClient.SendAsync(requestMessage);

        if (!response.IsSuccessStatusCode)
        {
            _logger.Log(LogLevel.Error, "Couldn't update userless token! Status code: {Code}", response.StatusCode);
            return;
        }

        _userlessToken = await response.Content.ReadFromJsonAsync<TokenResponse>();
        if (_userlessToken == null)
        {
            _logger.Log(LogLevel.Error, "Couldn't parse userless token! {Json}", await response.Content.ReadAsStringAsync());
            return;
        }

        _userlessTokenExpiration = DateTime.UtcNow.AddSeconds(_userlessToken.ExpiresIn);
    }
}
