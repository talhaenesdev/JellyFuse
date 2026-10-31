using FlowIoC.BaseModule.Adapters;
using FlowIoC.BaseModule.Constructables;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.AdsModule.Data.UnityObjects;
using Modules.AdsModule.RootsContexts;
using UnityEngine;

namespace Modules.AdsModule.Models
{
    public sealed class AdsModel : IConstructable
    {
        [Inject(nameof(AdsServiceContext))] private GameObject _root { get; set; }
        public CD_Ads Config { get; private set; }
        public bool IsPostConstructed { get; set; }
        public bool IsDeconstructed { get; set; }
        public void PostConstruct() => Config = _root.GetComponent<RootAdapter>().GetScriptable<CD_Ads>();
    }
}
