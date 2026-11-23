using System;
using Modules.PetHomeModule.Shared.Enums;
using UnityEngine;

namespace Modules.PetHomeModule.Shared.Data.ValueObjects
{
    /// <summary>One thing the pet home sells: a bowl, a bed, a toy, a decoration or a whole room.</summary>
    [Serializable]
    public sealed class PetHomeItemCVO
    {
        public string Id;
        public PetHomeCategory Category;
        public Sprite Sprite;
        [Min(0)] public int Price;
        [Tooltip("How tall it stands in the room, as a share of the floor's height. Rooms ignore it.")]
        [Range(0.05f, 1f)] public float Height = 0.25f;
        [Tooltip("Where it is put when bought, on the floor from 0,0 (front left) to 1,1 (back right).")]
        public Vector2 Spot = new(0.5f, 0.5f);
    }
}
