using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Modules.UiWidgetsModule.Entities
{
    /// <summary>
    /// Plays a reward being taken: coins burst out of where they were won and stream to the coin counter's place on
    /// the screen; anything else pops and rises away. Only a picture - the reward is already given. Put it on a
    /// popup's root and call Fly when the reward lands.
    /// </summary>
    public sealed class RewardFlyer : MonoBehaviour
    {
        [Tooltip("Where the pieces fly, drawn over everything else in it. Empty: this object.")]
        [SerializeField] private RectTransform _layer;
        [Tooltip("The coin counter's place on the screen, from bottom-left (0, 0) to top-right (1, 1).")]
        [SerializeField] private Vector2 _coinTarget = new(.03f, .898f);
        [SerializeField] private Vector2 _pieceSize = new(64, 64);
        [Tooltip("How far the coins scatter before they stream away.")]
        [SerializeField, Min(0)] private float _burstRadius = 90;
        [SerializeField, Min(.1f)] private float _flySeconds = .7f;
        [Tooltip("How far a reward that is not coins rises before it fades.")]
        [SerializeField, Min(0)] private float _riseDistance = 260;

        private readonly List<GameObject> _pieces = new();

        private RectTransform Layer => _layer != null ? _layer : (RectTransform)transform;

        /// <summary>Coins stream to the counter, as many pieces as given; anything else rises from where it was won.</summary>
        public void Fly(Sprite icon, RectTransform from, bool coins, int pieces = 1)
        {
            if (icon == null || from == null) return;
            Vector2 start = Local(from.position);
            if (!coins) { Rise(icon, start); return; }
            var rect = Layer.rect;
            Vector2 target = new Vector2(rect.xMin + rect.width * _coinTarget.x, rect.yMin + rect.height * _coinTarget.y) - rect.center;
            for (int i = 0; i < Mathf.Max(1, pieces); i++)
            {
                var piece = Piece(icon, start);
                piece.localScale = Vector3.zero;
                Vector2 burst = start + Random.insideUnitCircle * _burstRadius;
                float delay = i * .05f;
                DOTween.Sequence().SetUpdate(true).SetLink(piece.gameObject)
                    .Insert(delay, piece.DOScale(1, .2f).SetEase(Ease.OutBack))
                    .Insert(delay, To(piece, burst, .25f).SetEase(Ease.OutQuad))
                    .Insert(delay + .3f, To(piece, target, _flySeconds).SetEase(Ease.InCubic))
                    .Insert(delay + .3f + _flySeconds * .7f, piece.DOScale(.5f, _flySeconds * .3f))
                    .OnComplete(() => Remove(piece.gameObject));
            }
        }

        private void Rise(Sprite icon, Vector2 start)
        {
            var piece = Piece(icon, start);
            piece.localScale = Vector3.one * .4f;
            var image = piece.GetComponent<Image>();
            DOTween.Sequence().SetUpdate(true).SetLink(piece.gameObject)
                .Append(piece.DOScale(1.6f, .3f).SetEase(Ease.OutBack))
                .Append(To(piece, start + Vector2.up * _riseDistance, _flySeconds).SetEase(Ease.OutCubic))
                .Join(DOTween.To(() => image.color.a, a => image.color = new Color(1, 1, 1, a), 0, _flySeconds).SetEase(Ease.InQuad))
                .OnComplete(() => Remove(piece.gameObject));
        }

        // The UI tween shortcuts are not compiled in this project, so the anchored position is tweened by hand.
        private static Tween To(RectTransform piece, Vector2 to, float seconds) =>
            DOTween.To(() => piece.anchoredPosition, p => piece.anchoredPosition = p, to, seconds);

        private RectTransform Piece(Sprite icon, Vector2 at)
        {
            var go = new GameObject("Fx_Reward", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.layer = gameObject.layer;
            var piece = (RectTransform)go.transform;
            piece.SetParent(Layer, false);
            piece.SetAsLastSibling();
            piece.anchorMin = piece.anchorMax = new Vector2(.5f, .5f);
            piece.sizeDelta = _pieceSize;
            piece.anchoredPosition = at;
            var image = go.GetComponent<Image>();
            image.sprite = icon;
            image.preserveAspect = true;
            image.raycastTarget = false;
            _pieces.Add(go);
            return piece;
        }

        // A world point in the layer's own space, measured from its centre where the pieces are anchored.
        private Vector2 Local(Vector3 world)
        {
            Vector2 local = Layer.InverseTransformPoint(world);
            return local - Layer.rect.center;
        }

        private void Remove(GameObject piece)
        {
            _pieces.Remove(piece);
            if (piece != null) Destroy(piece);
        }

        // A popup closed mid-flight comes back from the pool with nothing left flying.
        private void OnDisable()
        {
            foreach (var piece in _pieces) if (piece != null) Destroy(piece);
            _pieces.Clear();
        }
    }
}
