using System;
using System.Collections.Generic;
using FlowIoC.BaseModule.Adapters;
using FlowIoC.BaseModule.Constructables;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.SharedData;
using Modules.ProfileModule.RootsContexts;
using Modules.ProfileModule.Shared.Data.UnityObjects;
using Modules.ProfileModule.Shared.Data.ValueObjects;
using UnityEngine;

namespace Modules.ProfileModule.Models
{
    public sealed class ProfileModel : IProfileModel, IConstructable
    {
        [Inject(nameof(ProfileSystemContext))] private GameObject _root { get; set; }
        [Inject] private ISharedDataModel _shared { get; set; }

        private readonly Dictionary<string, ProfileIconCVO> _icons = new(StringComparer.Ordinal);
        private readonly Dictionary<string, ProfileFrameCVO> _frames = new(StringComparer.Ordinal);
        private SD_PlayerProfile _profile;

        public bool IsPostConstructed { get; set; }
        public bool IsDeconstructed { get; set; }
        public ProfileIconVO CurrentIcon { get; private set; }
        public ProfileFrameVO CurrentFrame { get; private set; }

        public void PostConstruct()
        {
            _icons.Clear();
            _frames.Clear();
            var catalogue = _shared.GetScriptable<CD_ProfileIcons>();   // filed as shared: the leaderboard draws these icons too
            var frameCatalogue = _shared.GetScriptable<CD_ProfileFrames>();
            _profile = _shared.GetScriptable<SD_PlayerProfile>();

            if (catalogue != null)
            {
                foreach (ProfileIconCVO icon in catalogue.Icons)
                {
                    if (icon == null || string.IsNullOrWhiteSpace(icon.Id) || _icons.ContainsKey(icon.Id)) continue;
                    _icons.Add(icon.Id, icon);
                }
            }

            if (frameCatalogue != null)
            {
                foreach (ProfileFrameCVO frame in frameCatalogue.Frames)
                {
                    if (frame == null || string.IsNullOrWhiteSpace(frame.Id) || _frames.ContainsKey(frame.Id)) continue;
                    _frames.Add(frame.Id, frame);
                }
            }

            Reload();
        }

        public void Reload()
        {
            string selectedId = _profile != null ? _profile.SelectedIconId : null;
            if (!_icons.TryGetValue(selectedId ?? string.Empty, out ProfileIconCVO selected))
            {
                foreach (ProfileIconCVO icon in _icons.Values) { selected = icon; break; }
            }

            // An icon picked before icons had a price stays the player's.
            if (selected != null && selected.Price > 0) _profile?.AddOwnedIcon(selected.Id);
            CurrentIcon = selected != null ? new ProfileIconVO(selected) : null;
            string frameId = ResolveFrameId(_profile != null ? _profile.SelectedFrameId : null);
            // A frame worn before frames had a price stays the player's.
            if (!string.IsNullOrEmpty(frameId) && _frames[frameId].Price > 0) _profile?.AddOwnedFrame(frameId);
            CurrentFrame = !string.IsNullOrEmpty(frameId) ? new ProfileFrameVO(_frames[frameId]) : null;
            _profile?.SetProfile(NormalizeName(_profile.PlayerName), CurrentIcon?.Id, frameId);
        }

        public void Deconstruct() { _icons.Clear(); _frames.Clear(); }

        public bool Save(ProfileEditVO edit)
        {
            if (edit == null || !_icons.TryGetValue(edit.IconId ?? string.Empty, out ProfileIconCVO icon)) return false;
            // A locked icon is bought first; saving it as it is changes nothing.
            if (!Owns(icon)) return false;
            string frameId = ResolveFrameId(edit.FrameId);
            if (_frames.Count > 0 && string.IsNullOrEmpty(frameId)) return false;
            // So is a locked frame.
            if (!string.IsNullOrEmpty(frameId) && !OwnsFrame(_frames[frameId])) return false;
            _profile?.SetProfile(NormalizeName(edit.PlayerName), icon.Id, frameId);
            CurrentIcon = new ProfileIconVO(icon);
            CurrentFrame = !string.IsNullOrEmpty(frameId) ? new ProfileFrameVO(_frames[frameId]) : null;
            return true;
        }

        /// <summary>Coins the first picked name pays.</summary>
        public const int NAME_REWARD_COINS = 50;

        public bool NameRewardAvailable => _profile != null && !_profile.NameRewarded;

        /// <summary>The saved name is the player's own, and the reward for it was not paid yet: it is marked paid now.</summary>
        public bool ClaimNameReward()
        {
            if (!NameRewardAvailable) return false;
            string name = NormalizeName(_profile.PlayerName);
            if (string.Equals(name, "Player", System.StringComparison.OrdinalIgnoreCase)) return false;
            _profile.MarkNameRewarded();
            return true;
        }

        public ProfileSelectionRVO CreateSelection()
        {
            var result = new ProfileSelectionRVO
            {
                NameReward = NameRewardAvailable ? NAME_REWARD_COINS : 0,
                PlayerName = _profile != null ? NormalizeName(_profile.PlayerName) : "Player",
                SelectedIconId = CurrentIcon?.Id,
                SelectedFrameId = ResolveFrameId(_profile != null ? _profile.SelectedFrameId : null)
            };
            foreach (ProfileIconCVO icon in _icons.Values) result.Icons.Add(new ProfileIconVO(icon, Owns(icon)));
            foreach (ProfileFrameCVO frame in _frames.Values) result.Frames.Add(new ProfileFrameVO(frame, OwnsFrame(frame)));
            return result;
        }

        private bool Owns(ProfileIconCVO icon) =>
            icon.Price <= 0 || (_profile != null && System.Linq.Enumerable.Contains(_profile.OwnedIconIds, icon.Id));

        private bool OwnsFrame(ProfileFrameCVO frame) =>
            frame.Price <= 0 || (_profile != null && System.Linq.Enumerable.Contains(_profile.OwnedFrameIds, frame.Id));

        /// <summary>A frame is bought under its id with this in front, an icon under its plain id.</summary>
        public const string FRAME_PREFIX = "frame:";

        /// <summary>The coins an icon or a frame costs to unlock; 0 when it is free or already the player's, -1 when there is no such item.</summary>
        public int PriceToUnlock(string itemId)
        {
            itemId ??= string.Empty;
            if (itemId.StartsWith(FRAME_PREFIX, StringComparison.Ordinal))
                return !_frames.TryGetValue(itemId.Substring(FRAME_PREFIX.Length), out ProfileFrameCVO frame) ? -1 : OwnsFrame(frame) ? 0 : frame.Price;
            return !_icons.TryGetValue(itemId, out ProfileIconCVO icon) ? -1 : Owns(icon) ? 0 : icon.Price;
        }

        public void Unlock(string itemId)
        {
            if (itemId != null && itemId.StartsWith(FRAME_PREFIX, StringComparison.Ordinal)) _profile?.AddOwnedFrame(itemId.Substring(FRAME_PREFIX.Length));
            else _profile?.AddOwnedIcon(itemId);
        }

        private string ResolveFrameId(string requested)
        {
            if (!string.IsNullOrWhiteSpace(requested) && _frames.ContainsKey(requested)) return requested;
            foreach (string id in _frames.Keys) return id;
            return string.Empty;
        }

        private static string NormalizeName(string value)
        {
            string normalized = string.IsNullOrWhiteSpace(value) ? "Player" : value.Trim();
            return normalized.Length <= 16 ? normalized : normalized.Substring(0, 16);
        }
    }
}
