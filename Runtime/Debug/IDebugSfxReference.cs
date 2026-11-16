#if ENABLE_RPG_FRAMEWORK_DEBUG
namespace RPGFramework.Audio
{
    /// <summary>A sound playing as the debug room reads it: where it is, how long it is, and how it loops.</summary>
    public interface IDebugSfxReference
    {
        DebugTrack Track { get; }
    }
}
#endif
