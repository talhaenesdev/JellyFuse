using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FlowIoC.ConsoleModule;
using Unity.Services.Authentication;
using Unity.Services.CloudCode;
using Unity.Services.Core;

namespace Modules.StoreModule.Services.Sub
{
    /// <summary>What the server answered about one purchase.</summary>
    public enum PurchaseVerdict
    {
        Valid = 0,
        Invalid = 1,
        // The server could not be asked; the order stays pending and the store delivers it again.
        Unreachable = 2,
    }

    /// <summary>Asks the PurchaseVerify Cloud Code module whether Google Play really sold the product.</summary>
    public class PurchaseVerifySubService
    {
        private const string Module = "PurchaseVerify";
        private const string Function = "Verify";
        private readonly TimeSpan _signInWait = TimeSpan.FromSeconds(20);

        private class VerifyResult
        {
            public bool Valid { get; set; }
            public string Reason { get; set; }
        }

        public async Task<PurchaseVerdict> Verify(string productId, string purchaseToken, bool consumable)
        {
            if (string.IsNullOrEmpty(purchaseToken)) return PurchaseVerdict.Invalid;
            try
            {
                if (!await WaitForSignIn()) return PurchaseVerdict.Unreachable;
                var result = await CloudCodeService.Instance.CallModuleEndpointAsync<VerifyResult>(Module, Function,
                    new Dictionary<string, object>
                    {
                        { "productId", productId },
                        { "purchaseToken", purchaseToken },
                        { "consumable", consumable },
                    });
                if (result.Valid) return PurchaseVerdict.Valid;
                FlowLogger.LogWarning($"Purchase of {productId} refused by the server: {result.Reason}");
                return PurchaseVerdict.Invalid;
            }
            catch (Exception exception)
            {
                FlowLogger.LogWarning($"Purchase of {productId} not verified, will retry: {exception.Message}");
                return PurchaseVerdict.Unreachable;
            }
        }

        // The account module signs the player in at launch; the store waits for it rather than signing in itself.
        private async Task<bool> WaitForSignIn()
        {
            var deadline = DateTime.UtcNow + _signInWait;
            while (DateTime.UtcNow < deadline)
            {
                if (UnityServices.State == ServicesInitializationState.Initialized && AuthenticationService.Instance.IsSignedIn) return true;
                await Task.Delay(500);
            }
            return false;
        }
    }
}
