using System.Collections.Generic;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.DevToolsModule.Data.ValueObjects;
using Modules.DevToolsModule.Models;

namespace Modules.DevToolsModule.Services
{
    internal sealed class DevToolsService : IDevToolsService
    {
        [Inject] private DevToolsModel _model { get; set; }

        public IReadOnlyList<DevToggleCVO> Toggles => _model.Toggles;
        public bool PanelEnabled => _model.PanelEnabled;
        public bool Unlocked => _model.Unlocked;
        public float CornerSequenceSeconds => _model.CornerSequenceSeconds;
        public float CornerZone => _model.CornerZone;
        public bool TryUnlock(string pin) => _model.TryUnlock(pin);
        public bool IsOn(string key) => _model.IsOn(key);
        public bool Set(string key, bool on) => _model.Set(key, on);
    }
}
