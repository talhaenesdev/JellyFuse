using System;
using System.Collections.Generic;
using Modules.LocalizationModule.Shared.Data.UnityObjects;
using Modules.LocalizationModule.Shared.Data.ValueObjects;
using Modules.LocalizationModule.Shared.Enums;

namespace Modules.LocalizationModule.Models
{
    /// <summary>
    /// The language in use and its texts. The lookup is built once per language, so a Get is one
    /// dictionary read and allocates nothing.
    /// </summary>
    public class LocalizationModel : ILocalizationModel
    {
        private readonly Dictionary<string, string> _texts = new();

        public string Language { get; private set; } = "";
        public CD_Localization Config { get; private set; }
        public IReadOnlyList<LanguageCVO> Languages => Config != null ? Config.Languages : Array.Empty<LanguageCVO>();

        public void Load(CD_Localization config) => Config = config;

        public bool Offers(string code) => Config != null && Config.Find(code) != null;

        public void SetLanguage(string code)
        {
            Language = code;
            _texts.Clear();
            if (Config == null) return;
            foreach (var table in Config.Tables)
            {
                if (table == null || table.Usage != LocalizationUsage.Game) continue;
                foreach (var text in table.Texts)
                {
                    if (string.IsNullOrEmpty(text.Key) || _texts.ContainsKey(text.Key)) continue;
                    var value = text.In(code) ?? text.In(Config.SourceLanguage);
                    if (value != null) _texts[text.Key] = value;
                }
            }
        }

        public string Get(string key) => key != null && _texts.TryGetValue(key, out var text) ? text : key;
    }
}
