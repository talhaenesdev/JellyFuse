namespace Modules.LocalizationModule.Shared.Enums
{
    /// <summary>Who reads a table: the game, through ILocalizationService, or the Unity editor, through EditorTexts.</summary>
    public enum LocalizationUsage
    {
        Game = 0,
        Editor = 1,
    }
}
