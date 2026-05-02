using System;

namespace CreativeArcana.Audio
{
    public readonly struct AudioHandle : IEquatable<AudioHandle>
    {
        public readonly int Id;
        public bool IsValid => Id != 0;
        public static AudioHandle Invalid => default;

        public AudioHandle(int id)
        {
            Id = id;
        }

        public bool Equals(AudioHandle other) => Id == other.Id;
        public override bool Equals(object obj) => obj is AudioHandle other && Equals(other);
        public override int GetHashCode() => Id;

        public override string ToString() => IsValid ? Id.ToString() : "Invalid";
        
        public static bool operator ==(AudioHandle left, AudioHandle right) => left.Equals(right);
        public static bool operator !=(AudioHandle left, AudioHandle right) => !left.Equals(right);
    }
}