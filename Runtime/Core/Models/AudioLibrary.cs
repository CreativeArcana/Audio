using System.Collections.Generic;
using UnityEngine;

namespace CreativeArcana.Audio
{
    [CreateAssetMenu(menuName = "CreativeArcana/Audio/Data/AudioLibrary", fileName = "AudioLibrary")]
    public class AudioLibrary : ScriptableObject
    {
        [SerializeField] private List<AudioEntry> _entries;
        
        public IReadOnlyList<AudioEntry> Entries => _entries;
        
        private Dictionary<AudioId, AudioEntry> _map;
        
        private void OnEnable()
        {
            BuildMap();
        }

        public AudioEntry Get(AudioId id)
        {
            if (_map == null)
                BuildMap();
            
            _map.TryGetValue(id, out var entry);
            return entry;
        }
        
        private void BuildMap()
        {
            _map = new Dictionary<AudioId, AudioEntry>();

            if (_entries == null)
                return;

            foreach (var entry in _entries)
            {
                if (entry == null)
                    continue;
                
                if (string.IsNullOrWhiteSpace(entry.Id.Value))
                {
                    Debug.LogError($"AudioEntry has empty AudioId: {entry.name}", entry);
                    continue;
                }

                if (_map.ContainsKey(entry.Id))
                {
                    Debug.LogError($"Duplicate AudioId found: {entry.Id.Value}", entry);
                    continue;
                }

                _map.Add(entry.Id, entry);
            }
        }
    }
}