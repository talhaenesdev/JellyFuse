using FlowIoC.BaseModule.Injectable.Components;
using FlowIoC.ScreenModule.ViewsMediators.Screen;
using TMPro;
using UnityEngine;

namespace Modules.DevToolsModule.FpsScreenModule.ViewsMediators
{
    /// <summary>
    /// The frames-per-second readout. It counts frames itself and writes the number only every
    /// Interval seconds, so the readout costs next to nothing and does not rebuild its canvas each frame.
    /// </summary>
    [RequireComponent(typeof(ViewInjector))]
    public sealed class FpsScreenView : ScreenView
    {
        [SerializeField] private TMP_Text _label;
        [Tooltip("Seconds between two updates of the number.")]
        [SerializeField, Min(.1f)] private float _interval = .5f;
        [Tooltip("At or above this the number is drawn in Good Colour, below Bad Fps in Bad Colour, between in Warn Colour.")]
        [SerializeField] private int _goodFps = 55;
        [SerializeField] private int _badFps = 30;
        [SerializeField] private Color _goodColour = new Color(.55f, 1f, .55f, .85f);
        [SerializeField] private Color _warnColour = new Color(1f, .85f, .35f, .85f);
        [SerializeField] private Color _badColour = new Color(1f, .45f, .45f, .85f);

        private int _frames;
        private float _elapsed;
        // One string per whole number, made once, so the readout leaves no garbage behind.
        private readonly string[] _texts = new string[241];

        private void OnEnable() { _frames = 0; _elapsed = 0; }

        private void Update()
        {
            _frames++;
            _elapsed += Time.unscaledDeltaTime;
            if (_elapsed < _interval) return;
            int fps = Mathf.Clamp(Mathf.RoundToInt(_frames / _elapsed), 0, _texts.Length - 1);
            _frames = 0; _elapsed = 0;
            _label.text = _texts[fps] ??= fps + " FPS";
            _label.color = fps >= _goodFps ? _goodColour : fps < _badFps ? _badColour : _warnColour;
        }

        protected override void PlayShowAnimation() => ShowCompleted?.Invoke(this);
        protected override void PlayHideAnimation() => HideCompleted?.Invoke(this);
    }
}
