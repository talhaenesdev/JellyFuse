namespace Modules.GameplayModule.Shared.Data.ValueObjects
{
    /// <summary>The player's lives as the HUD shows them.</summary>
    public sealed class LivesStateRVO
    {
        public int Lives;
        public int MaxLives;
        /// <summary>Seconds until the next life comes back; 0 while lives are full.</summary>
        public float SecondsToNext;
    }
}
