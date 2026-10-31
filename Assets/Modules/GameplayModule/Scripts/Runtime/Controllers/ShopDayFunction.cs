using System;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>Today as the shop counts it: local days since 2000-01-01, so a once-a-day product opens again at the player's midnight.</summary>
    internal static class ShopDayFunction
    {
        private static readonly DateTime Epoch = new(2000, 1, 1);

        public static int Today() => (int)(DateTime.Now.Date - Epoch).TotalDays;
    }
}
