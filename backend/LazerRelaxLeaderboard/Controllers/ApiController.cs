using LazerRelaxLeaderboard.Contracts;
using LazerRelaxLeaderboard.Database;
using LazerRelaxLeaderboard.Database.Models;
using LazerRelaxLeaderboard.OsuApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using osu.Framework.Audio.Track;
using osu.Framework.Graphics.Textures;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Difficulty;
using osu.Game.Skinning;
using Beatmap = LazerRelaxLeaderboard.Database.Models.Beatmap;
using Score = LazerRelaxLeaderboard.Database.Models.Score;

namespace LazerRelaxLeaderboard.Controllers;

[ApiController]
[Route("[controller]")]
public class ApiController : ControllerBase
{
    private readonly DatabaseContext _databaseContext;

    public ApiController(DatabaseContext databaseContext)
    {
        _databaseContext = databaseContext;
    }

    [HttpGet("/scores")]
    public async Task<List<Score>> GetTopScores(int take = 50)
    {
        take = Math.Min(500, take);

        return await _databaseContext.Scores.AsNoTracking()
            .Where(x => x.Pp != null)
            .Where(x => !x.Hidden)
            .Where(x => x.IsBest)
            .Include(x => x.Beatmap)
            .Include(x => x.User)
            .OrderByDescending(x => x.Pp)
            .Take(take)
            .ToListAsync();
    }

    [HttpGet("/scores/recent")]
    public async Task<RecentScoresResponse> GetNewScores()
    {
        return new RecentScoresResponse
        {
            Scores = await _databaseContext.Scores.AsNoTracking()
                .Include(x => x.Beatmap)
                .Include(x => x.User)
                .Where(x => !x.Hidden)
                .OrderByDescending(x => x.Date)
                .Take(5)
                .ToListAsync(),
            ScoresToday = await _databaseContext.Scores.CountAsync(x => x.Date > DateTime.UtcNow.AddDays(-1))
        };
    }

    [HttpGet("/scores/{id}")]
    public async Task<Score?> GetScore(long id)
    {
        return await _databaseContext.Scores.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
    }

    [HttpGet("/players")]
    public async Task<PlayersResult> GetTopPlayers(int page = 1, string? countryCode = null, string? search = null)
    {
        const int take = 50;

        var query = _databaseContext.Users.AsNoTracking();

        if (!string.IsNullOrEmpty(search))
        {
            if (int.TryParse(search, out var id))
            {
                query = query.Where(x => x.Id == id);
            }
            else
            {
                query = query.Where(x => x.Username.ToUpper().StartsWith(search.ToUpper()));
            }
        }

        if (!string.IsNullOrEmpty(countryCode))
        {
            query = query.Where(x => x.CountryCode == countryCode);
        }

        if (page < 1)
        {
            page = 1;
        }

        var result = await query
            .Where(x => x.TotalPp != null)
            .OrderByDescending(x => x.TotalPp)
            .Skip((page - 1) * take)
            .Take(take)
            .ToListAsync();

        return new PlayersResult
        {
            Players = result,
            Total = await query.CountAsync(x => x.TotalPp != null)
        };
    }

    [HttpGet("/players/{username}")]
    public async Task<PlayersDataResponse?> GetPlayer(string username)
    {
        Database.Models.User? user;

        if (!int.TryParse(username, out var id))
        {
            user = await _databaseContext.Users.FirstOrDefaultAsync(x => x.Username.ToLower() == username.ToLower());
            if (user == null)
            {
                return null;
            }
        }
        else
        {
            user = await _databaseContext.Users.AsNoTracking()
                .Where(x => x.Id == id)
                .FirstOrDefaultAsync();
        }

        if (user != null)
        {
            int? rank = null;
            int? countryRank = null;
            if (user.TotalPp != null)
            {
                // todo: how expensive is it?
                rank = await _databaseContext.Database
                    .SqlQuery<int>(
                        $"select x.row_number as \"Value\" from (SELECT \"Id\", ROW_NUMBER() OVER(order by \"TotalPp\" desc) FROM \"Users\" where \"TotalPp\" is not null) x WHERE x.\"Id\" = {user.Id}")
                    .SingleOrDefaultAsync();

                countryRank = await _databaseContext.Database
                    .SqlQuery<int>(
                        $"select x.row_number as \"Value\" from (SELECT \"Id\", ROW_NUMBER() OVER(order by \"TotalPp\" desc) FROM \"Users\" where \"TotalPp\" is not null and \"CountryCode\" = {user.CountryCode}) x WHERE x.\"Id\" = {user.Id}")
                    .SingleOrDefaultAsync();
            }

            var countsPerMonth = await _databaseContext.Database.SqlQuery<ScoresPerMonthQuery>(
                $@"select date_trunc('month', date(""Date"")) as ""Month"", count(""Id"") as ""Count""
                       from ""Scores""
                       where ""UserId"" = {user.Id}
                       group by ""Month"" order by ""Month"";").ToArrayAsync();

            var counts = new List<PlaycountPerMonth>();

            var currentCount = 0;
            for (var i = 0; i <= Utils.MonthDifference(countsPerMonth[0].Month, DateTime.Today); i++)
            {
                if (currentCount <= 0)
                {
                    counts.Add(new PlaycountPerMonth
                        { Date = countsPerMonth[0].Month, Playcount = countsPerMonth[0].Count });
                    currentCount++;

                    continue;
                }

                if (currentCount >= countsPerMonth.Length)
                {
                    counts.Add(new PlaycountPerMonth { Date = countsPerMonth[0].Month.AddMonths(i), Playcount = 0 });

                    continue;
                }

                var diff = Utils.MonthDifference(countsPerMonth[currentCount].Month,
                    countsPerMonth[currentCount - 1].Month) - 1;
                if (diff > 0)
                {
                    for (var j = 0; j < diff; j++)
                    {
                        counts.Add(new PlaycountPerMonth
                            { Date = countsPerMonth[0].Month.AddMonths(i), Playcount = 0 });
                        i++;
                    }
                }

                counts.Add(new PlaycountPerMonth
                    { Date = countsPerMonth[currentCount].Month, Playcount = countsPerMonth[currentCount].Count });
                currentCount++;
            }

            // todo: make sure this isn't too slow
            return new PlayersDataResponse
            {
                Id = user.Id,
                Rank = rank,
                CountryRank = countryRank,
                CountryCode = user.CountryCode,
                TotalAccuracy = user.TotalAccuracy,
                Username = user.Username,
                UpdatedAt = user.UpdatedAt,
                TotalPp = user.TotalPp,
                Playcount = await _databaseContext.Scores.Where(x => !x.Hidden).CountAsync(x => x.UserId == user.Id),
                CountSS = await _databaseContext.Scores.Where(x => !x.Hidden).Where(x => x.UserId == user.Id).CountAsync(x => x.Grade == Grade.X || x.Grade == Grade.XH),
                CountS = await _databaseContext.Scores.Where(x => !x.Hidden).Where(x => x.UserId == user.Id).CountAsync(x => x.Grade == Grade.S || x.Grade == Grade.SH),
                CountA = await _databaseContext.Scores.Where(x => !x.Hidden).Where(x => x.UserId == user.Id).CountAsync(x => x.Grade == Grade.A),
                PlaycountsPerMonth = counts.ToArray()
            };
        }

        return null;
    }

    [HttpGet("/players/{username}/scores")]
    public async Task<List<Score>> GetPlayerScores(string username)
    {
        if (!int.TryParse(username, out var id))
        {
            var user = await _databaseContext.Users.FirstOrDefaultAsync(x => x.Username.ToLower() == username.ToLower());

            if (user == null)
            {
                return [];
            }

            id = user.Id;
        }

        var fullTopHundred = await _databaseContext.Scores.AsNoTracking()
            .Where(x => x.UserId == id)
            .Where(x => !x.Hidden)
            .Include(x => x.Beatmap)
            .OrderByDescending(x => x.Pp ?? double.MinValue)
            .ThenByDescending(x => x.TotalScore)
            .Take(100)
            .ToListAsync();

        if (fullTopHundred.Count < 100)
        {
            // don't bother adding more scores if we're already below 100 threshold
            return fullTopHundred;
        }

        var nonBests = fullTopHundred.Count(x => !x.IsBest);

        var additionalBestScores = await _databaseContext.Scores.AsNoTracking()
            .Where(x => x.UserId == id)
            .Where(x => x.IsBest)
            .Where(x => !x.Hidden)
            .Include(x => x.Beatmap)
            .OrderByDescending(x => x.Pp ?? double.MinValue)
            .ThenByDescending(x => x.TotalScore)
            .Skip(fullTopHundred.Count - nonBests)
            .Take(nonBests)
            .ToListAsync();

        return fullTopHundred.Concat(additionalBestScores)
            .OrderByDescending(x => x.Pp)
            .ThenByDescending(x => x.TotalScore).ToList();
    }

    [HttpGet("/players/{username}/scores/recent")]
    public async Task<List<Score>> GetRecentPlayerScores(string username)
    {
        if (!int.TryParse(username, out var id))
        {
            var user = await _databaseContext.Users.FirstOrDefaultAsync(x => x.Username.ToLower() == username.ToLower());

            if (user == null)
            {
                return [];
            }

            id = user.Id;
        }

        return await _databaseContext.Scores.AsNoTracking()
            .Where(x => x.UserId == id)
            .Where(x => !x.Hidden)
            .Include(x => x.Beatmap)
            .OrderByDescending(x => x.Date)
            .Where(x => x.Date > DateTime.UtcNow.AddDays(-14))
            .Take(10)
            .ToListAsync();
    }

    [HttpGet("/beatmaps")]
    public async Task<BeatmapsResponse> GetBeatmaps(int page = 1, string? search = null)
    {
        const int take = 30;

        var query = _databaseContext.Beatmaps.Where(x => x.Scores.Count > 0).AsNoTracking();

        if (!string.IsNullOrEmpty(search))
        {
            if (int.TryParse(search, out var id))
            {
                query = query.Where(x => x.Id == id);
            }
            else
            {
                // todo: definitely inefficient!!
                query = query.Where(x => x.Artist.ToUpper().StartsWith(search.ToUpper()) ||
                                         x.Title.ToUpper().StartsWith(search.ToUpper()) ||
                                         x.DifficultyName.ToUpper().StartsWith(search.ToUpper()));
            }
        }

        if (page < 1)
        {
            page = 1;
        }

        var result = await query
            .OrderByDescending(x => x.Scores.Count)
            .Skip((page - 1) * take)
            .Take(take)
            .Select(x => new ListingBeatmap
            {
                Id = x.Id,
                Artist = x.Artist,
                BeatmapSetId = x.BeatmapSetId,
                CreatorId = x.CreatorId,
                DifficultyName = x.DifficultyName,
                StarRating = x.StarRating,
                Status = x.Status,
                Title = x.Title,
                Playcount = x.Scores.Count
            })
            .ToListAsync();

        return new BeatmapsResponse
        {
            Beatmaps = result,
            Total = await query.CountAsync()
        };
    }

    [HttpGet("/beatmaps/{id}")]
    public async Task<Beatmap?> GetBeatmap(int id)
    {
        return await _databaseContext.Beatmaps.AsNoTracking()
            .Where(x => x.Id == id)
            .FirstOrDefaultAsync();
    }

    [HttpGet("/beatmaps/{id}/scores")]
    public async Task<List<Score>> GetBeatmapScores(int id)
    {
        var fullTopHundred = await _databaseContext.Scores.AsNoTracking()
            .Where(x => x.BeatmapId == id)
            .Where(x => !x.Hidden)
            .Include(x => x.User)
            .OrderByDescending(x => x.Pp)
            .ThenByDescending(x => x.TotalScore)
            .Take(100)
            .ToListAsync();

        if (fullTopHundred.Count < 100)
        {
            // don't bother adding more scores if we're already below 100 threshold
            return fullTopHundred;
        }

        var nonBests = fullTopHundred.Count(x => !x.IsBest);

        var additionalBestScores = await _databaseContext.Scores.AsNoTracking()
            .Where(x => x.BeatmapId == id)
            .Where(x => x.IsBest)
            .Where(x => !x.Hidden)
            .Include(x => x.User)
            .OrderByDescending(x => x.Pp)
            .ThenByDescending(x => x.TotalScore)
            .Skip(fullTopHundred.Count - nonBests)
            .Take(nonBests)
            .ToListAsync();

        return fullTopHundred.Concat(additionalBestScores)
            .OrderByDescending(x => x.Pp)
            .ThenByDescending(x => x.TotalScore).ToList();
    }

    [HttpGet("/stats")]
    public async Task<StatsResponse> GetStats()
    {
        var countsPerDayQuery = await _databaseContext.Database.SqlQuery<ScoresPerMonthQuery>(
            $@"select date_trunc('day', date(""Date"")) as ""Month"", count(""Id"") as ""Count""
                       from ""Scores""
                       where ""Date"" > {DateTime.UtcNow.AddMonths(-1)}
                       group by ""Month"" order by ""Month"";").ToArrayAsync();

        var countsPerDay = new List<PlaycountPerMonth>();

        for (var i = 0; i < countsPerDayQuery.Length; i++)
        {
            if (i > 0)
            {
                var dayDifference = (countsPerDayQuery[i].Month - countsPerDay.Last().Date).Days;
                if (dayDifference > 1)
                {
                    for (var j = 0; j < dayDifference - 1; j++)
                    {
                        countsPerDay.Add(new PlaycountPerMonth
                            { Date = countsPerDay.Last().Date.AddDays(1), Playcount = 0 });
                    }
                }
            }

            countsPerDay.Add(new PlaycountPerMonth
                { Date = countsPerDayQuery[i].Month, Playcount = countsPerDayQuery[i].Count });
        }

        var countsPerMonth = await _databaseContext.Database.SqlQuery<ScoresPerMonthQuery>(
            $@"select date_trunc('month', date(""Date"")) as ""Month"", count(""Id"") as ""Count""
                       from ""Scores""
                       group by ""Month"" order by ""Month"";").ToArrayAsync();

        var counts = new List<PlaycountPerMonth>();

        var currentCount = 0;
        for (var i = 0; i <= Utils.MonthDifference(countsPerMonth[0].Month, DateTime.Today); i++)
        {
            if (currentCount <= 0)
            {
                counts.Add(new PlaycountPerMonth
                    { Date = countsPerMonth[0].Month, Playcount = countsPerMonth[0].Count });
                currentCount++;

                continue;
            }

            if (currentCount >= countsPerMonth.Length)
            {
                counts.Add(new PlaycountPerMonth { Date = countsPerMonth[0].Month.AddMonths(i), Playcount = 0 });
                continue;
            }

            var diff =
                Utils.MonthDifference(countsPerMonth[currentCount].Month, countsPerMonth[currentCount - 1].Month) - 1;
            if (diff > 0)
            {
                for (var j = 0; j < diff; j++)
                {
                    counts.Add(new PlaycountPerMonth { Date = countsPerMonth[0].Month.AddMonths(i), Playcount = 0 });
                    i++;
                }
            }

            counts.Add(new PlaycountPerMonth
                { Date = countsPerMonth[currentCount].Month, Playcount = countsPerMonth[currentCount].Count });
            currentCount++;
        }

        return new StatsResponse
        {
            BeatmapsTotal = await _databaseContext.Beatmaps.CountAsync(),
            ScoresTotal = await _databaseContext.Scores.CountAsync(),
            UsersTotal = await _databaseContext.Users.CountAsync(),
            LatestScoreId = await _databaseContext.Scores.Select(x => x.Id).OrderByDescending(x => x).FirstAsync(),
            ScoresInAMonth = await _databaseContext.Scores.CountAsync(x => x.Date > DateTime.UtcNow.AddMonths(-1)),
            PlaycountPerDay = countsPerDay,
            PlaycountPerMonth = counts
        };
    }

    [HttpGet("/allowed-mods")]
    public AllowedModsResponse GetAllowedMods()
    {
        return new AllowedModsResponse
        {
            Mods = Utils.AllowedMods,
            ModSettings = Utils.AllowedModSettings.Select(x => x.Description).ToArray()
        };
    }

    [HttpGet("/countries")]
    public async Task<string[]> GetCountries()
    {
        return await _databaseContext.Users.AsNoTracking().Select(x => x.CountryCode).Distinct().OrderBy(x => x).ToArrayAsync();
    }

    [HttpGet("/pp-version")]
    public string GetPpVersion()
    {
        return new OsuDifficultyCalculator(new OsuRuleset().RulesetInfo, new FakeWorkingBeatmap()).Version.ToString();
    }

    public class FakeWorkingBeatmap : WorkingBeatmap
    {
        public FakeWorkingBeatmap() : base(new BeatmapInfo(), null) { }
        protected override IBeatmap GetBeatmap() => throw new NotImplementedException();
        public override Texture GetBackground() => throw new NotImplementedException();
        public override Stream GetStream(string storagePath) => throw new NotImplementedException();
        protected override Track GetBeatmapTrack() => throw new NotImplementedException();
        protected override ISkin GetSkin() => throw new NotImplementedException();
    }
}
