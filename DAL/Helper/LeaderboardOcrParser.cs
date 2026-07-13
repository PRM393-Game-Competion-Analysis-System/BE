using System.Text.Json;
using System.Text.RegularExpressions;
using DAL.DTO;

namespace DAL.Helper
{
    public static class LeaderboardOcrParser
    {
        private static readonly string[] EventKeywords =
            ["Bảng", "Xếp Hạng", "Chiến", "Giải", "Hạng", "Event", "Tournament"];

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public static GameOcrParseResult Parse(HfOcrResultDto ocr, string? gameName)
        {
            var result = new GameOcrParseResult { GameName = gameName };

            if (TryParseStructuredJson(ocr.FullText, result))
                return result;

            var validBlocks = ocr.TextBlocks
                .Where(b => !string.IsNullOrWhiteSpace(b.Text) && b.Confidence > 20)
                .ToList();

            var blocksWithBox = validBlocks
                .Where(b => b.BoundingBox != null)
                .ToList();

            if (blocksWithBox.Count > 0)
            {
                var rows = GroupBlocksIntoRows(blocksWithBox, yTolerance: 25);
                ParseRows(rows, result);
            }

            if (result.Leaderboard.Count == 0 && !string.IsNullOrWhiteSpace(ocr.FullText))
            {
                if (!TryParseStructuredJson(ocr.FullText, result))
                    ParseLines(ocr.FullText, result);
            }

            return result;
        }

        private static bool TryParseStructuredJson(string? text, GameOcrParseResult result)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            var jsonStart = text.IndexOf('[', StringComparison.Ordinal);
            if (jsonStart < 0)
                return false;

            var json = text[jsonStart..].Trim();
            if (!json.StartsWith('['))
                return false;

            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind != JsonValueKind.Array)
                    return false;

                var parsedAny = false;
                foreach (var item in doc.RootElement.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.Object)
                        continue;

                    if (!TryReadStructuredEntry(item, out var entry))
                        continue;

                    result.Leaderboard.Add(entry);
                    parsedAny = true;
                }

                return parsedAny;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private static bool TryReadStructuredEntry(JsonElement item, out LeaderboardEntryRaw entry)
        {
            entry = new LeaderboardEntryRaw();

            if (!TryReadRank(item, out var rank))
                return false;

            var playerName = ReadStringProperty(item, "Tên", "Ten", "PlayerName", "Name");
            if (string.IsNullOrWhiteSpace(playerName))
                return false;

            var guildName = ReadStringProperty(item, "Bang Hội", "Bang Hoi", "GuildName", "Guild");
            var scoreText = ReadStringProperty(item, "Lực Chiến", "Luc Chien", "Score", "Value");
            if (!TryParseScore(scoreText, out var score))
                return false;

            entry.Rank = rank;
            entry.PlayerName = playerName.Trim();
            entry.GuildName = string.IsNullOrWhiteSpace(guildName) ? null : guildName.Trim();
            entry.Score = score;
            return true;
        }

        private static bool TryReadRank(JsonElement item, out int rank)
        {
            rank = 0;
            foreach (var key in new[] { "Hạng", "Hang", "Rank" })
            {
                if (!item.TryGetProperty(key, out var value))
                    continue;

                return value.ValueKind switch
                {
                    JsonValueKind.Number => value.TryGetInt32(out rank) && rank is >= 1 and <= 999,
                    JsonValueKind.String => int.TryParse(value.GetString(), out rank) && rank is >= 1 and <= 999,
                    _ => false
                };
            }

            return false;
        }

        private static string? ReadStringProperty(JsonElement item, params string[] names)
        {
            foreach (var name in names)
            {
                if (!item.TryGetProperty(name, out var value))
                    continue;

                return value.ValueKind switch
                {
                    JsonValueKind.String => value.GetString(),
                    JsonValueKind.Number => value.GetRawText(),
                    _ => null
                };
            }

            return null;
        }

        private static void ParseRows(List<List<HfTextBlock>> rows, GameOcrParseResult result)
        {
            foreach (var row in rows)
            {
                var tokens = row
                    .Select(b => b.Text!.Trim())
                    .Where(t => !string.IsNullOrEmpty(t))
                    .ToList();

                if (tokens.Count == 0)
                    continue;

                var rowText = string.Join(" ", tokens);

                if (TryParseLeaderboardTokens(tokens, out var entry))
                {
                    result.Leaderboard.Add(entry);
                    continue;
                }

                if (result.ServerName == null &&
                    (Regex.IsMatch(rowText, @"\bS\d{1,3}\b") ||
                     rowText.Contains("Server", StringComparison.OrdinalIgnoreCase) ||
                     rowText.Contains("Máy chủ", StringComparison.OrdinalIgnoreCase)))
                {
                    result.ServerName = rowText;
                    continue;
                }

                if (result.EventName == null &&
                    EventKeywords.Any(k => rowText.Contains(k, StringComparison.OrdinalIgnoreCase)))
                {
                    result.EventName = rowText;
                }
            }
        }

        private static void ParseLines(string fullText, GameOcrParseResult result)
        {
            var lines = fullText
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim())
                .Where(l => l.Length > 0)
                .ToList();

            foreach (var line in lines)
            {
                if (TryParseLeaderboardLine(line, out var entry))
                {
                    result.Leaderboard.Add(entry);
                    continue;
                }

                if (result.ServerName == null &&
                    (Regex.IsMatch(line, @"\bS\d{1,3}\b") ||
                     line.Contains("Server", StringComparison.OrdinalIgnoreCase) ||
                     line.Contains("Máy chủ", StringComparison.OrdinalIgnoreCase)))
                {
                    result.ServerName = line;
                    continue;
                }

                if (result.EventName == null &&
                    EventKeywords.Any(k => line.Contains(k, StringComparison.OrdinalIgnoreCase)))
                {
                    result.EventName = line;
                }
            }
        }

        private static bool TryParseLeaderboardLine(string line, out LeaderboardEntryRaw entry)
        {
            entry = new LeaderboardEntryRaw();

            var tabParts = line.Split('\t', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (tabParts.Length >= 3 && TryParseLeaderboardTokens(tabParts.ToList(), out entry))
                return true;

            var multiSpaceParts = Regex.Split(line, @"\s{2,}")
                .Select(p => p.Trim())
                .Where(p => p.Length > 0)
                .ToList();
            if (multiSpaceParts.Count >= 3 && TryParseLeaderboardTokens(multiSpaceParts, out entry))
                return true;

            var rankLinePattern = new Regex(@"^(\d{1,3})\.?\s+(.+?)\s+([\d,\.]{4,})$");
            var match = rankLinePattern.Match(line);
            if (!match.Success || !int.TryParse(match.Groups[1].Value, out var rank) || rank is < 1 or > 999)
                return false;

            if (!TryParseScore(match.Groups[3].Value, out var score))
                return false;

            var middleParts = match.Groups[2].Value
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();

            if (middleParts.Count == 0)
                return false;

            string playerName;
            string? guildName = null;

            if (middleParts.Count == 1)
            {
                playerName = middleParts[0];
            }
            else
            {
                var guildCandidate = middleParts[^1];
                if (LooksLikeGuild(guildCandidate))
                {
                    guildName = guildCandidate;
                    playerName = string.Join(" ", middleParts.Take(middleParts.Count - 1));
                }
                else
                {
                    playerName = string.Join(" ", middleParts);
                }
            }

            entry.Rank = rank;
            entry.PlayerName = playerName;
            entry.GuildName = guildName;
            entry.Score = score;
            return true;
        }

        private static bool TryParseLeaderboardTokens(List<string> tokens, out LeaderboardEntryRaw entry)
        {
            entry = new LeaderboardEntryRaw();

            if (tokens.Count < 2)
                return false;

            if (!TryParseRankToken(tokens[0], out var rank))
                return false;

            var scoreIndex = FindScoreTokenIndex(tokens);
            if (scoreIndex <= 0 || !TryParseScore(tokens[scoreIndex], out var score))
                return false;

            var middle = tokens.Skip(1).Take(scoreIndex - 1).ToList();
            if (middle.Count == 0)
                return false;

            string playerName;
            string? guildName = null;

            if (middle.Count == 1)
            {
                playerName = middle[0];
            }
            else if (LooksLikeGuild(middle[^1]))
            {
                guildName = middle[^1];
                playerName = string.Join(" ", middle.Take(middle.Count - 1));
            }
            else
            {
                playerName = string.Join(" ", middle);
            }

            entry.Rank = rank;
            entry.PlayerName = playerName.Trim();
            entry.GuildName = string.IsNullOrWhiteSpace(guildName) ? null : guildName.Trim();
            entry.Score = score;
            return !string.IsNullOrWhiteSpace(entry.PlayerName);
        }

        private static int FindScoreTokenIndex(List<string> tokens)
        {
            for (var i = tokens.Count - 1; i >= 1; i--)
            {
                if (TryParseScore(tokens[i], out var score) && score >= 1000)
                    return i;
            }

            for (var i = tokens.Count - 1; i >= 1; i--)
            {
                if (TryParseScore(tokens[i], out _))
                    return i;
            }

            return -1;
        }

        private static bool TryParseRankToken(string token, out int rank)
        {
            rank = 0;
            var cleaned = token.Trim().TrimEnd('.');
            return int.TryParse(cleaned, out rank) && rank is >= 1 and <= 999;
        }

        private static bool TryParseScore(string? token, out double score)
        {
            score = 0;
            if (string.IsNullOrWhiteSpace(token))
                return false;

            var cleaned = token.Replace(",", "").Replace(".", "").Trim();
            return double.TryParse(cleaned, out score) && score > 0;
        }

        private static bool LooksLikeGuild(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return false;

            if (TryParseScore(token, out _))
                return false;

            return token.Contains('-') ||
                   token.Contains('_') ||
                   token.Contains("AE", StringComparison.OrdinalIgnoreCase) ||
                   token.Contains("Song", StringComparison.OrdinalIgnoreCase);
        }

        private static List<List<HfTextBlock>> GroupBlocksIntoRows(List<HfTextBlock> blocks, int yTolerance)
        {
            var sorted = blocks.OrderBy(b => b.BoundingBox!.Y).ToList();
            var rows = new List<List<HfTextBlock>>();
            List<HfTextBlock>? currentRow = null;
            var currentRowY = int.MinValue;

            foreach (var block in sorted)
            {
                var blockY = block.BoundingBox!.Y;
                if (currentRow == null || Math.Abs(blockY - currentRowY) > yTolerance)
                {
                    currentRow = [block];
                    rows.Add(currentRow);
                    currentRowY = blockY;
                }
                else
                {
                    currentRow.Add(block);
                }
            }

            foreach (var row in rows)
                row.Sort((a, b) => a.BoundingBox!.X.CompareTo(b.BoundingBox!.X));

            return rows;
        }
    }

    public class GameOcrParseResult
    {
        public string? GameName { get; set; }
        public string? ServerName { get; set; }
        public string? EventName { get; set; }
        public List<LeaderboardEntryRaw> Leaderboard { get; set; } = [];
    }

    public class LeaderboardEntryRaw
    {
        public int Rank { get; set; }
        public string PlayerName { get; set; } = "";
        public double Score { get; set; }
        public string? GuildName { get; set; }
    }
}
