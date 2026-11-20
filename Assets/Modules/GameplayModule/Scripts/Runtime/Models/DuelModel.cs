using System.Collections.Generic;
using UnityEngine;

namespace Modules.GameplayModule.Models
{
    internal sealed class DuelModel : IDuelModel
    {
        private readonly List<float> _exits = new();
        private IReadOnlyList<float> _opponentExits = new List<float>();

        public bool Active { get; private set; }
        public int LevelIndex { get; private set; } = -1;
        public string OpponentName { get; private set; } = string.Empty;
        public string OpponentIcon { get; private set; } = string.Empty;
        public IReadOnlyList<float> OpponentExits => _opponentExits;
        public float OpponentSeconds => _opponentExits.Count > 0 ? _opponentExits[_opponentExits.Count - 1] : float.MaxValue;
        public float RaceStart { get; private set; }
        public IReadOnlyList<float> Exits => _exits;
        public bool Finished { get; private set; }
        public string ChallengeTargetId { get; private set; } = string.Empty;
        public string ChallengeId { get; private set; } = string.Empty;
        public Dictionary<string, Modules.DuelModule.Data.ValueObjects.DuelChallengeVO> Pending { get; } = new();
        public HashSet<string> Shown { get; } = new();
        public Queue<Modules.DuelModule.Data.ValueObjects.ChallengeOutcomeVO> Outcomes { get; } = new();

        public string LiveMatchId { get; private set; } = string.Empty;
        public bool Live { get; private set; }
        public float LiveCountdown { get; private set; }
        public bool AwaitingLive { get; private set; }
        public Dictionary<string, Modules.DuelModule.Data.ValueObjects.DuelChallengeVO> LiveInvites { get; } = new();

        public void AwaitLive(string matchId)
        {
            LiveMatchId = matchId ?? string.Empty;
            AwaitingLive = !string.IsNullOrEmpty(LiveMatchId);
        }

        public void StopAwaitingLive() => AwaitingLive = false;

        public Modules.GameplayModule.Shared.Data.ValueObjects.DuelTargetVO RematchTarget { get; private set; }
        public void SetRematchTarget(Modules.GameplayModule.Shared.Data.ValueObjects.DuelTargetVO target) => RematchTarget = target;
        public bool RematchRequested { get; set; }
        public string CancelledMatchId { get; set; } = string.Empty;
        public bool Searching { get; set; }

        private readonly Dictionary<string, float> _declinedUntil = new();

        public void MarkDeclined(string playerId, float until)
        {
            if (!string.IsNullOrEmpty(playerId)) _declinedUntil[playerId] = until;
        }

        public bool IsDeclined(string playerId, float now) =>
            !string.IsNullOrEmpty(playerId) && _declinedUntil.TryGetValue(playerId, out float until) && now < until;

        public void StartLive(string matchId, float countdown)
        {
            LiveMatchId = matchId ?? string.Empty;
            Live = true;
            AwaitingLive = false;
            LiveCountdown = Mathf.Max(0, countdown);
        }

        public void SetChallengeTarget(string playerId) => ChallengeTargetId = playerId ?? string.Empty;
        public void SetChallenge(string challengeId) => ChallengeId = challengeId ?? string.Empty;

        public void Choose(int levelIndex, string opponentName, string opponentIcon, IReadOnlyList<float> opponentExits)
        {
            ChallengeTargetId = string.Empty;
            ChallengeId = string.Empty;
            RematchTarget = null;
            RematchRequested = false;
            Active = true;
            LevelIndex = levelIndex;
            OpponentName = opponentName ?? string.Empty;
            OpponentIcon = opponentIcon ?? string.Empty;
            _opponentExits = opponentExits ?? new List<float>();
            Finished = false;
            _exits.Clear();
        }

        public void StartRace(float now, float countdown)
        {
            RaceStart = now + Mathf.Max(0, countdown);
            Finished = false;
            _exits.Clear();
        }

        public bool RecordExits(int count, float now)
        {
            if (!Active || Finished || count <= _exits.Count) return false;
            float second = Mathf.Max(0, now - RaceStart);
            while (_exits.Count < count) _exits.Add(second);
            return true;
        }

        public void Finish() => Finished = true;

        public void End()
        {
            Active = false;
            Finished = false;
            Searching = false;
            ChallengeTargetId = string.Empty;
            ChallengeId = string.Empty;
            Live = false;
            AwaitingLive = false;
            LiveMatchId = string.Empty;
            _exits.Clear();
        }
    }
}
