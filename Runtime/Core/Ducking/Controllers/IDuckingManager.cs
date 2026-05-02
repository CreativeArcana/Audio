namespace CreativeArcana.Audio
{
    public interface IDuckingManager
    {
        void Apply(int audioId, DuckingProfile profile);
        void Release(int audioId);

        void Pause(int audioId);
        void Resume(int audioId);

        void Clear();
    }
}