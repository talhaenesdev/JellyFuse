using System.Collections.Generic;

namespace Modules.PetHomeModule.Shared.Data.ValueObjects
{
    /// <summary>How the pet home stands: the shop's items, which are the player's, what stands where, and the room.</summary>
    public sealed class PetHomeRVO
    {
        public IReadOnlyList<PetHomeItemCVO> Items;
        public IReadOnlyCollection<string> Owned;
        public IReadOnlyList<PlacedItemSVO> Placed;
        public string Room;
        /// <summary>Bowls the pet has eaten empty, by id.</summary>
        public IReadOnlyCollection<string> Empty;
        /// <summary>How happy the pet is, out of 100.</summary>
        public int Happiness;
    }
}
