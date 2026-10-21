using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.LocalizationModule.Constants;
using Modules.LocalizationModule.Models;
using Modules.LocalizationModule.Signals;
using UnityEngine;

namespace Modules.LocalizationModule.Controllers
{
    /// <summary>Switches to the language asked for, keeps the choice, and tells every screen. A language the game does not offer is refused.</summary>
    internal class ChangeLanguageCommand : Command
    {
        [Inject] private ILocalizationModel _model { get; set; }
        [InjectSignal] private LocalizationSignals _signals { get; set; }
        [SignalParam] private string _code { get; set; }

        public override void Execute()
        {
            if (!_model.Offers(_code))
            {
                FlowLogger.LogWarning($"ChangeLanguageCommand - the game does not offer the language '{_code}'. Add it to CD_Localization's Languages.");
                return;
            }
            if (_model.Language == _code) return;
            _model.SetLanguage(_code);
            PlayerPrefs.SetString(LocalizationConstants.PREFS_KEY, _code);
            PlayerPrefs.Save();
            _signals.Outgoing.LanguageChanged.Dispatch(_code);
        }
    }
}
