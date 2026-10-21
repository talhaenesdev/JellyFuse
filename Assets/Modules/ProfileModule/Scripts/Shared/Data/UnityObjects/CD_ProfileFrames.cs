using System.Collections.Generic;
using Modules.ProfileModule.Shared.Data.ValueObjects;
using UnityEngine;

namespace Modules.ProfileModule.Shared.Data.UnityObjects
{
    [CreateAssetMenu(fileName = "CD_ProfileFrames", menuName = "Jelly Fuse/Profile/Frame Catalogue")]
    public sealed class CD_ProfileFrames : ScriptableObject
    {
        [SerializeField] private List<ProfileFrameCVO> _frames = new();
        public IReadOnlyList<ProfileFrameCVO> Frames => _frames;
    }
}
