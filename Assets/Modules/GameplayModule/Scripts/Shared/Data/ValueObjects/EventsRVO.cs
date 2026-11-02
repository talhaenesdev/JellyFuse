using System.Collections.Generic;

namespace Modules.GameplayModule.Shared.Data.ValueObjects
{
    /// <summary>How the daily events stand now: the weekend bonus, the win streak, the login days and today's quests.</summary>
    public sealed class EventsRVO
    {
        /// <summary>What a win's coins are multiplied by today; 1 when it is no bonus day.</summary>
        public int WeekendMultiplier = 1;
        /// <summary>Levels won in a row.</summary>
        public int WinStreak;
        /// <summary>The streak's tier, 0 for none, up to CD_Events' tier count.</summary>
        public int WinStreakTier;
        /// <summary>Extra coins a win pays at this streak, in percent.</summary>
        public int WinStreakBonusPercent;
        public IReadOnlyList<LoginDayRVO> LoginDays = new List<LoginDayRVO>();
        public bool LoginClaimable;
        public IReadOnlyList<QuestSVO> Quests = new List<QuestSVO>();
        public bool QuestClaimable;
        /// <summary>When today's quests are replaced, in Unix seconds: the player's next midnight.</summary>
        public long QuestsResetAt;
    }
}
