#if UNITY_EDITOR
using FlowIoC.BaseModule.Signals;

namespace Modules.LocalizationModule.LocalizationTestModule.Signals
{
    /// <summary>What the test module says to its own commands.</summary>
    internal class LocalizationTestInternalSignals : ISignalHolder
    {
        public Signal Verify = new();
    }
}
#endif
