using FlowIoC.BaseModule.Injectable.Components;
using FlowIoC.BaseModule.ViewsMediators.View;
using FlowIoC.BaseModule.ViewsMediators.View.Data;
using FlowIoC.BaseModule.ViewsMediators.View.Enums;
using TMPro;
using UnityEngine;

namespace Modules.LocalizationModule.ViewsMediators
{
    /// <summary>
    /// A text that shows a key from the localization tables, in the language in use, and redraws itself when the
    /// language changes. Put it on any TextMeshPro text whose words do not depend on the game - a title, a button's
    /// label - and pick or create the key in its inspector, where every language's text can be edited in place.
    /// Text the game fills in itself (a score, a name, a countdown) stays with the screen that fills it.
    /// <para>
    /// It registers with the localization service's own Root by name rather than the screen it sits in, so a screen
    /// needs no binding of its own for it, and a screen opened from a pool keeps the one registration it had.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TMP_Text))]
    [RequireComponent(typeof(ViewInjector))]
    public class LocalizedTextView : MonoBehaviour, IView
    {
        /// <summary>The Root the localization service's context lives on, as the scene names it.</summary>
        public const string SERVICE_ROOT = "LocalizationServiceRoot";

        [Tooltip("The text's key in the localization tables, e.g. settings.title.")]
        [SerializeField] private string _key;
        [SerializeField] private TMP_Text _text;

        public bool IsRegistered { get; set; }

        public string Key
        {
            get => _key;
            set => _key = value;
        }

        public TMP_Text Text => _text != null ? _text : _text = GetComponent<TMP_Text>();

        public void Show(string text)
        {
            if (Text != null && text != null) Text.text = text;
        }

        private void Awake() => PointAtService();

        /// <summary>
        /// Tells this object's ViewInjector to find its context on the localization service's Root, whatever screen the
        /// text was authored in - the one case a view is not under its own Root.
        /// </summary>
        public void PointAtService()
        {
            var injector = GetComponent<ViewInjector>();
            if (injector == null) return;
            ViewInjectorData entry = injector.GetViewInjectorData(this);
            if (entry == null) return;
            entry.ContextSource = ViewContextSource.RootName;
            entry.RootName = SERVICE_ROOT;
        }

#if UNITY_EDITOR
        private void Reset()
        {
            _text = GetComponent<TMP_Text>();
            PointAtService();
        }
#endif
    }
}
