using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Modules.UiWidgetsModule.Entities
{
    /// <summary>
    /// Particles drawn as UI images, so they sit in a canvas with the button they decorate: sparkles that drift up
    /// around it while it idles, and a burst with a shockwave ring when it is pressed. Only a picture - it decides
    /// nothing and takes no taps. The particles live on a canvas of their own under this object, so their movement
    /// rebuilds only that canvas.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class UiParticleEmitter : MonoBehaviour, IPointerClickHandler
    {
        [Header("Look")]
        [Tooltip("Picked at random for each particle; white art takes the colours below.")]
        [SerializeField] private Sprite[] _sprites;
        [Tooltip("Picked at random for each particle.")]
        [SerializeField] private Color[] _colors = { Color.white };
        [Tooltip("A particle's size range, in units. Soft sparkle art is mostly glow, so it needs more than it looks.")]
        [SerializeField] private Vector2 _size = new(60, 95);

        [Header("Idle")]
        [Tooltip("Particles a second while idle; 0 makes none.")]
        [SerializeField, Min(0)] private float _rate = 2;
        [Tooltip("Idle only while this is shown - a notification dot. Empty: always.")]
        [SerializeField] private GameObject _onlyWhile;
        [SerializeField] private Vector2 _lifetime = new(1.1f, 1.8f);
        [Tooltip("How far an idle particle drifts up over its life.")]
        [SerializeField] private float _rise = 70;
        [Tooltip("How far past the edges an idle particle may start.")]
        [SerializeField] private float _margin = 12;

        [Header("Burst")]
        [Tooltip("A press of this object - or of the button on it - bursts.")]
        [SerializeField] private bool _burstOnClick = true;
        [SerializeField, Min(0)] private int _burstCount = 12;
        [SerializeField] private float _burstSpeed = 260;
        [Tooltip("Sprites a burst uses; empty takes the idle ones.")]
        [SerializeField] private Sprite[] _burstSprites;
        [Tooltip("The shockwave ring under a burst; empty draws none.")]
        [SerializeField] private Sprite _ring;
        [Tooltip("How far the ring grows, against this object's larger side.")]
        [SerializeField] private float _ringScale = 1.6f;

        private const int MAX = 40;

        private struct Particle
        {
            public RectTransform Rect;
            public Image Image;
            public Vector2 Position, Velocity;
            public float Age, Life, Size, Spin, Grow;
            public Color Color;
            public bool Ring;
        }

        private readonly List<Particle> _live = new();
        private readonly Stack<Image> _pool = new();
        private RectTransform _layer;
        private float _owed;

        private RectTransform Rect => (RectTransform)transform;

        private void OnDisable()
        {
            for (int i = _live.Count - 1; i >= 0; i--) Return(i);
            _owed = 0;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_burstOnClick) Burst();
        }

        /// <summary>Throws particles out from the middle, with the ring under them.</summary>
        public void Burst()
        {
            if (!isActiveAndEnabled) return;
            var sprites = _burstSprites != null && _burstSprites.Length > 0 ? _burstSprites : _sprites;
            if (_ring != null)
            {
                var size = Mathf.Max(Rect.rect.width, Rect.rect.height);
                Spawn(_ring, Color.white, Vector2.zero, Vector2.zero, .45f, size * .6f, 0, size * (_ringScale - .6f) / .45f, true);
            }
            for (int i = 0; i < _burstCount; i++)
            {
                var direction = Random.insideUnitCircle.normalized;
                Spawn(Pick(sprites), Pick(_colors), direction * 10, direction * _burstSpeed * Random.Range(.6f, 1.1f),
                    Random.Range(.5f, .8f), Random.Range(_size.x, _size.y) * .7f, Random.Range(-360f, 360f), 0, false);
            }
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            bool idle = _rate > 0 && (_onlyWhile == null || _onlyWhile.activeInHierarchy);
            if (idle)
            {
                _owed += _rate * dt;
                while (_owed >= 1) { _owed--; SpawnIdle(); }
            }
            for (int i = _live.Count - 1; i >= 0; i--)
            {
                var p = _live[i];
                p.Age += dt;
                if (p.Age >= p.Life) { Return(i); continue; }
                float t = p.Age / p.Life;
                p.Velocity *= p.Ring ? 1 : Mathf.Pow(.08f, dt);
                p.Position += p.Velocity * dt;
                p.Size += p.Grow * dt;
                p.Rect.anchoredPosition = p.Position;
                p.Rect.sizeDelta = Vector2.one * p.Size;
                p.Rect.localEulerAngles = new Vector3(0, 0, p.Spin * p.Age);
                // In over the first fifth, out over the last two fifths.
                float alpha = p.Ring ? 1 - t : Mathf.Min(Mathf.Clamp01(t / .2f), Mathf.Clamp01((1 - t) / .4f));
                var color = p.Color; color.a *= alpha;
                p.Image.color = color;
                _live[i] = p;
            }
        }

        private void SpawnIdle()
        {
            var rect = Rect.rect;
            var start = new Vector2(Random.Range(rect.xMin - _margin, rect.xMax + _margin), Random.Range(rect.yMin - _margin, rect.yMax + _margin)) - rect.center;
            float life = Random.Range(_lifetime.x, _lifetime.y);
            Spawn(Pick(_sprites), Pick(_colors), start, new Vector2(Random.Range(-10f, 10f), _rise / life), life,
                Random.Range(_size.x, _size.y), Random.Range(-90f, 90f), 0, false);
        }

        private void Spawn(Sprite sprite, Color color, Vector2 position, Vector2 velocity, float life, float size, float spin, float grow, bool ring)
        {
            if (sprite == null || _live.Count >= MAX) return;
            var image = _pool.Count > 0 ? _pool.Pop() : Create();
            image.sprite = sprite;
            image.gameObject.SetActive(true);
            var rect = (RectTransform)image.transform;
            if (ring) rect.SetAsFirstSibling(); else rect.SetAsLastSibling();
            _live.Add(new Particle
            {
                Rect = rect, Image = image, Position = position, Velocity = velocity, Life = life, Size = size,
                Spin = spin, Grow = grow, Color = color, Ring = ring,
            });
        }

        private Image Create()
        {
            if (_layer == null)
            {
                var layer = new GameObject("Fx_Particles", typeof(RectTransform), typeof(Canvas), typeof(LayoutElement));
                layer.layer = gameObject.layer;
                layer.GetComponent<LayoutElement>().ignoreLayout = true;
                _layer = (RectTransform)layer.transform;
                _layer.SetParent(transform, false);
                _layer.anchorMin = _layer.anchorMax = new Vector2(.5f, .5f);
                _layer.sizeDelta = Vector2.zero;
            }
            var go = new GameObject("Fx_Particle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.layer = gameObject.layer;
            var rect = (RectTransform)go.transform;
            rect.SetParent(_layer, false);
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            var image = go.GetComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;
            return image;
        }

        private void Return(int index)
        {
            var image = _live[index].Image;
            _live.RemoveAt(index);
            if (image == null) return;
            image.gameObject.SetActive(false);
            _pool.Push(image);
        }

        private static T Pick<T>(T[] items) => items == null || items.Length == 0 ? default : items[Random.Range(0, items.Length)];
    }
}
