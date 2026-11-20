using System;
using System.Collections.Generic;

namespace Modules.ProfileModule.Shared.Data.ValueObjects
{
    [Serializable]
    public sealed class ProfileSelectionRVO
    {
        public string PlayerName;
        public string SelectedIconId;
        public string SelectedFrameId;
        /// <summary>Coins paid for the first name the player picks; 0 once they have been paid.</summary>
        public int NameReward;
        public List<ProfileIconVO> Icons = new();
        public List<ProfileFrameVO> Frames = new();
    }
}
