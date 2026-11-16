using UnityEngine;

namespace RPGFramework.Audio
{
    /// <summary>
    /// How a stem plays on a channel's source — where it starts, how it loops, where it is and when it has ended — on a
    /// generator of ours, or on the web, which has no scriptable audio, on the source's own clip. The source keeps its
    /// volume, pan, pitch and mixer group. <see cref="StemPlaybacks.Create" /> picks one.
    /// </summary>
    internal interface IStemPlayback
    {
        /// <summary>The channel's source, for its volume, pan, pitch and mixer group.</summary>
        AudioSource Source { get; }

        /// <summary>Whether a loop is taken only by <see cref="Tick" />, each frame.</summary>
        bool LoopsOnTick { get; }

        /// <summary>Where the stem is, in its clip's seconds.</summary>
        double Position { get; }

        /// <summary>Whether a stem that does not loop has played to its end; a paused one has not.</summary>
        bool Ended { get; }

        /// <summary>
        /// Plays <paramref name="clip" /> from <paramref name="start" /> seconds, going back to <paramref name="loopStart" />
        /// at <paramref name="loopEnd" /> when it <paramref name="loops" />. Stems played in the same frame, given the same
        /// <paramref name="dspTime" />, start together.
        /// </summary>
        void Play(AudioClip clip, double start, bool loops, double loopStart, double loopEnd, double dspTime);

        /// <summary>Takes a loop the stem has passed the end of, when <see cref="LoopsOnTick" />.</summary>
        void Tick();

        /// <summary>Holds the stem where it is, until <see cref="UnPause" />.</summary>
        void Pause();

        void UnPause();

        void Stop();
    }
}
