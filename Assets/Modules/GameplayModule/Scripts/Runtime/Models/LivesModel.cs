using System;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using Modules.GameplayModule.Shared.Data.ValueObjects;

namespace Modules.GameplayModule.Models
{
    public sealed class LivesModel : ILivesModel
    {
        public Enums.WaitingLevelStart WaitingStart { get; set; }

        public void Refresh(SD_JellyProgress progress, CD_Lives config, long now)
        {
            if (!progress.LivesGranted)
            {
                progress.LivesGranted = true;
                progress.Lives = config.StartLives;
                progress.NextLifeAt = 0;
            }
            // Never more than MaxLives: a save from before the cap, or a lowered MaxLives, comes back down to it.
            if (progress.Lives >= config.MaxLives) { progress.Lives = config.MaxLives; progress.NextLifeAt = 0; return; }
            if (progress.NextLifeAt <= 0 || progress.NextLifeAt - now > config.RefillSeconds) progress.NextLifeAt = now + config.RefillSeconds;
            while (progress.Lives < config.MaxLives && now >= progress.NextLifeAt)
            {
                progress.Lives = Math.Min(config.MaxLives, progress.Lives + config.LivesPerRefill);
                progress.NextLifeAt += config.RefillSeconds;
            }
            if (progress.Lives >= config.MaxLives) progress.NextLifeAt = 0;
        }

        public void Lose(SD_JellyProgress progress, CD_Lives config, long now)
        {
            Refresh(progress, config, now);
            if (progress.Lives > 0) progress.Lives--;
            Refresh(progress, config, now);
        }

        public void Add(SD_JellyProgress progress, CD_Lives config, int lives, long now)
        {
            progress.Lives = Math.Clamp(progress.Lives + lives, 0, config.MaxLives);
            Refresh(progress, config, now);
        }

        public void Fill(SD_JellyProgress progress, CD_Lives config)
        {
            progress.LivesGranted = true;
            progress.Lives = config.MaxLives;
            progress.NextLifeAt = 0;
        }

        public LivesStateRVO State(SD_JellyProgress progress, CD_Lives config, long now) => new()
        {
            Lives = progress.Lives, MaxLives = config.MaxLives,
            SecondsToNext = progress.NextLifeAt > 0 ? Math.Max(0, progress.NextLifeAt - now) : 0,
        };
    }
}
