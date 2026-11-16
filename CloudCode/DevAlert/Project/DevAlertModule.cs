using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;

namespace DevAlert
{
    /// <summary>
    /// Tells the developer on Telegram that the hidden developer panel was reached: the PIN accepted or a
    /// wrong one tried. The bot's token and the chat it writes to are in the project's Secret Manager, so
    /// nothing in the game's build can be used to send messages. No player id goes in the message.
    /// </summary>
    public class DevAlertModule
    {
        private const string TokenSecret = "TELEGRAM_BOT_TOKEN";
        private const string ChatSecret = "TELEGRAM_CHAT_ID";
        private const int MaxText = 60;

        private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(8) };

        private readonly ILogger<DevAlertModule> _logger;

        public DevAlertModule(ILogger<DevAlertModule> logger) => _logger = logger;

        [CloudCodeFunction("PinEntered")]
        public async Task<bool> PinEntered(IExecutionContext context, IGameApiClient gameApiClient,
            bool accepted, string device, string version)
        {
            string token, chat;
            try
            {
                token = (await gameApiClient.SecretManager.GetSecret(context, TokenSecret)).Value;
                chat = (await gameApiClient.SecretManager.GetSecret(context, ChatSecret)).Value;
            }
            catch (Exception exception)
            {
                _logger.LogError("DevAlert - the Telegram secrets are not set: {Message}", exception.Message);
                return false;
            }

            string text = (accepted ? "✅ Dev paneli açıldı (doğru PIN)" : "❌ Dev paneli: yanlış PIN denendi")
                          + $"\nCihaz: {Clean(device)}\nSürüm: {Clean(version)}\nSaat (UTC): {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}";
            var body = JsonSerializer.Serialize(new Dictionary<string, string> { { "chat_id", chat }, { "text", text } });
            try
            {
                using var response = await Http.PostAsync($"https://api.telegram.org/bot{token}/sendMessage",
                    new StringContent(body, Encoding.UTF8, "application/json"));
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("DevAlert - Telegram answered {Status}", (int)response.StatusCode);
                    return false;
                }
                return true;
            }
            catch (Exception exception)
            {
                _logger.LogError("DevAlert - Telegram could not be reached: {Message}", exception.Message);
                return false;
            }
        }

        /// <summary>What the game sent, cut short and kept to one line: it is shown, never trusted.</summary>
        private static string Clean(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "-";
            value = value.Replace('\n', ' ').Replace('\r', ' ').Trim();
            return value.Length > MaxText ? value.Substring(0, MaxText) : value;
        }
    }

    public class ModuleConfig : ICloudCodeSetup
    {
        public void Setup(ICloudCodeConfig config) => config.Dependencies.AddSingleton(GameApiClient.Create());
    }
}
