using UnityEngine;

namespace Modules.ProfileModule.Shared.Data.UnityObjects
{
    [CreateAssetMenu(fileName = "SD_PlayerProfile", menuName = "Jelly Fuse/Profile/Player Profile")]
    public sealed class SD_PlayerProfile : ScriptableObject
    {
        [SerializeField] private string _playerName = "Player";
        [SerializeField] private string _selectedIconId;
        [SerializeField] private string _selectedFrameId;

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
