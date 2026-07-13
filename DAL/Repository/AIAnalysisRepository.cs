using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using DAL.DTO;
using DAL.Entities;
using DAL.Helper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Text.Json;

namespace DAL.Repository
{
    public class AIAnalysisRepository(Swd392GameAiContext context, HttpClient http, IConfiguration config) : IAIAnalysisRepository
    {
        private readonly Swd392GameAiContext _context = context;
        private readonly HttpClient _http = http;
        private readonly IConfiguration _config = config;
        private readonly Cloudinary _cloudinary = new(new Account(
                config["Cloudinary:CloudName"],
                config["Cloudinary:ApiKey"],
                config["Cloudinary:ApiSecret"]
            ));
        private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
        private const string OcrEndpoint = "https://hxvf123-demoocrserver.hf.space/api/v1/extract?language=vie";

        public async Task<Aianalysis> ProcessScreenshotAsync(IFormFile file, int userId, string gameName)
        {
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, file.OpenReadStream()),
                Folder = "AI upload"
            };
            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.Error != null)
                throw new Exception($"Cloudinary upload failed: {uploadResult.Error.Message}");

            var imageUrl = uploadResult.SecureUrl?.ToString() ?? throw new Exception("Cloudinary upload failed: No URL returned");
            return await ExecuteAnalysisFlow(imageUrl, userId, gameName);
        }

        public async Task<Aianalysis?> ProcessLatestImageFromCloudAsync(int userId, string gameName)
        {
            string? imageUrl = null;
            string? publicId = null;
            string? createdAt = null;

            var searchResult = await _cloudinary.Search()
                .Expression("folder:\"AirtestUpload\" AND resource_type:image")
                .SortBy("created_at", "desc")
                .MaxResults(1)
                .ExecuteAsync();

            var searchLatest = searchResult.Resources?.FirstOrDefault();
            if (searchLatest != null)
            {
                imageUrl = searchLatest.SecureUrl?.ToString();
                publicId = searchLatest.PublicId;
                createdAt = searchLatest.CreatedAt;
            }
            else
            {
                Console.WriteLine("Cloudinary: Search API returned 0 results for 'AirtestUpload'. Trying ListResources fallback...");
                var listParams = new ListResourcesByPrefixParams
                {
                    Type = "upload",
                    Prefix = "AirtestUpload/",
                    MaxResults = 1,
                    Direction = "desc"
                };
                var listResources = await _cloudinary.ListResourcesAsync(listParams);
                var listLatest = listResources.Resources?.FirstOrDefault();
                if (listLatest != null)
                {
                    imageUrl = listLatest.SecureUrl?.ToString();
                    publicId = listLatest.PublicId;
                    createdAt = listLatest.CreatedAt;
                }
            }

            if (string.IsNullOrEmpty(imageUrl))
            {
                Console.WriteLine("Cloudinary: Truly no images found in 'AirtestUpload' folder.");
                return null;
            }

            var existingUpload = await _context.Imageuploads
                .Include(u => u.Aianalyses)
                .FirstOrDefaultAsync(u => u.Imageurl == imageUrl);

            if (existingUpload != null && existingUpload.Aianalyses.Any())
            {
                var existingAnalysis = existingUpload.Aianalyses.OrderByDescending(a => a.Processedtime).First();
                Console.WriteLine($"Cloudinary: Image {publicId} already processed. Returning existing Analysis ID: {existingAnalysis.Analysisid}");
                return existingAnalysis;
            }

            Console.WriteLine($"Cloudinary: Found latest image! PublicID: {publicId}, CreatedAt: {createdAt}, URL: {imageUrl}");
            return await ExecuteAnalysisFlow(imageUrl, userId, gameName);
        }

        private async Task<Aianalysis> ExecuteAnalysisFlow(string imageUrl, int userId, string gameName)
        {
            var upload = new Imageupload
            {
                Userid = userId,
                Imageurl = imageUrl,
                Uploadtime = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified),
                Status = "Pending"
            };
            _context.Imageuploads.Add(upload);
            await _context.SaveChangesAsync();

            Aianalysis? analysis = null;
            try
            {
                // Call OCR API
                var ocr = await CallHfOcrWithUrl(imageUrl);

                var avgConfidence = ocr.TextBlocks.Count > 0
                    ? ocr.TextBlocks.Average(b => b.Confidence) / 100.0
                    : 0.5;

                analysis = new Aianalysis
                {
                    Uploadid = upload.Uploadid,
                    Aimodelversion = "hf-ocr-server-v1",
                    Confidencescore = avgConfidence,
                    Processedtime = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified)
                };
                _context.Aianalyses.Add(analysis);
                await _context.SaveChangesAsync();

                if (string.IsNullOrWhiteSpace(ocr.FullText) && ocr.TextBlocks.Count == 0)
                    throw new Exception("OCR returned no text content");

                // Store raw OCR text
                var rawText = ocr.FullText ?? "";
                _context.Aiextractedfields.Add(new Aiextractedfield
                {
                    Analysisid = analysis.Analysisid,
                    Rawtext = rawText.Length > 500 ? rawText[..500] : rawText,
                    Fieldtype = "RawText",
                    Confidence = avgConfidence
                });

                // Parse structured game data from OCR
                var gameData = LeaderboardOcrParser.Parse(ocr, gameName);

                Dictionary<string, Server> serverCache = [];
                Dictionary<string, Guild> guildCache = [];
                Dictionary<string, Player> playerCache = [];

                // 1. Resolve Game
                var targetGame = await _context.Games.FirstOrDefaultAsync(g => g.Gamename == gameName);
                if (targetGame != null)
                    _context.Aiextractedfields.Add(new Aiextractedfield
                    {
                        Analysisid = analysis.Analysisid,
                        Rawtext = gameName,
                        Fieldtype = "GameName",
                        Confidence = 1.0
                    });

                // 2. Resolve Server
                Server? targetServer = null;
                if (!string.IsNullOrEmpty(gameData.ServerName))
                {
                    targetServer = await _context.Servers.FirstOrDefaultAsync(s =>
                        s.Servername == gameData.ServerName &&
                        (targetGame == null || s.Gameid == targetGame.Gameid));

                    if (targetServer == null)
                    {
                        targetServer = new Server { Servername = gameData.ServerName, Game = targetGame };
                        _context.Servers.Add(targetServer);
                        await _context.SaveChangesAsync();
                    }
                    serverCache[gameData.ServerName] = targetServer;
                    _context.Aiextractedfields.Add(new Aiextractedfield
                    {
                        Analysisid = analysis.Analysisid,
                        Rawtext = gameData.ServerName,
                        Fieldtype = "ServerName",
                        Confidence = 0.90
                    });
                }

                // 3. Resolve Event
                Event? targetEvent = null;
                if (!string.IsNullOrEmpty(gameData.EventName))
                {
                    targetEvent = await _context.Events.FirstOrDefaultAsync(e =>
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
                        _context.Events.Add(targetEvent);
                        await _context.SaveChangesAsync();
                    }
                    _context.Aiextractedfields.Add(new Aiextractedfield
                    {
                        Analysisid = analysis.Analysisid,
                        Rawtext = gameData.EventName,
                        Fieldtype = "EventName",
                        Confidence = 0.90
                    });
                }

                // 4. Create Leaderboard
                var lb = new Leaderboard
                {
                    Title = $"Bảng xếp hạng {gameData.EventName ?? targetEvent?.Eventname ?? "mới"}",
                    Createdfromanalysisid = analysis.Analysisid,
                    Metrictype = "Score",
                    Event = targetEvent
                };
                _context.Leaderboards.Add(lb);
                await _context.SaveChangesAsync();

                // 5. Process Leaderboard Entries
                foreach (var entry in gameData.Leaderboard)
                {
                    if (string.IsNullOrEmpty(entry.PlayerName)) continue;

                    // Find or create Guild
                    Guild? playerGuild = null;
                    if (!string.IsNullOrEmpty(entry.GuildName))
                    {
                        if (!guildCache.TryGetValue(entry.GuildName, out playerGuild))
                        {
                            playerGuild = await _context.Guilds.FirstOrDefaultAsync(g =>
                                g.Guildname == entry.GuildName &&
                                (targetServer == null || g.Serverid == targetServer.Serverid));

                            if (playerGuild == null)
                            {
                                playerGuild = new Guild { Guildname = entry.GuildName, Server = targetServer };
                                _context.Guilds.Add(playerGuild);
                                await _context.SaveChangesAsync();
                            }
                            guildCache[entry.GuildName] = playerGuild;
                        }
                    }

                    // Find or create Player
                    if (!playerCache.TryGetValue(entry.PlayerName, out var player))
                    {
                        player = await _context.Players.FirstOrDefaultAsync(p =>
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
                            _context.Players.Add(player);
                            await _context.SaveChangesAsync();
                        }
                        else
                        {
                            if (player.Guildid == null && playerGuild != null) player.Guildid = playerGuild.Guildid;
                            if (player.Serverid == null && targetServer != null) player.Serverid = targetServer.Serverid;
                        }
                        playerCache[entry.PlayerName] = player;
                    }

                    _context.Leaderboardentries.Add(new Leaderboardentry
                    {
                        Leaderboardid = lb.Leaderboardid,
                        Playerid = player.Playerid,
                        Rank = entry.Rank,
                        Value = entry.Score
                    });
                }

                await _context.SaveChangesAsync();
                upload.Status = "Success";
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                if (upload != null)
                {
                    upload.Status = "Failed";
                    await _context.SaveChangesAsync();
                }
                Console.WriteLine($"Error processing OCR result: {ex.Message}");
                throw;
            }

            return analysis!;
        }

        // ── OCR API call ──────────────────────────────────────────────────────────

        private async Task<HfOcrResultDto> CallHfOcrWithUrl(string imageUrl)
        {
            // Download image from Cloudinary
            using var imageResponse = await _http.GetAsync(imageUrl);
            imageResponse.EnsureSuccessStatusCode();
            var imageBytes = await imageResponse.Content.ReadAsByteArrayAsync();
            var contentType = imageResponse.Content.Headers.ContentType?.MediaType ?? "image/jpeg";
            var fileName = Path.GetFileName(new Uri(imageUrl).LocalPath);
            if (string.IsNullOrWhiteSpace(fileName)) fileName = "image.jpg";

            return await PostToOcrApi(imageBytes, contentType, fileName);
        }

        private async Task<HfOcrResultDto> PostToOcrApi(byte[] imageBytes, string contentType, string fileName)
        {
            using var form = new MultipartFormDataContent();
            var imageContent = new ByteArrayContent(imageBytes);
            imageContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            form.Add(imageContent, "image", fileName);

            var response = await _http.PostAsync(OcrEndpoint, form);
            var raw = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"OCR API error ({response.StatusCode}): {raw}");

            return JsonSerializer.Deserialize<HfOcrResultDto>(raw, _jsonOptions)
                ?? throw new Exception("Failed to parse OCR result");
        }

        // ── Query methods (unchanged) ─────────────────────────────────────────────

        public async Task<(List<Aianalysis> Items, int TotalCount)> GetAllAsync(AIQueryParameters parameters, int? userId = null)
        {
            var query = _context.Aianalyses
                .Include(a => a.Upload)
                .Include(a => a.Aiextractedfields)
                .Include(a => a.Leaderboards)
                    .ThenInclude(lb => lb.Event)
                        .ThenInclude(e => e.Game)
                .Include(a => a.Leaderboards)
                    .ThenInclude(lb => lb.Leaderboardentries)
                        .ThenInclude(e => e.Player)
                            .ThenInclude(p => p.Guild)
                .Include(a => a.Leaderboards)
                    .ThenInclude(lb => lb.Leaderboardentries)
                        .ThenInclude(e => e.Player)
                            .ThenInclude(p => p.Server)
                .AsQueryable();

            if (userId.HasValue)
                query = query.Where(a => a.Upload != null && a.Upload.Userid == userId.Value);

            if (!string.IsNullOrEmpty(parameters.SearchTerm))
            {
                var search = parameters.SearchTerm.ToLower();
                query = query.Where(a =>
                    (a.Aimodelversion != null && a.Aimodelversion.ToLower().Contains(search)) ||
                    a.Aiextractedfields.Any(f => f.Rawtext != null && f.Rawtext.ToLower().Contains(search)));
            }

            if (parameters.StartDate.HasValue)
                query = query.Where(a => a.Processedtime >= parameters.StartDate.Value);

            if (parameters.EndDate.HasValue)
                query = query.Where(a => a.Processedtime <= parameters.EndDate.Value);

            if (!string.IsNullOrEmpty(parameters.GameName))
            {
                var gn = parameters.GameName.ToLower();
                query = query.Where(a => a.Aiextractedfields.Any(f =>
                    f.Fieldtype == "GameName" && f.Rawtext != null && f.Rawtext.ToLower().Contains(gn)));
            }

            var totalCount = await query.CountAsync();

            query = !string.IsNullOrEmpty(parameters.SortBy) && parameters.SortBy.ToLower() == "gamename"
                ? parameters.IsDescending
                    ? query.OrderByDescending(a => a.Aiextractedfields.Where(f => f.Fieldtype == "GameName").Select(f => f.Rawtext).FirstOrDefault())
                    : query.OrderBy(a => a.Aiextractedfields.Where(f => f.Fieldtype == "GameName").Select(f => f.Rawtext).FirstOrDefault())
                : parameters.IsDescending
                    ? query.OrderByDescending(a => a.Processedtime)
                    : query.OrderBy(a => a.Processedtime);

            var items = await query
                .Skip(Math.Max(0, (parameters.PageNumber - 1) * parameters.PageSize))
                .Take(parameters.PageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<Aianalysis?> GetByIdAsync(int id)
        {
            return await _context.Aianalyses.Include(a => a.Aiextractedfields).FirstOrDefaultAsync(a => a.Analysisid == id);
        }

        public async Task<Aianalysis?> GetByIdWithDetailsAsync(int id)
        {
            return await _context.Aianalyses
                .Include(a => a.Upload)
                .Include(a => a.Aiextractedfields)
                .Include(a => a.Leaderboards)
                    .ThenInclude(lb => lb.Event)
                        .ThenInclude(e => e.Game)
                .Include(a => a.Leaderboards)
                    .ThenInclude(lb => lb.Leaderboardentries)
                        .ThenInclude(e => e.Player)
                            .ThenInclude(p => p.Guild)
                .Include(a => a.Leaderboards)
                    .ThenInclude(lb => lb.Leaderboardentries)
                        .ThenInclude(e => e.Player)
                            .ThenInclude(p => p.Server)
                .FirstOrDefaultAsync(a => a.Analysisid == id);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var analysis = await _context.Aianalyses
                .Include(a => a.Aiextractedfields)
                .Include(a => a.Leaderboards)
                    .ThenInclude(lb => lb.Leaderboardentries)
                .Include(a => a.Upload)
                .FirstOrDefaultAsync(a => a.Analysisid == id);

            if (analysis == null) return false;

            foreach (var lb in analysis.Leaderboards)
                _context.Leaderboardentries.RemoveRange(lb.Leaderboardentries);

            _context.Leaderboards.RemoveRange(analysis.Leaderboards);
            _context.Aiextractedfields.RemoveRange(analysis.Aiextractedfields);

            if (analysis.Upload != null)
                _context.Imageuploads.Remove(analysis.Upload);

            _context.Aianalyses.Remove(analysis);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<string>> GetAirtestUploadImagesAsync()
        {
            var result = await _cloudinary.Search()
                .Expression("folder:\"AirtestUpload\" AND resource_type:image")
                .SortBy("created_at", "desc")
                .MaxResults(100)
                .ExecuteAsync();

            if (result == null || result.Resources == null) return [];

            return result.Resources
                .Select(r => r.SecureUrl?.ToString() ?? "")
                .Where(url => !string.IsNullOrEmpty(url))
                .ToList();
        }

        public async Task<List<HeatmapDto>> GetHeatmapDataAsync(int? userId = null)
        {
            var query = _context.Leaderboardentries
                .Include(e => e.Leaderboard)
                    .ThenInclude(l => l.Createdfromanalysis)
                        .ThenInclude(a => a.Upload)
                .Include(e => e.Player)
                .AsQueryable();

            if (userId.HasValue && userId.Value > 0)
            {
                query = query.Where(e =>
                    e.Leaderboard != null &&
                    e.Leaderboard.Createdfromanalysis != null &&
                    e.Leaderboard.Createdfromanalysis.Upload != null &&
                    e.Leaderboard.Createdfromanalysis.Upload.Userid == userId.Value);
            }

            var entries = await query
                .Where(e =>
                    e.Playerid.HasValue && e.Player != null && e.Value.HasValue &&
                    e.Leaderboard != null &&
                    e.Leaderboard.Createdfromanalysis != null &&
                    e.Leaderboard.Createdfromanalysis.Processedtime.HasValue)
                .Select(e => new
                {
                    PlayerId = e.Playerid!.Value,
                    PlayerName = e.Player!.Playername,
                    Value = e.Value!.Value,
                    Time = e.Leaderboard!.Createdfromanalysis!.Processedtime!.Value
                })
                .OrderBy(e => e.PlayerId)
                .ThenBy(e => e.Time)
                .ToListAsync();

            var heatmapDays = new Dictionary<DateTime, List<HeatmapPlayerDto>>();
            var random = new Random();

            for (int i = 0; i < entries.Count; i++)
            {
                var current = entries[i];
                if (i > 0 && entries[i - 1].PlayerId == current.PlayerId)
                {
                    var previous = entries[i - 1];
                    if (current.Value > previous.Value)
                    {
                        var date = current.Time.Date;
                        if (!heatmapDays.ContainsKey(date))
                            heatmapDays[date] = new List<HeatmapPlayerDto>();

                        heatmapDays[date].Add(new HeatmapPlayerDto
                        {
                            PlayerId = current.PlayerId,
                            PlayerName = current.PlayerName ?? "Unknown",
                            Time = current.Time.AddSeconds(random.Next(0, 59)).ToString("HH:mm:ss")
                        });
                    }
                }
            }

            return heatmapDays
                .OrderBy(kvp => kvp.Key)
                .Select(kvp => new HeatmapDto
                {
                    Date = kvp.Key.ToString("yyyy-MM-dd"),
                    Count = kvp.Value.Count,
                    Players = kvp.Value.OrderBy(p => p.Time).ToList()
                })
                .ToList();
        }
    }
}
