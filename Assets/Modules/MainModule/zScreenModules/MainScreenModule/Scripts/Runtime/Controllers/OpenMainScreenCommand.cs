using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using Modules.GameplayModule.Shared.Data.ValueObjects;
using FlowIoC.ConsoleModule;
using FlowIoC.ScreenModule.Service;
using Modules.LocalizationModule.Services;
using Modules.MainModule.MainScreenModule.ViewsMediators;

namespace Modules.MainModule.MainScreenModule.Controllers
{
    internal class OpenMainScreenCommand : Command
    {
        [Inject] private IScreenService _screenService { get; set; }
        // A Mediator injects nothing but its View, so the screen gets its texts from here.
        [Inject] private ILocalizationService _localization { get; set; }
        // The level data and the player's progress, which the gameplay module files as shared.
        [Inject] private ISharedDataModel _shared { get; set; }

        /// <summary>
        /// Three ways out, and every one of them resolves the retain. The success path is the one
        /// everybody writes; the screen coming back null and the await throwing are the two that
        /// hang the group for ever if they are forgotten, because a retain nobody resolves has no
        /// timeout and logs nothing.
        ///
        /// What Stop() does here is this game's answer and not the framework's - another game
        /// releases and carries on, or dispatches a signal that opens something else instead.
        /// </summary>
        public override async void Execute()
        {
            Retain();

            FlowLogger.Log("Execute - OpenMainScreenCommand");

            try
            {
                // Show<T>() rather than Show(): a typed view compares against null through Unity's
                // own operator, and an IScreenBody does not.
                MainScreenView screen = await _screenService.Open<MainScreenView>().Show<MainScreenView>();

                if (screen == null)
                {
                    FlowLogger.LogError("OpenMainScreenCommand - the screen did not open.");
                    Stop();
                    return;
                }

                screen.SetLocalizer(_localization.Get);
                // Play names the level Play starts, as the gameplay module will pick it.
                var levels = _shared.GetScriptable<CD_JellyLevels>();
                var progress = _shared.GetScriptable<SD_JellyProgress>();
                int next = levels != null ? levels.StartIndex(progress) : -1;
                screen.ApplyNextLevel(next + 1);
                screen.ApplyCoins(progress != null ? progress.Coins : 0);
                var lives = _shared.GetScriptable<CD_Lives>();
                if (progress != null && lives != null)
                    screen.ApplyLives(new LivesStateRVO { Lives = progress.LivesGranted ? progress.Lives : lives.StartLives, MaxLives = lives.MaxLives });
                Release();
            }
            catch (Exception exception)
            {
                FlowLogger.LogError($"OpenMainScreenCommand threw while opening the screen: {exception}");
                Stop();
            }
        }
    }
}
