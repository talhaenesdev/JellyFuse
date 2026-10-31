namespace Modules.GameplayModule.Shared.Data.ValueObjects
{
    /// <summary>The rewarded ads the result panels offer right now.</summary>
    public sealed class AdOffersRVO
    {
        public static readonly AdOffersRVO None = new();

        /// <summary>The win panel offers the win's coins multiplied by <see cref="Multiplier"/> for an ad.</summary>
        public bool DoubleReward;
        public int Multiplier;
        public string DoubleRewardKey;
        /// <summary>The lose panel offers <see cref="ExtraSeconds"/> more for an ad.</summary>
        public bool ExtraTime;
        public int ExtraSeconds;
        public string ExtraTimeKey;
    }
}
