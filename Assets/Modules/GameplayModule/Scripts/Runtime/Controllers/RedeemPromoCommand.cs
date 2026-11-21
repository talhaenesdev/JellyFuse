using System;
using System.Collections.Generic;
using FlowIoC.BaseModule.Attributes;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using FlowIoC.ConsoleModule;
using Modules.GameplayModule.Models;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using Modules.GameplayModule.Shared.Data.ValueObjects;
using Modules.GameplayModule.Shared.Enums;
using Modules.GameplayModule.Signals;
using Modules.PromoModule.Services;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>
    /// A promo code was typed: the server says what it pays, and its coins, lives and boosters are added here - the
    /// steps after this save them. The outcome goes out either way, so the screen can say so and the profile can
    /// unlock the avatars and frames it carries.
    /// </summary>
    [LongRetain(30)]
    internal class RedeemPromoCommand : Command
    {
        [Inject] private IPromoService _promo { get; set; }
        [Inject] private ILivesModel _lives { get; set; }
        [Inject] private ISharedDataModel _shared { get; set; }
        [InjectSignal] private GameplaySignals _signals { get; set; }
        [SignalParam] private string _code { get; set; }

        public override async void Execute()
        {
            Retain();
            var signals = _signals;
            var shared = _shared;
            var lives = _lives;
            try
            {
                var reward = await _promo.RedeemAsync(_code);
                if (reward == null || !reward.Ok)
                {
                    FlowLogger.Log($"RedeemPromoCommand - code not paid: {reward?.Reason}");
                    signals.Outgoing.PromoRedeemed.Dispatch(new PromoResultRVO(false, reward?.Reason ?? "offline"));
                    Stop();
                    return;
                }
                var progress = shared.GetScriptable<SD_JellyProgress>();
                if (progress != null)
                {
                    if (reward.Coins > 0)
                    {
                        progress.Coins += reward.Coins;
                        signals.Outgoing.CoinsEarned.Dispatch(reward.Coins);
                        signals.Outgoing.CoinsChanged.Dispatch(progress.Coins);
                    }
                    var livesConfig = shared.GetScriptable<CD_Lives>();
                    if (reward.Lives > 0 && livesConfig != null)
                        lives.Add(progress, livesConfig, reward.Lives, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                    if (reward.Rockets > 0) progress.Booster(BoosterType.Rocket).Count += reward.Rockets;
                    if (reward.Freezes > 0) progress.Booster(BoosterType.FreezeTime).Count += reward.Freezes;
                    if (reward.Ufos > 0) progress.Booster(BoosterType.Ufo).Count += reward.Ufos;
                }
                var items = new List<string>();
                foreach (var icon in reward.Icons) if (!string.IsNullOrEmpty(icon)) items.Add(icon);
                foreach (var frame in reward.Frames) if (!string.IsNullOrEmpty(frame)) items.Add("frame:" + frame);
                FlowLogger.Log($"RedeemPromoCommand - code paid {reward.Coins} coins, {items.Count} items.");
                signals.Outgoing.PromoRedeemed.Dispatch(new PromoResultRVO(true, string.Empty, reward.Coins, reward.Lives,
                    reward.Rockets + reward.Freezes + reward.Ufos, items));
                Release();
            }
            catch (Exception exception)
            {
                FlowLogger.LogError($"RedeemPromoCommand threw while redeeming: {exception}");
                signals.Outgoing.PromoRedeemed.Dispatch(new PromoResultRVO(false, "offline"));
                Stop();
            }
        }
    }
}
