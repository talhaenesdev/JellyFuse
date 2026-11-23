using System.Collections.Generic;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using Modules.PetHomeModule.Shared.Data.UnityObjects;
using Modules.PetHomeModule.Shared.Data.ValueObjects;
using Modules.PetHomeModule.Shared.Enums;
using UnityEngine;

namespace Modules.PetHomeModule.Models
{
    internal sealed class PetHomeModel : IPetHomeModel
    {
        [Inject] private ISharedDataModel _shared { get; set; }

        private CD_PetHome Config => _shared.GetScriptable<CD_PetHome>();
        private SD_PetHome Save => _shared.GetScriptable<SD_PetHome>();

        public PetHomeItemCVO Item(string id) => Config != null ? Config.Get(id) : null;
        public bool Owns(string id) => Save != null && Save.Owned.Contains(id);

        public void GiveStarters()
        {
            var save = Save;
            var config = Config;
            if (save == null || config == null || save.Started) return;
            save.Started = true;
            foreach (string id in config.StarterItems) { Own(id); Use(id); }
            if (!string.IsNullOrEmpty(config.StarterRoom)) { Own(config.StarterRoom); Use(config.StarterRoom); }
        }

        public void Own(string id)
        {
            if (Save != null && Item(id) != null && !Save.Owned.Contains(id)) Save.Owned.Add(id);
        }

        public void Use(string id)
        {
            var item = Item(id);
            if (item == null || !Owns(id)) return;
            if (item.Category == PetHomeCategory.Room) { Save.Room = id; return; }
            if (Save.Placed.Exists(p => p.Id == id)) return;
            var spot = FreeSpot(item.Spot);
            Save.Placed.Add(new PlacedItemSVO(id, spot.x, spot.y));
        }

        /// <summary>
        /// The nearest place to where an item would like to stand that nothing else stands on, searched in rings
        /// around it. A floor too full for any free place gives back the wish.
        /// </summary>
        private Vector2 FreeSpot(Vector2 wish)
        {
            const float STEP_X = .12f, STEP_Y = .1f;
            for (int ring = 0; ring <= 6; ring++)
            for (int dy = -ring; dy <= ring; dy++)
            for (int dx = -ring; dx <= ring; dx++)
            {
                if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != ring) continue;
                var at = new Vector2(wish.x + dx * STEP_X, wish.y + dy * STEP_Y);
                if (at.x < .05f || at.x > .95f || at.y < .03f || at.y > .95f) continue;
                if (IsFree(at)) return at;
            }
            return wish;
        }

        private bool IsFree(Vector2 at)
        {
            foreach (var placed in Save.Placed)
            {
                var other = Item(placed.Id);
                if (other == null || LIES_FLAT.Contains(other.Id)) continue;
                float x = (placed.X - at.x) / .14f, y = (placed.Y - at.y) / .1f;
                if (x * x + y * y < 1) return false;
            }
            return true;
        }

        /// <summary>A rug lies under things and a wall decoration hangs above them: neither takes up floor.</summary>
        private static readonly HashSet<string> LIES_FLAT = new() { "Decor_Rug", "Decor_Frame", "Decor_Lights" };

        public void Place(PlacedItemSVO placed)
        {
            if (placed == null || Save == null) return;
            var standing = Save.Placed.Find(p => p.Id == placed.Id);
            if (standing == null) return;
            standing.X = Mathf.Clamp01(placed.X);
            standing.Y = Mathf.Clamp01(placed.Y);
        }

        public void Store(string id) => Save?.Placed.RemoveAll(p => p.Id == id);

        public void Empty(string id)
        {
            var item = Item(id);
            if (Save == null || item == null || item.Category != PetHomeCategory.Bowl) return;
            if (!Save.Placed.Exists(p => p.Id == id) || Save.EmptyBowls.Contains(id)) return;
            Save.EmptyBowls.Add(id);
        }

        public void Refill(string id) => Save?.EmptyBowls.Remove(id);

        public PetHomeRVO Snapshot() => new()
        {
            Items = Config != null ? Config.Items : new List<PetHomeItemCVO>(),
            Owned = Save != null ? new HashSet<string>(Save.Owned) : new HashSet<string>(),
            Placed = Save != null ? Save.Placed.ConvertAll(p => new PlacedItemSVO(p.Id, p.X, p.Y)) : new List<PlacedItemSVO>(),
            Room = Save != null ? Save.Room : null,
            Empty = Save != null ? new HashSet<string>(Save.EmptyBowls) : new HashSet<string>(),
        };
    }
}
