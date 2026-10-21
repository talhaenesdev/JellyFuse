using System.Collections.Generic;
using Modules.DevToolsModule.Data.ValueObjects;

namespace Modules.DevToolsModule.Services
{
    /// <summary>
    /// Developer switches for trying a new system next to the old one. A Command that chooses between
    /// two behaviours asks IsOn with its key; the developer panel lists Toggles and flips them with Set.
    /// </summary>
    public interface IDevToolsService
    {
        IReadOnlyList<DevToggleCVO> Toggles { get; }
        bool IsOn(string key);
        /// <summary>Sets and keeps the switch on this device; false when CD_DevTools does not list the key.</summary>
        bool Set(string key, bool on);
    }
}
