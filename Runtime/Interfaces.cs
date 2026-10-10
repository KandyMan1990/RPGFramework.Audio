using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RPGFramework.Audio.Music;
using RPGFramework.Audio.Sfx;
using UnityEngine;
using UnityEngine.Audio;

namespace RPGFramework.Audio
{
    /// <summary>A player ticked each frame by the update driver on its GameObject.</summary>
    internal interface IAudioUpdatable
    {
        /// <summary>Called each frame by the player's update driver, while the driver is enabled.</summary>
        internal void Update();
    }

    /// <summary>One track of a song or a sound, played on a channel of its own.</summary>
    internal interface IStem
    {
        /// <summary>The audio it plays.</summary>
        internal AudioClip Clip { get; }

        /// <summary>How much of it goes to the reverb, 0 to 1.</summary>
        internal float ReverbSendLevel { get; }

        /// <summary>How much of it goes to the echo, 0 to 1.</summary>
        internal float EchoSendLevel { get; }
    }

    /// <summary>
    /// A song or a sound: what a bundle holds, and whose stems' audio data the players load and unload.
    /// </summary>
    internal interface IAudioAsset
    {
        /// <summary>Its stems, in the order their channels play them.</summary>
        internal IReadOnlyList<IStem> Tracks { get; }
    }

    /// <summary>A song: its stems, its stem states, where it loops, and what it sets on the reverb and the echo.</summary>
    internal interface IMusicAsset : IAudioAsset
    {
        /// <summary>The asset's name, whose hash is how the song is asked for.</summary>
        internal string Name { get; }

        /// <summary>Where the loop starts, in seconds, from the loop's start bar and the tempo sections.</summary>
        internal double LoopStartTime { get; }

        /// <summary>Where the loop ends and jumps back to its start, in seconds.</summary>
        internal double LoopEndTime { get; }

        /// <summary>Whether the song loops: ticked, with loop points that make sense.</summary>
        internal bool Loop { get; }

        /// <summary>What the song sets on PSX Reverb when it starts.</summary>
        internal ReverbSettings Reverb { get; }

        /// <summary>What the song sets on the echo when it starts.</summary>
        internal EchoSettings Echo { get; }

        /// <summary>
        /// Whether the echo's delay can change as the song plays: a note length, in a song with more than one tempo.
        /// </summary>
        internal bool EchoFollowsTempo { get; }

        /// <summary>
        /// Which tracks sound in the stem state with this name hash, one flag per track; 0 names the first state.
        /// </summary>
        internal bool[] GetStemsForState(ulong stateNameHash);

        /// <summary>
        /// The echo's delay <paramref name="seconds" /> into the song: its milliseconds, or its note length at the tempo
        /// there.
        /// </summary>
        internal float GetEchoDelayMilliseconds(double seconds);
    }

    /// <summary>
    /// Where the music player gets its songs: <see cref="MusicAssetProvider" />, listing them in the build, or
    /// <see cref="BundledMusicAssetProvider" />, from bundles. The player calls both members, never a game.
    /// </summary>
    public interface IMusicAssetProvider
    {
        /// <summary>
        /// The song with this name hash, held until a matching <see cref="Release" />: held twice, it needs releasing
        /// twice. One held already comes back as a finished task. Throws for a name the provider does not have.
        /// </summary>
        Task<MusicAsset> AcquireAsync(ulong nameHash);

        /// <summary>
        /// Lets go of one hold, so a provider that loaded the song can unload it once nothing holds it.
        /// </summary>
        void Release(ulong nameHash);
    }

    /// <summary>
    /// Plays one song at a time, two while one crossfades into another, in stems on the mixer's music channels.
    /// </summary>
    public interface IMusicPlayer : IDisposable
    {
        /// <summary>
        /// Plays a track, cutting whatever was playing, at <paramref name="volume" /> — see
        /// <see cref="SetSongVolumeAsync" />.
        /// </summary>
        Task PlayAsync(ulong nameHash, ulong initialStemStateHash = 0, float fadeInTime = 0f, float volume = 1f);

        /// <summary>
        /// Fades the playing track out and this one in to <paramref name="volume" /> over the same seconds, the two
        /// sharing the music channels while both sound, so it throws when together they have more stems than there are
        /// channels. A crossfade asked for during another cuts the track already fading out.
        /// </summary>
        Task CrossfadeAsync(ulong nameHash, ulong initialStemStateHash, float seconds, float volume = 1f);

        /// <summary>
        /// Sets the playing track's own volume, 0 to 1, over <paramref name="seconds" />, zero at once. A linear gain on
        /// its stems, under <see cref="SetVolume" />, which is the player's volume from the game's settings. Does
        /// nothing with no track playing; the next track plays at the volume it is started with.
        /// </summary>
        Task SetSongVolumeAsync(float volume, float seconds = 0f);

        /// <summary>
        /// Stops every track and unloads it, and returns what was playing — which track, where, and which stems — for
        /// the caller to keep and hand to <see cref="ResumeAsync" />. The player keeps nothing itself. With no track
        /// playing, or one that has reached its end, the snapshot is empty.
        /// </summary>
        MusicSnapshot Pause();

        /// <summary>
        /// Plays a paused track again from where it was, with the stems it had, fading whatever is playing out under it
        /// over <paramref name="seconds" /> as <see cref="CrossfadeAsync" /> does, zero cutting. It plays at
        /// <paramref name="volume" />, as <see cref="PlayAsync" /> does, not the volume it was paused at. An empty
        /// snapshot fades out whatever is playing instead, since nothing was playing when it was taken.
        /// </summary>
        Task ResumeAsync(MusicSnapshot snapshot, float seconds = 0f, float volume = 1f);

        /// <summary>
        /// Fades every track out over <paramref name="fadeTime" /> seconds, then stops and unloads it. A track still
        /// loading never starts.
        /// </summary>
        Task StopAsync(float fadeTime = 0.001f);

        /// <summary>
        /// Whether a track is playing: from when it is asked for, its clips still loading included, until it is stopped,
        /// paused, or reaches its end without looping. A track fading out under a stop is not.
        /// </summary>
        bool IsPlaying();

        /// <summary>Sets where songs come from. Set it before any song is played or preloaded.</summary>
        void SetMusicAssetProvider(IMusicAssetProvider provider);

        /// <summary>
        /// Builds the player, stopping whatever is playing: a music channel per mixer group, each playing one stem, so a
        /// track needs a group per stem and a crossfade's two tracks share them. The groups share one mixer, whose
        /// exposed parameters the player sets.
        /// </summary>
        void SetStemMixerGroups(AudioMixerGroup[] groups);

        /// <summary>
        /// Fades the playing track's stems to the stem state with this name hash over
        /// <paramref name="transitionLength" /> seconds, zero switching at once. Does nothing with no track playing.
        /// </summary>
        Task SetStemStateFadeAsync(ulong stemStateHash, float transitionLength);

        /// <summary>
        /// Switches the playing track's stems to the stem state with this name hash at once, ending any fade under way.
        /// Does nothing with no track playing.
        /// </summary>
        void SetStemStateImmediate(ulong stemStateHash);

        /// <summary>The player's volume from the game's settings, 0 to 1, as the mixer has it.</summary>
        float GetVolume();

        /// <summary>
        /// Sets the player's volume from the game's settings, 0 to 1, on the music bus and its reverb and echo sends
        /// alike, so the effects keep their level against the dry sound.
        /// </summary>
        void SetVolume(float percent);

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

        /// <summary>
        /// Loads these songs and their stems' audio data and keeps them until <see cref="Unload" />, so playing one
        /// starts at once rather than waiting for it to load. Optional: a song never preloaded plays all the same.
        /// </summary>
        Task PreloadAsync(IReadOnlyList<ulong> nameHashes);

        /// <summary>
        /// Lets go of songs <see cref="PreloadAsync" /> kept; one still playing stays until it stops.
        /// </summary>
        void Unload(IReadOnlyList<ulong> nameHashes);
    }

    /// <summary>A named moment in a sound, raised through <see cref="ISfxReference.OnEvent" /> as it plays.</summary>
    public interface ISfxEventData
    {
        /// <summary>The name <see cref="ISfxReference.OnEvent" /> passes when the event is raised.</summary>
        string EventName { get; }

        /// <summary>How far into the sound the event is raised, in seconds.</summary>
        float EventTriggerTime { get; }

        /// <summary>How far into the sound the event is raised, in samples, which is what the player checks.</summary>
        internal int EventTriggerTimeInSamples { get; }

        /// <summary>Whether a looping sound raises the event once only, rather than on every pass of the loop.</summary>
        internal bool RemoveEventOnceTriggered { get; }
    }

    /// <summary>A sound effect: its stems, its events, and where it loops.</summary>
    internal interface ISfxAsset : IAudioAsset
    {
        /// <summary>The events authored on it.</summary>
        internal IReadOnlyList<ISfxEventData> Events { get; }

        /// <summary>Whether it loops, between <see cref="LoopStart" /> and <see cref="LoopEnd" />, until stopped.</summary>
        internal bool Loop { get; }

        /// <summary>Where the loop starts, in samples.</summary>
        internal int LoopStart { get; }

        /// <summary>Where the loop ends and jumps back to its start, in samples.</summary>
        internal int LoopEnd { get; }
    }

    /// <summary>
    /// A sound <see cref="ISfxPlayer.PlayAsync" /> started, to pause, resume or stop it, and to hear its events.
    /// </summary>
    public interface ISfxReference
    {
        /// <summary>
        /// The event a sound that does not loop raises as it finishes.
        /// </summary>
        const string SFX_COMPLETE = "SfxComplete";

        /// <summary>Raised with an event's name and this reference as the sound reaches each of its events.</summary>
        event Action<string, ISfxReference> OnEvent;

        /// <summary>
        /// The sound's events, with <see cref="SFX_COMPLETE" /> last for a sound that does not loop.
        /// </summary>
        IReadOnlyList<ISfxEventData> Events { get; }

        /// <summary>The sound being played.</summary>
        internal ISfxAsset Asset { get; }

        /// <summary>The name hash it was asked for by, which the player releases to the provider when it ends.</summary>
        internal ulong NameHash { get; }

        /// <summary>
        /// Once the sound passes its loop's end, moves every stem back by the loop's length and re-arms the events that
        /// repeat.
        /// </summary>
        internal void CheckForLoop();

        /// <summary>
        /// Raises each event the sound has reached; once a sound that does not loop ends, raises any left and then
        /// <see cref="SFX_COMPLETE" />.
        /// </summary>
        internal void CheckForEventToRaise();

        /// <summary>Pauses its stems, holding its events back for as long as it stays paused.</summary>
        internal void Pause();

        /// <summary>Plays its stems on from where they paused.</summary>
        internal void Resume();

        /// <summary>Marks it finished, so it raises nothing more. The player stops its stems.</summary>
        internal void Stop();
    }

    /// <summary>
    /// Where the sfx player gets its sounds: <see cref="SfxAssetProvider" />, listing them in the build, or
    /// <see cref="BundledSfxAssetProvider" />, from bundles. The player calls both members, never a game.
    /// </summary>
    public interface ISfxAssetProvider
    {
        /// <summary>
        /// The sound with this name hash, held until a matching <see cref="Release" />: held twice, it needs releasing
        /// twice. One held already comes back as a finished task. Throws for a name the provider does not have.
        /// </summary>
        Task<SfxAsset> AcquireAsync(ulong nameHash);

        /// <summary>
        /// Lets go of one hold, so a provider that loaded the sound can unload it once nothing holds it.
        /// </summary>
        void Release(ulong nameHash);
    }

    /// <summary>
    /// Plays sounds on the mixer's sfx voices, as many at once as there are voices free, stopping the oldest for more.
    /// </summary>
    public interface ISfxPlayer : IDisposable
    {
        /// <summary>
        /// Plays a sound once it and its audio data have loaded, at once if it was preloaded. One still loading when
        /// <see cref="StopAll" /> or <see cref="PauseAll" /> comes never starts, and its reference comes back stopped.
        /// </summary>
        Task<ISfxReference> PlayAsync(ulong nameHash);

        /// <summary>Pauses one sound where it is, until <see cref="Resume" />.</summary>
        void Pause(ISfxReference sfxReference);

        /// <summary>
        /// Pauses every sound playing. One still loading never starts, and its reference comes back stopped.
        /// </summary>
        void PauseAll();

        /// <summary>Plays a paused sound on from where it paused.</summary>
        void Resume(ISfxReference sfxReference);

        /// <summary>Plays every paused sound on from where it paused.</summary>
        void ResumeAll();

        /// <summary>Stops a sound and lets go of it. One already stopped or finished is ignored.</summary>
        void Stop(ISfxReference sfxReference);

        /// <summary>
        /// Stops every sound playing. One still loading never starts, and its reference comes back stopped.
        /// </summary>
        void StopAll();

        /// <summary>Sets where sounds come from. Set it before any sound is played or preloaded.</summary>
        void SetSfxAssetProvider(ISfxAssetProvider provider);

        /// <summary>
        /// Builds the player: a voice per mixer group, each playing one stem. A sound needing more voices than are free
        /// stops the oldest sounds to make room. The groups share one mixer, whose exposed parameters the player sets.
        /// </summary>
        void SetStemMixerGroups(AudioMixerGroup[] groups);

        /// <summary>The player's volume from the game's settings, 0 to 1, as the mixer has it.</summary>
        float GetVolume();

        /// <summary>
        /// Sets the player's volume from the game's settings, 0 to 1, on the sfx bus and its reverb send alike, so the
        /// reverb keeps its level against the dry sound.
        /// </summary>
        void SetVolume(float percent);

        /// <summary>
        /// Loads these sounds and their audio data and keeps them until <see cref="Unload" />, so playing one starts on
        /// the frame it is asked for. Optional: a sound never preloaded plays two or three frames later.
        /// </summary>
        Task PreloadAsync(IReadOnlyList<ulong> nameHashes);

        /// <summary>
        /// Lets go of sounds <see cref="PreloadAsync" /> kept; one still playing stays until it ends.
        /// </summary>
        void Unload(IReadOnlyList<ulong> nameHashes);
    }

    /// <summary>
    /// Where a bundled provider's songs or sounds come from: each in a bundle of its own, opened and closed.
    /// </summary>
    internal interface IAudioBundleSource
    {
        /// <summary>
        /// Opens the bundle of the song or sound with this name hash and loads the one asset in it, keeping the bundle
        /// open until <see cref="UnloadAsync" />.
        /// </summary>
        Task<TAsset> LoadAsync<TAsset>(ulong nameHash) where TAsset : ScriptableObject, IAudioAsset;

        /// <summary>Closes the bundle <see cref="LoadAsync{TAsset}" /> opened, destroying its asset.</summary>
        Task UnloadAsync(ulong nameHash);
    }

    /// <summary>
    /// Opens a bundle in StreamingAssets: from the file, or by web request on Android and the web, which keep
    /// StreamingAssets where the file system cannot reach.
    /// </summary>
    internal interface IStreamingBundleLoader
    {
        /// <summary>Opens the bundle at <paramref name="path" />, throwing when there is none.</summary>
        Task<AssetBundle> LoadAsync(string path);
    }
}