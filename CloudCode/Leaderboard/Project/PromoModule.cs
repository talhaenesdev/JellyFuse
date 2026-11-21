using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;
using Unity.Services.CloudSave.Model;

namespace Leaderboard
{
    /// <summary>
    /// Promo codes. The codes live in private custom data ("promo" / "codes"), written from the dashboard; which codes a
    /// player has used lives in their protected data, so a code is never paid twice and a player cannot write either.
    /// A code answers what it pays and the game hands it out.
    /// </summary>
    public class PromoModule
    {
        private const string CustomId = "promo";
        private const string CodesKey = "codes";
        private const string RedeemedKey = "promo_redeemed";

        [CloudCodeFunction("RedeemCode")]
        public async Task<PromoReply> RedeemCode(IExecutionContext context, IGameApiClient api, string code)
        {
            code = (code ?? string.Empty).Trim().ToUpperInvariant();
            if (code.Length == 0 || code.Length > 32) return PromoReply.No("invalid");

            var redeemed = await ReadRedeemed(context, api);
            if (redeemed.Contains(code)) return PromoReply.No("already");

            // The use is counted under the codes' write lock, so a limited code cannot be taken past its limit.
            for (int attempt = 0; attempt < 3; attempt++)
            {
                var (codes, writeLock) = await ReadCodes(context, api);
                if (!codes.TryGetValue(code, out var promo) || !promo.Active) return PromoReply.No("invalid");
                if (promo.ExpiresAt > 0 && DateTimeOffset.UtcNow.ToUnixTimeSeconds() > promo.ExpiresAt) return PromoReply.No("expired");
                if (promo.MaxUses > 0 && promo.Uses >= promo.MaxUses) return PromoReply.No("used_up");
                promo.Uses++;
                try
                {
                    await api.CloudSaveData.SetPrivateCustomItemAsync(context, context.ServiceToken, context.ProjectId, CustomId,
                        new SetItemBody(CodesKey, codes, writeLock));
                }
                catch (Exception)
                {
                    continue; // somebody else used a code at the same moment: read again
                }
                redeemed.Add(code);
                await api.CloudSaveData.SetProtectedItemAsync(context, context.ServiceToken, context.ProjectId, context.PlayerId,
                    new SetItemBody(RedeemedKey, redeemed));
                return new PromoReply
                {
                    Ok = true, Coins = promo.Coins, Lives = promo.Lives, Rockets = promo.Rockets, Freezes = promo.Freezes,
                    Ufos = promo.Ufos, Icons = promo.Icons ?? new List<string>(), Frames = promo.Frames ?? new List<string>(),
                };
            }
            return PromoReply.No("busy");
        }

        private static async Task<(Dictionary<string, PromoCode>, string)> ReadCodes(IExecutionContext context, IGameApiClient api)
        {
            var response = await api.CloudSaveData.GetPrivateCustomItemsAsync(context, context.ServiceToken, context.ProjectId, CustomId, new List<string> { CodesKey });
            var items = response.Data?.Results;
            if (items == null || items.Count == 0 || items[0].Value == null) return (new Dictionary<string, PromoCode>(), null);
            var codes = JsonConvert.DeserializeObject<Dictionary<string, PromoCode>>(items[0].Value.ToString()) ?? new Dictionary<string, PromoCode>();
            // Codes are matched without regard to case.
            var upper = new Dictionary<string, PromoCode>();
            foreach (var pair in codes) upper[pair.Key.Trim().ToUpperInvariant()] = pair.Value;
            return (upper, items[0].WriteLock);
        }

        private static async Task<List<string>> ReadRedeemed(IExecutionContext context, IGameApiClient api)
        {
            var response = await api.CloudSaveData.GetProtectedItemsAsync(context, context.ServiceToken, context.ProjectId, context.PlayerId, new List<string> { RedeemedKey });
            var items = response.Data?.Results;
            if (items == null || items.Count == 0 || items[0].Value == null) return new List<string>();
            return JsonConvert.DeserializeObject<List<string>>(items[0].Value.ToString()) ?? new List<string>();
        }
    }

    public class PromoCode
    {
        public bool Active { get; set; } = true;
        public int Coins { get; set; }
        public int Lives { get; set; }
        public int Rockets { get; set; }
        public int Freezes { get; set; }
        public int Ufos { get; set; }
        public List<string> Icons { get; set; } = new();
        public List<string> Frames { get; set; } = new();
        /// <summary>How many players may use it; 0 is no limit.</summary>
        public int MaxUses { get; set; }
        public int Uses { get; set; }
        /// <summary>Unix seconds after which it no longer works; 0 never expires.</summary>
        public long ExpiresAt { get; set; }
    }

    public class PromoReply
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

        public static PromoReply No(string reason) => new() { Ok = false, Reason = reason };
    }
}
