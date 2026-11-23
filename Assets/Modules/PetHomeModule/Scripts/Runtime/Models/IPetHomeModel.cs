using Modules.PetHomeModule.Shared.Data.ValueObjects;

namespace Modules.PetHomeModule.Models
{
    /// <summary>The player's pet home and the rules that keep it valid: only owned items stand in the room, once each.</summary>
    internal interface IPetHomeModel
    {
        /// <summary>Whether the shop's config has been loaded yet.</summary>
        bool HasConfig { get; }
        void UseConfig(Shared.Data.UnityObjects.CD_PetHome config);
        PetHomeItemCVO Item(string id);
        bool Owns(string id);
        /// <summary>A new player's starter items, given once.</summary>
        void GiveStarters();
        void Own(string id);
        /// <summary>An owned item put in the room at its own spot, or an owned room moved into.</summary>
        void Use(string id);
        void Place(PlacedItemSVO placed);
        void Store(string id);
        /// <summary>A bowl standing in the room, eaten empty.</summary>
        void Empty(string id);
        void Refill(string id);
        PetHomeRVO Snapshot();
        /// <summary>Something done for the pet: happier, up to full - and a full pet is owed a reward.</summary>
        void Care(Shared.Enums.PetHomeCare care);
        /// <summary>The coins a full pet is owed, once; 0 when none are.</summary>
        int TakeReward();
        /// <summary>The time since the home was last seen: happiness wears off, and the pet eats from its bowls.</summary>
        void PassTime(System.DateTime nowUtc);
    }
}
