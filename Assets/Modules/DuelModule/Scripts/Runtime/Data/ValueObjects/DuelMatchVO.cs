using System.Collections.Generic;

namespace Modules.DuelModule.Data.ValueObjects
{
    /// <summary>The opponent the server found: a recorded run of another player, or a bot the game paces itself.</summary>
    public sealed class DuelMatchVO
    {
        public bool Found { get; set; }
        /// <summary>No recorded run fitted: Exits is empty and the game paces the opponent from the level's clock.</summary>
        public bool Bot { get; set; }
        /// <summary>The level to race, counted from 1.</summary>
        public int Level { get; set; }
        public string Name { get; set; } = string.Empty;
        /// <summary>The opponent's profile icon id.</summary>
        public string Icon { get; set; } = string.Empty;
        /// <summary>The second, from GO, each of the opponent's jellies left the board, in order.</summary>
        public List<float> Exits { get; set; } = new();
        public float Seconds { get; set; }
    }
}
