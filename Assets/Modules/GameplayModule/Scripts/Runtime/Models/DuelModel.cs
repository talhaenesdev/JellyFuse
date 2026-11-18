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

        public void SetChallengeTarget(string playerId) => ChallengeTargetId = playerId ?? string.Empty;
        public void SetChallenge(string challengeId) => ChallengeId = challengeId ?? string.Empty;

        public void Choose(int levelIndex, string opponentName, string opponentIcon, IReadOnlyList<float> opponentExits)
        {
            ChallengeTargetId = string.Empty;
            ChallengeId = string.Empty;
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
            ChallengeTargetId = string.Empty;
            ChallengeId = string.Empty;
            _exits.Clear();
        }
    }
}
