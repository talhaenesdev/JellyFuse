using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ScreenModule.Service;
using Modules.LoadingModule.LoadingScreenModule.Models;
using Modules.LoadingModule.LoadingScreenModule.ViewsMediators;

namespace Modules.LoadingModule.LoadingScreenModule.Controllers
{
    /// <summary>
    /// Hides the screen when it is still showing the set that ended. A set that began again during
    /// the hold (a retry) keeps its screen.
    /// </summary>
    internal class CloseLoadingScreenCommand : Command
    {
        [Inject] private IScreenService _screenService { get; set; }
        [Inject] private ILoadingScreenModel _model { get; set; }
        [SignalParam] private string _set { get; set; }

        public override void Execute()
        {
            if (!_model.IsClosed(_set)) return;
            if (!_screenService.Check.IsScreenActive<LoadingScreenView>()) return;
            if (!_screenService.TryGet.Screen(out LoadingScreenView screen) || screen == null) return;
            if (screen.ShowingSet == _set) screen.Hide();
        }
    }
}
