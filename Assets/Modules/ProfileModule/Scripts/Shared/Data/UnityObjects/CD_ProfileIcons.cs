using System.Collections.Generic;
using Modules.ProfileModule.Shared.Data.ValueObjects;
using UnityEngine;

namespace Modules.ProfileModule.Shared.Data.UnityObjects
{
    [CreateAssetMenu(fileName = "CD_ProfileIcons", menuName = "Jelly Fuse/Profile/Icon Catalogue")]
    public sealed class CD_ProfileIcons : ScriptableObject
    {
        [SerializeField] private List<ProfileIconCVO> _icons = new();

        public IReadOnlyList<ProfileIconCVO> Icons => _icons;
    }
}
