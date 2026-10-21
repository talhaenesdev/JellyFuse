using FlowIoC.BaseModule.Controller;
using FlowIoC.ConsoleModule;
using UnityEngine;

namespace Modules.MainModule.Controllers
{
    /// <summary>Closes the game; in the Editor, ends Play.</summary>
    internal sealed class QuitGameCommand : Command
    {
        public override void Execute()
        {
            FlowLogger.Log("QuitGameCommand - closing the game.");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
