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
            var catalogue = _root.GetComponent<RootAdapter>().GetScriptable<CD_ProfileIcons>();
            var frameCatalogue = _root.GetComponent<RootAdapter>().GetScriptable<CD_ProfileFrames>();
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

            CurrentIcon = selected != null ? new ProfileIconVO(selected) : null;
            string frameId = ResolveFrameId(_profile != null ? _profile.SelectedFrameId : null);
            CurrentFrame = !string.IsNullOrEmpty(frameId) ? new ProfileFrameVO(_frames[frameId]) : null;
            _profile?.SetProfile(NormalizeName(_profile.PlayerName), CurrentIcon?.Id, frameId);
        }

        public void Deconstruct() { _icons.Clear(); _frames.Clear(); }

        public bool Save(ProfileEditVO edit)
        {
            if (edit == null || !_icons.TryGetValue(edit.IconId ?? string.Empty, out ProfileIconCVO icon)) return false;
            string frameId = ResolveFrameId(edit.FrameId);
            if (_frames.Count > 0 && string.IsNullOrEmpty(frameId)) return false;
            _profile?.SetProfile(NormalizeName(edit.PlayerName), icon.Id, frameId);
            CurrentIcon = new ProfileIconVO(icon);
            CurrentFrame = !string.IsNullOrEmpty(frameId) ? new ProfileFrameVO(_frames[frameId]) : null;
            return true;
        }

        public ProfileSelectionRVO CreateSelection()
        {
            var result = new ProfileSelectionRVO
            {
                PlayerName = _profile != null ? NormalizeName(_profile.PlayerName) : "Player",
                SelectedIconId = CurrentIcon?.Id,
                SelectedFrameId = ResolveFrameId(_profile != null ? _profile.SelectedFrameId : null)
            };
            foreach (ProfileIconCVO icon in _icons.Values) result.Icons.Add(new ProfileIconVO(icon));
            foreach (ProfileFrameCVO frame in _frames.Values) result.Frames.Add(new ProfileFrameVO(frame));
            return result;
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
