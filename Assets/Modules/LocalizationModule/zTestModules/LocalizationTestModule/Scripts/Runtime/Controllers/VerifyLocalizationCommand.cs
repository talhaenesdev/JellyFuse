#if UNITY_EDITOR
using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.ConsoleModule;
using Modules.LocalizationModule.Constants;
using Modules.LocalizationModule.Editor;
using Modules.LocalizationModule.Services;
using Modules.LocalizationModule.Shared.Data.UnityObjects;
using Modules.LocalizationModule.Signals;
using UnityEngine;

namespace Modules.LocalizationModule.LocalizationTestModule.Controllers
{
    /// <summary>
    /// The module's checks, run in LocalizationTestScene against CD_Localization_Test (English,
    /// Turkish, German). The developer's saved language is put back afterwards. One expected warning
    /// appears in the console: the check that a language the game does not offer is refused.
    /// </summary>
    internal class VerifyLocalizationCommand : Command
    {
        [Inject] private ILocalizationService _localization { get; set; }
        [InjectSignal] private LocalizationSignals _signals { get; set; }
        private int _checks;

        public override void Execute()
        {
            _checks = 0;
            bool hadChoice = PlayerPrefs.HasKey(LocalizationConstants.PREFS_KEY);
            string savedChoice = PlayerPrefs.GetString(LocalizationConstants.PREFS_KEY, "");
            string started = _localization.Language;
            int heard = 0;
            Action<string> listener = _ => heard++;
            _signals.Outgoing.LanguageChanged.AddListener(listener);
            try
            {
                Check(_localization.Languages.Count == 3, "The game offers the three test languages");
                Check(!string.IsNullOrEmpty(started), "A language is picked before the first Launch");

                _localization.SetLanguage("en");
                Check(_localization.Language == "en" && _localization.Get("test.hello") == "Hello", "English text by key");
                heard = 0;
                _localization.SetLanguage("tr");
                Check(_localization.Language == "tr" && _localization.Get("test.hello") == "Merhaba", "Switching language switches the texts");
                Check(heard == 1, "A change of language is announced once");
                _localization.SetLanguage("tr");
                Check(heard == 1, "Picking the language in use announces nothing");
                Check(_localization.Format("test.apples", 3) == "3 elma", "Format fills in the values");
                Check(_localization.Get("test.only_english") == "Only in English", "A text missing in the language falls back to the source language");
                Check(_localization.Get("test.no_such_key") == "test.no_such_key", "A key no table has comes back as the key");
                _localization.SetLanguage("de");
                Check(_localization.Get("test.hello") == "Hallo" && _localization.Get("test.apples") == "{0} apples", "A partly translated language uses the source for the rest");
                _localization.SetLanguage("xx");
                Check(_localization.Language == "de", "A language the game does not offer is refused");
                Check(PlayerPrefs.GetString(LocalizationConstants.PREFS_KEY, "") == "de", "The choice is kept for the next launch");
                CheckCsv();
            }
            finally
            {
                _signals.Outgoing.LanguageChanged.RemoveListener(listener);
                _localization.SetLanguage(started);
                if (hadChoice) PlayerPrefs.SetString(LocalizationConstants.PREFS_KEY, savedChoice);
                else PlayerPrefs.DeleteKey(LocalizationConstants.PREFS_KEY);
                PlayerPrefs.Save();
            }
            FlowLogger.Log("Localization: " + _checks + " checks passed.");
        }

        private void CheckCsv()
        {
            var csv = new LocalizationCsv();
            var source = ScriptableObject.CreateInstance<CD_LocalizationTable>();
            var target = ScriptableObject.CreateInstance<CD_LocalizationTable>();
            try
            {
                var text = source.FindOrAdd("menu.quote");
                text.Set("en", "Say \"hi\", then go");
                text.Set("tr", "\"Selam\" de,\nsonra git");
                source.FindOrAdd("menu.empty").Set("en", "Only English");
                string file = csv.Export(source, new[] { "en", "tr" });
                int set = csv.Import(target, "﻿" + file, out string error);
                Check(error == null && set == 4, "A CSV export imports back cell for cell");
                Check(target.Find("menu.quote").In("tr") == "\"Selam\" de,\nsonra git" && target.Find("menu.quote").In("en") == "Say \"hi\", then go",
                    "Commas, quotes and line breaks survive the CSV");
                Check(target.Find("menu.empty").In("tr") == null, "An empty cell stays untranslated");
                csv.Import(target, "Name,en\nx,y", out error);
                Check(error != null, "A CSV without a Key column is refused");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(source);
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        private void Check(bool condition, string name)
        {
            if (!condition) throw new InvalidOperationException("Localization check failed: " + name);
            _checks++;
        }
    }
}
#endif
