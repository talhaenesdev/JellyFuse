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
        /// <summary>Coins it costs to unlock; 0 when free.</summary>
        public int Price;
        /// <summary>The player may pick it: free, bought, or already theirs.</summary>
        public bool Owned = true;

        public ProfileIconVO(ProfileIconCVO source, bool owned = true)
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
