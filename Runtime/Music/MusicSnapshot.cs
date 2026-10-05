namespace RPGFramework.Audio.Music
{
    /// <summary>
    /// What was playing when <see cref="IMusicPlayer.Pause" /> stopped it — which track, where, and which stems — for the
    /// caller to keep and hand back to <see cref="IMusicPlayer.ResumeAsync" />. The default is empty: nothing was playing.
    /// </summary>
    public readonly struct MusicSnapshot
    {
        internal readonly ulong  NameHash;
        internal readonly float  Position;
        internal readonly bool[] Stems;

        internal MusicSnapshot(ulong nameHash, float position, bool[] stems)
        {
            NameHash = nameHash;
            Position = position;
            Stems    = stems;
        }
    }
}
