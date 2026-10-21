using System.Collections.Generic;
using Modules.LocalizationModule.Shared.Data.ValueObjects;
using Modules.LocalizationModule.Shared.Enums;
using UnityEngine;
namespace Modules.LocalizationModule.Shared.Data.UnityObjects
{
    /// <summary>
    /// A set of texts, each by key, in every language. A game splits its texts into as many tables
    /// as it likes - one per screen, say - and lists them on CD_Localization. Each table exports to
    /// and imports from one CSV file: a Key column, then one column per language code.
    /// </summary>
    [CreateAssetMenu(fileName = "CD_LocalizationTable", menuName = "Localization/Table")]
    public sealed class CD_LocalizationTable : ScriptableObject
    {
        [Tooltip("Game: read by the game through ILocalizationService. Editor: the Unity editor's own labels, keyed by their English.")]
        public LocalizationUsage Usage = LocalizationUsage.Game;
        public List<LocalizedTextCVO> Texts = new();

        public LocalizedTextCVO Find(string key)
        {
            foreach (var text in Texts) if (text.Key == key) return text;
            return null;
        }
        /// <summary>The entry for the key, added empty when the table has none.</summary>
        public LocalizedTextCVO FindOrAdd(string key)
        {
            var text = Find(key);
            if (text != null) return text;
            text = new LocalizedTextCVO { Key = key };
            Texts.Add(text);
            return text;
        }
    }
}
