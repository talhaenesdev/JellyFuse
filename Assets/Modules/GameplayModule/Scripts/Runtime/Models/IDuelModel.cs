using System.Collections.Generic;

namespace Modules.GameplayModule.Models
{
    /// <summary>The duel being played: its level, the opponent's run, and the player's own as it goes.</summary>
    public interface IDuelModel
    {
        /// <summary>A duel was picked and its level is the one to play.</summary>
        bool Active { get; }
        /// <summary>Position in CD_JellyLevels of the duel level.</summary>
        int LevelIndex { get; }
        string OpponentName { get; }
        string OpponentIcon { get; }
        IReadOnlyList<float> OpponentExits { get; }
        /// <summary>The opponent's time to clear the board.</summary>
        float OpponentSeconds { get; }
        /// <summary>Realtime (Time.realtimeSinceStartup) the race starts at, after the countdown.</summary>
        float RaceStart { get; }
        /// <summary>The second, from GO, each of the player's jellies left the board.</summary>
        IReadOnlyList<float> Exits { get; }
        /// <summary>The race has been judged.</summary>
        bool Finished { get; }

        /// <summary>A challenge is being set for this player: the run is sent to them, nobody is raced.</summary>
        string ChallengeTargetId { get; }
        /// <summary>A challenge received is being raced: the result is answered to it.</summary>
        string ChallengeId { get; }
        /// <summary>Challenges waiting for this player, by id.</summary>
        Dictionary<string, Modules.DuelModule.Data.ValueObjects.DuelChallengeVO> Pending { get; }
        /// <summary>Challenge ids already put in front of the player.</summary>
        HashSet<string> Shown { get; }
        /// <summary>Outcomes of sent challenges not shown yet - held while a level is played.</summary>
        Queue<Modules.DuelModule.Data.ValueObjects.ChallengeOutcomeVO> Outcomes { get; }
        /// <summary>The live match being raced, or waited on; empty when the race is not live.</summary>
        string LiveMatchId { get; }
        /// <summary>The live race is on: the other player is racing the same board right now.</summary>
        bool Live { get; }
        /// <summary>Seconds of countdown the live start asked for.</summary>
        float LiveCountdown { get; }
        /// <summary>A live invite was sent and its answer is awaited.</summary>
        bool AwaitingLive { get; }
        /// <summary>Live invites received, by match id: who sent them and the level.</summary>
        Dictionary<string, Modules.DuelModule.Data.ValueObjects.DuelChallengeVO> LiveInvites { get; }
        void AwaitLive(string matchId);
        void StopAwaitingLive();
        /// <summary>The player of the last live race - kept after it ends, so Play again can invite them.</summary>
        Modules.GameplayModule.Shared.Data.ValueObjects.DuelTargetVO RematchTarget { get; }
        void SetRematchTarget(Modules.GameplayModule.Shared.Data.ValueObjects.DuelTargetVO target);
        /// <summary>Play again was pressed: the invite goes out once the finished level has been left.</summary>
        bool RematchRequested { get; set; }
        /// <summary>The live invite this player withdrew: a start that still arrives for it is not raced.</summary>
        string CancelledMatchId { get; set; }
        /// <summary>The player turned an invite down: no new invite goes to them until <paramref name="until"/>.</summary>
        void MarkDeclined(string playerId, float until);
        bool IsDeclined(string playerId, float now);
        /// <summary>The live start arrived: this match is raced now, after <paramref name="countdown"/> seconds.</summary>
        void StartLive(string matchId, float countdown);
        void SetChallengeTarget(string playerId);
        void SetChallenge(string challengeId);

        void Choose(int levelIndex, string opponentName, string opponentIcon, IReadOnlyList<float> opponentExits);
        /// <summary>The race starts <paramref name="countdown"/> seconds after <paramref name="now"/>; the player's run starts empty.</summary>
        void StartRace(float now, float countdown);
        /// <summary>Records exits up to <paramref name="count"/> at <paramref name="now"/>; false when none were new.</summary>
        bool RecordExits(int count, float now);
        void Finish();
        void End();
    }
}
