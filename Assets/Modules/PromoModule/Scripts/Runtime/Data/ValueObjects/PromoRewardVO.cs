using System.Collections.Generic;

namespace Modules.PromoModule.Data.ValueObjects
{
    /// <summary>What a promo code pays, or why it pays nothing: invalid, expired, used_up, already, offline.</summary>
    public sealed class PromoRewardVO
    {
        public bool Ok { get; set; }
        public string Reason { get; set; } = string.Empty;
        public int Coins { get; set; }
        public int Lives { get; set; }
        public int Rockets { get; set; }
        public int Freezes { get; set; }
        public int Ufos { get; set; }
        public List<string> Icons { get; set; } = new();
        public List<string> Frames { get; set; } = new();

        public static PromoRewardVO No(string reason) => new() { Ok = false, Reason = reason };
    }
}
