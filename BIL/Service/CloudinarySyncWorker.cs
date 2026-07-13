using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using DAL.DTO;
using DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text.Json;
using DAL.Helper;

namespace BIL.Service
{
    public class CloudinarySyncWorker(
        IServiceProvider serviceProvider,
        ILogger<CloudinarySyncWorker> logger,
        IConfiguration config,
        IHttpClientFactory httpClientFactory) : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider = serviceProvider;
        private readonly ILogger<CloudinarySyncWorker> _logger = logger;
        private readonly IConfiguration _config = config;
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
        private readonly Cloudinary _cloudinary = new(new Account(
                config["Cloudinary:CloudName"],
                config["Cloudinary:ApiKey"],
                config["Cloudinary:ApiSecret"]
            ));
        private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
        private const int INTERVAL_SECONDS = 180;
        private const string OcrEndpoint = "https://hxvf123-demoocrserver.hf.space/api/v1/extract?language=eng";

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("CloudinarySyncWorker started. Checking for new images every {Interval}s", INTERVAL_SECONDS);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<Swd392GameAiContext>();
                    var http = _httpClientFactory.CreateClient("OcrClient");

                    var allLatest = new List<(string imageUrl, string publicId)>();

                    var searchResult = await _cloudinary.Search()
                        .Expression("folder:\"AirtestUpload\" AND resource_type:image")
                        .SortBy("created_at", "desc")
                        .MaxResults(50)
                        .ExecuteAsync();

                    if (searchResult.Resources != null)
                    {
                        foreach (var res in searchResult.Resources)
                        {
                            if (!string.IsNullOrEmpty(res.SecureUrl?.ToString()) && !string.IsNullOrEmpty(res.PublicId))
                                allLatest.Add((res.SecureUrl.ToString(), res.PublicId));
                        }
                    }

                    if (allLatest.Count == 0)
                    {
                        var listParams = new ListResourcesByPrefixParams
                        {
                            Type = "upload",
                            Prefix = "AirtestUpload/",
                            MaxResults = 50,
                            Direction = "desc"
                        };
                        var listResources = await _cloudinary.ListResourcesAsync(listParams);
                        if (listResources.Resources != null)
                        {
                            foreach (var res in listResources.Resources)
                            {
                                if (!string.IsNullOrEmpty(res.SecureUrl?.ToString()) && !string.IsNullOrEmpty(res.PublicId))
                                    allLatest.Add((res.SecureUrl.ToString(), res.PublicId));
                            }
                        }
                    }

                    foreach (var (imageUrl, publicId) in allLatest)
                    {
                        var exists = await dbContext.Imageuploads.AnyAsync(u => u.Imageurl == imageUrl, stoppingToken);
                        if (!exists)
                        {
                            _logger.LogInformation("Found new image on Cloudinary: {PublicId}. Starting analysis...", publicId);
                            await ProcessNewImageAsync(dbContext, http, imageUrl);
                            await dbContext.SaveChangesAsync(stoppingToken);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in CloudinarySyncWorker");
                }

                await Task.Delay(TimeSpan.FromSeconds(INTERVAL_SECONDS), stoppingToken);
            }
        }

        private async Task ProcessNewImageAsync(Swd392GameAiContext context, HttpClient http, string imageUrl)
        {
            int systemUserId = 1;

            var upload = new Imageupload
            {
                Userid = systemUserId,
                Imageurl = imageUrl,
                Uploadtime = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified),
                Status = "Pending"
            };
            context.Imageuploads.Add(upload);
            await context.SaveChangesAsync();

            try
            {
                // Call OCR API
                var ocr = await CallHfOcrWithUrl(http, imageUrl);

                var avgConfidence = ocr.TextBlocks.Count > 0
                    ? ocr.TextBlocks.Average(b => b.Confidence) / 100.0
                    : 0.5;

                var analysis = new Aianalysis
                {
                    Uploadid = upload.Uploadid,
                    Aimodelversion = "hf-ocr-server-v1",
                    Confidencescore = avgConfidence,
                    Processedtime = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified)
                };
                context.Aianalyses.Add(analysis);
                await context.SaveChangesAsync();

                if (string.IsNullOrWhiteSpace(ocr.FullText) && ocr.TextBlocks.Count == 0)
                    throw new Exception("OCR returned no text content");

                var rawText = ocr.FullText ?? "";
                context.Aiextractedfields.Add(new Aiextractedfield
                {
                    Analysisid = analysis.Analysisid,
                    Rawtext = rawText.Length > 500 ? rawText[..500] : rawText,
                    Fieldtype = "RawText",
                    Confidence = avgConfidence
                });

                // Parse game name from existing DB (background worker doesn't know game name)
                string? gameName = null;
                var firstGame = await context.Games.OrderBy(g => g.Gameid).FirstOrDefaultAsync();
                gameName = firstGame?.Gamename;

                var gameData = LeaderboardOcrParser.Parse(ocr, gameName);

                // Resolve Game
                Game? targetGame = null;
                if (!string.IsNullOrEmpty(gameName))
                {
                    targetGame = await context.Games.FirstOrDefaultAsync(g => g.Gamename == gameName);
                    if (targetGame != null)
                        context.Aiextractedfields.Add(new Aiextractedfield
                        {
                            Analysisid = analysis.Analysisid,
                            Rawtext = gameName,
                            Fieldtype = "GameName",
                            Confidence = 1.0
                        });
                }

                // Resolve Server
                Server? targetServer = null;
                if (!string.IsNullOrEmpty(gameData.ServerName))
                {
                    targetServer = await context.Servers.FirstOrDefaultAsync(s =>
                        s.Servername == gameData.ServerName &&
                        (targetGame == null || s.Gameid == targetGame.Gameid));

                    if (targetServer == null)
                    {
                        targetServer = new Server { Servername = gameData.ServerName, Game = targetGame };
                        context.Servers.Add(targetServer);
                        await context.SaveChangesAsync();
                    }
                    context.Aiextractedfields.Add(new Aiextractedfield
                    {
                        Analysisid = analysis.Analysisid,
                        Rawtext = gameData.ServerName,
                        Fieldtype = "ServerName",
                        Confidence = 0.90
                    });
                }

                // Resolve Event
                Event? targetEvent = null;
                if (!string.IsNullOrEmpty(gameData.EventName))
                {
                    targetEvent = await context.Events.FirstOrDefaultAsync(e =>
                        e.Eventname == gameData.EventName &&
                        (targetGame == null || e.Gameid == targetGame.Gameid));

                    if (targetEvent == null)
                    {
                        targetEvent = new Event
                        {
                            Eventname = gameData.EventName,
                            Game = targetGame,
                            Startdate = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified)
                        };
                        context.Events.Add(targetEvent);
                        await context.SaveChangesAsync();
                    }
                    context.Aiextractedfields.Add(new Aiextractedfield
                    {
                        Analysisid = analysis.Analysisid,
                        Rawtext = gameData.EventName,
                        Fieldtype = "EventName",
                        Confidence = 0.90
                    });
                }

                // Create Leaderboard
                var lb = new Leaderboard
                {
                    Title = $"Bảng xếp hạng {gameData.EventName ?? targetEvent?.Eventname ?? "mới"}",
                    Createdfromanalysisid = analysis.Analysisid,
                    Metrictype = "Score",
                    Event = targetEvent
                };
                context.Leaderboards.Add(lb);
                await context.SaveChangesAsync();

                // Process Entries
                Dictionary<string, Guild> guildCache = [];
                Dictionary<string, Player> playerCache = [];

                foreach (var entry in gameData.Leaderboard)
                {
                    if (string.IsNullOrEmpty(entry.PlayerName)) continue;

                    Guild? playerGuild = null;
                    if (!string.IsNullOrEmpty(entry.GuildName))
                    {
                        if (!guildCache.TryGetValue(entry.GuildName, out playerGuild))
                        {
                            playerGuild = await context.Guilds.FirstOrDefaultAsync(g =>
                                g.Guildname == entry.GuildName &&
                                (targetServer == null || g.Serverid == targetServer.Serverid));

                            if (playerGuild == null)
                            {
                                playerGuild = new Guild { Guildname = entry.GuildName, Server = targetServer };
                                context.Guilds.Add(playerGuild);
                                await context.SaveChangesAsync();
                            }
                            guildCache[entry.GuildName] = playerGuild;
                        }
                    }

                    if (!playerCache.TryGetValue(entry.PlayerName, out var player))
                    {
                        player = await context.Players.FirstOrDefaultAsync(p =>
                            p.Playername == entry.PlayerName &&
                            (targetGame == null || p.Gameid == targetGame.Gameid));

                        if (player == null)
                        {
                            player = new Player
                            {
                                Playername = entry.PlayerName,
                                Game = targetGame,
                                Server = targetServer,
                                Guild = playerGuild
                            };
                            context.Players.Add(player);
                            await context.SaveChangesAsync();
                        }
                        else
                        {
                            if (player.Guildid == null && playerGuild != null) player.Guildid = playerGuild.Guildid;
                            if (player.Serverid == null && targetServer != null) player.Serverid = targetServer.Serverid;
                        }
                        playerCache[entry.PlayerName] = player;
                    }

                    context.Leaderboardentries.Add(new Leaderboardentry
                    {
                        Leaderboardid = lb.Leaderboardid,
                        Playerid = player.Playerid,
                        Rank = entry.Rank,
                        Value = entry.Score
                    });
                }

                await context.SaveChangesAsync();
                upload.Status = "Success";
                await context.SaveChangesAsync();
                _logger.LogInformation("Successfully processed image. Analysis ID: {Id}", analysis.Analysisid);
            }
            catch (Exception ex)
            {
                upload.Status = "Failed";
                await context.SaveChangesAsync();
                _logger.LogError(ex, "Failed to process image: {Url}", imageUrl);
            }
        }

        // ── OCR API call ──────────────────────────────────────────────────────────

        private static async Task<HfOcrResultDto> CallHfOcrWithUrl(HttpClient http, string imageUrl)
        {
            using var imageResponse = await http.GetAsync(imageUrl);
            imageResponse.EnsureSuccessStatusCode();
            var imageBytes = await imageResponse.Content.ReadAsByteArrayAsync();
            var contentType = imageResponse.Content.Headers.ContentType?.MediaType ?? "image/jpeg";
            var fileName = Path.GetFileName(new Uri(imageUrl).LocalPath);
            if (string.IsNullOrWhiteSpace(fileName)) fileName = "image.jpg";

            using var form = new MultipartFormDataContent();
            var imageContent = new ByteArrayContent(imageBytes);
            imageContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            form.Add(imageContent, "file", fileName);

            var response = await http.PostAsync(OcrEndpoint, form);
            var raw = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"OCR API error ({response.StatusCode}): {raw}");

            return JsonSerializer.Deserialize<HfOcrResultDto>(raw, _jsonOptions)
                ?? throw new Exception("Failed to parse OCR result");
        }
    }
}
