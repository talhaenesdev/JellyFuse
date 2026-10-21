using System;
using Modules.GameplayModule.Shared.Enums;

namespace Modules.GameplayModule.Shared.Data.ValueObjects
{
    /// <summary>A booster in the player's save: the uses left, and whether its free uses were already given.</summary>
    [Serializable]
    public sealed class BoosterSVO
    {
        public BoosterType Type;
        public int Count;
        public bool Granted;
    }
}
