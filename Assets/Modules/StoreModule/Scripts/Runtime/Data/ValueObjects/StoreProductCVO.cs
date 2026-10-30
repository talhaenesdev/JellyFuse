using System;

namespace Modules.StoreModule.Data.ValueObjects
{
    [Serializable]
    public class StoreProductCVO
    {
        /// <summary>The product id, the same as in Play Console.</summary>
        public string Id;
        /// <summary>Bought again and again (coins), or once for ever (no ads).</summary>
        public bool Consumable = true;
    }
}
