using System.Collections.Generic;
using Modules.PetHomeModule.Shared.Data.ValueObjects;
using UnityEngine;

namespace Modules.PetHomeModule.Shared.Data.UnityObjects
{
    /// <summary>
    /// The pet home's shop: every bowl, bed, toy, decoration and room, with its price. What a new player is given is
    /// listed by id - a bowl and a bed - with the room they start in, and stands in the room from the start.
    /// </summary>
    [CreateAssetMenu(fileName = "CD_PetHome", menuName = "Jelly Fuse/Pet Home")]
    public sealed class CD_PetHome : ScriptableObject
    {
        public List<PetHomeItemCVO> Items = new();
        [Tooltip("Ids a new player owns and finds in the room.")]
        public List<string> StarterItems = new();
        [Tooltip("The room a new player lives in.")]
        public string StarterRoom;

        public PetHomeItemCVO Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (var item in Items) if (item != null && item.Id == id) return item;
            return null;
        }
    }
}
