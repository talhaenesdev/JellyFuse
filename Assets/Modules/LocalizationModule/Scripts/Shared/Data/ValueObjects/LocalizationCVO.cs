using System;
using System.Collections.Generic;
using UnityEngine;
namespace Modules.LocalizationModule.Shared.Data.ValueObjects
{
    /// <summary>A language the game offers.</summary>
    [Serializable]
    public sealed class LanguageCVO
    {
        [Tooltip("A short code that never changes once texts use it: en, tr, de. It is the column name in CSV.")]
        public string Code = "en";
        [Tooltip("The language's own name, as a language picker shows it: English, Türkçe, Deutsch.")]
        public string Name = "English";
        [Tooltip("The device language that picks this one on a first launch.")]
        public SystemLanguage System = SystemLanguage.English;
    }

    /// <summary>One text in one language.</summary>
    [Serializable]
    public sealed class TranslationCVO
    {
        public string Code;
        [TextArea(1, 4)] public string Text;
    }

    /// <summary>One text, by key, in every language it has been written in.</summary>
    [Serializable]
    public sealed class LocalizedTextCVO
    {
        [Tooltip("What code asks for. For an Editor table the key is the English text itself.")]
        public string Key;
        public List<TranslationCVO> Translations = new();

        /// <summary>The text in the language, or null when it has none (an empty one counts as none).</summary>
        public string In(string code)
        {
            foreach (var translation in Translations)
                if (translation.Code == code) return string.IsNullOrEmpty(translation.Text) ? null : translation.Text;
            return null;
        }
        public void Set(string code, string text)
        {
            foreach (var translation in Translations)
                if (translation.Code == code) { translation.Text = text; return; }
            Translations.Add(new TranslationCVO { Code = code, Text = text });
        }
    }
}
