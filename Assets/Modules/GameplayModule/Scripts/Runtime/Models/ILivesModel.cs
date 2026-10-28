using Modules.GameplayModule.Shared.Data.UnityObjects;
using Modules.GameplayModule.Shared.Data.ValueObjects;

namespace Modules.GameplayModule.Models
{
    /// <summary>
    /// The rules of the player's lives, kept in SD_JellyProgress: start lives given once, a refill
    /// every CD_Lives.RefillMinutes until the player holds MaxLives. Times are Unix seconds.
    /// </summary>
    public interface ILivesModel
    {
        /// <summary>Gives the start lives the first time, and the refills due by <paramref name="now"/>.</summary>
        void Refresh(SD_JellyProgress progress, CD_Lives config, long now);
        /// <summary>One life fewer; the refill clock starts when it was not running.</summary>
        void Lose(SD_JellyProgress progress, CD_Lives config, long now);
        /// <summary>Adds lives, never past MaxLives.</summary>
        void Add(SD_JellyProgress progress, CD_Lives config, int lives, long now);
        /// <summary>Back to full, as after a whole wait.</summary>
        void Fill(SD_JellyProgress progress, CD_Lives config);
        LivesStateRVO State(SD_JellyProgress progress, CD_Lives config, long now);
        /// <summary>The level start held back for want of a life, run once one is bought; None when nothing waits.</summary>
        Enums.WaitingLevelStart WaitingStart { get; set; }
    }
}
