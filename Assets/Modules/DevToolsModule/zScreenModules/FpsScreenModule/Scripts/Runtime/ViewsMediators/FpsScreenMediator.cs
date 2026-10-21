using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.ViewsMediators.Mediator;

namespace Modules.DevToolsModule.FpsScreenModule.ViewsMediators
{
    /// <summary>The readout takes no input and says nothing; its Mediator only holds the View.</summary>
    public class FpsScreenMediator : IMediator
    {
        [Inject] private FpsScreenView _view { get; set; }

        public void OnRegister() { }
        public void OnRemove() { }
    }
}
