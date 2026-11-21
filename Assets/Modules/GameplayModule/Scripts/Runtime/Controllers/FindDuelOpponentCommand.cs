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
using Modules.ProfileModule.Shared.Data.UnityObjects;
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
        /// <summary>How often a bot turns a rematch down, the way some people do.</summary>
        private const float BOT_REMATCH_NO = .15f;

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
                int current = levels.StartIndex(progress);
                // The run's level is the next one to win, so every level before it has been won.
                int won = current;
                if (won < events.DuelMinLevel)
                {
                    Unavailable($"only {won} levels won, the duel opens at {events.DuelMinLevel}");
                    return;
                }

                // Nobody open took the invite: a bot races, on any level the player has won, picked at random - never the
                // same recorded player and level again and again.
                DuelMatchVO match;
                string rematchName = _duel.BotRematchName;
                if (!string.IsNullOrEmpty(rematchName))
                {
                    // A rematch asked of the bot just raced: it is waited on like a person, and mostly says yes.
                    string rematchIcon = _duel.BotRematchIcon;
                    _duel.BotRematchName = string.Empty;
                    _duel.BotRematchIcon = string.Empty;
                    _signals.Outgoing.LiveInviteSent.Dispatch(rematchName);
                    await System.Threading.Tasks.Task.Delay((int)(Random.Range(2f, 5.5f) * 1000));
                    if (Random.value < BOT_REMATCH_NO)
                    {
                        Unavailable($"{rematchName} (bot) turned the rematch down");
                        return;
                    }
                    match = new DuelMatchVO { Found = true, Bot = true, Name = rematchName, Icon = rematchIcon };
                }
                else
                {
                    // The search takes as long as a real one would: never the same few seconds twice.
                    float search = Mathf.Max(0, events.DuelMinSearch) + Random.Range(0.5f, 4.5f);
                    await System.Threading.Tasks.Task.Delay((int)(search * 1000));
                    match = new DuelMatchVO { Found = true, Bot = true, Name = BotName(), Icon = BotIcon(_shared.GetScriptable<CD_ProfileIcons>()) };
                }
                int index = levels.RandomPlayableBelow(won, events.DuelLevelFloor);
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
                if (match.Bot) _duel.MarkBot();
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

        /// <summary>
        /// A bot plays like a person: it takes a moment to read the board, then clears jellies in bursts with pauses to
        /// think, now and then a long one, and speeds up as the board empties. Its whole run is a share of the level's
        /// clock - and once in a while it is slow enough to lose.
        /// </summary>
        private static IReadOnlyList<float> BotExits(Shared.Data.ValueObjects.JellyLevelCVO level, CD_Events events)
        {
            int total = 0;
            foreach (var gate in level.Gates) total += gate.Stages.Count;
            total = Mathf.Max(1, total);
            float share = Random.Range(Mathf.Min(events.DuelBotMinShare, events.DuelBotMaxShare), Mathf.Max(events.DuelBotMinShare, events.DuelBotMaxShare));
            // An off day: slower than usual.
            if (Random.value < .15f) share = Mathf.Min(.95f, share * Random.Range(1.2f, 1.45f));
            float seconds = Mathf.Max(10f, level.Seconds * share);
            float reaction = Random.Range(1.2f, 3.2f);

            var gaps = new float[total];
            float sum = 0;
            for (int i = 0; i < total; i++)
            {
                // Quick moves in a row, a pause to think, sometimes a long stare; later moves come faster.
                float gap = Random.value < .6f ? Random.Range(.35f, .9f) : Random.Range(1f, 2.2f);
                if (Random.value < .1f) gap *= Random.Range(2f, 3.5f);
                gap *= Mathf.Lerp(1.25f, .8f, total > 1 ? i / (float)(total - 1) : 1f);
                sum += gaps[i] = gap;
            }
            float play = Mathf.Max(1f, seconds - reaction);
            var exits = new List<float>(total);
            float at = reaction;
            for (int i = 0; i < total; i++)
            {
                at += play * gaps[i] / sum;
                exits.Add(i == total - 1 ? seconds : at);
            }
            return exits;
        }

        // Names people pick for themselves: plain first names, nicknames, a number or a letter added.
        private static readonly string[] BOT_FIRST = {
            "Elif", "Zeynep", "Ece", "Defne", "Ayşe", "Merve", "Selin", "Buse", "İrem", "Melis", "Nehir", "Duru",
            "Mert", "Emre", "Kerem", "Burak", "Can", "Arda", "Yusuf", "Efe", "Baran", "Ali", "Ömer", "Kaan",
            "Luna", "Mia", "Emma", "Leo", "Noah", "Alex", "Sara", "Max", "Lina", "Adam" };
        private static readonly string[] BOT_NICK = {
            "jellyqueen", "puzzlemaster", "jöleci", "renkli", "minnoş", "bulmacasever", "pinkie", "starboy",
            "sugarbee", "tatlıcan", "mavişim", "gamer", "lucky", "kedicik", "panda", "sunny" };

        private static string BotName()
        {
            if (Random.value < .3f)
            {
                string nick = BOT_NICK[Random.Range(0, BOT_NICK.Length)];
                return Random.value < .5f ? nick + Random.Range(1, 100) : nick;
            }
            string name = BOT_FIRST[Random.Range(0, BOT_FIRST.Length)];
            float style = Random.value;
            if (style < .3f) return name;
            if (style < .5f) return name.ToLowerInvariant() + Random.Range(1, 100);
            if (style < .7f) return name + Random.Range(1990, 2012) % 100;
            if (style < .85f) return name + " " + (char)('A' + Random.Range(0, 26)) + ".";
            return name.ToLowerInvariant() + "_" + Random.Range(10, 1000);
        }

        /// <summary>An avatar from the catalogue, the free ones more often - the way real players' picks fall.</summary>
        private static string BotIcon(CD_ProfileIcons icons)
        {
            if (icons == null || icons.Icons.Count == 0) return string.Empty;
            var picks = new List<string>();
            foreach (var icon in icons.Icons)
            {
                if (icon == null || string.IsNullOrEmpty(icon.Id)) continue;
                int weight = icon.Price <= 0 ? 3 : 1;
                for (int i = 0; i < weight; i++) picks.Add(icon.Id);
            }
            return picks.Count == 0 ? string.Empty : picks[Random.Range(0, picks.Count)];
        }

        private void Unavailable(string why)
        {
            _duel.Searching = false;
            FlowLogger.Log($"FindDuelOpponentCommand - no duel: {why}.");
            _signals.Outgoing.DuelUnavailable.Dispatch();
            Stop();
        }
    }
}
