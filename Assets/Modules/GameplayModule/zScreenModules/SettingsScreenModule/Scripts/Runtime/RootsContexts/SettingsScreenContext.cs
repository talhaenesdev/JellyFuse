using FlowIoC.ScreenModule.Data;
using FlowIoC.ScreenModule.Enums;
using FlowIoC.ScreenModule.RootsContexts;
using Modules.GameplayModule.SettingsScreenModule.Controllers;
using Modules.GameplayModule.SettingsScreenModule.Models;
using Modules.GameplayModule.SettingsScreenModule.Signals;
using Modules.GameplayModule.SettingsScreenModule.ViewsMediators;

namespace Modules.GameplayModule.SettingsScreenModule.RootsContexts
{
    public class SettingsScreenContext : ScreenSubContext<SettingsScreenView, SettingsScreenMediator>
    {
        // Layer 2: over the gameplay screen (layer 1), which stays open underneath.
        protected override ScreenCVO Screen => new()
        {
            ManagerId = 0,
            Layer = 2,
            Tag = ScreenTag.Default,
            Load = ScreenLoadCVO.Addressable("SettingsScreen"),
            HasShowAnimation = false,
            HasHideAnimation = false,
        };

        private SettingsScreenSignals _signals;
        private SettingsScreenInternalSignals _internalSignals;

        public override void SignalBindings()
        {
            base.SignalBindings();
            _signals = InjectionBinderCrossContext.Bind<SettingsScreenSignals>();
            _internalSignals = InjectionBinder.Bind<SettingsScreenInternalSignals>();
        }

        public override void InjectionBindings()
        {
            base.InjectionBindings();
            InjectionBinder.Bind<ISettingsModel, SettingsModel>();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();
            CommandBinder.Bind(_internalSignals.ToggleOption).ToSequence<ToggleSettingOptionCommand>();
            CommandBinder.Bind(_signals.Incoming.OpenSettingsScreen).ToSequence<OpenSettingsScreenCommand>();
        }
    }
}
