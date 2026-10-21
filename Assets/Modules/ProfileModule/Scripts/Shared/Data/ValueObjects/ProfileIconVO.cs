using System;
using UnityEngine;

namespace Modules.ProfileModule.Shared.Data.ValueObjects
{
    [Serializable]
    public sealed class ProfileIconVO
    {
        public string Id;
        public string DisplayName;
        public Sprite Sprite;
        public Color FallbackColor;

        public ProfileIconVO(ProfileIconCVO source)
        {
            Id = source.Id;
            DisplayName = source.DisplayName;
            Sprite = source.Sprite;
            FallbackColor = source.FallbackColor;
        }
    }
}
