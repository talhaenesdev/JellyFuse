using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.AccountModule.Services;
using Modules.AccountModule.Signals;

namespace Modules.AccountModule.Controllers
{
    /// <summary>
    /// Signs the player in and announces who they are. Offline is not a failure of the flow: the
    /// step ends either way, and SignedIn goes out only when there is a player to name.
    /// </summary>
    internal sealed class SignInCommand : Command
    {
        [Inject] private IAccountService _account { get; set; }
        [InjectSignal] private AccountSignals _signals { get; set; }

        /// <summary>Signed in, offline, threw - every way out resolves the retain.</summary>
        public override async void Execute()
        {
            Retain();
            try
            {
                bool signedIn = await _account.SignInAsync();
                if (!signedIn)
                {
                    Release();
                    return;
                }

                string playerId = _account.PlayerId;
                var signals = _signals;
                Release();
                signals.Outgoing.SignedIn.Dispatch(playerId);
            }
            catch (Exception exception)
            {
                FlowLogger.LogError($"SignInCommand threw while signing in: {exception}");
                Stop();
            }
        }
    }
}
