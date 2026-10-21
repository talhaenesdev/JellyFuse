namespace Modules.DevToolsModule.Data.ValueObjects
{
    /// <summary>
    /// One value the developer panel's tuning page lets a developer drag: the key it is reported
    /// back under, its label, its range and its value now. Whoever owns the value sends the list and
    /// applies the changes; the panel only draws them.
    /// </summary>
    public sealed class DevSliderVO
    {
        public string Key;
        public string Label;
        public float Min, Max, Value;
        /// <summary>Whole numbers only.</summary>
        public bool Whole;
    }
}
