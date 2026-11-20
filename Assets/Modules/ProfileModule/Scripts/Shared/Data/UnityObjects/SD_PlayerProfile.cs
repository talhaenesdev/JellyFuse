using UnityEngine;

namespace Modules.ProfileModule.Shared.Data.UnityObjects
{
    [CreateAssetMenu(fileName = "SD_PlayerProfile", menuName = "Jelly Fuse/Profile/Player Profile")]
    public sealed class SD_PlayerProfile : ScriptableObject
    {
        [SerializeField] private string _playerName = "Player";
        [SerializeField] private string _selectedIconId;
        [SerializeField] private string _selectedFrameId;
        /// <summary>The coins for naming the profile were paid; they are paid once.</summary>
        [SerializeField] private bool _nameRewarded;
        public bool NameRewarded => _nameRewarded;
        public void MarkNameRewarded() => _nameRewarded = true;

        [Tooltip("Icons bought with coins.")]
        [SerializeField] private System.Collections.Generic.List<string> _ownedIconIds = new();

        public System.Collections.Generic.IReadOnlyList<string> OwnedIconIds => _ownedIconIds;
        public void AddOwnedIcon(string id) { if (!string.IsNullOrEmpty(id) && !_ownedIconIds.Contains(id)) _ownedIconIds.Add(id); }

        public string PlayerName => _playerName;
        public string SelectedIconId => _selectedIconId;
        public string SelectedFrameId => _selectedFrameId;

        public void SetProfile(string playerName, string iconId, string frameId)
        {
            _playerName = playerName;
            _selectedIconId = iconId;
            _selectedFrameId = frameId;
        }
    }
}
