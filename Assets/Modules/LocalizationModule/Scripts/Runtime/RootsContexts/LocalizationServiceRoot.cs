using FlowIoC.BaseModule.Root;

namespace Modules.LocalizationModule.RootsContexts
{
    /// <summary>
    /// The module's one presence in a scene. File the game's CD_Localization on its adapter's Shared
    /// Scriptables. Initialize Order -40: a Service, up before the loading service opens a screen.
    /// </summary>
    public class LocalizationServiceRoot : Root<LocalizationServiceContext>
    {
    }
}
