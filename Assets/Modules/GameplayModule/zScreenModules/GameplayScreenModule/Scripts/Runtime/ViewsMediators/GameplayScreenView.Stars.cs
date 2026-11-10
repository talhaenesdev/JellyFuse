using UnityEngine;
using UnityEngine.UI;

namespace Modules.GameplayModule.GameplayScreenModule.ViewsMediators
{
    /// <summary>The win panel's three stars: the ones earned light up one after another, the rest stay dimmed.</summary>
    public partial class GameplayScreenView
    {
        [Header("Stars")]
        [Tooltip("The win panel's stars, left to right.")]
        [SerializeField] private Image[] _winStars = new Image[0];
        [Tooltip("A star not earned.")]
        [SerializeField] private Color _starOff = new Color(.35f, .3f, .5f, .55f);
        [Tooltip("Seconds between one star lighting and the next.")]
        [SerializeField, Min(0)] private float _starPopDelay = .25f;

        private int _starsShown;
        private float _starsAt;

        /// <summary>Shows the stars the level just won earned; they light once the win panel is up.</summary>
        public void ApplyStars(int stars, bool best)
        {
            _starsShown = Mathf.Clamp(stars, 0, _winStars.Length);
            _starsAt = Time.unscaledTime + (_presentation != null ? _presentation.Hud.WinPanelDelay : 0) + .2f;
            foreach (var star in _winStars)
            {
                if (star == null) continue;
                star.gameObject.SetActive(true);
                star.color = _starOff;
                star.transform.localScale = Vector3.one;
            }
        }

        private void HideStars()
        {
            _starsShown = 0;
            foreach (var star in _winStars) if (star != null) star.gameObject.SetActive(false);
        }

        /// <summary>Called each frame: each earned star lights in turn, popping large and settling.</summary>
        private void StepStars()
        {
            for (int i = 0; i < _starsShown; i++)
            {
                var star = _winStars[i];
                if (star == null) continue;
                float t = (Time.unscaledTime - _starsAt - i * _starPopDelay) / .3f;
                if (t < 0) continue;
                star.color = Color.white;
                float k = Mathf.Clamp01(t);
                star.transform.localScale = Vector3.one * Mathf.Lerp(1.6f, 1f, k * k);
            }
        }
    }
}
