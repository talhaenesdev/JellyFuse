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
        [Tooltip("Bowls the pet has eaten empty, waiting to be filled.")]
        public List<string> EmptyBowls = new();
        [Tooltip("How happy the pet is, out of 100.")]
        public int Happiness = -1;
        [Tooltip("When the pet home was last looked at, as UTC ticks: time away is counted from here.")]
        public long LastSeen;
    }
}
