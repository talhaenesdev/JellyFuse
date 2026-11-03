namespace Modules.GameplayModule.Shared.Data.ValueObjects
{
    /// <summary>A spin of the slot machine: the symbol each reel stops on, left to right, and what the spin gave.</summary>
    public sealed class SlotResultRVO
    {
        /// <summary>Positions in CD_Events' slot symbols, one per reel.</summary>
        public int[] Reels = new int[3];
        public RewardCVO Reward = new();
        /// <summary>All three reels alike.</summary>
        public bool Jackpot;
    }
}
