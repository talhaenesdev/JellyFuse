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
        public List<ProfileIconVO> Icons = new();
        public List<ProfileFrameVO> Frames = new();
    }
}
