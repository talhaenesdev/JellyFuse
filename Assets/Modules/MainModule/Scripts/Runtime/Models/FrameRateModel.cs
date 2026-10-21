using FlowIoC.BaseModule.Adapters;
using FlowIoC.BaseModule.Constructables;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.MainModule.Data.UnityObjects;
using Modules.MainModule.RootsContexts;
using UnityEngine;

namespace Modules.MainModule.Models
{
    /// <summary>The frame rate CD_FrameRate asks for; 60 when no config is filed.</summary>
    public sealed class FrameRateModel : IConstructable
    {
        public const int FALLBACK = 60;

        [Inject(nameof(MainContext))] private GameObject _root { get; set; }

        public bool IsPostConstructed { get; set; }
        public bool IsDeconstructed { get; set; }
        public int TargetFrameRate { get; private set; } = FALLBACK;

        public void PostConstruct()
        {
            var adapter = _root.GetComponent<RootAdapter>();
            var config = adapter != null ? adapter.GetScriptable<CD_FrameRate>() : null;
            TargetFrameRate = config != null ? config.TargetFrameRate : FALLBACK;
        }

        public void Deconstruct() { }
    }
}
