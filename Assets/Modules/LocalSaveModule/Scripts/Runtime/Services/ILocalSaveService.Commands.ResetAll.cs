using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.LocalSaveModule.Models;

namespace Modules.LocalSaveModule.Services
{
    public partial interface ILocalSaveService
    {
        public static partial class Commands
        {
            /// <summary>
            /// Every persisted asset back to what a new player starts with, written to disk. The
            /// modules that read the assets keep what they read at startup: start the game again
            /// after it.
            /// </summary>
            public class ResetAll : Command
            {
                [Inject] private ILocalSaveModel _model { get; set; }

                public override void Execute() => _model.ResetAll();
            }
        }
    }
}
