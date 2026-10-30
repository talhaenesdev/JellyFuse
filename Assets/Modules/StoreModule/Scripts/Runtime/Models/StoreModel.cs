using FlowIoC.BaseModule.Adapters;
using FlowIoC.BaseModule.Constructables;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.StoreModule.Data.UnityObjects;
using Modules.StoreModule.RootsContexts;
using UnityEngine;

namespace Modules.StoreModule.Models
{
    public sealed class StoreModel : IConstructable
    {
        [Inject(nameof(StoreServiceContext))] private GameObject _root { get; set; }
        public CD_StoreCatalog Catalog { get; private set; }
        public bool IsPostConstructed { get; set; }
        public bool IsDeconstructed { get; set; }
        public void PostConstruct() => Catalog = _root.GetComponent<RootAdapter>().GetScriptable<CD_StoreCatalog>();
    }
}
