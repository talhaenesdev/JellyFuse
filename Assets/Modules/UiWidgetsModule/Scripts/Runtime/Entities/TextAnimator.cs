using System.Text.RegularExpressions;
using DG.Tweening;
using Modules.UiWidgetsModule.Data.UnityObjects;
using Modules.UiWidgetsModule.Data.ValueObjects;
using Modules.UiWidgetsModule.Enums;
using TMPro;
using UnityEngine;

namespace Modules.UiWidgetsModule.Entities
{
    /// <summary>
    /// Plays a CD_TextAnimation on the TextMeshPro text beside it, with DOTween: when it is shown, when its text
    /// changes, or when Play is called. Put it on any text, drag a preset onto it, and pick when it plays - no code.
    /// Stop puts the text back as it was. It holds no game rules: what it shows is the screen's business.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TMP_Text))]
    public sealed class TextAnimator : MonoBehaviour
    {
        [Tooltip("The animation it plays. Empty: it does nothing.")]
        [SerializeField] private CD_TextAnimation _preset;
        [Tooltip("Plays each time the text is shown.")]
        [SerializeField] private bool _playOnEnable = true;
        [Tooltip("Plays each time the text changes - a score, a counter. Count Up needs this to know the number before.")]
        [SerializeField] private bool _playOnTextChange;
        [Tooltip("On: runs on real time, while the game is paused too.")]
        [SerializeField] private bool _ignoreTimeScale = true;

        private TMP_Text _text;
        private RectTransform _rect;
        private Sequence _sequence;
        private Vector3 _scale = Vector3.one;
        private Vector2 _position;
        private float _alpha = 1;
        private bool _playing, _writing, _waving, _counting;
        // The text a count is heading for: stopped half way, it is shown at once.
        private string _countTo;
        private string _lastText;
        // What it wrote itself last: TextMeshPro reports a change when it next draws, after the write is over.
        private string _written;
        private TextAnimationCVO _wave;
        private float _waveTime;

        public CD_TextAnimation Preset { get => _preset; set => _preset = value; }
        public bool IsPlaying => _playing;

        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
            _rect = (RectTransform)transform;
            _lastText = _text.text;
        }
        private void OnEnable()
        {
            TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
            if (_playOnEnable) Play();
        }
        private void OnDisable()
        {
            TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
            Stop();
        }

        /// <summary>Plays its preset from the start.</summary>
        public void Play() => Play(_preset);
        /// <summary>Plays this preset from the start, in place of whatever was playing.</summary>
        public void Play(CD_TextAnimation preset) => Play(preset, null);
        /// <summary>Sets the text and plays the preset on it - Count Up counts from the number shown before.</summary>
        public void SetText(string text)
        {
            string before = _text.text;
            _writing = true; _text.text = text; _writing = false;
            _lastText = text; _written = text;
            Play(_preset, before);
        }

        /// <summary>Stops, and puts the text back as it stood.</summary>
        public void Stop()
        {
            if (_sequence != null && _sequence.IsActive()) _sequence.Kill();
            _sequence = null;
            if (!_playing) return;
            _playing = false; _waving = false;
            if (_text == null) return;
            if (_counting && _countTo != null) { _writing = true; _text.text = _written = _countTo; _writing = false; _lastText = _countTo; }
            _counting = false;
            _rect.localScale = _scale;
            _rect.anchoredPosition = _position;
            _text.alpha = _alpha;
            _text.maxVisibleCharacters = 99999;
            if (_wave != null) { _wave = null; _text.ForceMeshUpdate(); }
        }

        private void Play(CD_TextAnimation preset, string before)
        {
            if (preset == null || preset.Effects == null || preset.Effects.Count == 0 || !isActiveAndEnabled) return;
            Stop();
            // What it goes back to; read now, so a layout that moved it since is kept.
            _scale = _rect.localScale; _position = _rect.anchoredPosition; _alpha = _text.alpha;
            _playing = true;
            _sequence = DOTween.Sequence().SetUpdate(_ignoreTimeScale).SetTarget(this);
            bool loops = false;
            foreach (var effect in preset.Effects)
            {
                var tween = Effect(effect, before);
                if (tween == null) continue;
                loops |= effect.Loop || effect.Type == TextAnimationType.Wave;
                _sequence.Insert(effect.Delay, tween);
            }
            if (!loops) _sequence.OnComplete(() => _playing = false);
        }

        private Tween Effect(TextAnimationCVO effect, string before)
        {
            float time = Mathf.Max(.01f, effect.Duration);
            switch (effect.Type)
            {
                case TextAnimationType.Pop:
                    _rect.localScale = _scale * effect.Strength;
                    return _rect.DOScale(_scale, time).SetEase(effect.Ease);
                case TextAnimationType.Punch:
                    return _rect.DOPunchScale(_scale * effect.Strength, time, 6, .6f);
                case TextAnimationType.FadeIn:
                    _text.alpha = 0;
                    return DOTween.To(() => _text.alpha, a => _text.alpha = a, _alpha, time).SetEase(effect.Ease);
                case TextAnimationType.SlideIn:
                    _rect.anchoredPosition = _position + effect.Offset;
                    return DOTween.To(() => _rect.anchoredPosition, p => _rect.anchoredPosition = p, _position, time).SetEase(effect.Ease);
                case TextAnimationType.Typewriter:
                {
                    _text.ForceMeshUpdate();
                    int letters = _text.textInfo.characterCount;
                    _text.maxVisibleCharacters = 0;
                    return DOTween.To(() => _text.maxVisibleCharacters, n => _text.maxVisibleCharacters = n, letters, Mathf.Max(time, letters * effect.CharDelay)).SetEase(Ease.Linear);
                }
                case TextAnimationType.Shake:
                {
                    // Side to side, quickly, dying away by the end.
                    float progress = 0;
                    return DOTween.To(() => progress, p =>
                        {
                            progress = p;
                            _rect.anchoredPosition = _position + new Vector2(Mathf.Sin(p * time * 50) * effect.Strength * (1 - p), 0);
                        }, 1, time).SetEase(Ease.Linear)
                        .OnComplete(() => _rect.anchoredPosition = _position);
                }
                case TextAnimationType.Pulse:
                {
                    var pulse = _rect.DOScale(_scale * (1 + effect.Strength), time * .5f).SetEase(effect.Ease == Ease.OutBack ? Ease.InOutSine : effect.Ease);
                    return pulse.SetLoops(effect.Loop ? -1 : 2, LoopType.Yoyo);
                }
                case TextAnimationType.Wave:
                    _wave = effect; _waveTime = 0; _waving = true;
                    // The letters move in LateUpdate; this keeps the sequence alive while they do.
                    return DOTween.To(() => 0f, _ => { }, 1, time).SetLoops(-1);
                case TextAnimationType.CountUp:
                {
                    var current = _text.text;
                    if (!Number(before ?? _lastText, out long from) || !Number(current, out long to) || from == to) return null;
                    // While it counts, the texts it writes are its own: TextMeshPro reports them frames later, and
                    // none of them is a new text to count to.
                    _counting = true; _countTo = current;
                    return DOTween.To(() => (float)from, v => Write(current, (long)Mathf.Round(v)), to, time).SetEase(effect.Ease)
                        .OnComplete(() => { Write(current, to); _counting = false; _lastText = current; });
                }
            }
            return null;
        }

        private static readonly Regex NUMBER = new(@"-?\d+");
        private static bool Number(string text, out long value)
        {
            value = 0;
            if (string.IsNullOrEmpty(text)) return false;
            var match = NUMBER.Match(text);
            return match.Success && long.TryParse(match.Value, out value);
        }
        /// <summary>The text with its first number replaced, written without counting as a change.</summary>
        private void Write(string template, long value)
        {
            _writing = true;
            _text.text = _written = NUMBER.Replace(template, value.ToString(), 1);
            _writing = false;
        }

        private void OnTextChanged(Object changed)
        {
            if (changed != _text || _writing || _counting || _text.text == _written) return;
            string before = _lastText;
            _lastText = _text.text;
            if (_playOnTextChange && before != _lastText) Play(_preset, before);
        }

        /// <summary>The wave moves each letter up and down, a little after the one before it.</summary>
        private void LateUpdate()
        {
            if (!_waving || _wave == null || _text == null) return;
            _waveTime += _ignoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime;
            _text.ForceMeshUpdate();
            var info = _text.textInfo;
            float round = Mathf.Max(.01f, _wave.Duration);
            for (int i = 0; i < info.characterCount; i++)
            {
                var c = info.characterInfo[i];
                if (!c.isVisible) continue;
                float y = Mathf.Sin((_waveTime - i * _wave.CharDelay) / round * Mathf.PI * 2) * _wave.Strength;
                var vertices = info.meshInfo[c.materialReferenceIndex].vertices;
                for (int v = 0; v < 4; v++) vertices[c.vertexIndex + v].y += y;
            }
            _text.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
        }

        private void OnDestroy() { if (_sequence != null && _sequence.IsActive()) _sequence.Kill(); }
    }
}
