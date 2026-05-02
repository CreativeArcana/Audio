using System.Collections.Generic;

namespace CreativeArcana.Audio
{
    public delegate void SetChannelDuckingMultiplier(AudioChannel channel, float multiplier, float fadeDuration = 0);

    /// <summary>
    /// Notes:
    /// - If multiple ducking profiles for a channel are active, the newest active ducking takes priority.
    /// - Paused ducking profiles remain registered but are temporarily removed from the active priority stack.
    /// - When all active ducking for a channel are released or paused, the multiplier resets to 1.
    /// </summary>
    public class DuckingManager : IDuckingManager
    {
        private readonly SetChannelDuckingMultiplier _setChannelDuckingMultiplier;

        // int = AudioHandle.Id
        // Registered profiles that are not fully released yet.
        private readonly Dictionary<int, DuckingProfile> _profiles = new();

        // Active, non-paused ids per ducked channel.
        // The last id in the list is the newest active ducking source and has priority.
        private readonly Dictionary<AudioChannel, List<int>> _activeByChannel = new();

        // Paused profile ids.
        private readonly HashSet<int> _pausedIds = new();

        public DuckingManager(SetChannelDuckingMultiplier setChannelDuckingMultiplier)
        {
            _setChannelDuckingMultiplier = setChannelDuckingMultiplier;
        }

        public void Apply(int audioId, DuckingProfile profile)
        {
            if (audioId == 0) // Invalid Id
                return;

            // If this id already exists, clean its previous contribution first.
            // This prevents stale channel references if the profile changes.
            if (_profiles.ContainsKey(audioId))
                Release(audioId);

            if (!profile.Enabled || profile.DuckChannels == null)
                return;

            _profiles[audioId] = profile;
            _pausedIds.Remove(audioId);

            foreach (var channel in GetUniqueChannels(profile))
            {
                AddActiveId(channel, audioId);

                _setChannelDuckingMultiplier(
                    channel,
                    profile.DuckVolume,
                    profile.FadeInDuration
                );
            }
        }

        public void Release(int audioId)
        {
            if (audioId == 0) // Invalid Id
                return;

            if (!_profiles.Remove(audioId, out var profile))
                return;

            _pausedIds.Remove(audioId);

            if (profile.DuckChannels == null)
                return;

            foreach (var channel in GetUniqueChannels(profile))
            {
                RemoveActiveId(channel, audioId);
                RefreshChannelDuckingAfterRemoval(channel, profile.FadeOutDuration);
            }
        }

        public void Pause(int audioId)
        {
            if (audioId == 0) // Invalid Id
                return;

            if (!_profiles.TryGetValue(audioId, out var profile))
                return;

            if (!_pausedIds.Add(audioId))
                return;

            if (profile.DuckChannels == null)
                return;

            foreach (var channel in GetUniqueChannels(profile))
            {
                RemoveActiveId(channel, audioId);
                RefreshChannelDuckingAfterRemoval(channel, profile.FadeOutDuration);
            }
        }

        public void Resume(int audioId)
        {
            if (audioId == 0) // Invalid Id
                return;

            if (!_profiles.TryGetValue(audioId, out var profile))
                return;

            if (!_pausedIds.Remove(audioId))
                return;

            if (!profile.Enabled || profile.DuckChannels == null)
                return;

            foreach (var channel in GetUniqueChannels(profile))
            {
                AddActiveId(channel, audioId);

                // Resumed audio becomes the newest active ducking source.
                _setChannelDuckingMultiplier(
                    channel,
                    profile.DuckVolume,
                    profile.FadeInDuration
                );
            }
        }

        public void Clear()
        {
            foreach (var channel in _activeByChannel.Keys)
            {
                _setChannelDuckingMultiplier(channel, 1f);
            }

            _profiles.Clear();
            _activeByChannel.Clear();
            _pausedIds.Clear();
        }

        private void AddActiveId(AudioChannel channel, int audioId)
        {
            if (!_activeByChannel.TryGetValue(channel, out var list))
            {
                list = new List<int>();
                _activeByChannel[channel] = list;
            }

            // Move to newest position.
            list.Remove(audioId);
            list.Add(audioId);
        }

        private void RemoveActiveId(AudioChannel channel, int audioId)
        {
            if (!_activeByChannel.TryGetValue(channel, out var list))
                return;

            list.Remove(audioId);

            if (list.Count == 0)
                _activeByChannel.Remove(channel);
        }

        private void RefreshChannelDuckingAfterRemoval(AudioChannel channel, float fallbackFadeOutDuration)
        {
            if (!_activeByChannel.TryGetValue(channel, out var list) || list.Count == 0)
            {
                _setChannelDuckingMultiplier(channel, 1f, fallbackFadeOutDuration);
                return;
            }

            var newestId = list[^1];

            if (_profiles.TryGetValue(newestId, out var newestProfile))
            {
                _setChannelDuckingMultiplier(
                    channel,
                    newestProfile.DuckVolume,
                    newestProfile.FadeInDuration
                );
            }
            else
            {
                // Defensive fallback.
                list.Remove(newestId);
                RefreshChannelDuckingAfterRemoval(channel, fallbackFadeOutDuration);
            }
        }

        private static IEnumerable<AudioChannel> GetUniqueChannels(DuckingProfile profile)
        {
            var seen = new HashSet<AudioChannel>();

            foreach (var channel in profile.DuckChannels)
            {
                if (seen.Add(channel))
                    yield return channel;
            }
        }
    }
}
