namespace Modules.UiWidgetsModule.Enums
{
    /// <summary>What one text animation effect does.</summary>
    public enum TextAnimationType
    {
        /// <summary>Grows from Strength (as a share of its size) up to its size.</summary>
        Pop = 0,
        /// <summary>A quick swell by Strength and back, springing.</summary>
        Punch = 1,
        /// <summary>Fades in from clear.</summary>
        FadeIn = 2,
        /// <summary>Slides in from Offset (canvas units) to where it stands.</summary>
        SlideIn = 3,
        /// <summary>Its letters appear one after another, Char Delay seconds apart.</summary>
        Typewriter = 4,
        /// <summary>Shakes sideways by Strength canvas units, settling.</summary>
        Shake = 5,
        /// <summary>Swells by Strength and back, over and over.</summary>
        Pulse = 6,
        /// <summary>Its letters bob up and down by Strength canvas units, one after another, over and over.</summary>
        Wave = 7,
        /// <summary>The number in it counts from the one it showed before to the new one.</summary>
        CountUp = 8,
    }
}
