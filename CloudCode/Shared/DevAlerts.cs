using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;
using Unity.Services.CloudSave.Model;

// Compiled into every Cloud Code module that tells the developer something (DevAlert, PurchaseVerify, Leaderboard):
// modules are deployed apart and cannot call each other, so each project links this one file.
namespace DevAlerts
{
    /// <summary>Sends one message to the developer's Telegram chat. The bot token and chat id are Secret Manager secrets.</summary>
    public static class Telegram
    {
        private const string TokenSecret = "TELEGRAM_BOT_TOKEN";
        private const string ChatSecret = "TELEGRAM_CHAT_ID";
        private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(8) };

        public static async Task<bool> Send(IExecutionContext context, IGameApiClient api, string text, ILogger logger)
        {
            string token, chat;
            try
            {
                token = (await api.SecretManager.GetSecret(context, TokenSecret)).Value;
                chat = (await api.SecretManager.GetSecret(context, ChatSecret)).Value;
            }
            catch (Exception exception)
            {
                logger.LogError("DevAlerts - the Telegram secrets are not set: {Message}", exception.Message);
                return false;
            }
            var body = JsonSerializer.Serialize(new Dictionary<string, string> { { "chat_id", chat }, { "text", text } });
            try
            {
                using var response = await Http.PostAsync($"https://api.telegram.org/bot{token}/sendMessage",
                    new StringContent(body, Encoding.UTF8, "application/json"));
                if (response.IsSuccessStatusCode) return true;
                logger.LogError("DevAlerts - Telegram answered {Status}", (int)response.StatusCode);
                return false;
            }
            catch (Exception exception)
            {
                logger.LogError("DevAlerts - Telegram could not be reached: {Message}", exception.Message);
                return false;
            }
        }
    }

    /// <summary>
    /// The day's counts, kept in a private Cloud Save custom item no player can read or write, and the daily summary.
    /// Two events at the same moment may count once, and the summary may rarely come twice: these are for a glance,
    /// not for accounting.
    /// </summary>
    public static class DevCounters
    {
        public const string PinAccepted = "pinAccepted";
        public const string PinRefused = "pinRefused";
        public const string Errors = "errors";
        public const string Purchases = "purchases";
        public const string PurchasesRefused = "purchasesRefused";
        public const string Wins = "wins";
        private const string CustomId = "dev-alert";
        private const string SummaryKey = "summary-sent";

        private static string Key(DateTime day) => "counts-" + day.ToString("yyyyMMdd");

        /// <summary>Adds one to today's count of this kind and answers the new count; 0 when it could not be stored.</summary>
        public static async Task<int> Add(IExecutionContext context, IGameApiClient api, string kind, ILogger logger)
        {
            try
            {
                var day = DateTime.UtcNow.Date;
                var counts = await Read(context, api, day, logger);
                counts[kind] = (counts.TryGetValue(kind, out int value) ? value : 0) + 1;
                await api.CloudSaveData.SetPrivateCustomItemAsync(context, context.ServiceToken, context.ProjectId, CustomId,
                    new SetItemBody(Key(day), counts));
                return counts[kind];
            }
            catch (Exception exception)
            {
                logger.LogError("DevAlerts - the count could not be stored: {Message}", exception.Message);
                return 0;
            }
        }

        public static async Task<Dictionary<string, int>> Read(IExecutionContext context, IGameApiClient api, DateTime day, ILogger logger)
        {
            try
            {
                var response = await api.CloudSaveData.GetPrivateCustomItemsAsync(context, context.ServiceToken, context.ProjectId, CustomId,
                    new List<string> { Key(day) });
                var items = response.Data?.Results;
                if (items == null || items.Count == 0 || items[0].Value == null) return new Dictionary<string, int>();
                return JsonSerializer.Deserialize<Dictionary<string, int>>(items[0].Value.ToString()) ?? new Dictionary<string, int>();
            }
            catch (Exception exception)
            {
                logger.LogError("DevAlerts - the counts could not be read: {Message}", exception.Message);
                return new Dictionary<string, int>();
            }
        }

        /// <summary>
        /// Sends yesterday's summary (UTC) if it has not gone yet. Called by every event, so the first one of a new day
        /// sends it - no schedule to set up. The day is marked sent before sending, so two players cannot both send it.
        /// </summary>
        public static async Task SummaryIfDue(IExecutionContext context, IGameApiClient api, ILogger logger)
        {
            try
            {
                var yesterday = DateTime.UtcNow.Date.AddDays(-1);
                string mark = yesterday.ToString("yyyyMMdd");
                var response = await api.CloudSaveData.GetPrivateCustomItemsAsync(context, context.ServiceToken, context.ProjectId, CustomId,
                    new List<string> { SummaryKey });
                var items = response.Data?.Results;
                string last = items != null && items.Count > 0 && items[0].Value != null ? items[0].Value.ToString().Trim('"') : string.Empty;
                if (string.CompareOrdinal(last, mark) >= 0) return;
                await api.CloudSaveData.SetPrivateCustomItemAsync(context, context.ServiceToken, context.ProjectId, CustomId,
                    new SetItemBody(SummaryKey, mark));
                var counts = await Read(context, api, yesterday, logger);
                int Get(string key) => counts.TryGetValue(key, out int value) ? value : 0;
                await Telegram.Send(context, api, string.Join(Environment.NewLine,
                    $"📊 Günlük özet ({yesterday:yyyy-MM-dd}, UTC)",
                    $"🏆 Kazanılan bölüm: {Get(Wins)}",
                    $"💰 Doğrulanan satın alma: {Get(Purchases)}",
                    $"⛔ Reddedilen satın alma: {Get(PurchasesRefused)}",
                    $"⚠️ Hata: {Get(Errors)}",
                    $"✅ Doğru PIN: {Get(PinAccepted)}",
                    $"❌ Yanlış PIN: {Get(PinRefused)}"), logger);
            }
            catch (Exception exception)
            {
                logger.LogError("DevAlerts - the daily summary could not be sent: {Message}", exception.Message);
            }
        }

        /// <summary>Counts the event, then sends yesterday's summary if this is the first event of a new day.</summary>
        public static async Task<int> Track(IExecutionContext context, IGameApiClient api, string kind, ILogger logger)
        {
            await SummaryIfDue(context, api, logger);
            return await Add(context, api, kind, logger);
        }
    }
}
