#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using FlowIoC.BaseModule.Attributes;
using FlowIoC.Editor.ModulePanels;
using Modules.LocalizationModule.Constants;
using Modules.LocalizationModule.Shared.Data.UnityObjects;
using Modules.LocalizationModule.Shared.Enums;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
namespace Modules.LocalizationModule.Editor
{
    /// <summary>
    /// Localization from the editor: the developer's own editor language, the language the game
    /// starts in on this machine, how much of each table is translated, and CSV export and import
    /// for translators. It reads and writes the tables and prefs; it never touches a running game.
    /// </summary>
    internal sealed class LocalizationPanel : ModulePanel
    {
        private const string MENU_PATH = "Tools/FlowIoC-Modules/Localization/Panel";
        private const int MENU_PRIORITY = -1080;

        private readonly LocalizationCsv _csv = new();
        private string _result;

        [MenuItem(MENU_PATH, false, MENU_PRIORITY)]
        private static void Open() => ModulePanelWindow.Open<LocalizationPanel>();
        // The project's own tools live under Floria; the same panel opens from there.
        [MenuItem("Floria/Localization", false, 50)]
        private static void OpenFromFloria() => ModulePanelWindow.Open<LocalizationPanel>();

        public override string Title => "Localization";
        public override string Module => "LocalizationModule";
        public override string Subtitle => "Languages, translations and CSV for translators";
        public override FlowRole Role => FlowRole.Service;

        public override void Draw(ModulePanelPainter painter)
        {
            var config = EditorTexts.Localization();
            var languages = config != null ? config.Languages : new List<Shared.Data.ValueObjects.LanguageCVO>();
            var names = languages.Select(l => l.Name + " (" + l.Code + ")").ToList();

            painter.Heading("Editor language");
            var editorNames = new List<string> { "English" };
            editorNames.AddRange(languages.Where(l => l.Code != EditorTexts.ENGLISH).Select(l => l.Name));
            var editorCodes = new List<string> { EditorTexts.ENGLISH };
            editorCodes.AddRange(languages.Where(l => l.Code != EditorTexts.ENGLISH).Select(l => l.Code));
            int current = Mathf.Max(0, editorCodes.IndexOf(EditorTexts.Code));
            int picked = painter.Popup("Editor language", current, editorNames.ToArray(), "The language of the inspectors and tools on this machine only.");
            if (picked != current) { EditorTexts.Code = editorCodes[picked]; InternalEditorUtility.RepaintAllViews(); }
            bool collect = EditorPrefs.GetBool(EditorTexts.COLLECT_PREF, true);
            painter.Actions("While on, an editor text no Editor table has is added to the first one, untranslated, ready to fill in.",
                new ModulePanelAction(collect ? "Collecting new editor texts: on" : "Collecting new editor texts: off",
                    () => EditorPrefs.SetBool(EditorTexts.COLLECT_PREF, !collect)));

            painter.Space();
            painter.Heading("Game language");
            if (config == null)
            {
                painter.Warning("No CD_Localization in the project. Create one (Create > Localization > Localization) and file it on LocalizationServiceRoot.");
                return;
            }
            string saved = PlayerPrefs.GetString(LocalizationConstants.PREFS_KEY, "");
            int game = languages.FindIndex(l => l.Code == saved);
            var gameNames = new List<string> { "Device language" };
            gameNames.AddRange(names);
            int gamePicked = painter.Popup("Start the game in", game + 1, gameNames.ToArray(),
                "What the game starts in on this machine, as if the player had picked it. Device language clears the choice.");
            if (gamePicked != game + 1)
            {
                if (gamePicked == 0) PlayerPrefs.DeleteKey(LocalizationConstants.PREFS_KEY);
                else PlayerPrefs.SetString(LocalizationConstants.PREFS_KEY, languages[gamePicked - 1].Code);
                PlayerPrefs.Save();
            }

            painter.Space();
            painter.Heading("Tables");
            var codes = languages.Select(l => l.Code).ToList();
            var tables = EditorTexts.Tables(LocalizationUsage.Game).Concat(EditorTexts.Tables(LocalizationUsage.Editor)).ToList();
            if (tables.Count == 0) painter.Note("No CD_LocalizationTable yet. Create one: Create > Localization > Table.");
            foreach (var table in tables)
            {
                var progress = new StringBuilder(table.Texts.Count + " texts");
                foreach (var code in codes)
                {
                    if (table.Usage == LocalizationUsage.Editor && code == EditorTexts.ENGLISH) continue;
                    progress.Append(" · ").Append(code).Append(' ').Append(table.Texts.Count(t => t.In(code) != null));
                }
                bool listed = config.Tables.Contains(table);
                painter.Field(table.name + (table.Usage == LocalizationUsage.Editor ? " (editor)" : listed ? "" : " (not on CD_Localization)"), progress.ToString());
                painter.Actions(
                    new ModulePanelAction("Select", () => { Selection.activeObject = table; EditorGUIUtility.PingObject(table); }),
                    new ModulePanelAction("Export CSV", () => Export(table, codes)),
                    new ModulePanelAction("Import CSV", () => Import(table)));
            }
            if (!string.IsNullOrEmpty(_result)) painter.Note(_result);
        }

        private void Export(CD_LocalizationTable table, List<string> codes)
        {
            string path = EditorUtility.SaveFilePanel("Export " + table.name, "", table.name + ".csv", "csv");
            if (string.IsNullOrEmpty(path)) return;
            File.WriteAllText(path, _csv.Export(table, codes), new UTF8Encoding(true));
            _result = "Exported " + table.Texts.Count + " texts to " + path;
        }

        private void Import(CD_LocalizationTable table)
        {
            string path = EditorUtility.OpenFilePanel("Import into " + table.name, "", "csv");
            if (string.IsNullOrEmpty(path)) return;
            Undo.RecordObject(table, "Import " + table.name);
            int set = _csv.Import(table, File.ReadAllText(path, Encoding.UTF8), out string error);
            if (error != null) { _result = error; return; }
            EditorUtility.SetDirty(table);
            AssetDatabase.SaveAssetIfDirty(table);
            _result = "Imported " + set + " texts into " + table.name + " from " + Path.GetFileName(path);
        }
    }
}
#endif
