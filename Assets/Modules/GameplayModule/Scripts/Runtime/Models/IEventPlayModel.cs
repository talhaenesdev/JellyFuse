using Modules.GameplayModule.Shared.Enums;

namespace Modules.GameplayModule.Models
{
    /// <summary>A level played for an event: which event, and which level of the game it plays.</summary>
    public interface IEventPlayModel
    {
        EventPlayKind Kind { get; }
        bool Active { get; }
        int LevelIndex { get; }
        void Begin(EventPlayKind kind, int levelIndex);
        void End();
    }

    public sealed class EventPlayModel : IEventPlayModel
    {
        public EventPlayKind Kind { get; private set; }
        public bool Active => Kind != EventPlayKind.None && LevelIndex >= 0;
        public int LevelIndex { get; private set; } = -1;

        public void Begin(EventPlayKind kind, int levelIndex) { Kind = kind; LevelIndex = levelIndex; }
        public void End() { Kind = EventPlayKind.None; LevelIndex = -1; }
    }
}
