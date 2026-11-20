using System;
using UnityEngine;

namespace Modules.ProfileModule.Shared.Data.ValueObjects
{
    [Serializable]
    public sealed class ProfileFrameVO
    {
        public string Id;
        public string DisplayName;
        public Sprite Sprite;
        public Color FallbackColor;
        /// <summary>Coins it costs to unlock.</summary>
        public int Price;
        /// <summary>The player may wear it: free, or bought.</summary>
        public bool Owned = true;

        public ProfileFrameVO(ProfileFrameCVO source, bool owned = true)
        {
            Price = source.Price;
            Owned = owned;
            Id = source.Id;
            DisplayName = source.DisplayName;
            Sprite = source.Sprite;
            FallbackColor = source.FallbackColor;
        }
    }
}
