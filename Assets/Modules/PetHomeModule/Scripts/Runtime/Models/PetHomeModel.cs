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
            if (!Save.Placed.Exists(p => p.Id == id)) Save.Placed.Add(new PlacedItemSVO(id, item.Spot.x, item.Spot.y));
        }

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
