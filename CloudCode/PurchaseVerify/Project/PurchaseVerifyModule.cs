using System;
using System.Threading.Tasks;
using DevAlerts;
using Google;
using Google.Apis.AndroidPublisher.v3;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;

namespace PurchaseVerify
{
    /// <summary>
    /// Asks Google Play whether a purchase token is a real, paid purchase of the product the game claims.
    /// The game grants nothing it has not heard "Valid" for.
    /// </summary>
    public class PurchaseVerifyModule
    {
        // The service account's JSON key, stored in the project's Secret Manager under this name.
        private const string ServiceAccountSecret = "GOOGLE_PLAY_SERVICE_ACCOUNT";
        private const string PackageName = "com.floriagames.jellyfuse";

        private const int Purchased = 0;
        private const int NotConsumed = 0;

        private readonly ILogger<PurchaseVerifyModule> _logger;

        public PurchaseVerifyModule(ILogger<PurchaseVerifyModule> logger) => _logger = logger;

        [CloudCodeFunction("Verify")]
        public async Task<VerifyResult> Verify(IExecutionContext context, IGameApiClient gameApiClient,
            string productId, string purchaseToken, bool consumable)
        {
            if (string.IsNullOrEmpty(productId) || string.IsNullOrEmpty(purchaseToken))
                return VerifyResult.Invalid("missing product or token");

            var secret = await gameApiClient.SecretManager.GetSecret(context, ServiceAccountSecret);
            var credential = GoogleCredential.FromJson(secret.Value).CreateScoped(AndroidPublisherService.Scope.Androidpublisher);
            using var publisher = new AndroidPublisherService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "JellyFusePurchaseVerify",
            });

            try
            {
                var purchase = await publisher.Purchases.Products.Get(PackageName, productId, purchaseToken).ExecuteAsync();
                if (purchase.PurchaseState != Purchased) return await Refused(context, gameApiClient, productId, $"purchase state {purchase.PurchaseState}");
                // A consumable already consumed was granted once; the same token shown again is a replay.
                if (consumable && purchase.ConsumptionState != NotConsumed) return await Refused(context, gameApiClient, productId, "already consumed");
                _logger.LogInformation("Verified {ProductId} order {OrderId} for {PlayerId}", productId, purchase.OrderId, context.PlayerId);
                await DevCounters.Track(context, gameApiClient, DevCounters.Purchases, _logger);
                // A licence tester's order is free; it is told apart so a test does not read as income.
                string test = purchase.PurchaseType == 0 ? " (test hesabı, ücretsiz)" : "";
                await Telegram.Send(context, gameApiClient, string.Join(Environment.NewLine, $"💰 Satın alma doğrulandı{test}",
                    $"Ürün: {productId}", $"Saat (UTC): {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}"), _logger);
                return VerifyResult.Ok();
            }
            catch (GoogleApiException exception) when (exception.HttpStatusCode == System.Net.HttpStatusCode.BadRequest
                                                         || exception.HttpStatusCode == System.Net.HttpStatusCode.NotFound
                                                         || exception.HttpStatusCode == System.Net.HttpStatusCode.Gone)
            {
                // Google does not know the token: forged, or for another app or product.
                return await Refused(context, gameApiClient, productId, $"google answered {(int)exception.HttpStatusCode}");
            }
        }

        /// <summary>A purchase the server will not grant: counted, told on Telegram, and refused.</summary>
        private async Task<VerifyResult> Refused(IExecutionContext context, IGameApiClient gameApiClient, string productId, string reason)
        {
            await DevCounters.Track(context, gameApiClient, DevCounters.PurchasesRefused, _logger);
            await Telegram.Send(context, gameApiClient, string.Join(Environment.NewLine, "⛔ Satın alma reddedildi",
                $"Ürün: {productId}", $"Neden: {reason}", $"Saat (UTC): {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}"), _logger);
            return VerifyResult.Invalid(reason);
        }
    }

    public class VerifyResult
    {
        public bool Valid { get; set; }
        public string Reason { get; set; }

        public static VerifyResult Ok() => new() { Valid = true, Reason = string.Empty };
        public static VerifyResult Invalid(string reason) => new() { Valid = false, Reason = reason };
    }

    public class ModuleConfig : ICloudCodeSetup
    {
        public void Setup(ICloudCodeConfig config) => config.Dependencies.AddSingleton(GameApiClient.Create());
    }
}
