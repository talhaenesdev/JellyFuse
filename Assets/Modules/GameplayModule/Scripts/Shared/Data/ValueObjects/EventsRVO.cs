using System.Collections.Generic;

namespace Modules.GameplayModule.Shared.Data.ValueObjects
{
    /// <summary>How the daily events stand now: the weekend bonus, the win streak, the login days, today's quests and the prize wheel.</summary>
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
        /// <summary>The prize wheel's slices clockwise from the top.</summary>
        public IReadOnlyList<WheelSliceCVO> Wheel = new List<WheelSliceCVO>();
        /// <summary>Spins the player can take now: today's free one while it is left, and those ads gave. 0: an ad gives one.</summary>
        public int WheelSpins;
        /// <summary>Slot machine spins the player can take now; 0: an ad gives one.</summary>
        public int SlotSpins;
        /// <summary>Scratch cards the player can take now; 0: an ad gives one.</summary>
        public int ScratchCards;
        /// <summary>Whether today's daily challenge is there to play: a level is set for today and it is not won yet.</summary>
        public bool DailyChallengeReady;
        /// <summary>What winning today's daily challenge gives.</summary>
        public RewardCVO DailyChallengeReward;
        /// <summary>Speed Runs the player may start now.</summary>
        public int SpeedRunTriesLeft;
        /// <summary>More runs rewarded ads can still give today.</summary>
        public int SpeedRunAdLeft;
        public bool SpeedRunWon;
        public int SpeedRunLevels;
        /// <summary>The level the player is on, counted from 1.</summary>
        public int Level;
        /// <summary>The level Star Rush and Color Carnival can be played from.</summary>
        public int EventsUnlockLevel;
        /// <summary>The level the Speed Run can be played from.</summary>
        public int SpeedRunUnlockLevel;
        public int SpeedRunCoins;
        public int SpeedRunRockets;
        public int SpeedRunFreezes;
        /// <summary>The colour today's carnival asks for.</summary>
        public Modules.GameplayModule.Shared.Enums.JellyColour CarnivalColour;
        /// <summary>Cells of that colour taken through doors today.</summary>
        public int CarnivalPoints;
        /// <summary>Today's chests already opened, from the first.</summary>
        public int CarnivalOpened;
        public IReadOnlyList<CarnivalChestCVO> CarnivalChests = new List<CarnivalChestCVO>();
        /// <summary>A chest reached waits to be opened.</summary>
        public bool CarnivalClaimable;
        /// <summary>When today's carnival ends, unix seconds.</summary>
        public long CarnivalEndsAt;
        /// <summary>Stars won this week on normal levels.</summary>
        public int StarRushStars;
        /// <summary>The meter's steps already paid, from the first.</summary>
        public int StarRushClaimed;
        public IReadOnlyList<StarRushMilestoneCVO> StarRushMilestones = new List<StarRushMilestoneCVO>();
        /// <summary>A reached step waits to be paid.</summary>
        public bool StarRushClaimable;
        /// <summary>When this week's meter empties, unix seconds.</summary>
        public long StarRushEndsAt;
    }
}
