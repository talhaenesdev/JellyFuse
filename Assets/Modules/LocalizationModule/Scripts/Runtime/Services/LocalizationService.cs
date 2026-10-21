using System.Collections.Generic;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.LocalizationModule.Models;
using Modules.LocalizationModule.Shared.Data.ValueObjects;
using Modules.LocalizationModule.Signals;

namespace Modules.LocalizationModule.Services
{
    /// <summary>
    /// Reads answer from the model at once; a change of language goes to a Command through the
    /// internal signals, so it is a step the Flow Console shows and its checks are taken there.
    /// </summary>
    public class LocalizationService : ILocalizationService
    {
        [Inject] private ILocalizationModel _model { get; set; }
        [InjectSignal] private LocalizationInternalSignals _signals { get; set; }

        public string Language => _model.Language;
        public IReadOnlyList<LanguageCVO> Languages => _model.Languages;

        public string Get(string key) => _model.Get(key);

        public string Format(string key, params object[] values)
        {
            string text = _model.Get(key);
            try { return string.Format(text, values); }
            catch (System.FormatException) { return text; }
        }

        public void SetLanguage(string code) => _signals.SetLanguage.Dispatch(code);
    }
}
