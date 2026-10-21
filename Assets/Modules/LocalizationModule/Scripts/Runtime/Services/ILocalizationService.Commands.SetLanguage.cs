using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;

namespace Modules.LocalizationModule.Services
{
    public partial interface ILocalizationService
    {
        public static partial class Commands
        {
            /// <summary>
            /// Switches the language as a step of a game's own sequence:
            /// <c>.ToSequence&lt;ILocalizationService.Commands.SetLanguage&gt;("tr")</c>.
            /// </summary>
            public class SetLanguage : Command<string>
            {
                [Inject] private ILocalizationService _localization { get; set; }
                public override void Execute(string code) => _localization.SetLanguage(code);
            }
        }
    }
}
