#if ENABLE_RPG_FRAMEWORK_DEBUG
namespace RPGFramework.Audio
{
    /// <summary>The music player as the debug room reads it: the song playing, where it is, and how it loops.</summary>
    public interface IDebugMusicPlayer
    {
        /// <summary>The song playing; false when none is, or the one asked for is still loading.</summary>
        bool TryGetPlaying(out DebugTrack track);
    }
}
#endif
