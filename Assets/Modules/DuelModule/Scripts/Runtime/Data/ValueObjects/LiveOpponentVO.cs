namespace Modules.DuelModule.Data.ValueObjects
{
    /// <summary>A player whose game is open now, found to be invited to a live race.</summary>
    public sealed class LiveOpponentVO
    {
        public string PlayerId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        /// <summary>The highest level they have won, counted from 1.</summary>
        public int Level { get; set; }
    }
}
