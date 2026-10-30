using System.Collections.Generic;
using Modules.GameplayModule.Shared.Data.ValueObjects;
using UnityEngine;

namespace Modules.GameplayModule.Shared.Data.UnityObjects
{
    /// <summary>What each product the shop sells for real money gives the player.</summary>
    [CreateAssetMenu(fileName = "CD_ShopProducts", menuName = "Jelly Fuse/Shop Products")]
    public sealed class CD_ShopProducts : ScriptableObject
    {
        public List<ShopProductCVO> Products = new();

        public ShopProductCVO Find(string productId) => Products.Find(p => p != null && p.ProductId == productId);
    }
}
