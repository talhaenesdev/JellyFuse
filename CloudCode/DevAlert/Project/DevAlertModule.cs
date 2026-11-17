using System;
using System.Threading.Tasks;
using DevAlerts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;

namespace DevAlert
{
    /// <summary>
    /// Tells the developer on Telegram what happens in the live game: the hidden developer panel reached (the PIN
    /// accepted or a wrong one tried) and errors the game ran into. Every event is counted for the daily summary,
    /// which the first event of a new day sends. The bot's token and the chat it writes to are in the project's Secret
    /// Manager, so nothing in the game's build can be used to send messages. No player id, IP or location goes in a
    /// message.
    /// </summary>
    public class DevAlertModule
    {
        private const int MaxText = 60;
        private const int MaxError = 300;
        // Past this many errors in a day only the count goes up; the summary still tells how many there were.
        private const int ErrorMessagesPerDay = 30;

        private readonly ILogger<DevAlertModule> _logger;

        public DevAlertModule(ILogger<DevAlertModule> logger) => _logger = logger;

        [CloudCodeFunction("PinEntered")]
        public async Task<bool> PinEntered(IExecutionContext context, IGameApiClient gameApiClient,
            bool accepted, string device, string version, string os = null, string language = null, string region = null,
            string memory = null, string screen = null)
        {
            await DevCounters.Track(context, gameApiClient, accepted ? DevCounters.PinAccepted : DevCounters.PinRefused, _logger);
            return await Telegram.Send(context, gameApiClient, string.Join(Environment.NewLine,
                accepted ? "✅ Dev paneli açıldı (doğru PIN)" : "❌ Dev paneli: yanlış PIN denendi",
                $"Cihaz: {Clean(device)}",
                $"Sistem: {Clean(os)}",
                $"Dil / bölge: {Clean(language)} / {Clean(region)}",
                $"RAM: {Clean(memory)} MB · Ekran: {Clean(screen)}",
                $"Sürüm: {Clean(version)}",
                $"Saat (UTC): {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}"), _logger);
        }

        [CloudCodeFunction("ErrorReported")]
        public async Task<bool> ErrorReported(IExecutionContext context, IGameApiClient gameApiClient,
            string message, string version, string device)
        {
            int today = await DevCounters.Track(context, gameApiClient, DevCounters.Errors, _logger);
            if (today > ErrorMessagesPerDay) return false;
            return await Telegram.Send(context, gameApiClient, string.Join(Environment.NewLine,
                "⚠️ Oyunda hata",
                Clean(message, MaxError),
                $"Cihaz: {Clean(device)} · Sürüm: {Clean(version)}",
                $"Saat (UTC): {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}",
                today == ErrorMessagesPerDay ? $"(Bugünün {ErrorMessagesPerDay}. hatası; kalanlar sadece günlük özette sayılacak.)" : string.Empty), _logger);
        }

        /// <summary>What the game sent, cut short and kept to one line: it is shown, never trusted.</summary>
        private static string Clean(string value, int max = MaxText)
        {
            if (string.IsNullOrWhiteSpace(value)) return "-";
            value = value.Replace('\n', ' ').Replace('\r', ' ').Trim();
            return value.Length > max ? value.Substring(0, max) : value;
        }
    }

    public class ModuleConfig : ICloudCodeSetup
    {
        public void Setup(ICloudCodeConfig config) => config.Dependencies.AddSingleton(GameApiClient.Create());
    }
}
