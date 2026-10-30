using FlowIoC.BaseModule.Attributes;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.AppUpdateModule.Services;
using UnityEngine;

namespace Modules.AppUpdateModule.Controllers
{
    /// <summary>
    /// Asks Play for a newer version and, when there is one, makes the player install it: backing out
    /// of Play's update screen closes the game. A check or an update that fails lets the game go on.
    /// </summary>
    [LongRetain]
    internal class RunMandatoryUpdateCommand : Command
    {
        [Inject] private IAppUpdateService _appUpdate { get; set; }

        public override void Execute()
        {
            Retain();
            _appUpdate.FetchImmediateUpdate(OnAnswered);
        }

        private void OnAnswered(bool available)
        {
            if (!available)
            {
                Release();
                return;
            }

            _appUpdate.StartImmediateUpdate(OnNotUpdated);
        }

        private void OnNotUpdated(bool canceled)
        {
            if (canceled)
            {
                FlowLogger.Log("Update declined - closing the game");
                Release();
                Application.Quit();
                return;
            }

            FlowLogger.LogWarning("Play update failed - the game goes on");
            Release();
        }
    }
}
