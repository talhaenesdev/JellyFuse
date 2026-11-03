namespace Modules.GameplayModule.Shared.Data.ValueObjects
{
    /// <summary>A scratch card dealt: what lies under each of its nine cells, which prize it won, and what it gave.</summary>
    public sealed class ScratchCardRVO
    {
        /// <summary>Positions in CD_Events' scratch prizes, row by row from the top left; -1 is an empty cell.</summary>
        public int[] Cells = new int[9];
        /// <summary>The prize three cells show; -1 when the card won none and gave the consolation.</summary>
        public int WonPrize = -1;
        public RewardCVO Reward = new();
    }
}
