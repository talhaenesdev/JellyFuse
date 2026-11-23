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
        [Tooltip("A bowl: how it looks once the pet has eaten it empty.")]
        public Sprite EmptySprite;
        [Tooltip("A room: where its painted floor is, as a share of the room picture - x, y from the bottom left, width, height. Items and the pet stay inside it.")]
        public Rect Floor = new(0.3f, 0.08f, 0.4f, 0.42f);
    }
}
