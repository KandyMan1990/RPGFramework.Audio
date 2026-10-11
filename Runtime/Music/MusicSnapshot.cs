namespace RPGFramework.Audio.Music
{
    /// <summary>
    /// What was playing when <see cref="IMusicPlayer.Pause" /> stopped it — which track, where, which stems, and the
    /// echo and reverb in force — for the caller to keep and hand back to <see cref="IMusicPlayer.ResumeAsync" />. The
    /// default is empty: nothing was playing.
    /// </summary>
    public readonly struct MusicSnapshot
    {
        internal readonly MusicEffects Effects;
        internal readonly ulong        NameHash;
        internal readonly float        Position;
        internal readonly bool[]       Stems;

        internal MusicSnapshot(MusicEffects effects,
                               ulong        nameHash,
                               float        position,
                               bool[]       stems)
        {
            Effects  = effects;
            NameHash = nameHash;
            Position = position;
            Stems    = stems;
        }
    }
}