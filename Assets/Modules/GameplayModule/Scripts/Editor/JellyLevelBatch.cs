#if UNITY_EDITOR
using System.Diagnostics;
using System.IO;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using UnityEditor;
using UnityEngine;

namespace Modules.GameplayModule.Editor
{
    /// <summary>
    /// Fills a run of level slots with JellyLevelGenerator drafts, a few seconds of work a call, so a long batch can be
    /// driven call by call without holding the Editor. Where it stands - the level being made, its seed and how many
    /// drafts it has had - is kept in Temp/LevelGen.state, and every level made goes to Temp/LevelGen.log. Level n
    /// goes to CD_JellyLevel_(n + 4), the asset that holds it in CD_JellyLevels.
    /// </summary>
    public sealed class JellyLevelBatch
    {
        public const string LOG = "Temp/LevelGen.log";
        private const string STATE = "Temp/LevelGen.state";
        private const string FOLDER = "Assets/Modules/GameplayModule/Scriptables/";
        private const int MAX_TRIES = 120;

        private readonly CD_JellyLevels _list;
        private int _number, _last, _seed, _tries;

        private JellyLevelBatch(CD_JellyLevels list) => _list = list;

        /// <summary>Starts a batch over levels <paramref name="first"/> to <paramref name="last"/>, replacing any batch under way.</summary>
        public static void Begin(int first, int last, int seed)
        {
            File.WriteAllText(STATE, $"{first} {last} {seed} 0");
            File.AppendAllText(LOG, $"start {first}-{last}\n");
        }

        /// <summary>Works on the batch for about <paramref name="milliseconds"/>; false once every level is made.</summary>
        public static bool Work(CD_JellyLevels list, int milliseconds)
        {
            if (!File.Exists(STATE)) return false;
            var batch = new JellyLevelBatch(list);
            var parts = File.ReadAllText(STATE).Split(' ');
            batch._number = int.Parse(parts[0]); batch._last = int.Parse(parts[1]);
            batch._seed = int.Parse(parts[2]); batch._tries = int.Parse(parts[3]);
            var clock = Stopwatch.StartNew();
            while (batch._number <= batch._last && clock.ElapsedMilliseconds < milliseconds) batch.Step();
            if (batch._number > batch._last)
            {
                File.Delete(STATE);
                File.AppendAllText(LOG, "done\n");
                return false;
            }
            File.WriteAllText(STATE, $"{batch._number} {batch._last} {batch._seed} {batch._tries}");
            return true;
        }

        private void Step()
        {
            var generator = JellyLevelGenerator.ForLevel(_number);
            // A wide map the solver cannot crack in half the tries is made a plain board instead, so no level is left out.
            if (generator.Wide && _tries >= MAX_TRIES / 2) generator.Wide = false;
            var draft = generator.Make(_number, _seed++, _list, 1);
            _tries++;
            if (draft == null)
            {
                if (_tries < MAX_TRIES) return;
                File.AppendAllText(LOG, $"{_number}: gave up after {_tries} drafts\n");
            }
            else
            {
                var asset = AssetDatabase.LoadAssetAtPath<CD_JellyLevel>(FOLDER + "CD_JellyLevel_" + (_number + 4) + ".asset");
                string keep = asset.name;
                EditorUtility.CopySerialized(draft.Level, asset);
                asset.name = keep;
                EditorUtility.SetDirty(asset);
                Object.DestroyImmediate(draft.Level);
                AssetDatabase.SaveAssets();
                File.AppendAllText(LOG, $"{_number}: {draft.Solve} after {_tries} drafts, {asset.Seconds}s, {asset.Difficulty}\n");
            }
            _number++;
            _tries = 0;
        }
    }
}
#endif
