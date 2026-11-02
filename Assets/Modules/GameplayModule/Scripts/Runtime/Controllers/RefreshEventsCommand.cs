using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using Modules.GameplayModule.Shared.Data.UnityObjects;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>
    /// A new day picks new quests and may start the login run again. With no progress or events asset
    /// filed there are no events, and the sequence stops here.
    /// </summary>
    internal class RefreshEventsCommand : Command
    {
        [Inject] private ISharedDataModel _shared { get; set; }

        public override void Execute()
        {
            var progress = _shared.GetScriptable<SD_JellyProgress>();
            var config = _shared.GetScriptable<CD_Events>();
            if (progress == null || config == null)
            {
                Retain();
                Stop();
                return;
            }
            EventsFunction.RefreshDay(progress, config);
        }
    }
}
