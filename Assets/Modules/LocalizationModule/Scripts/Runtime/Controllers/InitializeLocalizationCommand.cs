using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using FlowIoC.ConsoleModule;
using Modules.LocalizationModule.Constants;
using Modules.LocalizationModule.Models;
using Modules.LocalizationModule.Shared.Data.UnityObjects;
using Modules.LocalizationModule.Signals;
using UnityEngine;

namespace Modules.LocalizationModule.Controllers
{
    /// <summary>
    /// Loads CD_Localization and picks the language: the player's earlier choice, else the device's
    /// language when the game offers it, else the default. Runs from Setup, so every screen hears
    /// LanguageChanged before its first Launch.
    /// </summary>
    internal class InitializeLocalizationCommand : Command
    {
        [Inject] private ISharedDataModel _shared { get; set; }
        [Inject] private ILocalizationModel _model { get; set; }
        [InjectSignal] private LocalizationSignals _signals { get; set; }

        public override void Execute()
        {
            var config = _shared.GetScriptable<CD_Localization>();
            if (config == null || config.Languages.Count == 0)
            {
                FlowLogger.LogError("InitializeLocalizationCommand - no CD_Localization with a language is filed on LocalizationServiceRoot's Shared Scriptables. Texts show their keys.");
                return;
            }
            _model.Load(config);
            string saved = PlayerPrefs.GetString(LocalizationConstants.PREFS_KEY, "");
            var device = config.Find(Application.systemLanguage);
            string code = _model.Offers(saved) ? saved
                : device != null ? device.Code
                : _model.Offers(config.DefaultLanguage) ? config.DefaultLanguage
                : config.Languages[0].Code;
            _model.SetLanguage(code);
            FlowLogger.Log($"InitializeLocalizationCommand - language {code}");
            _signals.Outgoing.LanguageChanged.Dispatch(code);
        }
    }
}
