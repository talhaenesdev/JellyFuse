using System.Collections.Generic;
using Modules.DevToolsModule.Data.ValueObjects;
using UnityEngine;

namespace Modules.DevToolsModule.Data.UnityObjects
{
    /// <summary>
    /// The developer switches the panel lists, in order. Filed on DevToolsServiceRoot. A system that
    /// wants a switch adds an entry here and asks IDevToolsService.IsOn with its key.
    /// </summary>
    [CreateAssetMenu(fileName = "CD_DevTools", menuName = "Dev Tools/Config")]
    public sealed class CD_DevTools : ScriptableObject
    {
        [Tooltip("Off for a store build: the hidden panel never opens, and every switch reads its DefaultOn, " +
                 "whatever this device saved - a switch left on while testing never reaches a player.")]
        public bool PanelEnabled = true;
        public List<DevToggleCVO> Toggles = new();
    }
}
