using System.Collections.Generic;

namespace Modules.GameplayModule.Shared.Data.ValueObjects
{
    /// <summary>The race that starts with the duel level: who the opponent is and how their run went.</summary>
    public sealed class DuelRaceRVO
    {
        public string OpponentName { get; }
        /// <summary>The opponent's profile icon id.</summary>
        public string OpponentIcon { get; }
        /// <summary>The second, from GO, each of the opponent's jellies left the board, in order.</summary>
        public IReadOnlyList<float> OpponentExits { get; }
        /// <summary>The jellies a run takes out of the board.</summary>
        public int Total { get; }
        /// <summary>Seconds of countdown before GO.</summary>
        public float Countdown { get; }
        /// <summary>The level raced, counted from 1.</summary>
        public int Level { get; }

        public DuelRaceRVO(string opponentName, string opponentIcon, IReadOnlyList<float> opponentExits, int total, float countdown, int level)
        {
            OpponentName = opponentName;
            OpponentIcon = opponentIcon;
            OpponentExits = opponentExits;
            Total = total;
            Countdown = countdown;
            Level = level;
        }
    }
}
