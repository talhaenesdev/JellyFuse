using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;
using Unity.Services.CloudSave.Model;
using Unity.Services.Leaderboards.Model;

namespace Leaderboard
{
    /// <summary>
    /// Scores a won level on the server, so a player cannot post a score of their own making. Each level's best
    /// score is kept in the player's protected Cloud Save data (readable by the game, writable only from here);
    /// the all-time board holds the sum of those bests, the weekly board the points won this week.
    /// </summary>
    public class LeaderboardModule
    {
        public const string AllTime = "all_time";
        public const string Weekly = "weekly";
        private const string BestKey = "level_best";
        private const string ProfileKey = "profile";
        private const int MaxNameLength = 16;

        private const int MaxLevel = 5000;
        private const int MaxSeconds = 600;
        private static readonly double[] DifficultyFactor = { 1, 1.5, 2.5 };

        private readonly ILogger<LeaderboardModule> _logger;
        public LeaderboardModule(ILogger<LeaderboardModule> logger) => _logger = logger;

        /// <summary>
        /// 100 a level times its difficulty, plus 5 a second left on the clock - none when the player bought extra time.
        /// </summary>
        public static int Points(int difficulty, int secondsLeft, bool extraTime)
        {
            double factor = DifficultyFactor[Math.Clamp(difficulty, 0, DifficultyFactor.Length - 1)];
            int speed = extraTime ? 0 : Math.Max(0, secondsLeft) * 5;
            return (int)Math.Round(100 * factor) + speed;
        }

        [CloudCodeFunction("SubmitLevel")]
        public async Task<SubmitResult> SubmitLevel(IExecutionContext context, IGameApiClient api,
            int level, int difficulty, int secondsTotal, int secondsLeft, bool extraTime)
        {
            if (level < 1 || level > MaxLevel) return SubmitResult.Refused("level out of range");
            secondsTotal = Math.Clamp(secondsTotal, 0, MaxSeconds);
            secondsLeft = Math.Clamp(secondsLeft, 0, secondsTotal);
            int points = Points(difficulty, secondsLeft, extraTime);

            var bests = await ReadBests(context, api);
            bests.TryGetValue(level.ToString(), out int previous);
            bool record = points > previous;
            if (record)
            {
                bests[level.ToString()] = points;
                await api.CloudSaveData.SetProtectedItemAsync(context, context.ServiceToken, context.ProjectId, context.PlayerId,
                    new SetItemBody(BestKey, bests));
            }
            long total = 0;
            foreach (var best in bests.Values) total += best;

            var project = Guid.Parse(context.ProjectId);
            await api.Leaderboards.AddLeaderboardPlayerScoreAsync(context, context.AccessToken, project, AllTime, context.PlayerId,
                new AddLeaderboardScore(total));
            // The weekly board adds every win's points up (its update type is Aggregate), so playing counts there.
            await api.Leaderboards.AddLeaderboardPlayerScoreAsync(context, context.AccessToken, project, Weekly, context.PlayerId,
                new AddLeaderboardScore(points));

            _logger.LogInformation("Level {Level} scored {Points} for {PlayerId}, total {Total}", level, points, context.PlayerId, total);
            return new SubmitResult { Accepted = true, Points = points, Best = Math.Max(points, previous), Total = total, Record = record };
        }

        /// <summary>
        /// The name, icon and frame the player picked in the game, kept in their public Cloud Save data so every board shows
        /// them. The name is trimmed and cut to MaxNameLength; an empty one falls back to the Authentication name.
        /// </summary>
        [CloudCodeFunction("SetProfile")]
        public async Task<ProfileResult> SetProfile(IExecutionContext context, IGameApiClient api, string name, string icon, string frame)
        {
            name = (name ?? string.Empty).Trim();
            if (name.Length > MaxNameLength) name = name.Substring(0, MaxNameLength);
            // Every player sees this name: a rude one is stored as the plain fallback instead.
            name = NameFilter.Clean(name);
            icon = (icon ?? string.Empty).Trim();
            if (icon.Length > 64) icon = icon.Substring(0, 64);
            frame = (frame ?? string.Empty).Trim();
            if (frame.Length > 64) frame = frame.Substring(0, 64);
            await api.CloudSaveData.SetPublicItemAsync(context, context.ServiceToken, context.ProjectId, context.PlayerId,
                new SetItemBody(ProfileKey, new PlayerProfile { Name = name, Icon = icon, Frame = frame }));
            return new ProfileResult { Saved = true, Name = name };
        }

        /// <summary>The top of one board and the asking player's own row, names included.</summary>
        [CloudCodeFunction("GetBoard")]
        public async Task<BoardResult> GetBoard(IExecutionContext context, IGameApiClient api, string board, int limit)
        {
            if (board != AllTime && board != Weekly) board = AllTime;
            limit = Math.Clamp(limit, 1, 100);
            var project = Guid.Parse(context.ProjectId);
            var result = new BoardResult();
            var page = await api.Leaderboards.GetLeaderboardScoresAsync(context, context.AccessToken, project, board, null, 0, limit);
            foreach (var entry in page.Data.Results)
                result.Top.Add(new BoardRow { Rank = entry.Rank + 1, Name = entry.PlayerName, Tag = Tag(entry.PlayerName, entry.PlayerId), Score = (long)entry.Score, IsMe = entry.PlayerId == context.PlayerId });
            try
            {
                var mine = await api.Leaderboards.GetLeaderboardPlayerScoreAsync(context, context.AccessToken, project, board, context.PlayerId, null);
                result.Me = new BoardRow { Rank = mine.Data.Rank + 1, Name = mine.Data.PlayerName, Tag = Tag(mine.Data.PlayerName, context.PlayerId), Score = (long)mine.Data.Score, IsMe = true };
            }
            catch (Exception)
            {
                // No score on this board yet: the player has no row.
            }
            var rows = new List<(BoardRow row, string playerId)>();
            foreach (var entry in page.Data.Results)
                rows.Add((result.Top[rows.Count], entry.PlayerId));
            if (result.Me != null) rows.Add((result.Me, context.PlayerId));
            await Task.WhenAll(rows.ConvertAll(item => ApplyProfile(context, api, item.row, item.playerId)));
            return result;
        }

        /// <summary>The player's picked name and icon over the row; a player who never picked one keeps their row as it is.</summary>
        private static async Task ApplyProfile(IExecutionContext context, IGameApiClient api, BoardRow row, string playerId)
        {
            try
            {
                var response = await api.CloudSaveData.GetPublicItemsAsync(context, context.ServiceToken, context.ProjectId, playerId,
                    new List<string> { ProfileKey });
                var items = response.Data?.Results;
                if (items == null || items.Count == 0 || items[0].Value == null) return;
                var profile = JsonConvert.DeserializeObject<PlayerProfile>(items[0].Value.ToString());
                if (profile == null) return;
                if (!string.IsNullOrWhiteSpace(profile.Name)) row.Name = profile.Name;
                row.Icon = profile.Icon ?? string.Empty;
                row.Frame = profile.Frame ?? string.Empty;
            }
            catch (Exception)
            {
                // A profile that cannot be read leaves the row with its Authentication name.
            }
        }

        /// <summary>
        /// What tells two players of one name apart: the number Authentication gives every name ("Name#1234" shows
        /// "#1234"), or the start of the player id when the name carries none. Fixed for a player, whatever they rename to.
        /// </summary>
        private static string Tag(string authName, string playerId)
        {
            int hash = authName?.LastIndexOf('#') ?? -1;
            if (hash >= 0 && hash < authName.Length - 1) return authName.Substring(hash);
            return string.IsNullOrEmpty(playerId) ? string.Empty : "#" + playerId.Substring(0, Math.Min(6, playerId.Length)).ToUpperInvariant();
        }

        private static async Task<Dictionary<string, int>> ReadBests(IExecutionContext context, IGameApiClient api)
        {
            var response = await api.CloudSaveData.GetProtectedItemsAsync(context, context.ServiceToken, context.ProjectId, context.PlayerId,
                new List<string> { BestKey });
            var items = response.Data?.Results;
            if (items == null || items.Count == 0 || items[0].Value == null) return new Dictionary<string, int>();
            return JsonConvert.DeserializeObject<Dictionary<string, int>>(items[0].Value.ToString()) ?? new Dictionary<string, int>();
        }
    }

    public class SubmitResult
    {
        public bool Accepted { get; set; }
        public string Reason { get; set; } = string.Empty;
        public int Points { get; set; }
        public int Best { get; set; }
        public long Total { get; set; }
        public bool Record { get; set; }

        public static SubmitResult Refused(string reason) => new() { Accepted = false, Reason = reason };
    }

    public class BoardRow
    {
        public int Rank { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Tag { get; set; } = string.Empty;
        public long Score { get; set; }
        public bool IsMe { get; set; }
        public string Icon { get; set; } = string.Empty;
        public string Frame { get; set; } = string.Empty;
    }

    public class ProfileResult
    {
        public bool Saved { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class PlayerProfile
    {
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Frame { get; set; } = string.Empty;
    }

    public class BoardResult
    {
        public List<BoardRow> Top { get; set; } = new();
        public BoardRow Me { get; set; }
    }

    public class ModuleConfig : ICloudCodeSetup
    {
        public void Setup(ICloudCodeConfig config) => config.Dependencies.AddSingleton(GameApiClient.Create());
    }
}
