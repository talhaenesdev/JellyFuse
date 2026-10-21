using System;
using UnityEngine;

namespace Modules.ProfileModule.Shared.Data.ValueObjects
{
    [Serializable]
    public sealed class ProfileFrameCVO
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField] private Sprite _sprite;
        [SerializeField] private Color _fallbackColor = Color.white;

        public string Id => _id;
        public string DisplayName => _displayName;
        public Sprite Sprite => _sprite;
        public Color FallbackColor => _fallbackColor;
    }
}
