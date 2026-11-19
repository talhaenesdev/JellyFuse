using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;
using Unity.Services.CloudSave.Model;

namespace Leaderboard
{
    /// <summary>
    /// The ghost race: a player races the recorded run of another player on the same level. Runs are kept per level in
    /// private custom data the game cannot write; FindDuel hands out one of them, SubmitDuel files a finished run. When
    /// no run of somebody else fits, FindDuel answers a bot and the game paces it from the level's own clock.
    /// </summary>
    public class DuelModule
    {
        private const string CustomId = "duel-ghosts";
        private const string LevelsKey = "levels";
        private const int GhostsPerLevel = 20;
        private const int MaxLevel = 5000;
        private const int MaxExits = 200;
        private const float MinSeconds = 3f;
        private const float MaxSeconds = 900f;
        private const int Window = 40;

        private static readonly string[] BotNames =
        {
            "JellyBean", "Wobbly", "Gummy", "Squishy", "Bubbles", "Fizzy", "Puddin", "Sprinkle", "Mochi", "Boba",
            "Taffy", "Noodle", "Pickle", "Peaches", "Muffin", "Biscuit", "Cocoa", "Waffles", "Marsh", "Bloop",
        };
        private static readonly string[] BotIcons = { "pink", "orange", "purple", "green", "red", "blue", "penguin", "mint", "yellow", "owl", "dragon", "jelly" };

        private readonly ILogger<DuelModule> _logger;
        public DuelModule(ILogger<DuelModule> logger) => _logger = logger;

        /// <summary>
        /// An opponent for a player who has reached <paramref name="maxLevel"/>: a recorded run of another player on a
        /// level up to it, the most recent levels first, or a bot on a level near it.
        /// </summary>
        [CloudCodeFunction("FindDuel")]
        public async Task<DuelMatch> FindDuel(IExecutionContext context, IGameApiClient api, int maxLevel)
        {
            maxLevel = Math.Clamp(maxLevel, 1, MaxLevel);
            var random = new Random();
            try
            {
                var levels = await Read<List<int>>(context, api, LevelsKey) ?? new List<int>();
                var fits = levels.Where(level => level <= maxLevel && level > maxLevel - Window).OrderBy(_ => random.Next()).ToList();
                foreach (int level in fits)
                {
                    var ghosts = await Read<List<DuelGhost>>(context, api, GhostKey(level)) ?? new List<DuelGhost>();
                    var others = ghosts.Where(ghost => ghost.PlayerId != context.PlayerId).ToList();
                    if (others.Count == 0) continue;
                    var picked = others[random.Next(others.Count)];
                    return new DuelMatch
                    {
                        Found = true, Level = level, Name = picked.Name, Icon = picked.Icon, Exits = picked.Exits, Seconds = picked.Seconds, Fp = picked.Fp,
                    };
                }
            }
            catch (Exception exception)
            {
                _logger.LogError("FindDuel - the ghosts could not be read: {Message}", exception.Message);
            }
            int botLevel = Math.Max(1, maxLevel - random.Next(Math.Min(Window, maxLevel)));
            return new DuelMatch
            {
                Found = true, Bot = true, Level = botLevel,
                Name = BotNames[random.Next(BotNames.Length)], Icon = BotIcons[random.Next(BotIcons.Length)],
            };
        }

        /// <summary>
        /// Files a won duel run so later players can race it: the second of every jelly that left the board, in order.
        /// A run whose seconds are out of order or out of range is refused.
        /// </summary>
        [CloudCodeFunction("SubmitDuel")]
        public async Task<DuelSubmitResult> SubmitDuel(IExecutionContext context, IGameApiClient api, int level, List<float> exits, string fp)
        {
            if (level < 1 || level > MaxLevel) return new DuelSubmitResult { Reason = "level out of range" };
            if (exits == null || exits.Count == 0 || exits.Count > MaxExits) return new DuelSubmitResult { Reason = "no exits" };
            for (int i = 0; i < exits.Count; i++)
                if (exits[i] < 0 || exits[i] > MaxSeconds || (i > 0 && exits[i] < exits[i - 1])) return new DuelSubmitResult { Reason = "bad exits" };
            float seconds = exits[exits.Count - 1];
            if (seconds < MinSeconds) return new DuelSubmitResult { Reason = "too fast" };

            var profile = await ReadProfile(context, api);
            var ghosts = await Read<List<DuelGhost>>(context, api, GhostKey(level)) ?? new List<DuelGhost>();
            // One run a player a level: the newest replaces the old.
            ghosts.RemoveAll(ghost => ghost.PlayerId == context.PlayerId);
            ghosts.Add(new DuelGhost
            {
                PlayerId = context.PlayerId, Name = profile.Name, Icon = profile.Icon, Exits = exits.Select(e => (float)Math.Round(e, 2)).ToList(), Seconds = seconds,
                Fp = (fp ?? string.Empty).Length > 64 ? fp.Substring(0, 64) : fp ?? string.Empty,
            });
            if (ghosts.Count > GhostsPerLevel) ghosts.RemoveRange(0, ghosts.Count - GhostsPerLevel);
            await Write(context, api, GhostKey(level), ghosts);

            var levels = await Read<List<int>>(context, api, LevelsKey) ?? new List<int>();
            if (!levels.Contains(level))
            {
                levels.Add(level);
                await Write(context, api, LevelsKey, levels);
            }
            _logger.LogInformation("Duel run filed on level {Level} for {PlayerId}: {Seconds}s", level, context.PlayerId, seconds);
            return new DuelSubmitResult { Accepted = true };
        }

        private const int MaxInbox = 10;
        private const int ChallengeHours = 24;

        /// <summary>
        /// A challenge to one player: the sender's cleared run on a level, waiting in that player's inbox for a day. The
        /// target finds it with GetChallenges, races it and answers; the sender then finds the outcome with GetChallenges.
        /// </summary>
        [CloudCodeFunction("SendChallenge")]
        public async Task<DuelSubmitResult> SendChallenge(IExecutionContext context, IGameApiClient api, string targetId, int level, List<float> exits)
        {
            if (string.IsNullOrEmpty(targetId) || targetId == context.PlayerId) return new DuelSubmitResult { Reason = "no target" };
            if (level < 1 || level > MaxLevel || exits == null || exits.Count == 0 || exits.Count > MaxExits) return new DuelSubmitResult { Reason = "bad run" };
            for (int i = 1; i < exits.Count; i++) if (exits[i] < exits[i - 1]) return new DuelSubmitResult { Reason = "bad exits" };
            float seconds = exits[exits.Count - 1];
            if (seconds < MinSeconds || seconds > MaxSeconds) return new DuelSubmitResult { Reason = "bad time" };
            var profile = await ReadProfile(context, api);
            var inbox = Fresh(await Read<List<DuelChallenge>>(context, api, InboxKey(targetId)) ?? new List<DuelChallenge>());
            // One waiting challenge a sender: a new one replaces the old.
            inbox.RemoveAll(c => c.FromId == context.PlayerId);
            inbox.Add(new DuelChallenge
            {
                Id = Guid.NewGuid().ToString("N"), FromId = context.PlayerId, FromName = profile.Name, FromIcon = profile.Icon,
                Level = level, Exits = exits.Select(e => (float)Math.Round(e, 2)).ToList(), Seconds = seconds,
                SentAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            });
            if (inbox.Count > MaxInbox) inbox.RemoveRange(0, inbox.Count - MaxInbox);
            await Write(context, api, InboxKey(targetId), inbox);
            return new DuelSubmitResult { Accepted = true };
        }

        /// <summary>The challenges waiting for the caller, and the outcomes of the ones the caller sent; outcomes are handed out once.</summary>
        [CloudCodeFunction("GetChallenges")]
        public async Task<ChallengeMail> GetChallenges(IExecutionContext context, IGameApiClient api)
        {
            var mail = new ChallengeMail
            {
                Challenges = Fresh(await Read<List<DuelChallenge>>(context, api, InboxKey(context.PlayerId)) ?? new List<DuelChallenge>()),
                Results = await Read<List<ChallengeOutcome>>(context, api, ResultsKey(context.PlayerId)) ?? new List<ChallengeOutcome>(),
            };
            if (mail.Results.Count > 0) await Write(context, api, ResultsKey(context.PlayerId), new List<ChallengeOutcome>());
            return mail;
        }

        /// <summary>
        /// The caller answered a challenge: declined, or raced it - <paramref name="seconds"/> their time, -1 when they did not
        /// clear the board. The challenge leaves the inbox and the sender is told.
        /// </summary>
        [CloudCodeFunction("AnswerChallenge")]
        public async Task<DuelSubmitResult> AnswerChallenge(IExecutionContext context, IGameApiClient api, string challengeId, bool declined, float seconds)
        {
            var inbox = await Read<List<DuelChallenge>>(context, api, InboxKey(context.PlayerId)) ?? new List<DuelChallenge>();
            var challenge = inbox.FirstOrDefault(c => c.Id == challengeId);
            if (challenge == null) return new DuelSubmitResult { Reason = "no such challenge" };
            inbox.Remove(challenge);
            await Write(context, api, InboxKey(context.PlayerId), inbox);
            var profile = await ReadProfile(context, api);
            var results = await Read<List<ChallengeOutcome>>(context, api, ResultsKey(challenge.FromId)) ?? new List<ChallengeOutcome>();
            results.Add(new ChallengeOutcome
            {
                OpponentName = profile.Name, OpponentIcon = profile.Icon, Level = challenge.Level, Declined = declined,
                OpponentSeconds = declined ? -1 : seconds, Seconds = challenge.Seconds,
                SenderWon = declined || seconds < 0 || seconds > challenge.Seconds,
            });
            if (results.Count > MaxInbox) results.RemoveRange(0, results.Count - MaxInbox);
            await Write(context, api, ResultsKey(challenge.FromId), results);
            return new DuelSubmitResult { Accepted = true };
        }

        private static List<DuelChallenge> Fresh(List<DuelChallenge> inbox)
        {
            long oldest = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - ChallengeHours * 3600L;
            inbox.RemoveAll(c => c.SentAt < oldest);
            return inbox;
        }

        private static string InboxKey(string playerId) => "inbox-" + playerId;
        private static string ResultsKey(string playerId) => "results-" + playerId;
        private static string GhostKey(int level) => "ghosts-" + level;

        private static async Task<T> Read<T>(IExecutionContext context, IGameApiClient api, string key) where T : class
        {
            var response = await api.CloudSaveData.GetPrivateCustomItemsAsync(context, context.ServiceToken, context.ProjectId, CustomId,
                new List<string> { key });
            var items = response.Data?.Results;
            if (items == null || items.Count == 0 || items[0].Value == null) return null;
            return JsonConvert.DeserializeObject<T>(items[0].Value.ToString());
        }

        private static Task Write(IExecutionContext context, IGameApiClient api, string key, object value) =>
            api.CloudSaveData.SetPrivateCustomItemAsync(context, context.ServiceToken, context.ProjectId, CustomId, new SetItemBody(key, value));

        private static async Task<PlayerProfile> ReadProfile(IExecutionContext context, IGameApiClient api)
        {
            try
            {
                var response = await api.CloudSaveData.GetPublicItemsAsync(context, context.ServiceToken, context.ProjectId, context.PlayerId,
                    new List<string> { "profile" });
                var items = response.Data?.Results;
                if (items != null && items.Count > 0 && items[0].Value != null)
                {
                    var profile = JsonConvert.DeserializeObject<PlayerProfile>(items[0].Value.ToString());
                    if (profile != null && !string.IsNullOrWhiteSpace(profile.Name)) return profile;
                }
            }
            catch (Exception)
            {
                // No profile: the run races under a plain name.
            }
            return new PlayerProfile { Name = "Player" };
        }
    }

    public class DuelGhost
    {
        public string PlayerId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public List<float> Exits { get; set; } = new();
        public float Seconds { get; set; }
        /// <summary>The fingerprint of the level as it was when this run was played.</summary>
        public string Fp { get; set; } = string.Empty;
    }

    public class DuelMatch
    {
        public bool Found { get; set; }
        /// <summary>No recorded run fitted: the game paces the opponent from the level's clock.</summary>
        public bool Bot { get; set; }
        public int Level { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        /// <summary>The second each of the opponent's jellies left the board; empty for a bot.</summary>
        public List<float> Exits { get; set; } = new();
        public float Seconds { get; set; }
        /// <summary>The fingerprint of the level the run was played on; the game races it only on the same level.</summary>
        public string Fp { get; set; } = string.Empty;
    }

    public class DuelChallenge
    {
        public string Id { get; set; } = string.Empty;
        public string FromId { get; set; } = string.Empty;
        public string FromName { get; set; } = string.Empty;
        public string FromIcon { get; set; } = string.Empty;
        public int Level { get; set; }
        public List<float> Exits { get; set; } = new();
        public float Seconds { get; set; }
        public long SentAt { get; set; }
    }

    public class ChallengeOutcome
    {
        public string OpponentName { get; set; } = string.Empty;
        public string OpponentIcon { get; set; } = string.Empty;
        public int Level { get; set; }
        public bool Declined { get; set; }
        /// <summary>The challenged player's time; -1 when they declined or did not clear the board.</summary>
        public float OpponentSeconds { get; set; }
        /// <summary>The sender's own time on the challenge.</summary>
        public float Seconds { get; set; }
        public bool SenderWon { get; set; }
    }

    public class ChallengeMail
    {
        public List<DuelChallenge> Challenges { get; set; } = new();
        public List<ChallengeOutcome> Results { get; set; } = new();
    }

    public class DuelSubmitResult
    {
        public bool Accepted { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
