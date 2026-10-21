using Modules.LoadingModule.Shared.Data.ValueObjects;
using UnityEngine;

namespace Modules.LoadingModule.LoadingScreenModule.Models
{
    /// <summary>
    /// The last snapshot, close and failure seen for each set, so the opening Command can fill a
    /// screen that finished loading after the news arrived. A Running snapshot clears the close
    /// and the failure - that is a re-run. The background is remembered the same way: the art
    /// may land before the screen instance exists, and the opening Command applies it then.
    /// </summary>
    public interface ILoadingScreenModel
    {
        /// <summary>The art the addressable load brought, or null until it lands.</summary>
        Sprite Background { get; }

        void Remember(LoadingSetStatusRVO status);
        void RememberClosed(string set);
        void RememberFailed(string set, string step);
        void RememberBackground(Sprite background);
        bool TryGetLatest(string set, out LoadingSetStatusRVO status);
        bool IsClosed(string set);
        string FailedStepOf(string set);
        /// <summary>The screen for the set opened at this realtime; the first call for a run counts.</summary>
        void RememberOpened(string set, float time);
        /// <summary>Seconds the set's screen still has to stay up at <paramref name="now"/>; 0 when it may close.</summary>
        float RemainingHold(string set, float now);
    }
}
