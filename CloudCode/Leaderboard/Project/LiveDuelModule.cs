using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;
using Unity.Services.CloudSave.Model;

namespace Leaderboard
{
    /// <summary>
    /// The live duel: two players race the same level at the same time. Everything between them goes as push messages
    /// to each player's game - the invite, the start (sent to both at once, with the countdown to wait), each jelly the
    /// other cleared, and the other finishing or leaving. The match itself is kept in private custom data, so a message
    /// is only ever sent to the two players of the match.
    /// </summary>
    public class LiveDuelModule
    {
        private const string CustomId = "duel-live";
        /// <summary>How long an invite waits for an answer.</summary>
        private const int InviteSeconds = 30;
        /// <summary>The countdown both games run before GO, long enough for both messages to arrive.</summary>
        private const int StartDelayMs = 4500;

        private readonly ILogger<LiveDuelModule> _logger;
        public LiveDuelModule(ILogger<LiveDuelModule> logger) => _logger = logger;

        /// <summary>The caller invites a player whose game is open to race a level now; answers the match id, or empty.</summary>
        [CloudCodeFunction("InviteLive")]
        public async Task<LiveReply> InviteLive(IExecutionContext context, IGameApiClient api, IPushClient push, string targetId, int level)
        {
            if (string.IsNullOrEmpty(targetId) || targetId == context.PlayerId || level < 1) return LiveReply.No("bad invite");
            var profile = await Profile(context, api, context.PlayerId);
            var match = new LiveMatch
            {
                Id = Guid.NewGuid().ToString("N"), A = context.PlayerId, B = targetId, Level = level,
                AName = profile.Name, AIcon = profile.Icon, State = "invited", CreatedAt = Now(),
            };
            await Save(context, api, match);
            await Send(context, push, targetId, "duel.invite", new { matchId = match.Id, fromName = match.AName, fromIcon = match.AIcon, level, seconds = InviteSeconds });
            return new LiveReply { Ok = true, MatchId = match.Id };
        }

        /// <summary>The invited player answers. Yes, and both games are told to count down and start together.</summary>
        [CloudCodeFunction("AnswerLive")]
        public async Task<LiveReply> AnswerLive(IExecutionContext context, IGameApiClient api, IPushClient push, string matchId, bool accept)
        {
            var match = await Load(context, api, matchId);
            if (match == null || match.B != context.PlayerId || match.State != "invited") return LiveReply.No("no such invite");
            if (Now() - match.CreatedAt > InviteSeconds) { match.State = "expired"; await Save(context, api, match); return LiveReply.No("expired"); }
            if (!accept)
            {
                match.State = "declined";
                await Save(context, api, match);
                await Send(context, push, match.A, "duel.declined", new { matchId });
                return new LiveReply { Ok = true, MatchId = matchId };
            }
            var profile = await Profile(context, api, context.PlayerId);
            match.BName = profile.Name;
            match.BIcon = profile.Icon;
            match.State = "racing";
            await Save(context, api, match);
            await Send(context, push, match.A, "duel.start", new { matchId, level = match.Level, delayMs = StartDelayMs, opponentName = match.BName, opponentIcon = match.BIcon, opponentId = match.B });
            await Send(context, push, match.B, "duel.start", new { matchId, level = match.Level, delayMs = StartDelayMs, opponentName = match.AName, opponentIcon = match.AIcon, opponentId = match.A });
            return new LiveReply { Ok = true, MatchId = matchId };
        }

        /// <summary>The inviter changed their mind before an answer: the invite is withdrawn and its player told.</summary>
        [CloudCodeFunction("CancelLive")]
        public async Task<LiveReply> CancelLive(IExecutionContext context, IGameApiClient api, IPushClient push, string matchId)
        {
            var match = await Load(context, api, matchId);
            if (match == null || match.A != context.PlayerId || match.State != "invited") return LiveReply.No("nothing to cancel");
            match.State = "cancelled";
            await Save(context, api, match);
            await Send(context, push, match.B, "duel.cancelled", new { matchId });
            return new LiveReply { Ok = true, MatchId = matchId };
        }

        /// <summary>The caller cleared another jelly: the other player's bar moves.</summary>
        [CloudCodeFunction("ProgressLive")]
        public async Task<LiveReply> ProgressLive(IExecutionContext context, IGameApiClient api, IPushClient push, string matchId, int done, int total)
        {
            var match = await Load(context, api, matchId);
            if (match == null || match.State != "racing") return LiveReply.No("not racing");
            string other = Other(match, context.PlayerId);
            if (other == null) return LiveReply.No("not in match");
            await Send(context, push, other, "duel.progress", new { matchId, done, total });
            return new LiveReply { Ok = true, MatchId = matchId };
        }

        /// <summary>
        /// The caller's race ended: cleared in <paramref name="seconds"/>, or not cleared (-1). The first to clear wins; the
        /// other player is told at once. A player who leaves the race loses it.
        /// </summary>
        [CloudCodeFunction("FinishLive")]
        public async Task<LiveReply> FinishLive(IExecutionContext context, IGameApiClient api, IPushClient push, string matchId, float seconds)
        {
            var match = await Load(context, api, matchId);
            if (match == null) return LiveReply.No("no such match");
            string other = Other(match, context.PlayerId);
            if (other == null) return LiveReply.No("not in match");
            bool won = match.State == "racing" && seconds >= 0;
            if (match.State == "racing")
            {
                match.State = "done";
                match.Winner = won ? context.PlayerId : other;
                await Save(context, api, match);
                await Send(context, push, other, "duel.over", new { matchId, youWon = !won, opponentSeconds = seconds });
            }
            return new LiveReply { Ok = true, MatchId = matchId, Won = match.Winner == context.PlayerId };
        }

        private const string OnlineId = "duel-online";
        private const string RosterKey = "roster";
        /// <summary>A game that beat this recently counts as open; the heartbeat comes every 90 seconds.</summary>
        private const int OnlineSeconds = 180;

        /// <summary>
        /// The heartbeat files the caller as open, with the highest level they have won, so a duel search can find them.
        /// The roster is one item written under its write lock and pruned of the players who went quiet.
        /// </summary>
        public static async Task MarkOnline(IExecutionContext context, IGameApiClient api, int level)
        {
            for (int attempt = 0; attempt < 3; attempt++)
            {
                try
                {
                    var (roster, writeLock) = await ReadRoster(context, api);
                    long now = Now();
                    var keep = new Dictionary<string, OnlineEntry>();
                    foreach (var pair in roster)
                        if (now - pair.Value.At <= OnlineSeconds) keep[pair.Key] = pair.Value;
                    keep[context.PlayerId] = new OnlineEntry { At = now, Level = level };
                    await api.CloudSaveData.SetPrivateCustomItemAsync(context, context.ServiceToken, context.ProjectId, OnlineId,
                        new SetItemBody(RosterKey, keep, writeLock));
                    return;
                }
                catch (Exception)
                {
                    // Another beat wrote first: read again and retry.
                }
            }
        }

        /// <summary>
        /// A player whose game is open now and who has won at least <paramref name="minLevel"/> levels, picked at random,
        /// for the caller to invite; Ok false when nobody is.
        /// </summary>
        [CloudCodeFunction("FindLive")]
        public async Task<LiveReply> FindLive(IExecutionContext context, IGameApiClient api, int minLevel)
        {
            var (roster, _) = await ReadRoster(context, api);
            long now = Now();
            var open = new List<KeyValuePair<string, OnlineEntry>>();
            foreach (var pair in roster)
                if (pair.Key != context.PlayerId && now - pair.Value.At <= OnlineSeconds && pair.Value.Level >= minLevel) open.Add(pair);
            if (open.Count == 0) return LiveReply.No("nobody online");
            var pick = open[Random.Shared.Next(open.Count)];
            var profile = await Profile(context, api, pick.Key);
            return new LiveReply { Ok = true, TargetId = pick.Key, Name = profile.Name, Icon = profile.Icon, Level = pick.Value.Level };
        }

        private static async Task<(Dictionary<string, OnlineEntry>, string)> ReadRoster(IExecutionContext context, IGameApiClient api)
        {
            var response = await api.CloudSaveData.GetPrivateCustomItemsAsync(context, context.ServiceToken, context.ProjectId, OnlineId, new List<string> { RosterKey });
            var items = response.Data?.Results;
            if (items == null || items.Count == 0 || items[0].Value == null) return (new Dictionary<string, OnlineEntry>(), null);
            var roster = JsonConvert.DeserializeObject<Dictionary<string, OnlineEntry>>(items[0].Value.ToString()) ?? new Dictionary<string, OnlineEntry>();
            return (roster, items[0].WriteLock);
        }

        private static string Other(LiveMatch match, string playerId) => match.A == playerId ? match.B : match.B == playerId ? match.A : null;

        private async Task Send(IExecutionContext context, IPushClient push, string playerId, string type, object body)
        {
            try { await push.SendPlayerMessageAsync(context, JsonConvert.SerializeObject(body), type, playerId); }
            catch (Exception exception) { _logger.LogError("LiveDuel - {Type} could not reach {Player}: {Message}", type, playerId, exception.Message); }
        }

        private static long Now() => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        private static async Task<LiveMatch> Load(IExecutionContext context, IGameApiClient api, string matchId)
        {
            if (string.IsNullOrEmpty(matchId)) return null;
            var response = await api.CloudSaveData.GetPrivateCustomItemsAsync(context, context.ServiceToken, context.ProjectId, CustomId, new List<string> { matchId });
            var items = response.Data?.Results;
            if (items == null || items.Count == 0 || items[0].Value == null) return null;
            return JsonConvert.DeserializeObject<LiveMatch>(items[0].Value.ToString());
        }

        private static Task Save(IExecutionContext context, IGameApiClient api, LiveMatch match) =>
            api.CloudSaveData.SetPrivateCustomItemAsync(context, context.ServiceToken, context.ProjectId, CustomId, new SetItemBody(match.Id, match));

        private static async Task<PlayerProfile> Profile(IExecutionContext context, IGameApiClient api, string playerId)
        {
            try
            {
                var response = await api.CloudSaveData.GetPublicItemsAsync(context, context.ServiceToken, context.ProjectId, playerId, new List<string> { "profile" });
                var items = response.Data?.Results;
                if (items != null && items.Count > 0 && items[0].Value != null)
                {
                    var profile = JsonConvert.DeserializeObject<PlayerProfile>(items[0].Value.ToString());
                    if (profile != null && !string.IsNullOrWhiteSpace(profile.Name)) return profile;
                }
            }
            catch (Exception)
            {
                // No profile: a plain name.
            }
            return new PlayerProfile { Name = "Player" };
        }
    }

    public class LiveMatch
    {
        public string Id { get; set; } = string.Empty;
        public string A { get; set; } = string.Empty;
        public string B { get; set; } = string.Empty;
        public string AName { get; set; } = string.Empty;
        public string AIcon { get; set; } = string.Empty;
        public string BName { get; set; } = string.Empty;
        public string BIcon { get; set; } = string.Empty;
        public int Level { get; set; }
        public string State { get; set; } = string.Empty;
        public long CreatedAt { get; set; }
        public string Winner { get; set; } = string.Empty;
    }

    public class OnlineEntry
    {
        public long At { get; set; }
        public int Level { get; set; }
    }

    public class LiveReply
    {
        public bool Ok { get; set; }
        public string TargetId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public int Level { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string MatchId { get; set; } = string.Empty;
        public bool Won { get; set; }

        public static LiveReply No(string reason) => new() { Ok = false, Reason = reason };
    }
}
