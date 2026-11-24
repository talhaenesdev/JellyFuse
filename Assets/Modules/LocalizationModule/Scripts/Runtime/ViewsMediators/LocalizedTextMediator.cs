using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.ViewsMediators.Mediator;
using Modules.LocalizationModule.Signals;

namespace Modules.LocalizationModule.ViewsMediators
{
    /// <summary>Asks for a LocalizedTextView to be filled when it registers, and again whenever the language changes.</summary>
    public class LocalizedTextMediator : IMediator
    {
        [Inject] private LocalizedTextView _view { get; set; }
        [InjectSignal] private LocalizationSignals _signals { get; set; }
        [InjectSignal] private LocalizationInternalSignals _internalSignals { get; set; }

        public void OnRegister()
        {
            _signals.Outgoing.LanguageChanged.AddListener(OnLanguageChanged);
            _internalSignals.FillText.Dispatch(_view);
        }

        public void OnRemove() => _signals.Outgoing.LanguageChanged.RemoveListener(OnLanguageChanged);

        private void OnLanguageChanged(string code) => _internalSignals.FillText.Dispatch(_view);
    }
}
