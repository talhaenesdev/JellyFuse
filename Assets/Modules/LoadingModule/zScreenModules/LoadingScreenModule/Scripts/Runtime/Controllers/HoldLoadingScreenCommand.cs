using System;
using System.Threading.Tasks;
using FlowIoC.BaseModule.Attributes;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.LoadingModule.LoadingScreenModule.Models;
using UnityEngine;

namespace Modules.LoadingModule.LoadingScreenModule.Controllers
{
    /// <summary>
    /// Keeps a set's screen up until its MinimumScreenSeconds have passed since it opened. The set
    /// itself has already ended, so nothing waits on this but the screen. Three ways out - no hold,
    /// the wait ended, the wait threw - and each one resolves the retain.
    /// </summary>
    [LongRetain]
    internal class HoldLoadingScreenCommand : Command
    {
        [Inject] private ILoadingScreenModel _model { get; set; }
        [SignalParam] private string _set { get; set; }

        public override async void Execute()
        {
            Retain();
            float hold = _model.RemainingHold(_set, Time.realtimeSinceStartup);
            if (hold <= 0f)
            {
                Release();
                return;
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(hold));
                Release();
            }
            catch (Exception exception)
            {
                FlowLogger.LogError($"HoldLoadingScreenCommand threw while holding the loading screen: {exception}");
                Stop();
            }
        }
    }
}
