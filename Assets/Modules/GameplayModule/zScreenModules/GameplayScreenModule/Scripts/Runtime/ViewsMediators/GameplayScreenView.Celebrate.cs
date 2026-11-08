using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace Modules.GameplayModule.GameplayScreenModule.ViewsMediators
{
    /// <summary>
    /// The win, celebrated: the win card pops in with a little overshoot and confetti rains down the screen behind
    /// it for a couple of seconds. The confetti pieces are kept and reused; leaving the panel stops it all.
    /// </summary>
    public partial class GameplayScreenView
    {
        private const int CONFETTI = 46;
        private const float CONFETTI_SECONDS = 2.6f;
        private static readonly Color[] CONFETTI_COLOURS =
        {
            new(1f, .82f, .25f), new(1f, .42f, .55f), new(.45f, .85f, 1f), new(.6f, 1f, .5f), new(.8f, .55f, 1f), Color.white,
        };

        private readonly List<RectTransform> _confetti = new();
        private Coroutine _celebration;

        private void Celebrate(RectTransform panel)
        {
            if (_celebration != null) StopCoroutine(_celebration);
            _celebration = StartCoroutine(CelebrateWin(panel));
        }

        private IEnumerator CelebrateWin(RectTransform panel)
        {
            var dim = (RectTransform)panel.parent;
            var area = dim.rect;
            while (_confetti.Count < CONFETTI)
            {
                var piece = new GameObject("Img_Confetti", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                piece.GetComponent<Image>().raycastTarget = false;
                _confetti.Add(piece);
            }
            var velocity = new Vector2[CONFETTI];
            var spin = new float[CONFETTI];
            var sway = new float[CONFETTI];
            for (int i = 0; i < CONFETTI; i++)
            {
                var piece = _confetti[i];
                piece.SetParent(dim, false);
                // Behind the card, over the dim.
                piece.SetSiblingIndex(panel.GetSiblingIndex());
                piece.anchorMin = piece.anchorMax = piece.pivot = new Vector2(.5f, .5f);
                piece.sizeDelta = new Vector2(Random.Range(.012f, .02f), Random.Range(.02f, .034f)) * area.height;
                piece.anchoredPosition = new Vector2(Random.Range(area.xMin, area.xMax), area.yMax + Random.Range(20f, area.height * .35f));
                piece.localRotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
                piece.GetComponent<Image>().color = CONFETTI_COLOURS[Random.Range(0, CONFETTI_COLOURS.Length)];
                piece.gameObject.SetActive(true);
                // Fast enough to cross the whole screen in the time it rains, whatever the screen size.
                velocity[i] = new Vector2(Random.Range(-.03f, .03f) * area.width, -Random.Range(.5f, .8f) * area.height);
                spin[i] = Random.Range(-360f, 360f);
                sway[i] = Random.Range(0f, 6.3f);
            }

            // The card pops in: from small, past its size, and back.
            for (float t = 0; t < CONFETTI_SECONDS; t += Time.unscaledDeltaTime)
            {
                float dt = Time.unscaledDeltaTime;
                float pop = t < .18f ? Mathf.Lerp(.6f, 1.08f, t / .18f) : t < .32f ? Mathf.Lerp(1.08f, 1f, (t - .18f) / .14f) : 1f;
                panel.localScale = Vector3.one * pop;
                float fade = Mathf.Clamp01((CONFETTI_SECONDS - t) / .5f);
                for (int i = 0; i < CONFETTI; i++)
                {
                    var piece = _confetti[i];
                    var position = piece.anchoredPosition + velocity[i] * dt;
                    position.x += Mathf.Sin(t * 4f + sway[i]) * 40f * dt;
                    piece.anchoredPosition = position;
                    piece.localRotation = Quaternion.Euler(0, 0, piece.localEulerAngles.z + spin[i] * dt);
                    // Flutter: the piece turns edge-on and back as it falls.
                    piece.localScale = new Vector3(Mathf.Abs(Mathf.Sin(t * 6f + sway[i])) * .7f + .3f, 1, 1);
                    var image = piece.GetComponent<Image>();
                    var colour = image.color; colour.a = fade; image.color = colour;
                }
                yield return null;
            }
            StopCelebration(panel);
        }

        /// <summary>The confetti away and the card at its own size.</summary>
        private void StopCelebration(RectTransform panel = null)
        {
            if (_celebration != null) { StopCoroutine(_celebration); _celebration = null; }
            foreach (var piece in _confetti) if (piece != null) piece.gameObject.SetActive(false);
            if (panel != null) panel.localScale = Vector3.one;
            else if (_winPanel != null) _winPanel.localScale = Vector3.one;
        }
    }
}
