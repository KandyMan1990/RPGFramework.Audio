using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RPGFramework.Audio.Music;
using UnityEngine;
using UnityEngine.Audio;

namespace RPGFramework.Audio
{
    internal interface IAudioUpdatable
    {
        internal void Update();
    }

    internal interface IStem
    {
        internal AudioClip Clip            { get; }
        internal float     ReverbSendLevel { get; }
    }

    internal interface IMusicAsset
    {
        internal string               Name          { get; }
        internal double               LoopStartTime { get; }
        internal double               LoopEndTime   { get; }
        internal bool                 Loop          { get; }
        internal IReadOnlyList<IStem> Tracks        { get; }
        internal ReverbSettings       Reverb        { get; }
        internal bool[]               GetStemsForState(ulong stateNameHash);
    }

    public interface IMusicAssetProvider
    {
        internal IMusicAsset GetMusicAsset(ulong nameHash);
    }

    public interface IMusicPlayer
    {
        /// <summary>
        /// Plays a track, cutting whatever was playing, at <paramref name="volume" /> — see
        /// <see cref="SetSongVolumeAsync" />.
        /// </summary>
        Task  PlayAsync(ulong nameHash, ulong initialStemStateHash = 0, float fadeInTime = 0f, float volume = 1f);

        /// <summary>
        /// Fades the playing track out and this one in to <paramref name="volume" /> over the same seconds, the two
        /// sharing the music channels while both sound, so it throws when together they have more stems than there are
        /// channels. A crossfade asked for during another cuts the track already fading out. A paused track picks up
        /// where it was, as with <see cref="PlayAsync" />.
        /// </summary>
        Task  CrossfadeAsync(ulong nameHash, ulong initialStemStateHash, float seconds, float volume = 1f);

        /// <summary>
        /// Sets the playing track's own volume, 0 to 1, over <paramref name="seconds" />, zero at once. A linear gain on
        /// its stems, under <see cref="SetVolume" />, which is the player's volume from the game's settings. Does
        /// nothing with no track playing; the next track plays at the volume it is started with.
        /// </summary>
        Task  SetSongVolumeAsync(float volume, float seconds = 0f);

        /// <summary>
        /// Stops the track and unloads it, remembering where it was: a later <see cref="PlayAsync" /> of the same track
        /// picks up from there.
        /// </summary>
        void  Pause();
        Task  StopAsync(float fadeTime = 0.001f);

        /// <summary>
        /// Whether a track is playing: from when it is asked for, its clips still loading included, until it is stopped,
        /// paused, or reaches its end without looping. A track fading out under a stop is not.
        /// </summary>
        bool  IsPlaying();
        void  ClearPausedMusic();
        void  SetMusicAssetProvider(IMusicAssetProvider provider);
        void  SetStemMixerGroups(AudioMixerGroup[]      groups);
        Task  SetStemStateFadeAsync(ulong               stemStateHash, float transitionLength);
        void  SetStemStateImmediate(ulong               stemStateHash);
        float GetVolume();
        void  SetVolume(float percent);

        /// <summary>
        /// Switches PSX Reverb, which every sound shares, to a preset, until the next song that names one or the next
        /// call. A different preset cuts the reverb's tail. Does nothing unless <c>com.rpgframework.psxreverb</c> is
        /// installed.
        /// </summary>
        void SetReverbPreset(ReverbPreset preset);

        /// <summary>
        /// Sets how loud PSX Reverb plays for every sound, 0 to 1, until the next song that names a volume or the next
        /// call. Does nothing unless <c>com.rpgframework.psxreverb</c> is installed.
        /// </summary>
        void SetReverbVolume(float volume);
    }

    public interface ISfxEventData
    {
        string        EventName                 { get; }
        float         EventTriggerTime          { get; }
        internal int  EventTriggerTimeInSamples { get; }
        internal bool RemoveEventOnceTriggered  { get; }
    }

    internal interface ISfxAsset
    {
        internal IReadOnlyList<IStem>         Tracks    { get; }
        internal IReadOnlyList<ISfxEventData> Events    { get; }
        internal bool                         Loop      { get; }
        internal int                          LoopStart { get; }
        internal int                          LoopEnd   { get; }
    }

    public interface ISfxReference
    {
        event Action<string, ISfxReference> OnEvent;
        IReadOnlyList<ISfxEventData>        Events { get; }
        internal ISfxAsset                  Asset  { get; }
        internal void                       CheckForLoop();
        internal void                       CheckForEventToRaise();
        internal void                       Pause();
        internal void                       Resume();
        internal void                       Stop();
    }

    public interface ISfxAssetProvider
    {
        internal ISfxAsset GetSfxAsset(ulong nameHash);
    }

    public interface ISfxPlayer
    {
        ISfxReference Play(ulong          nameHash);
        void          Pause(ISfxReference sfxReference);
        void          PauseAll();
        void          Resume(ISfxReference sfxReference);
        void          ResumeAll();
        void          Stop(ISfxReference sfxReference);
        void          StopAll();
        void          SetSfxAssetProvider(ISfxAssetProvider provider);
        void          SetStemMixerGroups(AudioMixerGroup[]  groups);
        float         GetVolume();
        void          SetVolume(float percent);
    }
}