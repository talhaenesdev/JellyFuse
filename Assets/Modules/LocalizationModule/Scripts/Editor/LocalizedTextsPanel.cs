#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using FlowIoC.BaseModule.Attributes;
using FlowIoC.Editor.ModulePanels;
using Modules.LocalizationModule.ViewsMediators;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Modules.LocalizationModule.Editor
{
    /// <summary>
    /// Every text in the prefab being edited, or under the selected object: which show a key, which still hold
    /// fixed words, and which the game fills in itself. A fixed text is localized with one press, or all of them at
    /// once; a key can be searched for, with the code that asks for it.
    /// </summary>
    internal sealed class LocalizedTextsPanel : ModulePanel
    {
        private const string MENU_PATH = "Tools/FlowIoC-Modules/Localization/Texts";

        private readonly LocalizedTextTools _tools = new();
        private readonly LocalizedTextIndex _index = new();
        private string _search = "";
        private string _result;

        [MenuItem(MENU_PATH, false, -1079)]
        private static void Open() => ModulePanelWindow.Open<LocalizedTextsPanel>();
        [MenuItem("Floria/Texts", false, 51)]
        private static void OpenFromFloria() => ModulePanelWindow.Open<LocalizedTextsPanel>();

        public override string Title => "Texts";
        public override string Module => "LocalizationModule";
        public override string Subtitle => "Every text on a screen, and its translations";
        public override FlowRole Role => FlowRole.Service;

        public override void Draw(ModulePanelPainter painter)
        {
            // Finding a text comes first: it is what the panel is opened for most.
            DrawSearch(painter);
            painter.Space();

            var root = Root(out string where);
            painter.Heading("Texts in " + where);
            if (root == null)
            {
                painter.Note("Open a screen's prefab, or select an object in the scene: its texts are listed here.");
                return;
            }

            var texts = root.GetComponentsInChildren<TMP_Text>(true);
            var fixedTexts = texts.Where(_tools.IsFixed).ToList();
            int localized = texts.Count(t => t.GetComponent<LocalizedTextView>() != null);
            painter.Field("Localized", localized + " of " + texts.Length);
            painter.Field("Fixed words", fixedTexts.Count.ToString(),
                "Texts that hold words but show no key: they read the same in every language.");

            var table = _tools.DefaultTable();
            if (fixedTexts.Count > 0 && table != null)
                painter.Actions($"Gives every fixed text a key in {table.name}, carrying its words as they read now. Texts with the same words share one key. Undoable.",
                    new ModulePanelAction($"Localize all {fixedTexts.Count}", () => LocalizeAll(fixedTexts, table)));
            if (!string.IsNullOrEmpty(_result)) painter.Note(_result);

            painter.Space();
            foreach (var text in texts)
            {
                var view = text.GetComponent<LocalizedTextView>();
                string words = Short(text.text);
                if (view != null)
                {
                    var (_, entry) = _tools.Find(view.Key);
                    string state = entry == null ? "key missing: " + view.Key : view.Key;
                    painter.Field(text.name, state, words);
                    painter.Actions(new ModulePanelAction("Select", () => Selection.activeObject = view));
                }
                else if (fixedTexts.Contains(text))
                {
                    painter.Field(text.name, "fixed: \"" + words + "\"");
                    painter.Actions(
                        new ModulePanelAction("Select", () => Selection.activeObject = text),
                        new ModulePanelAction("Localize", () => Selection.activeObject = _tools.Localize(text, table), table != null));
                }
                else
                {
                    painter.Field(text.name, string.IsNullOrEmpty(words) ? "filled by the game" : "filled by the game (\"" + words + "\")");
                }
            }
        }

        private void DrawSearch(ModulePanelPainter painter)
        {
            painter.Heading("Find a text in the whole game",
                new ModulePanelAction("Rescan", () => _index.Build()));
            _search = painter.TextField("Search", _search, help: "Part of the words as they read in the game, in any language, or of a key.");
            if (string.IsNullOrWhiteSpace(_search))
            {
                painter.Note("Type a few words you see in the game. Each match shows the screens that show it, with Open to go straight to the text, and the code lines that fill it.");
                return;
            }
            if (!_index.Built) _index.Build();

            var found = _tools.Search(_search, 15);
            if (found.Count == 0) painter.Note("No text holds \"" + _search + "\".");
            foreach (var (key, words) in found)
            {
                painter.Space();
                painter.Field(key, Short(words));
                foreach (var place in _index.PlacesOf(key))
                {
                    var target = place;
                    painter.Field("   in " + System.IO.Path.GetFileNameWithoutExtension(place.Prefab), place.Path);
                    painter.Actions(new ModulePanelAction("Open", () => _index.Open(target)));
                }
                foreach (var line in _index.CodeOf(key).Take(4))
                {
                    var target = line;
                    painter.Field("   code " + System.IO.Path.GetFileNameWithoutExtension(line.File) + ":" + line.Line, Short(line.Text));
                    painter.Actions(new ModulePanelAction("Open", () => _index.Open(target)));
                }
                if (_index.PlacesOf(key).Count == 0 && _index.CodeOf(key).Count == 0)
                    painter.Note("   Not shown anywhere yet.");
            }
        }

        private void LocalizeAll(List<TMP_Text> texts, Shared.Data.UnityObjects.CD_LocalizationTable table)
        {
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Localize all texts");
            foreach (var text in texts) _tools.Localize(text, table);
            Undo.CollapseUndoOperations(group);
            _result = $"{texts.Count} texts localized. Save the prefab to keep them.";
        }

        /// <summary>The prefab open in Prefab Mode, else the selected object's top parent.</summary>
        private static GameObject Root(out string where)
        {
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null)
            {
                where = stage.prefabContentsRoot.name;
                return stage.prefabContentsRoot;
            }
            var selected = Selection.activeGameObject;
            where = selected != null ? selected.name : "nothing selected";
            return selected;
        }

        private static string Short(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            string flat = text.Replace("\n", " ");
            return flat.Length > 48 ? flat.Substring(0, 48) + "…" : flat;
        }
    }
}
#endif
