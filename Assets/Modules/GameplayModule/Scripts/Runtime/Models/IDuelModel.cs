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

        void Choose(int levelIndex, string opponentName, string opponentIcon, IReadOnlyList<float> opponentExits);
        /// <summary>The race starts <paramref name="countdown"/> seconds after <paramref name="now"/>; the player's run starts empty.</summary>
        void StartRace(float now, float countdown);
        /// <summary>Records exits up to <paramref name="count"/> at <paramref name="now"/>; false when none were new.</summary>
        bool RecordExits(int count, float now);
        void Finish();
        void End();
    }
}
