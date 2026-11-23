using Modules.PetHomeModule.Shared.Data.ValueObjects;

namespace Modules.PetHomeModule.Models
{
    /// <summary>The player's pet home and the rules that keep it valid: only owned items stand in the room, once each.</summary>
    internal interface IPetHomeModel
    {
        PetHomeItemCVO Item(string id);
        bool Owns(string id);
        /// <summary>A new player's starter items, given once.</summary>
        void GiveStarters();
        void Own(string id);
        /// <summary>An owned item put in the room at its own spot, or an owned room moved into.</summary>
        void Use(string id);
        void Place(PlacedItemSVO placed);
        void Store(string id);
        PetHomeRVO Snapshot();
    }
}
