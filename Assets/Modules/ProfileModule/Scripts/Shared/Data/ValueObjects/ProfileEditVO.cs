using System;

namespace Modules.ProfileModule.Shared.Data.ValueObjects
{
    [Serializable]
    public sealed class ProfileEditVO
    {
        public string PlayerName;
        public string IconId;
        public string FrameId;
    }
}
