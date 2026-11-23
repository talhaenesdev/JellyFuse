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

        [Header("Happiness")]
        [Tooltip("How happy a new pet is, out of 100.")]
        public int StartHappiness = 50;
        [Tooltip("Happiness a finished meal gives.")]
        public int HappyAte = 20;
        [Tooltip("Happiness a game with a ball gives.")]
        public int HappyPlayed = 12;
        [Tooltip("Happiness a pat on the head gives.")]
        public int HappyPetted = 3;
        [Tooltip("Happiness a nap in a bed gives.")]
        public int HappySlept = 10;
        [Tooltip("Happiness lost for every hour the player is away.")]
        public float HappyLossPerHour = 2;
        [Tooltip("Coins given each time the pet's happiness fills up.")]
        public int HappyReward = 50;
        [Tooltip("Where happiness starts again after a reward.")]
        public int HappyAfterReward = 40;
        [Tooltip("While the player is away the pet eats a full bowl this often, in hours, leaving it empty.")]
        public float HoursPerMeal = 4;

        public PetHomeItemCVO Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (var item in Items) if (item != null && item.Id == id) return item;
            return null;
        }
    }
}
