using System.Collections.Generic;

namespace Modules.GameplayModule.Models
{
    /// <summary>A Speed Run in play: its levels in order, the one being played, and the clock it carries from level to level.</summary>
    public interface ISpeedRunModel
    {
        bool Active { get; }
        IReadOnlyList<int> Levels { get; }
        /// <summary>The level in play, from 0.</summary>
        int Step { get; }
        /// <summary>Seconds the run has left, as the next level starts.</summary>
        float Seconds { get; }
        /// <summary>Whether this is the day's first run - the one that pays the bonus.</summary>
        bool FirstTry { get; }
        int CurrentLevel { get; }
        bool IsLast { get; }
        void Begin(IReadOnlyList<int> levels, float seconds, bool firstTry);
        /// <summary>The level in play was won with this much time left: the next one starts with it.</summary>
        void Advance(float secondsLeft);
        void End();
    }
}
