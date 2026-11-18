using FlowIoC.BaseModule.Signals;
using Modules.GameplayModule.Shared.Data.ValueObjects;

namespace Modules.GameplayModule.DuelScreenModule.Signals
{
    public class DuelScreenSignals : ISignalHolder
    {
        public DuelScreenSignalsIncoming Incoming = new();
        public DuelScreenSignalsOutgoing Outgoing = new();
    }

    public class DuelScreenSignalsIncoming
    {
        /// <summary>The player asked for a duel: the search panel shows while an opponent is found.</summary>
        public Signal OpenSearch = new();
        /// <summary>No opponent was found: the search panel says so and offers to close.</summary>
        public Signal ShowUnavailable = new();
        /// <summary>The duel level is on the board: the countdown runs, then the race bar.</summary>
        public Signal<DuelRaceRVO> StartRace = new();
        /// <summary>The player's share of the board cleared, 0 to 1.</summary>
        public Signal<float> ApplyProgress = new();
        public Signal<DuelResultRVO> ShowResult = new();
        /// <summary>The duel level was left.</summary>
        public Signal Close = new();
        /// <summary>A challenge waits for this player: Accept or Decline.</summary>
        public Signal<ChallengeInviteRVO> ShowChallenge = new();
        /// <summary>A challenge this player sent was answered.</summary>
        public Signal<ChallengeOutcomeRVO> ShowOutcome = new();
    }

    public class DuelScreenSignalsOutgoing
    {
        /// <summary>The player closed the search panel when no opponent was found.</summary>
        public Signal SearchClosed = new();
        /// <summary>A challenge received, by id, was accepted.</summary>
        public Signal<string> ChallengeAccepted = new();
        public Signal<string> ChallengeDeclined = new();
    }
}
