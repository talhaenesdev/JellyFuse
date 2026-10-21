using System.Collections.Generic;
using Modules.LocalizationModule.Shared.Data.ValueObjects;
using UnityEngine;
namespace Modules.LocalizationModule.Shared.Data.UnityObjects
{
    /// <summary>
    /// A game's localization: the languages it offers, the one its texts are written in first, and
    /// its tables. Filed once, on LocalizationServiceRoot's Shared Scriptables.
    /// </summary>
    [CreateAssetMenu(fileName = "CD_Localization", menuName = "Localization/Localization")]
    public sealed class CD_Localization : ScriptableObject
    {
        [Tooltip("The language every text is written in first. A text missing in the chosen language shows in this one; missing here too, its key shows.")]
        public string SourceLanguage = "en";
        [Tooltip("Used on a first launch when the device's language is not offered.")]
        public string DefaultLanguage = "en";
        public List<LanguageCVO> Languages = new() { new LanguageCVO() };
        public List<CD_LocalizationTable> Tables = new();

        public LanguageCVO Find(string code)
        {
            foreach (var language in Languages) if (language.Code == code) return language;
            return null;
        }
        public LanguageCVO Find(SystemLanguage system)
        {
            foreach (var language in Languages) if (language.System == system) return language;
            return null;
        }
    }
}
