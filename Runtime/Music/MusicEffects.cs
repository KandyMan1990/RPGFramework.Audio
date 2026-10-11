namespace RPGFramework.Audio.Music
{
    /// <summary>
    /// The echo and PSX Reverb settings in force when music was paused, as the mixer held them, whoever set them. The
    /// reverb's are zero without PSX Reverb installed. The default is none taken: a song starting applies its own.
    /// </summary>
    internal readonly struct MusicEffects
    {
        internal readonly float EchoDelay;
        internal readonly float EchoDecay;
        internal readonly float ReverbPreset;
        internal readonly float ReverbDepth;
        internal readonly bool  Taken;

        internal MusicEffects(float echoDelay,
                              float echoDecay,
                              float reverbPreset,
                              float reverbDepth)
        {
            EchoDelay    = echoDelay;
            EchoDecay    = echoDecay;
            ReverbPreset = reverbPreset;
            ReverbDepth  = reverbDepth;
            Taken        = true;
        }
    }
}