using System.Collections.Generic;

namespace Modules.GameplayModule.Shared.Data.ValueObjects
{
    /// <summary>
    /// A promo code's outcome, for the screen that asked and for the profile: what was paid, or why nothing was
    /// (invalid, expired, used_up, already, offline). Items are icon ids, and frame ids with "frame:" in front.
    /// </summary>
    public sealed class PromoResultRVO
    {
        public readonly bool Ok;
        public readonly string Reason;
        public readonly int Coins;
        public readonly int Lives;
        public readonly int Boosters;
        public readonly IReadOnlyList<string> Items;

        public PromoResultRVO(bool ok, string reason, int coins = 0, int lives = 0, int boosters = 0, IReadOnlyList<string> items = null)
        {
            Ok = ok;
            Reason = reason ?? string.Empty;
            Coins = coins;
            Lives = lives;
            Boosters = boosters;
            Items = items ?? new List<string>();
        }
    }
}
