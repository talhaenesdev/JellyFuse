using System;
using UnityEngine;

namespace Modules.DevToolsModule.Data.ValueObjects
{
    /// <summary>One developer switch: the key the code asks for, the label the panel shows, and its value before anyone flips it.</summary>
    [Serializable]
    public sealed class DevToggleCVO
    {
        [Tooltip("The name the code asks IDevToolsService for. Never change it once code reads it.")]
        public string Key;
        [Tooltip("What the developer panel shows next to the switch.")]
        public string Label;
        [Tooltip("The value until the switch is flipped on this device.")]
        public bool DefaultOn;
        [Tooltip("The page of the developer panel the switch is on: \"game\" (gameplay rules) or \"display\" (what the screens show).")]
        public string Page = "game";
    }
}
