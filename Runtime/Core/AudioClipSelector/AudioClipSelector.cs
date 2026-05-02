using System.Collections.Generic;
using UnityEngine;

namespace CreativeArcana.Audio
{
    public sealed class AudioClipSelector : IAudioClipSelector
    {
        private sealed class ClipSelectionContext
        {
            public int LastIndex = -1;
            public int SequenceIndex;
            public int[] ShuffleBag;
            public int ShuffleIndex;
        }

        private readonly Dictionary<AudioEntry, ClipSelectionContext> _contexts = new();

        public AudioClip GetClip(AudioEntry entry)
        {
            if (entry == null || entry.Clips == null || entry.Clips.Count == 0)
                return null;

            var clips = entry.Clips;

            return entry.PlaybackMode switch
            {
                AudioPlaybackMode.Single => clips[0],
                AudioPlaybackMode.Random => clips[Random.Range(0, clips.Count)],
                AudioPlaybackMode.RandomNoRepeat => GetRandomNoRepeat(entry),
                AudioPlaybackMode.Sequential => GetSequential(entry),
                AudioPlaybackMode.Shuffle => GetShuffle(entry),
                _ => clips[0]
            };
        }

        private ClipSelectionContext GetContext(AudioEntry entry)
        {
            if (!_contexts.TryGetValue(entry, out var context))
            {
                context = new ClipSelectionContext();
                _contexts.Add(entry, context);
            }

            return context;
        }

        private AudioClip GetRandomNoRepeat(AudioEntry entry)
        {
            var clips = entry.Clips;
            var context = GetContext(entry);

            if (clips.Count == 1)
            {
                context.LastIndex = 0;
                return clips[0];
            }

            int index;
            do
            {
                index = Random.Range(0, clips.Count);
            }
            while (index == context.LastIndex);

            context.LastIndex = index;
            return clips[index];
        }

        private AudioClip GetSequential(AudioEntry entry)
        {
            var clips = entry.Clips;
            var context = GetContext(entry);

            // Safety for runtime/editor changes in clip count.
            context.SequenceIndex %= clips.Count;

            var index = context.SequenceIndex;
            context.SequenceIndex = (context.SequenceIndex + 1) % clips.Count;

            context.LastIndex = index;
            return clips[index];
        }

        private AudioClip GetShuffle(AudioEntry entry)
        {
            var clips = entry.Clips;
            var context = GetContext(entry);

            if (clips.Count == 1)
            {
                context.LastIndex = 0;
                return clips[0];
            }

            if (context.ShuffleBag == null || context.ShuffleBag.Length != clips.Count)
                InitializeShuffle(context, clips.Count);

            if (context.ShuffleIndex >= context.ShuffleBag.Length)
                Reshuffle(context);

            var index = context.ShuffleBag[context.ShuffleIndex++];
            context.LastIndex = index;

            return clips[index];
        }

        private void InitializeShuffle(ClipSelectionContext context, int count)
        {
            context.ShuffleBag = new int[count];

            for (var i = 0; i < count; i++)
                context.ShuffleBag[i] = i;

            Reshuffle(context);
        }

        private void Reshuffle(ClipSelectionContext context)
        {
            var previousLast = context.LastIndex;

            for (var i = context.ShuffleBag.Length - 1; i > 0; i--)
            {
                var j = Random.Range(0, i + 1);
                (context.ShuffleBag[i], context.ShuffleBag[j]) =
                    (context.ShuffleBag[j], context.ShuffleBag[i]);
            }

            // Prevent the first clip of the new bag from being the same
            // as the last clip of the previous bag.
            if (context.ShuffleBag.Length > 1 && context.ShuffleBag[0] == previousLast)
            {
                var swapIndex = Random.Range(1, context.ShuffleBag.Length);
                (context.ShuffleBag[0], context.ShuffleBag[swapIndex]) =
                    (context.ShuffleBag[swapIndex], context.ShuffleBag[0]);
            }

            context.ShuffleIndex = 0;
        }

        public void Clear()
        {
            _contexts.Clear();
        }
    }
}
