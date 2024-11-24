using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Discord.WebSocket;
using Discord;
using LazerRelaxLeaderboard.BackgroundServices;
using LazerRelaxLeaderboard.Config;
using LazerRelaxLeaderboard.Database;
using LazerRelaxLeaderboard.OsuApi;
using LazerRelaxLeaderboard.OsuApi.Interfaces;
using LazerRelaxLeaderboard.Services;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Npgsql;
using Serilog;
using Serilog.Settings.Configuration;
using SerilogTracing;
using UAParser;
using DiscordConfig = LazerRelaxLeaderboard.Config.DiscordConfig;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration, new ConfigurationReaderOptions() { SectionName = "Logging" })
    .ReadFrom.Services(services));

using var tracer = new ActivityListenerConfiguration()
    .Instrument.AspNetCoreRequests()
    .TraceToSharedLogger();

// Add services to the container.

var dbConfig = builder.Configuration.GetSection("Database");
var osuConfig = builder.Configuration.GetSection("osuApi");
var basePath = builder.Configuration.GetValue<string>("PathBase");

builder.Services.Configure<OsuApiConfig>(osuConfig);

var connectionString = new NpgsqlConnectionStringBuilder
{
    Host = dbConfig["Host"],
    Port = int.Parse(dbConfig["Port"]!),
    Database = dbConfig["Database"],
    Username = dbConfig["Username"],
    Password = dbConfig["Password"]
};

builder.Services.AddDbContext<DatabaseContext>(options =>
    options.UseNpgsql(connectionString.ConnectionString));

builder.Services.AddHttpContextAccessor();

builder.Services.AddOptions<DiscordConfig>()
    .BindConfiguration(nameof(DiscordConfig))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddHostedService<DiscordBackgroundService>();

var config = new DiscordSocketConfig
{
    GatewayIntents = GatewayIntents.GuildMessages | GatewayIntents.GuildEmojis | GatewayIntents.Guilds
};

builder.Services.AddSingleton(config);
builder.Services.AddSingleton<DiscordSocketClient>();
builder.Services.AddTransient<IDiscordService, DiscordService>();

builder.Services.AddHttpClient<OsuApiProvider>();
builder.Services.AddSingleton<IOsuApiProvider, OsuApiProvider>();
builder.Services.AddTransient<IKeyAuthService, KeyAuthService>();
builder.Services.AddTransient<IPpService, PpService>();
builder.Services.AddHostedService<LeaderboardUpdateService>();

builder.Services.AddRateLimiter(_ => _
    .AddTokenBucketLimiter(policyName: "token", options =>
    {
        options.TokenLimit = 3;
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 2;
        options.ReplenishmentPeriod = TimeSpan.FromSeconds(5);
        options.TokensPerPeriod = 1;
        options.AutoReplenishment = true;
    }));

builder.Services.AddControllers((options =>
{
    options.OutputFormatters.RemoveType(typeof(HttpNoContentOutputFormatter));
    options.OutputFormatters.Insert(0, new HttpNoContentOutputFormatter
    {
        TreatNullValueAsNoContent = false
    });
})).AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });


// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (context, httpContext) =>
    {
        var parsedUserAgent = Parser.GetDefault()?.Parse(httpContext.Request.Headers.UserAgent);
        context.Set("Browser", parsedUserAgent?.Browser.ToString());
        context.Set("Device", parsedUserAgent?.Device.ToString());
        context.Set("OS", parsedUserAgent?.OS.ToString());
    };
});

app.UseForwardedHeaders(new ForwardedHeadersOptions { ForwardedHeaders = ForwardedHeaders.All });

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseCors(x =>
    {
        x.WithOrigins("http://localhost:3000").AllowAnyHeader().AllowAnyMethod().AllowCredentials();
        x.WithOrigins("http://localhost:8080").AllowAnyHeader().AllowAnyMethod().AllowCredentials();
    });

    app.UseSwagger(c =>
    {
        c.RouteTemplate = "swagger/{documentName}/swagger.json";
        c.PreSerializeFilters.Add((swaggerDoc, httpReq) =>
        {
            var scheme = app.Environment.IsStaging() ? "https" : httpReq.Scheme;
            swaggerDoc.Servers = new List<OpenApiServer>
                { new() { Url = $"{scheme}://{httpReq.Host.Value}{basePath}" } };
        });
    });
    app.UseSwaggerUI();
}

if (app.Environment.IsStaging() || app.Environment.IsProduction())
{
    app.Use((context, next) =>
    {
        context.Request.Scheme = "https";

        return next(context);
    });
}

app.UsePathBase(basePath);
app.MapControllers();
app.UseRateLimiter();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetService<DatabaseContext>();
    context?.Database.Migrate();
}

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
