using System.Collections.Generic;

namespace Modules.GameplayModule.Models
{
    public sealed class SpeedRunModel : ISpeedRunModel
    {
        private readonly List<int> _levels = new();
        public bool Active { get; private set; }
        public IReadOnlyList<int> Levels => _levels;
        public int Step { get; private set; }
        public float Seconds { get; private set; }
        public bool FirstTry { get; private set; }
        public int CurrentLevel => Step >= 0 && Step < _levels.Count ? _levels[Step] : -1;
        public bool IsLast => Step >= _levels.Count - 1;

        public void Begin(IReadOnlyList<int> levels, float seconds, bool firstTry)
        {
            _levels.Clear();
            if (levels != null) _levels.AddRange(levels);
            Step = 0;
            Seconds = seconds;
            FirstTry = firstTry;
            Active = _levels.Count > 0;
        }

        public void Advance(float secondsLeft)
        {
            if (!Active) return;
            Seconds = secondsLeft;
            Step++;
        }

        public void End()
        {
            Active = false;
            _levels.Clear();
            Step = 0;
            Seconds = 0;
        }
    }
}
