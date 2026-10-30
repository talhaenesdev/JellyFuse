using System.Collections.Generic;
using Modules.StoreModule.Data.ValueObjects;
using UnityEngine;

namespace Modules.StoreModule.Data.UnityObjects
{
    [CreateAssetMenu(fileName = "CD_StoreCatalog", menuName = "Store/CD_StoreCatalog")]
    public sealed class CD_StoreCatalog : ScriptableObject
    {
        public List<StoreProductCVO> Products = new();
    }
}
