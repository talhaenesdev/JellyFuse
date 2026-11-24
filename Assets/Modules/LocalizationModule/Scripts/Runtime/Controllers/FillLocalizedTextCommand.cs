using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.LocalizationModule.Services;
using Modules.LocalizationModule.ViewsMediators;

namespace Modules.LocalizationModule.Controllers
{
    /// <summary>A localized text shows its key's words in the language in use. A text with no key keeps what it has.</summary>
    internal sealed class FillLocalizedTextCommand : Command
    {
        [Inject] private ILocalizationService _localization { get; set; }
        [SignalParam] private LocalizedTextView _view { get; set; }

        public override void Execute()
        {
            if (_view == null || string.IsNullOrEmpty(_view.Key)) return;
            _view.Show(_localization.Get(_view.Key));
        }
    }
}
