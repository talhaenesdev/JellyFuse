using System.Threading.Tasks;
using Modules.PromoModule.Data.ValueObjects;

namespace Modules.PromoModule.Services
{
    /// <summary>Redeems promo codes on the server. Never throws: offline or not signed in answers Reason "offline".</summary>
    public interface IPromoService
    {
        /// <summary>Sends a code; the answer says what it pays, which the caller hands out.</summary>
        Task<PromoRewardVO> RedeemAsync(string code);
    }
}
