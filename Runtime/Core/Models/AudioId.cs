using System;
using UnityEngine;

namespace CreativeArcana.Audio
{
    [Serializable]
    public struct AudioId : IEquatable<AudioId>
    {
        [SerializeField] private string _value;
        public string Value => _value;

        public AudioId(string value)
        {
            _value = value;
        }
        
        internal void SetId(string id)
        {
            _value = id;
        }

        public bool Equals(AudioId other)
        {
            return _value == other._value;
        }

        public override bool Equals(object obj)
        {
            return obj is AudioId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (_value != null ? _value.GetHashCode() : 0);
        }
        
        public override string ToString() => _value; //for debug
        
        public static bool operator ==(AudioId left, AudioId right) => left.Equals(right);
        public static bool operator !=(AudioId left, AudioId right) => !left.Equals(right);
    }
}