namespace Modules.UiWidgetsModule.Shared.Enums
{
    /// <summary>How a panel comes in when it opens.</summary>
    public enum PanelOpenStyle
    {
        /// <summary>Grows from Start Scale to its size.</summary>
        Pop = 0,
        /// <summary>Slides up from Slide Distance below.</summary>
        SlideUp = 1,
        /// <summary>Drops from Slide Distance above.</summary>
        SlideDown = 2,
        /// <summary>Only fades in.</summary>
        Fade = 3,
    }
}
