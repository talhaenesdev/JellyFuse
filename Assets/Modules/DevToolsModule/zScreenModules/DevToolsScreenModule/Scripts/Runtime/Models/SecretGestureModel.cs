namespace Modules.DevToolsModule.DevToolsScreenModule.Models
{
    /// <summary>
    /// Where the secret corner gesture stands: how many corners were tapped in order, and when the
    /// first was. The order is top-left, top-right, bottom-right, bottom-left.
    /// </summary>
    internal sealed class SecretGestureModel
    {
        public const int CORNERS = 4;

        public bool IsListening { get; set; }
        public bool IsHooked { get; set; }
        /// <summary>Corners tapped in order so far, 0 to 3.</summary>
        public int Step { get; private set; }
        public float StartedAt { get; private set; }

        /// <summary>Takes a tap on a corner (0-3, or -1 for none) at a time; true when it finished the gesture.</summary>
        public bool Tap(int corner, float now, float windowSeconds)
        {
            if (Step > 0 && now - StartedAt > windowSeconds) Step = 0;
            if (corner != Step)
            {
                // A tap on the first corner starts afresh; anything else breaks the sequence.
                Step = corner == 0 ? 1 : 0;
                if (Step == 1) StartedAt = now;
                return false;
            }
            if (Step == 0) StartedAt = now;
            Step++;
            if (Step < CORNERS) return false;
            Step = 0;
            return true;
        }
    }
}
