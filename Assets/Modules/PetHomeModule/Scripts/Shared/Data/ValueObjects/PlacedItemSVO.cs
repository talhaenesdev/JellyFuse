using System;

namespace Modules.PetHomeModule.Shared.Data.ValueObjects
{
    /// <summary>An item standing in the room, and where on the floor - 0,0 front left to 1,1 back right.</summary>
    [Serializable]
    public sealed class PlacedItemSVO
    {
        public string Id;
        public float X;
        public float Y;

        public PlacedItemSVO() { }
        public PlacedItemSVO(string id, float x, float y) { Id = id; X = x; Y = y; }
    }
}
