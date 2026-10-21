using FlowIoC.BaseModule.Constructables;
using UnityEngine;
namespace Modules.NotificationModule.Models
{
    /// <summary>The player's on/off preference, kept in PlayerPrefs, and the number a key is scheduled under.</summary>
    internal sealed class NotificationModel : IConstructable
    {
        public const string PREFERENCE_KEY = "Notification.Enabled";
        public bool Enabled { get; private set; } = true;
        public bool IsPostConstructed { get; set; }
        public bool IsDeconstructed { get; set; }
        public void PostConstruct() => Enabled = PlayerPrefs.GetInt(PREFERENCE_KEY, 1) == 1;
        public void SetEnabled(bool enabled)
        {
            Enabled = enabled;
            PlayerPrefs.SetInt(PREFERENCE_KEY, enabled ? 1 : 0);
            PlayerPrefs.Save();
        }
        /// <summary>A key's number on the device: the same on every run, which string.GetHashCode is not.</summary>
        public int IdOf(string key)
        {
            unchecked
            {
                int hash = 23;
                foreach (char c in key ?? "") hash = hash * 31 + c;
                return hash & 0x7fffffff;
            }
        }
    }
}
