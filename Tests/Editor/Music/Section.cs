using RPGFramework.Audio.Music;

namespace RPGFramework.Audio.Tests.Music
{
    /// <summary>A tempo section as an author fills it in.</summary>
    internal readonly struct Section
    {
        internal readonly int       StartBar;
        internal readonly float     BPM;
        internal readonly int       BeatsPerBar;
        internal readonly NoteValue BeatUnit;

        internal Section(int startBar, float bpm, int beatsPerBar, NoteValue beatUnit)
        {
            StartBar    = startBar;
            BPM         = bpm;
            BeatsPerBar = beatsPerBar;
            BeatUnit    = beatUnit;
        }
    }
}
