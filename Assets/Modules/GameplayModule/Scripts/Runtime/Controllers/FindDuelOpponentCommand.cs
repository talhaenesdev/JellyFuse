using System;
using System.Collections.Generic;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using FlowIoC.ConsoleModule;
using Modules.DuelModule.Data.ValueObjects;
using Modules.DuelModule.Services;
using Modules.GameplayModule.Models;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using Modules.GameplayModule.Signals;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Modules.GameplayModule.Controllers
{
    /// <summary>
    /// The duel card when no player whose game is open took the invite: a bot on a level the player has already won,
    /// picked at random, paced from the level's own clock. With too few levels won the flow stops and DuelUnavailable
    /// says so.
    /// </summary>
    internal class FindDuelOpponentCommand : Command
    {
        [Inject] private IJellyPuzzleModel _model { get; set; }
        [Inject] private IDuelModel _duel { get; set; }
        [Inject] private IDuelService _duelService { get; set; }
        [Inject] private ISharedDataModel _shared { get; set; }
        [InjectSignal] private GameplaySignals _signals { get; set; }

        public override async void Execute()
        {
            Retain();
            try
            {
                var levels = _shared.GetScriptable<CD_JellyLevels>();
                var events = _shared.GetScriptable<CD_Events>();
                var progress = _shared.GetScriptable<SD_JellyProgress>();
                if (levels == null || events == null)
                {
                    Unavailable("the level or event config is not filed");
                    return;
                }
                int current = _model.LevelIndex >= 0 ? _model.LevelIndex : levels.StartIndex(progress);
                // The run's level is the next one to win, so every level before it has been won.
                int won = current;
                if (won < events.DuelMinLevel)
                {
                    Unavailable($"only {won} levels won, the duel opens at {events.DuelMinLevel}");
                    return;
                }

                // Nobody open took the invite: a bot races, on any level the player has won, picked at random - never the
                // same recorded player and level again and again.
                await System.Threading.Tasks.Task.Delay((int)(Mathf.Max(0, events.DuelMinSearch) * 1000));
                var match = new DuelMatchVO { Found = true, Bot = true, Name = BotName(), Icon = string.Empty };
                int index = levels.RandomPlayableBelow(won);
                match.Level = index + 1;
                var level = index >= 0 ? levels.Get(index) : null;
                if (level == null)
                {
                    Unavailable($"level {match.Level} cannot be played");
                    return;
                }

                IReadOnlyList<float> exits = match.Exits;
                // A run from another version of the level would race a different board: a bot races this one instead.
                bool sameBoard = !string.IsNullOrEmpty(match.Fp) && match.Fp == LevelFingerprintFunction.Of(level);
                if (match.Bot || !sameBoard || exits == null || exits.Count == 0 || index != match.Level - 1)
                    exits = BotExits(level.CreateLevel(levels.Palette), events);
                _model.Daily = false;
                _duel.Choose(index, match.Name, match.Icon, exits);
                _duel.Searching = false;
                FlowLogger.Log($"FindDuelOpponentCommand - racing {match.Name}{(match.Bot ? " (bot)" : string.Empty)} on level {index + 1}, their time {_duel.OpponentSeconds:0.0}s.");
                Release();
            }
            catch (Exception exception)
            {
                _duel.Searching = false;
                FlowLogger.LogError($"FindDuelOpponentCommand threw while finding an opponent: {exception}");
                Stop();
            }
        }

        /// <summary>A bot clears the board in a share of the level's clock, its jellies leaving at an uneven pace.</summary>
        private static IReadOnlyList<float> BotExits(Shared.Data.ValueObjects.JellyLevelCVO level, CD_Events events)
        {
            int total = 0;
            foreach (var gate in level.Gates) total += gate.Stages.Count;
            total = Mathf.Max(1, total);
            float share = Random.Range(Mathf.Min(events.DuelBotMinShare, events.DuelBotMaxShare), Mathf.Max(events.DuelBotMinShare, events.DuelBotMaxShare));
            float seconds = Mathf.Max(10f, level.Seconds * share);
            var weights = new float[total];
            float sum = 0;
            for (int i = 0; i < total; i++) sum += weights[i] = Random.Range(0.5f, 1.5f);
            var exits = new List<float>(total);
            float at = 0;
            for (int i = 0; i < total; i++)
            {
                at += seconds * weights[i] / sum;
                exits.Add(i == total - 1 ? seconds : at);
            }
            return exits;
        }

        private static readonly string[] BOT_NAMES = { "JellyBean", "Wobbly", "Gummy", "Squishy", "Bubbles", "Fizzy", "Mochi", "Boba", "Taffy", "Noodle" };
        private static string BotName() => BOT_NAMES[Random.Range(0, BOT_NAMES.Length)];

        private void Unavailable(string why)
        {
            _duel.Searching = false;
            FlowLogger.Log($"FindDuelOpponentCommand - no duel: {why}.");
            _signals.Outgoing.DuelUnavailable.Dispatch();
            Stop();
        }
    }
}
