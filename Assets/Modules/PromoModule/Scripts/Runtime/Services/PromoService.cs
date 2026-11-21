using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FlowIoC.ConsoleModule;
using Modules.PromoModule.Data.ValueObjects;
using Unity.Services.Authentication;
using Unity.Services.CloudCode;
using Unity.Services.Core;

namespace Modules.PromoModule.Services
{
    public sealed class PromoService : IPromoService
    {
        private const string MODULE = "Leaderboard";
        private const string REDEEM = "RedeemCode";
        private static readonly TimeSpan TIMEOUT = TimeSpan.FromSeconds(20);

        public async Task<PromoRewardVO> RedeemAsync(string code)
        {
            if (UnityServices.State != ServicesInitializationState.Initialized || !AuthenticationService.Instance.IsSignedIn)
                return PromoRewardVO.No("offline");
            try
            {
                var call = CloudCodeService.Instance.CallModuleEndpointAsync<PromoRewardVO>(MODULE, REDEEM,
                    new Dictionary<string, object> { { "code", code ?? string.Empty } });
                var finished = await Task.WhenAny(call, Task.Delay(TIMEOUT));
                if (finished != call)
                {
                    _ = call.ContinueWith(late => _ = late.Exception, TaskContinuationOptions.OnlyOnFaulted);
                    return PromoRewardVO.No("offline");
                }
                return await call ?? PromoRewardVO.No("offline");
            }
            catch (Exception exception)
            {
                FlowLogger.Log($"PromoService - the code did not reach the server: {exception.Message}");
                return PromoRewardVO.No("offline");
            }
        }
    }
}
