using System.Collections.Generic;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.ProfileModule.Models;

namespace Modules.ProfileModule.Controllers
{
    /// <summary>Avatars and frames given to the player become theirs; the step after this saves the profile.</summary>
    internal sealed class GrantProfileItemsCommand : Command
    {
        [Inject] private IProfileModel _model { get; set; }
        [SignalParam] private IReadOnlyList<string> _items { get; set; }

        public override void Execute()
        {
            if (_items == null || _items.Count == 0)
            {
                Retain();
                Stop();
                return;
            }
            foreach (var item in _items) _model.Unlock(item);
            FlowLogger.Log($"GrantProfileItemsCommand - {_items.Count} items unlocked.");
        }
    }
}
