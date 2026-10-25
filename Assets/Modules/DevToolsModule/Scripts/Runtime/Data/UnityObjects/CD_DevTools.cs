using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
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
        [Tooltip("Off for a store build: the usual taps open nothing, and every switch reads its DefaultOn, " +
                 "whatever this device saved - a switch left on while testing never reaches a player. " +
                 "The secret corner gesture and the PIN still open the panel on this device.")]
        public bool PanelEnabled = true;
        public List<DevToggleCVO> Toggles = new();

        [Header("Secret unlock")]
        [Tooltip("Type a new PIN here and it is turned into Pin Hash and cleared. Only the hash is kept, so the " +
                 "PIN is not in the build as text.")]
        [SerializeField] private string _setPin = string.Empty;
        [Tooltip("SHA-256 of the PIN. Empty: the secret gesture opens nothing.")]
        public string PinHash = string.Empty;
        [Tooltip("Seconds to tap the four corners in - top-left, top-right, bottom-right, bottom-left.")]
        [Min(.5f)] public float CornerSequenceSeconds = 3f;
        [Tooltip("How far into the screen a corner reaches, as a share of the screen's shorter side.")]
        [Range(.05f, .4f)] public float CornerZone = .18f;

        /// <summary>The hash a PIN is kept as; salted with the game so a hash from elsewhere does not match.</summary>
        public static string HashPin(string pin)
        {
            using (var sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes("jellyfuse.devtools:" + (pin ?? string.Empty).Trim()));
                var text = new StringBuilder(bytes.Length * 2);
                foreach (byte b in bytes) text.Append(b.ToString("x2"));
                return text.ToString();
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(_setPin)) return;
            PinHash = HashPin(_setPin);
            _setPin = string.Empty;
        }
#endif
    }
}
