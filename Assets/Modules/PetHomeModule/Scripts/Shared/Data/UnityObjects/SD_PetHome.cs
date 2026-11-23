using System.Collections.Generic;
using Modules.PetHomeModule.Shared.Data.ValueObjects;
using UnityEngine;

namespace Modules.PetHomeModule.Shared.Data.UnityObjects
{
    /// <summary>The player's pet home, kept between runs: what they bought, what stands where, and the room.</summary>
    [CreateAssetMenu(fileName = "SD_PetHome", menuName = "Jelly Fuse/Pet Home Save")]
    public sealed class SD_PetHome : ScriptableObject
    {
        [Tooltip("The starter items have been given.")]
        public bool Started;
        public List<string> Owned = new();
        public List<PlacedItemSVO> Placed = new();
        public string Room;
    }
}
