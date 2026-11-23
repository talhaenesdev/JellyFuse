using System;
using System.Collections.Generic;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using Modules.PetHomeModule.Shared.Enums;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Modules.PetHomeModule.PetHomeScreenModule.ViewsMediators
{
    /// <summary>
    /// The player's animal living in the room: it sits, wanders, and goes over to what stands on the floor - eats from
    /// a bowl, sleeps in a bed, swats a toy, sniffs a decoration - and purrs when tapped. All of it is how it looks;
    /// nothing it does changes the game. Frames are the board animal's, 16 to a row, drawn facing right.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class PetHomePetView : MonoBehaviour, IPointerClickHandler
    {
        public struct Spot
        {
            public string Id;
            public Vector2 Floor;
            public PetHomeCategory Category;
            public bool Empty;
            /// <summary>How wide the item is drawn, so the pet stands at its edge rather than in it.</summary>
            public float Width;
            /// <summary>A ball the player threw: the pet picks it up.</summary>
            public bool Fetch;
            /// <summary>On its way back with the ball in its mouth.</summary>
            public bool Back;
        }

        private const int ROW = 16;
        // Frames rows.
        private const int WALK = 0, SIT = 1, SIT_DOWN = 2, SLEEP = 3, STRETCH = 4, GROOM = 5;
        // TouchFrames rows.
        private const int PETTED = 0, HOP = 4, CURIOUS = 6, SWAT = 7;
        // EatFrames rows.
        private const int EAT = 1, FULL = 2;

        [SerializeField] private PetHomeScreenView _screen;
        [SerializeField] private Image _image;
        [Tooltip("How tall it stands, as a share of the floor's height, at the front.")]
        [SerializeField] private float _height = .3f;
        [SerializeField] private float _fps = 12f;
        [Tooltip("Floor heights a second it walks.")]
        [SerializeField] private float _speed = .3f;
        [Tooltip("Where the mouth is while eating, as a share of the frame from its middle: forward, up. Measured off the eat frames.")]
        [SerializeField] private Vector2 _mouth = new(.37f, .37f);

        public Action Tapped;
        /// <summary>Held still while the room is being edited.</summary>
        public bool Paused { get; set; }

        private enum Act { Sit, Walk, Once, Loop }

        private CD_BoardAnimal _animal;
        private readonly List<Spot> _spots = new();
        private Vector2 _floor = new(.5f, .3f);
        private Vector2 _target;
        private Spot? _goal;
        private string _carrying;
        private bool _facingLeft;
        private Act _act;
        private Sprite[] _clip;
        private int _row;
        private float _clock;
        private float _until;
        private Action _then;

        public void SetAnimal(CD_BoardAnimal animal)
        {
            _animal = animal;
            _image.enabled = animal != null && animal.Frames != null && animal.Frames.Length >= ROW;
            Sit(1.5f);
        }

        public void SetSpots(List<Spot> spots)
        {
            _spots.Clear();
            _spots.AddRange(spots);
        }

        /// <summary>Called over to a spot - the item the player tapped.</summary>
        public void GoTo(Spot spot)
        {
            LetGo();
            Walk(Beside(spot), spot);
        }

        /// <summary>Walked over to a point the player tapped on the floor, to sit there a while.</summary>
        public void WalkTo(Vector2 floor)
        {
            LetGo();
            Walk(new Vector2(Mathf.Clamp01(floor.x), Mathf.Clamp01(floor.y)), null);
        }

        /// <summary>The player threw a ball: it runs over to pick it up and bring it back.</summary>
        public void Fetch(Spot ball)
        {
            LetGo();
            ball.Fetch = true;
            Walk(Beside(ball), ball);
        }

        /// <summary>Drops whatever it carries where it stands - called over, petted, or the room being edited.</summary>
        public void LetGo()
        {
            if (_carrying == null) return;
            _carrying = null;
            _screen.DropCarried();
        }

        public void Pet()
        {
            LetGo();
            Play(_animal?.TouchFrames, PETTED, 2, () => Sit(2f));
        }

        public void OnPointerClick(PointerEventData eventData) => Tapped?.Invoke();

        private void Update()
        {
            if (!_image.enabled || Paused) return;
            _clock += Time.deltaTime;
            if (_act == Act.Walk) StepTowardTarget();
            else if (_clock >= _until) { var next = _then; _then = null; (next ?? Decide)(); }
            Draw();
        }

        // ---- what it does next ----

        private void Decide()
        {
            float roll = UnityEngine.Random.value;
            // A full bowl draws it more than anything else in the room.
            int full = _spots.FindIndex(s => s.Category == PetHomeCategory.Bowl && !s.Empty);
            if (full >= 0 && roll < .35f) GoTo(_spots[full]);
            else if (_spots.Count > 0 && roll < .6f) GoTo(_spots[UnityEngine.Random.Range(0, _spots.Count)]);
            else if (roll < .8f) Walk(new Vector2(UnityEngine.Random.Range(.1f, .9f), UnityEngine.Random.Range(.05f, .8f)), null);
            else if (roll < .9f) Play(_animal.Frames, GROOM, 1, () => Sit(2f));
            else Play(_animal.Frames, STRETCH, 1, () => Sit(2f));
        }

        private void Arrive()
        {
            var goal = _goal;
            // Whatever it came for, it turns to face it.
            if (goal.HasValue && goal.Value.Category != PetHomeCategory.Bed && !goal.Value.Back) _facingLeft = goal.Value.Floor.x < _floor.x;
            if (goal.HasValue && goal.Value.Back)
            {
                // Home with the ball: it drops it at the player's feet and is very pleased with itself.
                LetGo();
                Play(_animal.TouchFrames, HOP, 1, () => Sit(2f));
                return;
            }
            if (goal.HasValue && goal.Value.Fetch)
            {
                _carrying = goal.Value.Id;
                _screen.PickUp(_carrying);
                Walk(_screen.FetchPoint(), new Spot { Id = _carrying, Category = PetHomeCategory.Toy, Back = true });
                return;
            }
            switch (goal?.Category)
            {
                case PetHomeCategory.Bowl when goal.Value.Empty:
                    Play(_animal.TouchFrames, CURIOUS, 1, () => Sit(2f));
                    break;
                case PetHomeCategory.Bowl:
                    string bowl = goal.Value.Id;
                    Loop(_animal.EatFrames, EAT, 4f, () =>
                    {
                        _screen.PetAte(bowl);
                        Play(_animal.EatFrames, FULL, 1, () => Sit(2f));
                    });
                    break;
                case PetHomeCategory.Bed:
                    Play(_animal.Frames, SIT_DOWN, 1, () => Loop(_animal.Frames, SLEEP, 7f, () => Play(_animal.Frames, STRETCH, 1, () => Sit(1.5f))));
                    break;
                case PetHomeCategory.Toy when _screen.Rolls(goal.Value.Id):
                    // One swat sends a ball off the way it is facing, and it hops after it for joy.
                    string toy = goal.Value.Id;
                    bool left = _facingLeft;
                    Play(_animal.TouchFrames, SWAT, 1, () =>
                    {
                        _screen.Kick(toy, left);
                        Play(_animal.TouchFrames, HOP, 1, () => Sit(1.2f));
                    });
                    break;
                case PetHomeCategory.Toy:
                    Loop(_animal.TouchFrames, SWAT, 3f, () => Play(_animal.TouchFrames, HOP, 1, () => Sit(1.5f)));
                    break;
                case PetHomeCategory.Decor:
                    Play(_animal.TouchFrames, CURIOUS, 1, () => Sit(2f));
                    break;
                default:
                    Sit(UnityEngine.Random.Range(2f, 4.5f));
                    break;
            }
        }

        /// <summary>
        /// Where it stands to use an item, worked out in the room's pixels so it holds in a room of any size: on a bed;
        /// at a bowl or a toy side on, its mouth over the near half; beside a decoration. It comes at it from the side
        /// it is already on, and from the other side when that one is off the floor.
        /// </summary>
        private Vector2 Beside(Spot spot)
        {
            Vector2 at = _screen.FloorToLocal(spot.Floor);
            if (spot.Category == PetHomeCategory.Bed) return _screen.LocalToFloor(at - new Vector2(0, 2));
            float h = PetHeight(spot.Floor.y);
            float reach = spot.Category == PetHomeCategory.Decor
                ? h * .45f + spot.Width * .45f
                : h * _mouth.x - spot.Width * .12f;
            float side = _screen.FloorToLocal(_floor).x <= at.x ? -1 : 1;
            // Just behind the item, so the bowl's rim is drawn over the mouth dipping into it.
            var stand = _screen.LocalToFloor(at + new Vector2(side * reach, 2));
            if (stand.x < 0 || stand.x > 1) stand = _screen.LocalToFloor(at + new Vector2(-side * reach, 2));
            return new Vector2(Mathf.Clamp01(stand.x), Mathf.Clamp01(stand.y));
        }

        private float PetHeight(float depth) => _screen.FloorHeight * _height * _screen.DepthScale(depth);

        // ---- acts ----

        private void Sit(float seconds) => Begin(Act.Sit, _animal?.Frames, SIT, seconds, null);

        private void Walk(Vector2 target, Spot? goal)
        {
            if (_animal == null) return;
            _target = target;
            _goal = goal;
            Begin(Act.Walk, _animal.Frames, WALK, float.MaxValue, null);
        }

        private void Play(Sprite[] clip, int row, int times, Action then)
        {
            if (!Has(clip, row)) { clip = _animal?.Frames; row = SIT; }
            Begin(Act.Once, clip, row, times * ROW / _fps, then);
        }

        private void Loop(Sprite[] clip, int row, float seconds, Action then)
        {
            if (!Has(clip, row)) { clip = _animal?.Frames; row = SIT; }
            Begin(Act.Loop, clip, row, seconds, then);
        }

        private void Begin(Act act, Sprite[] clip, int row, float seconds, Action then)
        {
            _act = act;
            _clip = clip;
            _row = row;
            _clock = 0;
            _until = seconds;
            _then = then;
        }

        private static bool Has(Sprite[] clip, int row) => clip != null && clip.Length >= (row + 1) * ROW;

        private void StepTowardTarget()
        {
            float height = Mathf.Max(1, _screen.FloorHeight);
            Vector2 here = _screen.FloorToLocal(_floor);
            Vector2 there = _screen.FloorToLocal(_target);
            Vector2 toward = there - here;
            float step = _speed * height * Time.deltaTime * _screen.DepthScale(_floor.y);
            if (Mathf.Abs(toward.x) > 1f) _facingLeft = toward.x < 0;
            if (toward.magnitude <= step)
            {
                _floor = _target;
                Arrive();
                return;
            }
            Vector2 moved = here + toward.normalized * step;
            var rect = _screen.FloorToLocal(Vector2.one);
            _floor = new Vector2(rect.x > 0 ? moved.x / rect.x : 0, rect.y > 0 ? moved.y / rect.y : 0);
        }

        private void Draw()
        {
            if (!Has(_clip, _row)) return;
            int frame = (int)(_clock * _fps) % ROW;
            if (_act == Act.Once) frame = Mathf.Min(ROW - 1, (int)(_clock * _fps) % ROW);
            var sprite = _clip[_row * ROW + frame];
            if (sprite == null) return;
            _image.sprite = sprite;

            var rect = (RectTransform)transform;
            rect.anchoredPosition = _screen.FloorToLocal(_floor);
            var reference = _animal.Frames[0];
            float h = PetHeight(_floor.y);
            float scale = reference != null && reference.rect.height > 0 ? h / reference.rect.height : 1;
            rect.sizeDelta = sprite.rect.size * scale;
            rect.localScale = new Vector3(_facingLeft ? -1 : 1, 1, 1);
        }
    }
}
