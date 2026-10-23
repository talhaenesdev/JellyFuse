#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Modules.LocalizationModule.Shared.Data.UnityObjects;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using Modules.GameplayModule.Shared.Data.ValueObjects;
using Modules.GameplayModule.Shared.Enums;
using UnityEditor;
using UnityEngine;

namespace Modules.GameplayModule.Editor
{
    /// <summary>
    /// Level designer for CD_JellyLevel. One tool at a time, and the board is where everything happens:
    /// click empty space to place, click an object to select it, drag it to move, right-click to delete.
    /// The colour and shape rows edit the selection when there is one, and set what the next click places.
    /// The board's outermost row and column are the wall round it; doors are gaps in that wall.
    /// </summary>
    [CustomEditor(typeof(CD_JellyLevel))]
    public sealed class JellyLevelEditor : UnityEditor.Editor
    {
        private enum Tool { Jelly = 0, Door = 1, Wall = 2, Hole = 3, Erase = 4, Tutorial = 5 }
        private enum Selection { None = 0, Shape = 1, Door = 2 }

        // Shapes offered in the Shape rows, in two rows of six. Cells are (x, y) with y up; ↻ rotates.
        private readonly (string Name, Vector2Int[] Cells)[] _presets =
        {
            ("Single", new[] { new Vector2Int(0, 0) }),
            ("Two", new[] { new Vector2Int(0, 0), new Vector2Int(1, 0) }),
            ("Three", new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0) }),
            ("Four", new[] { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2), new Vector2Int(0, 3) }),
            ("Square", new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) }),
            ("T", new[] { new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1), new Vector2Int(1, 0) }),
            ("S", new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(2, 1) }),
            ("Z", new[] { new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(1, 0), new Vector2Int(2, 0) }),
            ("L", new[] { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2), new Vector2Int(1, 0) }),
            ("J", new[] { new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(1, 2), new Vector2Int(0, 0) }),
            ("Small L", new[] { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(1, 0) }),
            ("Small J", new[] { new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(0, 0) }),
        };
        // Cells drawn round the board: the space off it. Walls left there by a board made smaller show up
        // here and can be erased. The width is the developer's taste, kept on this machine only.
        private const string MARGIN_PREF = "JellyFuse.LevelEditor.Margin";
        private int _margin = 1;
        // The editor's text in the developer's language (LocalizationModule). The English stays here.
        private readonly EditorTexts _texts = new();
        private string T(string english) => _texts.T(english);
        private string F(string english, params object[] values) => string.Format(_texts.T(english), values);
        private Tool _tool = Tool.Jelly;
        private Selection _selection;
        private int _shape, _door, _stage, _doorSize = 1, _undoGroup = -1, _tutorialStep;
        private JellyColour _colour = JellyColour.Pink;
        private List<Vector2Int> _brush = new() { Vector2Int.zero };
        private bool _dragging;
        private Vector2Int _dragFrom, _hovered;
        private bool _palette, _settings, _advanced;
        private Vector2 _scroll;
        private string _notice;

        // The map view's size is the user's taste, kept on this machine only.
        private const string CELL_PREF = "JellyFuse.LevelEditor.CellSize", HEIGHT_PREF = "JellyFuse.LevelEditor.ViewHeight";
        private float _cellSize = 28, _viewHeight = 420;

        // The inspector repaints on every mouse move over the map, so lookups that hit the asset
        // database or run the level's checks are cached and redone only when something changed.
        private List<CD_JellyLevels> _lists;
        private List<string> _errors;
        private int _errorsDirtyCount = -1;
        private void ForgetLists() { _lists = null; _paletteAsset = null; }

        private void OnEnable()
        {
            Undo.undoRedoPerformed += OnUndo;
            EditorApplication.projectChanged += ForgetLists;
            _cellSize = EditorPrefs.GetFloat(CELL_PREF, 28);
            _viewHeight = EditorPrefs.GetFloat(HEIGHT_PREF, 420);
            _margin = EditorPrefs.GetInt(MARGIN_PREF, 1);
        }
        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndo;
            EditorApplication.projectChanged -= ForgetLists;
        }
        private void OnUndo() { _dragging = false; Validate((CD_JellyLevel)target); Repaint(); }

        private void Change(CD_JellyLevel level, string label, Action action)
        {
            Undo.RecordObject(level, label);
            action();
            // A door opens the wall in front of it: a door placed, moved or widened onto walls takes them away.
            foreach (var wall in level.WallsInDoors()) level.Walls.Remove(wall);
            EditorUtility.SetDirty(level);
            _notice = null;
        }
        /// <summary>A level saved with walls in its doors (from before doors cleared them) is mended when it is opened.</summary>
        private static void ClearWallsInDoors(CD_JellyLevel level)
        {
            var walls = level.WallsInDoors();
            if (walls.Count == 0) return;
            Undo.RecordObject(level, "Clear walls in doors");
            foreach (var wall in walls) level.Walls.Remove(wall);
            EditorUtility.SetDirty(level);
        }
        private void Validate(CD_JellyLevel level)
        {
            if (_selection == Selection.Shape && _shape >= level.Shapes.Count) _selection = Selection.None;
            if (_selection == Selection.Door && _door >= level.Doors.Count) _selection = Selection.None;
            if (_selection == Selection.Door) _stage = Mathf.Clamp(_stage, 0, Mathf.Max(0, level.Doors[_door].Queue.Count - 1));
            else _stage = 0;
        }

        public override void OnInspectorGUI()
        {
            var level = (CD_JellyLevel)target;
            ClearWallsInDoors(level);
            Validate(level);
            DrawHeader(level);
            DrawOldExits(level);
            DrawTools();
            if (_tool is Tool.Jelly or Tool.Door || _selection != Selection.None) { DrawColours(level); DrawShapes(level); }
            if (_tool == Tool.Door || _selection == Selection.Door) DrawDoorSizes(level);
            if (_tool == Tool.Tutorial) DrawTutorial(level);
            DrawGrid(level);
            if (!string.IsNullOrEmpty(_notice)) EditorGUILayout.HelpBox(T(_notice), MessageType.Warning);
            DrawSelected(level);
            DrawBalance(level);
            DrawFooter(level);
        }

        // ---------- header, tools, rows ----------

        private void DrawHeader(CD_JellyLevel level)
        {
            EditorGUILayout.LabelField(T("JELLY • LEVEL DESIGNER"), EditorStyles.boldLabel);
            _settings = EditorGUILayout.Foldout(_settings, F("Level settings: {0}  |  {1} × {2}  |  {3} s", level.LevelName, level.Width, level.Height, level.Seconds), true);
            if (!_settings) return;
            serializedObject.Update();
            string[] fields = { "LevelName", "Hint", "Width", "Height", "Seconds", "Difficulty", "FreeArrowAfterFusion" };
            string[] labels = { "Level name", "Hint", "Width", "Height", "Time (s)", "Difficulty", "Free arrows after fusion" };
            for (int i = 0; i < fields.Length; i++) EditorGUILayout.PropertyField(serializedObject.FindProperty(fields[i]), new GUIContent(T(labels[i])));
            serializedObject.ApplyModifiedProperties();
            DrawPalette(level);
        }

        private void DrawOldExits(CD_JellyLevel level)
        {
            if (!level.HasOldExits) return;
            EditorGUILayout.HelpBox(T("This level still has exits from before doors. The game turns them into doors as it loads; convert them here to edit them as doors."), MessageType.Warning);
            if (GUILayout.Button(T("Convert old exits to doors"), GUILayout.Height(24)))
            { Change(level, "Convert old exits", level.ConvertOldExits); _selection = Selection.None; }
        }

        private void DrawTools()
        {
            EditorGUILayout.Space(4);
            var labels = new[] { T("Jelly"), T("Door"), T("Wall"), T("Empty"), T("Erase"), T("Tutorial") };
            var tool = (Tool)GUILayout.Toolbar((int)_tool, labels, GUILayout.Height(26));
            if (tool != _tool) { _tool = tool; _dragging = false; _notice = null; }
            string help = _tool switch
            {
                Tool.Jelly => "Click empty floor: place a jelly.  Click a jelly: select.  Drag: move.",
                Tool.Door => "Click the wall round the board (its outermost cells): place a door.  Click a door: select it, then set the jellies it takes below the board.  Drag: move.",
                Tool.Wall => "Drag to paint walls.",
                Tool.Hole => "Drag to paint empty cells: no floor, nothing can pass.",
                Tool.Tutorial => "Click a cell of the jelly to lead, then the cells to lead it through, in order; the last one is where it is let go of (a door cell sends it out). Right-click takes back the last cell.",
                _ => "Click or drag: delete what is underneath.",
            };
            EditorGUILayout.HelpBox(T(help) + "   " + T("Right-click deletes with any tool.  Esc deselects."), MessageType.None);
        }

        private CD_JellyPalette _paletteAsset;
        /// <summary>The shared palette: the one the level list uses, else any in the project.</summary>
        private CD_JellyPalette Palette()
        {
            if (_paletteAsset != null) return _paletteAsset;
            _paletteAsset = LevelLists().Select(l => l.Palette).FirstOrDefault(p => p != null);
            if (_paletteAsset == null)
            {
                var guid = AssetDatabase.FindAssets("t:CD_JellyPalette").FirstOrDefault();
                if (guid != null) _paletteAsset = AssetDatabase.LoadAssetAtPath<CD_JellyPalette>(AssetDatabase.GUIDToAssetPath(guid));
            }
            return _paletteAsset;
        }
        private IEnumerable<JellyColour> Colours(CD_JellyLevel level) =>
            (Palette() != null ? Palette().Colours.Select(c => c.Colour) : Enumerable.Empty<JellyColour>())
                .Concat(level.ExtraColours.Select(c => (JellyColour)c.Id));
        private string ColourName(CD_JellyLevel level, JellyColour colour) =>
            level.ExtraColours.FirstOrDefault(c => c.Id == (int)colour)?.Name ?? Palette()?.Find(colour)?.Name ?? colour.ToString();
        private Color Tint(CD_JellyLevel level, JellyColour colour) =>
            level.ExtraColours.FirstOrDefault(c => c.Id == (int)colour)?.Tint ?? Palette()?.Find(colour)?.Tint ?? Color.gray;

        private void DrawColours(CD_JellyLevel level)
        {
            var current = _selection == Selection.None ? _colour : SelectedColour(level);
            var colours = Colours(level).ToList();
            if (Palette() == null) EditorGUILayout.HelpBox(T("No CD_JellyPalette found. Create one and set it on CD_JellyLevels."), MessageType.Warning);
            const int perRow = 5;
            for (int row = 0; row < colours.Count; row += perRow)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(row == 0 ? T("Colour") : "", GUILayout.Width(50));
                foreach (var colour in colours.Skip(row).Take(perRow))
                {
                    var old = GUI.backgroundColor; GUI.backgroundColor = Tint(level, colour);
                    if (GUILayout.Button((colour == current ? "● " : "") + ColourName(level, colour), GUILayout.Height(22)))
                    {
                        _colour = colour;
                        if (_selection != Selection.None) Change(level, "Change colour", () => SetSelectedColour(level, colour));
                    }
                    GUI.backgroundColor = old;
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawShapes(CD_JellyLevel level)
        {
            var colour = Tint(level, _selection == Selection.None ? _colour : SelectedColour(level));
            var current = _selection == Selection.None ? _brush : Relative(SelectedCells(level));
            const int perRow = 6;
            for (int row = 0; row < _presets.Length; row += perRow)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(row == 0 ? T("Shape") : "", GUILayout.Width(50));
                foreach (var preset in _presets.Skip(row).Take(perRow))
                {
                    var rect = GUILayoutUtility.GetRect(28, 40, GUILayout.ExpandWidth(true));
                    var old = GUI.backgroundColor;
                    // A preset is lit when the current shape is it in any rotation.
                    if (Rotations(preset.Cells).Any(r => Same(r, current))) GUI.backgroundColor = new Color(.35f, .75f, 1);
                    if (GUI.Button(rect, new GUIContent("", T(preset.Name)))) SetShape(level, preset.Cells.ToList());
                    GUI.backgroundColor = old;
                    DrawMiniature(new Rect(rect.x + 4, rect.y + 4, rect.width - 8, rect.height - 8), preset.Cells, colour);
                }
                if (row == 0)
                {
                    if (GUILayout.Button(new GUIContent("↻", T("Rotate")), GUILayout.Width(30), GUILayout.Height(40)))
                        SetShape(level, Relative(current.Select(c => new Vector2Int(c.y, -c.x))));
                }
                else GUILayout.Space(34);
                EditorGUILayout.EndHorizontal();
            }
        }
        private static bool Same(IEnumerable<Vector2Int> a, IEnumerable<Vector2Int> b) => a.Count() == b.Count() && !a.Except(b).Any();
        private static IEnumerable<List<Vector2Int>> Rotations(IEnumerable<Vector2Int> cells)
        {
            var shape = Relative(cells);
            for (int i = 0; i < 4; i++) { yield return shape; shape = Relative(shape.Select(c => new Vector2Int(c.y, -c.x))); }
        }

        private void SetShape(CD_JellyLevel level, List<Vector2Int> shape)
        {
            _brush = shape;
            if (_selection == Selection.None) return;
            if (_selection == Selection.Door)
            {
                var door = level.Doors[_door];
                if (Across(door.Side, shape) > door.Size) _notice = "That jelly is wider than the door, so it cannot pass. Make the door wider.";
                Change(level, "Change shape", () => door.Queue[_stage].Cells = shape);
                return;
            }
            var origin = Origin(SelectedCells(level));
            var cells = shape.Select(c => c + origin).ToList();
            if (!CanPlaceShape(level, cells, _shape))
            { _notice = "That shape does not fit here. Move it or make room first."; return; }
            Change(level, "Change shape", () => level.Shapes[_shape].Cells = cells);
        }

        /// <summary>The door widths, 1 to 4 cells. They set the next door, or the selected one's.</summary>
        private void DrawDoorSizes(CD_JellyLevel level)
        {
            int current = _selection == Selection.Door ? level.Doors[_door].Size : _doorSize;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(T("Door"), GUILayout.Width(50));
            for (int size = 1; size <= CD_JellyLevel.MAX_DOOR; size++)
            {
                var old = GUI.backgroundColor;
                if (size == current) GUI.backgroundColor = new Color(.35f, .75f, 1);
                if (GUILayout.Button(size + "x", GUILayout.Height(22)))
                {
                    _doorSize = size;
                    if (_selection == Selection.Door) SetDoorSize(level, size);
                }
                GUI.backgroundColor = old;
            }
            EditorGUILayout.EndHorizontal();
        }
        private void SetDoorSize(CD_JellyLevel level, int size)
        {
            var door = level.Doors[_door];
            int start = StartAt(level, door.Side, door.Start, size);
            if (!CanPlaceDoor(level, door.Side, start, size, _door)) { _notice = "A door that wide does not fit there."; return; }
            Change(level, "Door size", () => { door.Size = size; door.Start = start; });
        }

        private void DrawPalette(CD_JellyLevel level)
        {
            _palette = EditorGUILayout.Foldout(_palette, T("Extra colours"), true);
            if (!_palette) return;
            EditorGUILayout.HelpBox(T("Colours only this level uses. The shared ones are in CD_JellyPalette."), MessageType.Info);
            foreach (var colour in level.ExtraColours.ToArray())
            {
                EditorGUILayout.BeginHorizontal();
                string name = EditorGUILayout.TextField(colour.Name);
                Color tint = EditorGUILayout.ColorField(GUIContent.none, colour.Tint, true, false, false, GUILayout.Width(65));
                if (name != colour.Name || tint != colour.Tint) Change(level, "Edit colour", () => { colour.Name = name; colour.Tint = tint; });
                bool used = level.Shapes.Any(p => (int)p.Colour == colour.Id) || level.Doors.SelectMany(d => d.Queue).Any(s => (int)s.Colour == colour.Id);
                using (new EditorGUI.DisabledScope(used))
                    if (GUILayout.Button(new GUIContent(T("Delete"), T("Recolour what uses it first.")), GUILayout.Width(45)))
                        Change(level, "Delete colour", () => level.ExtraColours.Remove(colour));
                EditorGUILayout.EndHorizontal();
            }
            if (GUILayout.Button(T("+ Add colour")))
                Change(level, "Add colour", () => { var colour = level.AddColour(); colour.Name = "New colour " + colour.Id; });
        }

        // ---------- selection ----------

        private JellyShapeCVO SelectedStage(CD_JellyLevel level) => level.Doors[_door].Queue.Count > 0 ? level.Doors[_door].Queue[_stage] : null;
        private List<Vector2Int> SelectedCells(CD_JellyLevel level) =>
            _selection == Selection.Shape ? level.Shapes[_shape].Cells : SelectedStage(level)?.Cells ?? new List<Vector2Int> { Vector2Int.zero };
        private JellyColour SelectedColour(CD_JellyLevel level) =>
            _selection == Selection.Shape ? level.Shapes[_shape].Colour : SelectedStage(level)?.Colour ?? _colour;
        private void SetSelectedColour(CD_JellyLevel level, JellyColour colour)
        {
            if (_selection == Selection.Shape) level.Shapes[_shape].Colour = colour;
            else if (SelectedStage(level) != null) SelectedStage(level).Colour = colour;
        }

        private void DrawSelected(CD_JellyLevel level)
        {
            if (_selection == Selection.None) return;
            EditorGUILayout.Space(4);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            if (_selection == Selection.Shape)
            {
                var shape = level.Shapes[_shape];
                EditorGUILayout.LabelField(F("Selected jelly · {0} · {1} cells", ColourName(level, shape.Colour), shape.Cells.Count), EditorStyles.boldLabel);
                string name = EditorGUILayout.TextField(T("Name"), shape.Name);
                if (name != shape.Name) Change(level, "Rename", () => shape.Name = name);
                DrawFrozen(level, shape.Frozen, shape.ThawAfter, T("Frozen: the jelly cannot be moved or fused until this many jellies have gone through doors."),
                    (frozen, thaw) => { shape.Frozen = frozen; shape.ThawAfter = thaw; });
                bool arrow = EditorGUILayout.Toggle(new GUIContent(T("Arrow block"), T("On: the jelly moves only one way, set below.")), shape.ArrowBlock);
                if (arrow != shape.ArrowBlock) Change(level, "Arrow block", () => shape.ArrowBlock = arrow);
                if (shape.ArrowBlock)
                {
                    var axis = (JellyAxis)EditorGUILayout.Popup(new GUIContent(T("Moves"), T("The one way the jelly can move.")), (int)shape.ArrowAxis,
                        new[] { new GUIContent(T("Horizontal")), new GUIContent(T("Vertical")) });
                    if (axis != shape.ArrowAxis) Change(level, "Arrow way", () => shape.ArrowAxis = axis);
                }
                if (GUILayout.Button(T("Delete this jelly"))) { Change(level, "Delete jelly", () => level.Shapes.RemoveAt(_shape)); _selection = Selection.None; }
            }
            else DrawSelectedDoor(level);
            EditorGUILayout.EndVertical();
        }

        /// <summary>The frozen switch of a jelly or a door, and how many jellies through doors thaw it.</summary>
        private void DrawFrozen(CD_JellyLevel level, bool frozen, int thawAfter, string tip, System.Action<bool, int> apply)
        {
            bool on = EditorGUILayout.Toggle(new GUIContent(T("Frozen"), tip), frozen);
            int thaw = thawAfter;
            if (on) thaw = Mathf.Max(1, EditorGUILayout.IntField(new GUIContent(T("Thaws after"), T("How many jellies must go through doors, anywhere on the board, before it thaws.")), thawAfter));
            if (on != frozen || thaw != thawAfter) Change(level, "Frozen", () => apply(on, thaw));
        }

        private void DrawSelectedDoor(CD_JellyLevel level)
        {
            var door = level.Doors[_door];
            EditorGUILayout.LabelField(F("Selected door {0} · {1} · {2}x", _door + 1, T(door.Side.ToString().ToLower()), door.Size), EditorStyles.boldLabel);
            string name = EditorGUILayout.TextField(T("Name"), door.Name);
            if (name != door.Name) Change(level, "Rename", () => door.Name = name);
            bool fit = EditorGUILayout.Toggle(new GUIContent(T("Fit to jelly"), T("On: for each jelly the door opens only as wide as that jelly, from its first cell, and the rest of it is wall. Off: it stays open across its whole size.")), door.FitToJelly);
            if (fit != door.FitToJelly) Change(level, "Fit to jelly", () => door.FitToJelly = fit);
            // Off: the door draws its jelly in from CD_JellyLevels' Default Pull Reach. On: from its own.
            bool own = EditorGUILayout.Toggle(new GUIContent(T("Own pull reach"), T("Off: the door uses Default Pull Reach from the level list. On: it uses its own, set below.")), door.OwnPullReach >= 0);
            if (own != door.OwnPullReach >= 0) Change(level, "Pull reach", () => door.OwnPullReach = own ? 1 : -1);
            if (door.OwnPullReach >= 0)
            {
                int pull = EditorGUILayout.IntSlider(new GUIContent(T("Pull reach"), T("A jelly this door waits for, let go of in its rows (or columns) with up to this many free cells between it and the door, is drawn the rest of the way in. 0: it has to be put right in front of the door.")), door.OwnPullReach, 0, 10);
                if (pull != door.OwnPullReach) Change(level, "Pull reach", () => door.OwnPullReach = pull);
            }
            DrawFrozen(level, door.Frozen, door.ThawAfter, T("Frozen: the door takes nothing, and stands like wall, until this many jellies have gone through the other doors."),
                (frozen, thaw) => { door.Frozen = frozen; door.ThawAfter = thaw; });
            EditorGUILayout.LabelField(T("The jellies it takes, in order: when one passes, the next is waited for. Select one to set its colour and shape above."), EditorStyles.wordWrappedMiniLabel);
            const int perRow = 6;
            for (int row = 0; row < door.Queue.Count; row += perRow)
            {
                EditorGUILayout.BeginHorizontal();
                for (int i = row; i < Mathf.Min(row + perRow, door.Queue.Count); i++)
                {
                    var stage = door.Queue[i];
                    var rect = GUILayoutUtility.GetRect(40, 44, GUILayout.ExpandWidth(true));
                    var old = GUI.backgroundColor;
                    if (i == _stage) GUI.backgroundColor = new Color(.35f, .75f, 1);
                    if (GUI.Button(rect, new GUIContent("", (i + 1) + ". " + ColourName(level, stage.Colour)))) { _stage = i; _notice = null; }
                    GUI.backgroundColor = old;
                    GUI.Label(new Rect(rect.x + 3, rect.y + 1, 20, 14), (i + 1).ToString(), EditorStyles.miniBoldLabel);
                    if (stage.Cells.Count > 0) DrawMiniature(new Rect(rect.x + 6, rect.y + 12, rect.width - 12, rect.height - 16), stage.Cells, Tint(level, stage.Colour));
                }
                EditorGUILayout.EndHorizontal();
            }
            if (GUILayout.Button(new GUIContent(T("+ Add next jelly"), T("The door takes it after the last one. It starts as a copy of the selected jelly.")), GUILayout.Height(26)))
            {
                var current = SelectedStage(level);
                Change(level, "Add jelly to door", () => door.Queue.Add(new JellyShapeCVO
                    { Name = "Jelly " + (door.Queue.Count + 1), Colour = current?.Colour ?? _colour, Cells = (current?.Cells ?? _brush).ToList() }));
                _stage = door.Queue.Count - 1;
            }
            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(_stage <= 0)) if (GUILayout.Button(T("← Earlier"))) MoveStage(level, -1);
            using (new EditorGUI.DisabledScope(_stage >= door.Queue.Count - 1)) if (GUILayout.Button(T("Later →"))) MoveStage(level, 1);
            using (new EditorGUI.DisabledScope(door.Queue.Count <= 1))
                if (GUILayout.Button(T("Remove this jelly"))) { Change(level, "Remove jelly from door", () => door.Queue.RemoveAt(_stage)); _stage = Mathf.Max(0, _stage - 1); }
            if (GUILayout.Button(T("Delete door"))) { Change(level, "Delete door", () => level.Doors.RemoveAt(_door)); _selection = Selection.None; }
            EditorGUILayout.EndHorizontal();
        }
        private void MoveStage(CD_JellyLevel level, int direction)
        {
            var list = level.Doors[_door].Queue;
            int from = _stage, to = from + direction;
            if (to < 0 || to >= list.Count) return;
            Change(level, "Reorder door", () => { var entry = list[from]; list.RemoveAt(from); list.Insert(to, entry); });
            _stage = to;
        }

        // ---------- rules ----------

        private static Vector2Int Origin(IEnumerable<Vector2Int> cells) =>
            cells.Any() ? new Vector2Int(cells.Min(c => c.x), cells.Min(c => c.y)) : Vector2Int.zero;
        private static List<Vector2Int> Relative(IEnumerable<Vector2Int> cells)
        {
            var list = cells.ToList(); var origin = Origin(list);
            return list.Count == 0 ? new List<Vector2Int> { Vector2Int.zero } : list.Select(c => c - origin).ToList();
        }
        /// <summary>How many rows (left and right doors) or columns (top and bottom) the shape spans: what it needs of a door.</summary>
        private static int Across(JellySide side, List<Vector2Int> cells) => cells.Count == 0 ? 0 :
            side is JellySide.Left or JellySide.Right ? cells.Max(c => c.y) - cells.Min(c => c.y) + 1 : cells.Max(c => c.x) - cells.Min(c => c.x) + 1;
        private static bool Inside(CD_JellyLevel level, Vector2Int c) => c.x >= 0 && c.x < level.Width && c.y >= 0 && c.y < level.Height;
        private bool CanPlaceShape(CD_JellyLevel level, List<Vector2Int> cells, int ignore) =>
            cells.All(c => Inside(level, c) && !level.Walls.Contains(c) && !level.Holes.Contains(c) && (!level.IsFrame(c) || DoorAt(level, c) >= 0) &&
                           !level.Shapes.Where((_, i) => i != ignore).Any(p => p.Cells.Contains(c)));
        private int ShapeAt(CD_JellyLevel level, Vector2Int cell) => level.Shapes.FindIndex(p => p.Cells.Contains(cell));

        /// <summary>Which side's wall a board cell is, and how far along it; false for the corners and everything inside.</summary>
        private static bool OnRing(CD_JellyLevel level, Vector2Int cell, out JellySide side, out int lane)
        {
            side = JellySide.Bottom; lane = 0;
            bool alongX = cell.x > 0 && cell.x < level.Width - 1, alongY = cell.y > 0 && cell.y < level.Height - 1;
            if (cell.y == 0 && alongX) { side = JellySide.Bottom; lane = cell.x; return true; }
            if (cell.y == level.Height - 1 && alongX) { side = JellySide.Top; lane = cell.x; return true; }
            if (cell.x == 0 && alongY) { side = JellySide.Left; lane = cell.y; return true; }
            if (cell.x == level.Width - 1 && alongY) { side = JellySide.Right; lane = cell.y; return true; }
            return false;
        }
        /// <summary>The cells of the wall round the board a door opens.</summary>
        private static IEnumerable<Vector2Int> RingCells(CD_JellyLevel level, JellySide side, int start, int size) =>
            JellyGateCVO.EdgeCells(side, start, size, level.Width, level.Height);
        private bool CanPlaceDoor(CD_JellyLevel level, JellySide side, int start, int size, int ignore)
        {
            if (start < 1 || start + size > level.SideLength(side) - 1) return false;
            var cells = JellyGateCVO.EdgeCells(side, start, size, level.Width, level.Height);
            // A wall there is no obstacle - placing the door clears it. An empty cell is.
            if (cells.Any(level.Holes.Contains)) return false;
            return !level.Doors.Where((d, i) => i != ignore && d.Side == side).Any(d => start < d.Start + d.Size && d.Start < start + size);
        }
        private int DoorAt(CD_JellyLevel level, Vector2Int cell)
        {
            if (!OnRing(level, cell, out var side, out int lane)) return -1;
            return level.Doors.FindIndex(d => d.Side == side && lane >= d.Start && lane < d.Start + d.Size);
        }
        /// <summary>Where a door of <paramref name="size"/> would start with its first cell under the pointer, kept on its side.</summary>
        private static int StartAt(CD_JellyLevel level, JellySide side, int lane, int size) =>
            Mathf.Clamp(lane, 1, Mathf.Max(1, level.SideLength(side) - 1 - size));

        // ---------- board ----------

        private List<Vector2Int> Ghost(CD_JellyLevel level, out bool valid)
        {
            valid = false;
            if (_dragging && _selection == Selection.Shape)
            {
                var cells = level.Shapes[_shape].Cells.Select(c => c + _hovered - _dragFrom).ToList();
                valid = CanPlaceShape(level, cells, _shape);
                return cells;
            }
            bool door = (_dragging && _selection == Selection.Door) || (!_dragging && _tool == Tool.Door && DoorAt(level, _hovered) < 0);
            if (door && OnRing(level, _hovered, out var side, out int lane))
            {
                int size = _dragging ? level.Doors[_door].Size : _doorSize;
                int start = StartAt(level, side, lane, size);
                valid = CanPlaceDoor(level, side, start, size, _dragging ? _door : -1);
                return RingCells(level, side, start, size).ToList();
            }
            if (!_dragging && _tool == Tool.Jelly && Inside(level, _hovered) && ShapeAt(level, _hovered) < 0)
            {
                var cells = _brush.Select(c => c + _hovered).ToList();
                valid = CanPlaceShape(level, cells, -1);
                return cells;
            }
            return new List<Vector2Int>();
        }

        private void DrawGrid(CD_JellyLevel level)
        {
            int columns = level.Width + _margin * 2, rows = level.Height + _margin * 2;
            float size = _cellSize;
            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.Height(_viewHeight));
            var rect = GUILayoutUtility.GetRect(columns * size, rows * size, GUILayout.ExpandWidth(false));
            var e = Event.current;
            bool over = rect.Contains(e.mousePosition);
            if (over || _dragging)
                _hovered = new Vector2Int(Mathf.FloorToInt((e.mousePosition.x - rect.x) / size) - _margin,
                    level.Height + _margin - 1 - Mathf.FloorToInt((e.mousePosition.y - rect.y) / size));
            bool valid = false;
            var ghost = over || _dragging ? Ghost(level, out valid) : new List<Vector2Int>();

            if (e.type == EventType.Repaint) Paint(level, rect, size, ghost, valid);
            HandleInput(level, rect, e, ghost, valid);
            if (over) Repaint();
            EditorGUILayout.EndScrollView();
            DrawHeightHandle(rows * size + 16);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent(T("Cell size"), T("How big each cell of the map is drawn.")), GUILayout.Width(70));
            float cell = EditorGUILayout.Slider(_cellSize, 14, 72);
            if (!Mathf.Approximately(cell, _cellSize)) { _cellSize = cell; EditorPrefs.SetFloat(CELL_PREF, cell); }
            if (GUILayout.Button(new GUIContent(T("Fit"), T("Size the cells so the whole map fits the view.")), GUILayout.Width(40)))
            {
                _cellSize = Mathf.Clamp(Mathf.Min((EditorGUIUtility.currentViewWidth - 48) / columns, (_viewHeight - 16) / rows), 14, 72);
                EditorPrefs.SetFloat(CELL_PREF, _cellSize);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent(T("Outside area"), T("How many cells round the board the map shows.")), GUILayout.Width(70));
            int margin = EditorGUILayout.IntSlider(_margin, 0, 6);
            if (margin != _margin) { _margin = margin; EditorPrefs.SetInt(MARGIN_PREF, margin); }
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>A bar under the map: drag it to make the map view taller or shorter. The height is remembered.</summary>
        private void DrawHeightHandle(float fullHeight)
        {
            var bar = GUILayoutUtility.GetRect(1, 10, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(new Rect(bar.x, bar.y + 4, bar.width, 2), new Color(1, 1, 1, .25f));
            EditorGUIUtility.AddCursorRect(bar, MouseCursor.ResizeVertical);
            int id = GUIUtility.GetControlID(FocusType.Passive);
            var e = Event.current;
            switch (e.GetTypeForControl(id))
            {
                case EventType.MouseDown when e.button == 0 && bar.Contains(e.mousePosition):
                    GUIUtility.hotControl = id; e.Use(); break;
                case EventType.MouseDrag when GUIUtility.hotControl == id:
                    _viewHeight = Mathf.Clamp(_viewHeight + e.delta.y, 120, Mathf.Max(120, fullHeight + 40)); e.Use(); Repaint(); break;
                case EventType.MouseUp when GUIUtility.hotControl == id:
                    GUIUtility.hotControl = 0; EditorPrefs.SetFloat(HEIGHT_PREF, _viewHeight); e.Use(); break;
            }
        }

        private Rect CellRect(CD_JellyLevel level, Rect rect, float size, Vector2Int cell) =>
            new Rect(rect.x + (cell.x + _margin) * size, rect.y + (level.Height + _margin - 1 - cell.y) * size, size - 1, size - 1);

        private void Paint(CD_JellyLevel level, Rect rect, float size, List<Vector2Int> ghost, bool valid)
        {
            var label = new GUIStyle(EditorStyles.miniBoldLabel) { alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } };
            var selected = _selection == Selection.Shape ? level.Shapes[_shape].Cells : new List<Vector2Int>();
            for (int y = -_margin; y < level.Height + _margin; y++) for (int x = -_margin; x < level.Width + _margin; x++)
            {
                var cell = new Vector2Int(x, y);
                var box = CellRect(level, rect, size, cell);
                // The outermost cells are the wall round the board, painted or not.
                // Off the board is dark; the board's outermost cells are the wall round it; the rest is floor.
                Color tint = !Inside(level, cell) ? new Color(.12f, .14f, .18f) : level.IsFrame(cell) ? new Color(.42f, .45f, .52f) : new Color(.27f, .31f, .38f);
                string text = "";
                if (level.Walls.Contains(cell)) { tint = new Color(.55f, .58f, .64f); text = "▇"; }
                if (level.Holes.Contains(cell)) { tint = Color.black; text = "×"; }
                int shape = ShapeAt(level, cell);
                if (shape >= 0)
                {
                    var placed = level.Shapes[shape];
                    tint = Tint(level, placed.Colour); text = (shape + 1).ToString();
                    // A frozen jelly reads icy, with how many jellies thaw it.
                    if (placed.Frozen) { tint = Color.Lerp(tint, FROZEN, .55f); text = "❄" + placed.ThawAfter; }
                    if (placed.ArrowBlock) text = (placed.ArrowAxis == JellyAxis.Horizontal ? "↔" : "↕") + (placed.Frozen ? text : "");
                }
                if (selected.Contains(cell) && !_dragging) EditorGUI.DrawRect(box, Color.white);
                var inner = selected.Contains(cell) && !_dragging ? new Rect(box.x + 2, box.y + 2, box.width - 4, box.height - 4) : box;
                if (ghost.Contains(cell)) tint = valid ? new Color(.3f, .85f, .45f) : new Color(.95f, .3f, .25f);
                EditorGUI.DrawRect(inner, tint);
                if (text != "") GUI.Label(box, text, label);
            }
            for (int i = 0; i < level.Doors.Count; i++) PaintDoor(level, rect, size, i, ghost, label);
            if (_tool == Tool.Tutorial) PaintTutorial(level, rect, size, label);
            // Board outline, so inside and outside read apart at a glance.
            var board = new Rect(rect.x + _margin * size - 1, rect.y + _margin * size - 1, level.Width * size + 1, level.Height * size + 1);
            var line = new Color(1, 1, 1, .5f);
            EditorGUI.DrawRect(new Rect(board.x, board.y, board.width, 1), line);
            EditorGUI.DrawRect(new Rect(board.x, board.yMax, board.width, 1), line);
            EditorGUI.DrawRect(new Rect(board.x, board.y, 1, board.height), line);
            EditorGUI.DrawRect(new Rect(board.xMax, board.y, 1, board.height), line);
        }
        /// <summary>A door as the game draws it: one bar over its cells with the jelly it waits for on it, and how many come after.</summary>
        private void PaintDoor(CD_JellyLevel level, Rect rect, float size, int index, List<Vector2Int> ghost, GUIStyle label)
        {
            var door = level.Doors[index];
            var cells = RingCells(level, door.Side, door.Start, door.Size).ToList();
            if (cells.Count == 0 || (_dragging && _selection == Selection.Door && index == _door)) return;
            var area = CellRect(level, rect, size, cells[0]);
            foreach (var cell in cells) area = Rect.MinMaxRect(Mathf.Min(area.xMin, CellRect(level, rect, size, cell).xMin), Mathf.Min(area.yMin, CellRect(level, rect, size, cell).yMin),
                Mathf.Max(area.xMax, CellRect(level, rect, size, cell).xMax), Mathf.Max(area.yMax, CellRect(level, rect, size, cell).yMax));
            bool chosen = _selection == Selection.Door && index == _door;
            var stage = door.Queue.Count == 0 ? null : door.Queue[chosen ? _stage : 0];
            var colour = stage != null ? Tint(level, stage.Colour) : Color.gray;
            if (chosen) EditorGUI.DrawRect(area, Color.white);
            var inner = chosen ? new Rect(area.x + 2, area.y + 2, area.width - 4, area.height - 4) : area;
            EditorGUI.DrawRect(inner, Color.Lerp(colour, Color.black, .55f));
            if (ghost.Intersect(cells).Any()) return;
            if (stage != null && stage.Cells.Count > 0) DrawMiniature(new Rect(inner.x + 2, inner.y + 2, inner.width - 4, inner.height - 4), stage.Cells, colour);
            int after = door.Queue.Count - 1 - (chosen ? _stage : 0);
            if (after > 0) GUI.Label(new Rect(inner.xMax - 22, inner.y - 2, 22, 14), "+" + after, label);
            if (door.Frozen)
            {
                EditorGUI.DrawRect(inner, new Color(FROZEN.r, FROZEN.g, FROZEN.b, .45f));
                GUI.Label(inner, "❄" + door.ThawAfter, label);
            }
        }
        private static readonly Color FROZEN = new(.62f, .86f, 1f);

        private void HandleInput(CD_JellyLevel level, Rect rect, Event e, List<Vector2Int> ghost, bool valid)
        {
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape) { _dragging = false; _selection = Selection.None; e.Use(); return; }
            if (e.rawType == EventType.MouseUp)
            {
                if (_dragging && e.button == 0)
                {
                    _dragging = false;
                    if (_hovered != _dragFrom) Drop(level, valid);
                }
                if (_undoGroup >= 0) { Undo.CollapseUndoOperations(_undoGroup); _undoGroup = -1; }
                return;
            }
            if (!rect.Contains(e.mousePosition) || (e.type != EventType.MouseDown && e.type != EventType.MouseDrag) || e.button > 1) return;
            if (e.type == EventType.MouseDown) { Undo.IncrementCurrentGroup(); _undoGroup = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Edit level"); }
            var cell = _hovered;
            if (_tool == Tool.Tutorial) { if (e.type == EventType.MouseDown) TutorialClick(level, cell, e.button == 1); }
            else if (e.button == 1 || _tool == Tool.Erase) EraseAt(level, cell);
            else if (_dragging) { }
            else if (e.type == EventType.MouseDown) Click(level, cell, ghost, valid);
            else if (_tool is Tool.Wall or Tool.Hole) PaintCell(level, cell);
            e.Use();
        }

        /// <summary>A dragged jelly or door is let go: it moves there if it fits.</summary>
        private void Drop(CD_JellyLevel level, bool valid)
        {
            if (!valid) { _notice = "It does not fit there."; return; }
            if (_selection == Selection.Shape)
            {
                var shift = _hovered - _dragFrom;
                Change(level, "Move", () => level.Shapes[_shape].Cells = level.Shapes[_shape].Cells.Select(c => c + shift).ToList());
            }
            else if (_selection == Selection.Door && OnRing(level, _hovered, out var side, out int lane))
            {
                var door = level.Doors[_door];
                Change(level, "Move door", () => { door.Side = side; door.Start = StartAt(level, side, lane, door.Size); });
            }
        }

        private void Click(CD_JellyLevel level, Vector2Int cell, List<Vector2Int> ghost, bool valid)
        {
            _notice = null;
            int shape = ShapeAt(level, cell), door = DoorAt(level, cell);
            if (_tool is Tool.Jelly or Tool.Door && shape >= 0)
            { _selection = Selection.Shape; _shape = shape; _tool = Tool.Jelly; _dragging = true; _dragFrom = cell; return; }
            if (_tool is Tool.Jelly or Tool.Door && door >= 0)
            {
                if (_selection != Selection.Door || _door != door) _stage = 0;
                _selection = Selection.Door; _door = door; _tool = Tool.Door; _dragging = true; _dragFrom = cell; return;
            }
            if (_tool is Tool.Wall or Tool.Hole) { PaintCell(level, cell); return; }
            if (_tool == Tool.Jelly && !Inside(level, cell)) { _selection = Selection.None; return; }
            if (_tool == Tool.Door && !OnRing(level, cell, out _, out _))
            { _selection = Selection.None; _notice = level.IsFrame(cell) ? "A corner cannot hold a door." : "Doors go in the wall round the board: its outermost cells."; return; }
            if (!valid)
            {
                _notice = _tool == Tool.Jelly ? "The jelly must fit on empty floor, inside the wall round the board."
                    : "A door must fit on its side between the corners, clear of other doors, with no empty cell in front of it.";
                return;
            }
            if (_tool == Tool.Jelly)
            {
                Change(level, "Add jelly", () => level.Shapes.Add(new JellyShapeCVO { Name = "Jelly " + (level.Shapes.Count + 1), Colour = _colour, Cells = ghost }));
                // A new piece is placed, not selected: the colour and shape rows keep setting what
                // the next click places, so several can be placed in a row.
                _selection = Selection.None;
                return;
            }
            OnRing(level, cell, out var side, out int lane);
            var added = new JellyDoorCVO { Name = "Door " + (level.Doors.Count + 1), Side = side, Start = StartAt(level, side, lane, _doorSize), Size = _doorSize };
            added.Queue.Add(new JellyShapeCVO { Name = "Jelly 1", Colour = _colour, Cells = _brush.ToList() });
            if (Across(side, _brush) > _doorSize) _notice = "That jelly is wider than the door, so it cannot pass. Make the door wider.";
            Change(level, "Add door", () => level.Doors.Add(added));
            // A new door is selected, so the jellies it takes can be set right away.
            _selection = Selection.Door; _door = level.Doors.Count - 1; _stage = 0;
        }

        // ---------- tutorial ----------

        private static readonly Color[] TUTORIAL_COLOURS = { new(1f, .85f, .2f), new(.3f, .9f, 1f), new(1f, .45f, .8f), new(.5f, 1f, .4f) };
        /// <summary>The level's tutorial steps: pick one to edit, add one, clear or delete it.</summary>
        private void DrawTutorial(CD_JellyLevel level)
        {
            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField(T("Tutorial steps"), EditorStyles.boldLabel);
            if (level.Tutorial.Count == 0) EditorGUILayout.HelpBox(T("No tutorial. Add a step, then click on the map."), MessageType.None);
            _tutorialStep = Mathf.Clamp(_tutorialStep, 0, Mathf.Max(0, level.Tutorial.Count - 1));
            for (int i = 0; i < level.Tutorial.Count; i++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    var old = GUI.backgroundColor;
                    GUI.backgroundColor = TUTORIAL_COLOURS[i % TUTORIAL_COLOURS.Length];
                    string text = (i == _tutorialStep ? "● " : "") + F("Step {0}: {1} cells", i + 1, level.Tutorial[i].Path.Count);
                    if (GUILayout.Button(text, GUILayout.Height(22))) _tutorialStep = i;
                    GUI.backgroundColor = old;
                    int index = i;
                    if (GUILayout.Button(T("Clear"), GUILayout.Width(50))) Change(level, "Clear tutorial step", () => level.Tutorial[index].Path.Clear());
                    if (GUILayout.Button(T("Delete"), GUILayout.Width(55))) { Change(level, "Delete tutorial step", () => level.Tutorial.RemoveAt(index)); GUIUtility.ExitGUI(); }
                }
            }
            // The selected step's text: a localization key, and the English for when the key has no translation.
            if (level.Tutorial.Count > 0)
            {
                var selected = level.Tutorial[_tutorialStep];
                EditorGUI.BeginChangeCheck();
                string key = EditorGUILayout.TextField(new GUIContent(T("Text key"), T("The localization key of the text shown during this step. Empty: no text.")), selected.TextKey);
                EditorGUILayout.LabelField(T("Text (English)"));
                string text = EditorGUILayout.TextArea(selected.Text ?? "", GUILayout.MinHeight(36));
                if (EditorGUI.EndChangeCheck()) Change(level, "Tutorial text", () => { selected.TextKey = key; selected.Text = text; });
            }
            if (GUILayout.Button(T("+ Add step"), GUILayout.Height(24)))
            {
                Change(level, "Add tutorial step", () => level.Tutorial.Add(new JellyTutorialStepCVO()));
                _tutorialStep = level.Tutorial.Count - 1;
            }
        }
        private void TutorialClick(CD_JellyLevel level, Vector2Int cell, bool takeBack)
        {
            _notice = null;
            if (level.Tutorial.Count == 0)
            {
                if (takeBack) return;
                Change(level, "Add tutorial step", () => level.Tutorial.Add(new JellyTutorialStepCVO()));
                _tutorialStep = 0;
            }
            var step = level.Tutorial[Mathf.Clamp(_tutorialStep, 0, level.Tutorial.Count - 1)];
            if (takeBack) { if (step.Path.Count > 0) Change(level, "Take back tutorial cell", () => step.Path.RemoveAt(step.Path.Count - 1)); return; }
            // The first step starts on a jelly as the level begins; a later one on where the earlier steps leave one.
            if (step.Path.Count == 0 && _tutorialStep == 0 && ShapeAt(level, cell) < 0) { _notice = "The first cell of a step is on the jelly to lead."; return; }
            if (step.Path.Count > 0 && step.Path[^1] == cell) return;
            Change(level, "Add tutorial cell", () => step.Path.Add(cell));
        }
        /// <summary>Every step's route over the map, the one being edited strongest, its clicked cells numbered.</summary>
        private void PaintTutorial(CD_JellyLevel level, Rect rect, float size, GUIStyle label)
        {
            for (int i = 0; i < level.Tutorial.Count; i++)
            {
                var step = level.Tutorial[i];
                var colour = TUTORIAL_COLOURS[i % TUTORIAL_COLOURS.Length];
                colour.a = i == _tutorialStep ? .55f : .2f;
                foreach (var cell in step.Route())
                {
                    var box = CellRect(level, rect, size, cell);
                    EditorGUI.DrawRect(new Rect(box.x + size * .25f, box.y + size * .25f, box.width * .5f, box.height * .5f), colour);
                }
                if (i != _tutorialStep) continue;
                for (int p = 0; p < step.Path.Count; p++)
                    GUI.Label(CellRect(level, rect, size, step.Path[p]), p == 0 ? "✋" : (p == step.Path.Count - 1 ? "◎" : p.ToString()), label);
            }
        }

        private void PaintCell(CD_JellyLevel level, Vector2Int cell)
        {
            if (!Inside(level, cell)) return;
            if (ShapeAt(level, cell) >= 0) { _notice = "Cannot paint over a jelly."; return; }
            var list = _tool == Tool.Wall ? level.Walls : level.Holes;
            if (list.Contains(cell)) return;
            Change(level, "Paint board", () => { level.Walls.Remove(cell); level.Holes.Remove(cell); list.Add(cell); });
        }

        private void EraseAt(CD_JellyLevel level, Vector2Int cell)
        {
            int shape = ShapeAt(level, cell), door = DoorAt(level, cell);
            if (shape >= 0) { Change(level, "Delete jelly", () => level.Shapes.RemoveAt(shape)); _selection = Selection.None; }
            else if (door >= 0) { Change(level, "Delete door", () => level.Doors.RemoveAt(door)); _selection = Selection.None; }
            else if (level.Walls.Contains(cell) || level.Holes.Contains(cell))
                Change(level, "Erase", () => { level.Walls.Remove(cell); level.Holes.Remove(cell); });
        }

        private void DrawMiniature(Rect rect, IEnumerable<Vector2Int> cells, Color colour)
        {
            var list = cells.ToList();
            var origin = Origin(list);
            int width = list.Max(c => c.x) - origin.x + 1, height = list.Max(c => c.y) - origin.y + 1;
            float size = Mathf.Min(rect.width / width, rect.height / height, 10);
            foreach (var c in list)
                EditorGUI.DrawRect(new Rect(rect.center.x - width * size / 2 + (c.x - origin.x) * size, rect.center.y + height * size / 2 - (c.y - origin.y + 1) * size, size - 1, size - 1), colour);
        }

        // ---------- checks and footer ----------

        private void DrawBalance(CD_JellyLevel level)
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField(T("Balance: each colour's jelly cells must equal the cells its doors take"), EditorStyles.boldLabel);
            var stages = level.Doors.SelectMany(d => d.Queue).ToList();
            foreach (var colour in Colours(level))
            {
                int start = level.Shapes.Where(p => p.Colour == colour).Sum(p => p.Cells.Count);
                int goal = stages.Where(s => s.Colour == colour).Sum(s => s.Cells.Count);
                if (start == 0 && goal == 0) continue;
                var rect = EditorGUILayout.GetControlRect();
                EditorGUI.DrawRect(new Rect(rect.x, rect.y + 3, 12, 12), Tint(level, colour));
                string state = start == goal ? "✓" : start < goal ? F("→ {0} jelly cells missing", goal - start) : F("→ {0} extra jelly cells", start - goal);
                GUI.Label(new Rect(rect.x + 18, rect.y, rect.width - 18, rect.height), F("{0}:  {1} jelly / {2} at doors   {3}", ColourName(level, colour), start, goal, state), EditorStyles.miniLabel);
            }
            // Redone when the level is marked dirty (every edit and undo does that), not on every repaint.
            int dirty = EditorUtility.GetDirtyCount(level);
            if (_errors == null || dirty != _errorsDirtyCount)
            {
                // A balance issue is already shown in the colour rows above.
                _errors = level.Issues().Where(e => e.Format != CD_JellyLevel.BALANCE_ISSUE).Select(e => F(e.Format, e.Values)).Distinct().ToList();
                _errorsDirtyCount = dirty;
            }
            var errors = _errors;
            if (errors.Count == 0) EditorGUILayout.HelpBox(T("Level is valid. Test solvability in Play Mode."), MessageType.Info);
            foreach (var error in errors.Take(8)) EditorGUILayout.HelpBox(error, MessageType.Warning);
        }
        private List<CD_JellyLevels> LevelLists() => _lists ??= AssetDatabase.FindAssets("t:CD_JellyLevels")
            .Select(g => AssetDatabase.LoadAssetAtPath<CD_JellyLevels>(AssetDatabase.GUIDToAssetPath(g))).Where(l => l != null).ToList();
        private void DrawListPlace(CD_JellyLevel level)
        {
            var lists = LevelLists().ToList();
            var owner = lists.FirstOrDefault(l => l.Levels.Contains(level));
            EditorGUILayout.BeginHorizontal();
            if (owner != null)
                EditorGUILayout.LabelField(F("In the level list: {0} / {1}", owner.Levels.IndexOf(level) + 1, owner.Count), EditorStyles.miniBoldLabel);
            else
            {
                EditorGUILayout.LabelField(T("Not in the level list, so the game never opens it."), EditorStyles.miniBoldLabel);
                if (lists.Count > 0 && GUILayout.Button(T("Add to the end of the list"), GUILayout.Width(160)))
                {
                    Undo.RecordObject(lists[0], "Add level to list");
                    lists[0].Levels.Add(level); EditorUtility.SetDirty(lists[0]); AssetDatabase.SaveAssetIfDirty(lists[0]);
                }
            }
            if (lists.Count > 0 && GUILayout.Button(T("Open list"), GUILayout.Width(80))) UnityEditor.Selection.activeObject = owner ?? lists[0];
            EditorGUILayout.EndHorizontal();
        }

        private void DrawFooter(CD_JellyLevel level)
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(T("Save"))) AssetDatabase.SaveAssetIfDirty(level);
            if (GUILayout.Button(new GUIContent(T("Duplicate as new level"),
                    T("The copy is added to the level list right after this level."))))
            {
                AssetDatabase.SaveAssetIfDirty(level);
                string path = AssetDatabase.GenerateUniqueAssetPath(AssetDatabase.GetAssetPath(level));
                if (!string.IsNullOrEmpty(path) && AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(level), path))
                {
                    var copy = AssetDatabase.LoadAssetAtPath<CD_JellyLevel>(path);
                    foreach (var list in LevelLists().Where(l => l.Levels.Contains(level)))
                    {
                        Undo.RecordObject(list, "Add level to list");
                        list.Levels.Insert(list.Levels.IndexOf(level) + 1, copy);
                        EditorUtility.SetDirty(list);
                        AssetDatabase.SaveAssetIfDirty(list);
                    }
                    UnityEditor.Selection.activeObject = copy;
                }
            }
            EditorGUILayout.EndHorizontal();
            DrawListPlace(level);
            EditorGUILayout.LabelField(T("Press Restart in Play Mode to load edits. Ctrl+Z undoes."), EditorStyles.wordWrappedMiniLabel);
            _advanced = EditorGUILayout.Foldout(_advanced, T("Advanced: raw data"), true);
            if (!_advanced) return;
            serializedObject.Update();
            foreach (var field in new[] { "Walls", "Holes", "Shapes", "Doors" })
            {
                var property = serializedObject.FindProperty(field);
                EditorGUILayout.PropertyField(property, _texts.Label(property), true);
            }
            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>Turns the old exits of every level in the project into doors, in one undo step.</summary>
        [MenuItem("Floria/Gameplay/Convert Old Exits To Doors", false, 20)]
        private static void ConvertAllLevels()
        {
            var levels = AssetDatabase.FindAssets("t:CD_JellyLevel")
                .Select(g => AssetDatabase.LoadAssetAtPath<CD_JellyLevel>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(l => l != null && l.HasOldExits).ToList();
            foreach (var level in levels)
            {
                Undo.RecordObject(level, "Convert old exits");
                level.ConvertOldExits();
                EditorUtility.SetDirty(level);
                AssetDatabase.SaveAssetIfDirty(level);
            }
            Debug.Log(levels.Count == 0 ? "No level has old exits." : "Converted the old exits of " + levels.Count + " levels to doors.");
        }
    }
}
#endif
