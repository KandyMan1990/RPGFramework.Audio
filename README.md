# RPGFramework.Audio
Music in stems and sound effects for Unity: made for the RPG Framework, and usable in any Unity project on its own (see [Using it on its own](#using-it-on-its-own)).

Requires Unity 6000.0 or newer. It references Unity, Unity.Mathematics and RPGFramework.Hashing, and nothing else — Hashing is a single static class with no dependencies of its own, so this package can still be dropped into a project that has none of the rest of the framework. It drives its own per frame update from a component on the GameObject each player creates for its audio sources, so there is no update manager to wire up.

Audio is played using Unity's built in audio systems.  The samples contain a mixer asset that has 16 channels reserved for playing music and 16 channels for sound effects, however this is just an example, a mixer could have 2 channels, it could have 200 channels, 16 and 16 just seemed like a reasonable number that would cover the vast majority of use cases.

When it comes to looping, be it music or sfx, there should be trailing sound after the loop point in case Unity's audio system overruns the buffer size when processing, e.g. if Unity processes audio in say 64 byte chunks but your audio clip isn't divisible by 64, it could cause noise/pops/clicks or it could cause Unity to think the audio has finished playing and stop looping.  For music, I've found an additional bar of music generally covers the overflow, and for sfx, at least 1 second past the loop point has stopped any errors occurring.  Neither player sets `AudioSource.loop`; both watch the playhead and seek it back, and that detection only happens once per mixer buffer, so the tail in the audio is what covers the overshoot.

## Assets are addressed by name, not by list position

Every call that asks for a track or a sound takes a `ulong` — the FNV-1a 64 hash of the asset's own name:

```csharp
await m_MusicPlayer.PlayAsync(Fnv1a64.Hash("Overworld"));
await m_SfxPlayer.PlayAsync(Fnv1a64.Hash("Sword_Hit"));
```

Each provider indexes its list by that hash when it is enabled.  These used to be list indices, which meant inserting or reordering an entry silently repointed every caller with nothing to report it — the wrong sound just played.  A name survives reordering, and a rename fails loudly at the call site instead.  Renaming an asset is now the thing that breaks callers.

Music stem states are named and hashed the same way.  A state hash of zero means "the first state the asset lists", which is what `PlayAsync` defaults to for a caller that doesn't care about layering.

Rather than hashing string literals everywhere, both provider inspectors can generate an enum of their contents — see [Editor tooling](#editor-tooling) below.

## Mixer setup

The players expect a particular mixer graph, and it is worth setting up before writing any code:

* Each music stem plays on its own mixer group (`MusicTrack0` … `MusicTrack15`), and those groups are children of a **Music** bus.
* Each stem group sends a percentage of its output to a **MusicReverbSend** bus, through an exposed parameter named `{GroupName}_Send`, e.g. `MusicTrack0_Send`.
* **MusicReverbSend** feeds a **Reverb** bus, which is wet only.  Its effects run Receive, then a Send into the Reverb bus's Receive, then its Attenuation, with that fader left at −80 dB: a group always plays into its parent, so a fader left open would add a second, dry copy of every stem's send to the mix.  The level of its Send is what's exposed as `MusicReverbSend`.
* So Music is the dry path and MusicReverbSend is the wet path — two parallel signals, not a bus and one of its sends.
* Each music stem group also sends to a **MusicEcho** bus, through `{GroupName}_EchoSend`.  MusicEcho holds a Receive, Unity's **Echo** with its Drymix at 0 and Wetmix at 100%, its Attenuation, and a Send on into the Reverb bus's Receive: it carries the echoes alone, and they reach the reverb too.  The Echo's Delay and Decay are exposed as `EchoDelay` and `EchoDecay`, and the bus's volume as `MusicEcho`.
* Sfx is arranged like the reverb half, with `Sfx`, `SfxReverbSend` and `SfxTrack{N}_Send`; sound effects don't echo.

`Music`, `MusicReverbSend`, `MusicEcho`, `EchoDelay`, `EchoDecay`, `Sfx`, `SfxReverbSend`, every `{GroupName}_Send` and every `MusicTrack{N}_EchoSend` must be exposed on the mixer, and with PSX Reverb installed, `ReverbPreset` and `ReverbDepth` too (see [PSX reverb](#psx-reverb)).  A volume bus or reverb parameter that isn't exposed logs an error naming it when the player reads or writes it.  A `{GroupName}_Send` or `_EchoSend` that isn't exposed is skipped silently, so that stem simply sends nothing to the reverb or the echo.

`SetVolume` writes the same dB to the dry bus, the reverb send bus and the echo bus, which keeps the dry/wet ratio constant as volume changes.  Attenuating only the dry bus would leave the reverb ringing on its own channel.

Volume is a 0-1 percentage rather than dB, mapped with a perceptual curve so that a slider at half way sounds half as loud.  Zero maps to -80 dB.

## Setting up a player

Both players are plain C# classes.  Every member is an explicit interface implementation, so hold the interface, not the concrete type:

```csharp
IMusicPlayer musicPlayer = new UnityMusicPlayer();
musicPlayer.SetMusicAssetProvider(m_MusicAssetProvider);
musicPlayer.SetStemMixerGroups(m_MusicMixerGroups);

ISfxPlayer sfxPlayer = new UnitySfxPlayer();
sfxPlayer.SetSfxAssetProvider(m_SfxAssetProvider);
sfxPlayer.SetStemMixerGroups(m_SfxMixerGroups);
```

`SetStemMixerGroups` is what builds the player: it creates a `MusicPlayer` or `SfxPlayer` GameObject marked `DontDestroyOnLoad`, one child AudioSource per mixer group named after that group, and the component that ticks the player each frame.  Nothing works before it is called, and calling it again rebuilds everything.

`IMusicPlayer` and `ISfxPlayer` are both `IDisposable`: `musicPlayer.Dispose()` stops everything playing and destroys the player GameObject.

The package does not guard against being used before it is configured — there are no null-provider or mixer-group checks.  Those always pass once it is wired up correctly, and a mistake fails on the first frame of the first run.

## Where the audio comes from

The players ask a **provider** for each song and sound, and a game chooses one of two when it sets the players up.  Everything else — the players, the mixer, the inspector preview, the generated enums — is the same either way.

| | In the build | From bundles |
| --- | --- | --- |
| Provider | The `MusicAssetProvider` and `SfxAssetProvider` assets, listing every song and sound | `new BundledMusicAssetProvider()` and `new BundledSfxAssetProvider()`, made in code |
| Where the audio lives | Inside the player | A bundle per song and per sound in `StreamingAssets/Audio/Music` and `StreamingAssets/Audio/Sfx`, each named by its name hash |
| Build step | None | **Build bundles** on each provider asset, again whenever its songs or sounds change |
| Playing something new | Load its audio data | Open its bundle, load it, load its audio data: two or three frames more |
| When it stops | Unload its audio data | Unload its audio data, and its bundle once nothing holds it |
| Suits | Samples, prototypes, a game with no plans for content packs | A game that wants content packs, or its audio out of memory between scenes |

**From bundles**, the provider assets are still where songs and sounds are listed and named, and where Build bundles is pressed — but nothing the game builds may reference them, or Unity puts every song in the player as well.  Make the bundled providers in code instead:

```csharp
musicPlayer.SetMusicAssetProvider(new BundledMusicAssetProvider());
sfxPlayer.SetSfxAssetProvider(new BundledSfxAssetProvider());
```

**Build bundles** builds a bundle per song or sound the provider lists, for the editor's active platform, with LZ4 compression; it rebuilds only what changed, and removes the bundles of songs and sounds no longer listed.  Play mode reads the bundles too, so build them again after changing a song or sound.  A clip two songs or sounds share is copied into each bundle, so the build warns naming it.  On Android and the web, where StreamingAssets can't be read from the file system, the bundles are fetched by web request.

**Preloading** is optional, on both players and with either provider:

```csharp
await sfxPlayer.PreloadAsync(new[] { Fnv1a64.Hash("Menu_Move"), Fnv1a64.Hash("Menu_Confirm") });
// ...a play of either starts on the frame it's asked for...
sfxPlayer.Unload(new[] { Fnv1a64.Hash("Menu_Move"), Fnv1a64.Hash("Menu_Confirm") });
```

It loads each song or sound and its audio data, and keeps them until `Unload`, so a play starts at once.  Anything not preloaded plays all the same, a little later.  A song or sound still playing when it's unloaded stays until it stops.

A game can write a provider of its own — Addressables, a server, an archive — by implementing `IMusicAssetProvider` or `ISfxAssetProvider`: `AcquireAsync` hands back a song or sound, held until the player's matching `Release`.

## Music

The 16 channels give the option of playing music via stems instead of a bounced track, where each stem can send to a realtime reverb bus for live processing of reverb with varying send amounts per stem.

Every song sounding at once shares those channels, one per stem, so a song can play on any of them — set every music group up the same way.  A track with more stems than there are free channels throws: one wider than the mixer, or a crossfade whose two tracks together are (see [Crossfades](#crossfades)).  That is deliberate: music channels are a fixed part of setting the player up, so too few of them is a setup mistake that should fail immediately rather than quietly play the track with its top layers missing.  Sfx behaves differently — see below.

### Stem states

Music can also have stems fade in and out should something happen in game where a transition would be preferred instead of starting a different track.

Which stems are audible is authored on the asset as a list of named **stem states** — a name plus one tick per stem, in track order.  A caller asks for a state by name, so what "combat" sounds like is decided on the asset rather than by whatever is asking:

```csharp
await musicPlayer.SetStemStateFadeAsync(Fnv1a64.Hash("Combat"), 2f);
musicPlayer.SetStemStateImmediate(Fnv1a64.Hash("Exploration"));
```

A zero fade length routes to the immediate path, so one call covers both, and with no track playing both do nothing.  `PlayAsync` takes an optional state hash too, so a track can start already layered, along with an optional fade in time.

Two things happen automatically when an asset loads, in builds as well as in the editor:

* An asset that declares no state gets one named `Default` with every stem audible, so an asset can be played without a state having been authored first.
* States resize with the track list, and a stem added later arrives audible in every state.  Adding a stem means wanting to hear it; untick it in the states that shouldn't have it.

Two states sharing a name makes one of them unreachable, so the asset warns about duplicate names while authoring.

### Crossfades

`CrossfadeAsync` fades the playing track out and another in over the same number of seconds, both in a straight line:

```csharp
await musicPlayer.CrossfadeAsync(Fnv1a64.Hash("World Map"), Fnv1a64.Hash("Exploration"), 2f);
```

* **The two tracks share the music channels while both sound**, each stem on a free one, so nothing extra is needed in the mixer.  If the two together have more stems than there are channels, it throws, naming both tracks, the channels they need and the channels there are — sixteen channels cannot crossfade a ten-stem track into an eight-stem one.  Add music groups to the mixer and to `SetStemMixerGroups`.
* **At most two tracks sound.**  A crossfade asked for while another is still fading a track out cuts that track.
* The track fading out plays on at full volume until the new one's clips have loaded, so nothing dips while they load.
* Crossfading to the track already playing does nothing, as `PlayAsync` does.  With nothing playing, it is a fade in.
* Stem states apply to the track fading in.  Its reverb settings apply when it starts, and a different preset cuts the reverb's tail under the track fading out.
* `StopAsync` fades out both.
* An optional last argument is the volume it fades in to, 0 to 1 — see [Song volume](#song-volume).

### Song volume

Each track has a volume of its own, 0 to 1, for the game to set — quiet music in a quiet room — apart from `SetVolume`, which is the player's from the game's settings.  `PlayAsync` and `CrossfadeAsync` take one, defaulting to full, and `SetSongVolumeAsync` changes the playing track's, at once or over some seconds:

```csharp
await musicPlayer.SetSongVolumeAsync(0.25f, 2f);
```

It is a straight gain on the track's stems, and fades go to it rather than to full.  With nothing playing it does nothing, so a game that wants a level kept from one track to the next keeps it and passes it to the next `PlayAsync`, as the field does.

### Looping

Music is looped by bar: give the asset its tempo and the start and end bar to loop. The tempo is a list of **sections**, each a start bar with its BPM and time signature.  The first starts at bar 1 and each runs until the next one starts, so a song that changes tempo or time signature part way through still loops on its bars; most songs need just the one.  The inspector heads each section with what it says, such as *From bar 9: 90 BPM, 6/8*.  The time signature is beats per bar plus the note that gets the beat, so compound signatures such as 6/8 or 12/8 give the correct bar length rather than being approximated in 4/4. BPM is read as quarter notes per minute, which is what a DAW reports, so a 6/8 bar at 120 BPM is 1.5 seconds.

A bar's time adds up every section before it, then its place in its own, so changing a section moves where every later bar lands in the audio.  The first bar is bar 1, the end bar must come after the start bar, and each section must start after the one before it.  An asset marked to loop whose sections or bar range can't produce a loop logs a warning naming the asset and the problem, and plays through without looping.

A track that doesn't loop costs nothing per frame — the update component is only enabled while there is a loop point to watch, or an echo following the song's tempo (below).  `IsPlaying` says whether a track is playing: from when it is asked for, while its clips load included, until it is stopped, paused, or reaches its end without looping.

### Pause and resume

`Pause` stops the music and returns a `MusicSnapshot` of what was playing — the track, where it was, and which stems it had — for you to keep.  `ResumeAsync` plays it again from there, so music can be put aside for a battle and its victory music, and picked up afterwards:

```csharp
MusicSnapshot overworld = musicPlayer.Pause();

await musicPlayer.PlayAsync(Fnv1a64.Hash("Battle"));

// ...the battle, then the victory music...

await musicPlayer.ResumeAsync(overworld, 1f);
```

* `ResumeAsync` fades whatever is playing out under the resumed track over the seconds given, as `CrossfadeAsync` does; zero cuts.  It takes a volume, as `PlayAsync` does, rather than restoring the one the track was paused at.
* Pausing with nothing playing returns an empty snapshot, and resuming an empty snapshot fades out whatever is playing, since nothing was when it was taken.
* The player keeps no snapshot itself, so one can be kept as long as it is needed, and more than one at once.  `PlayAsync` of a paused track starts it from the beginning.
* Pausing during a crossfade keeps the track fading in and stops both.
* A paused track's clips are unloaded, and resuming loads them again.

### Echo

A song can echo some of its stems, on Unity's Echo effect on a bus of its own (see [Mixer setup](#mixer-setup)).  Each stem has an **Echo Send Level** beside its reverb send, for how much of it goes in, and the song's **Echo** settings say how far apart the repeats are and how quickly they fade:

* **Timing** — *None* leaves the echo as the last song set it; a song whose stems send nothing to it needs nothing here.  *Milliseconds* sets the delay once, when the song starts.  *Note Length* takes it from the song's tempo, as a note value played straight, dotted or as a triplet, and follows it into each tempo section: a dotted eighth is 432.7 ms at 104 BPM.
* **Decay** — each repeat as a share of the one before: 0.5 halves it, and 1 never fades.

The delay is held between 10 and 5000 ms, the effect's own range.  The repeats go on into the reverb, so they sit in the song's room, and like the reverb's tail they die away naturally when the music is paused.  In a crossfade, the song coming in sets the echo.  The inspector's preview skips the mixer, so the echo is heard only in play mode.

### PSX reverb

With [PSX Reverb](https://github.com/KandyMan1990/RPGFramework.PSXReverb) (`com.rpgframework.psxreverb`) installed on the Reverb bus, a music asset gains a **Reverb** group: an optional preset and an optional volume, each applied when the song starts, including when a paused song resumes.  A setting left unticked leaves the reverb as the last song or script set it.  The volume can be set with its 0 to 1 slider or as PSX Reverb's depth, 0 to 127, and each row has a **Default** button that puts back PSX Reverb's own: studio C, and depth 40.

The volume is 0 to 1, and goes to PSX Reverb's Depth, 0 to 127, in a straight line.  It is how loud the reverb plays for every sound while the song is on, sound effects included, where a stem's send level is how much of that one stem goes in.  A different preset clears the reverb, cutting its tail.

`SetReverbPreset` and `SetReverbVolume` set either directly, and hold until the next song that names that setting, or the next call:

```csharp
musicPlayer.SetReverbPreset(ReverbPreset.Hall);
musicPlayer.SetReverbVolume(0.5f);
```

The player writes PSX Reverb's Preset and Depth through parameters exposed as `ReverbPreset` and `ReverbDepth`: right-click each slider on the effect, choose to expose it to script, and rename it.

Without the package, the Reverb group is hidden, the two calls do nothing and nothing is written to the mixer.  A song's settings are still kept on the asset, so removing the package and adding it back loses nothing.

### Import settings

Ideally, music stems should be imported with the following settings:
* Load In Background checked
* Load Type: Compressed in Memory
* Preload Audio Data unchecked
* Compression Format: Vorbis
* Quality: 60-70 (the shipped preset uses 70)
* Sample Rate Setting: Preserve Sample Rate

An existing preset exists to copy/paste into the folder where music is stored to automatically apply these settings when music is imported

The player loads a stem's sample data before scheduling it and releases it when the track stops, unless a track still sounding, or one preloaded, uses the same clip, and only for clips that were imported with Preload Audio Data off.  A stem imported without its preset gets Unity's default of preload on, and the player then leaves it alone entirely — it stays resident rather than silently failing to reload.

## Sfx

Sound effects behave similarly to music, however since a sfx won't have a tempo/bpm, they can be looped by specifying start/end time in audio samples

`PlayAsync` returns an `ISfxReference` for that one playing sound once it has loaded and started, which is what `Pause`, `Resume` and `Stop` take; a preloaded sound starts on the frame it's asked for.  `PauseAll`, `ResumeAll` and `StopAll` act on everything currently playing, and a sound still loading when `PauseAll` or `StopAll` comes never starts: its reference comes back already stopped, so there's no null to check for.

### Voices

Sfx voices are a pool, and running out of them is a normal runtime condition rather than an error.  A sound with more stems than there are voices plays as many as it can, and a sound arriving when every voice is busy evicts the oldest playing sound.  This is the opposite of how music handles a shortage of channels, on purpose: a missing music layer is far more noticeable than a missing sfx voice.

Sample data is loaded when a sound is about to play and released when nothing is playing it, so what stays resident is what is currently audible.  As with music, that only applies to clips imported with Preload Audio Data off.

### Events

Events can also be specified in the sfx asset, with a name and time to raise the event.
For example, if you have an enemy death sfx and you want to tie animation starting to a sudden change in the sfx like an explosion, raise an event at that point in the sfx asset and listen for it in code

```csharp
ISfxReference sfxReference = await sfxPlayer.PlayAsync(Fnv1a64.Hash("Enemy_Death"));
sfxReference.OnEvent += (eventName, source) => Debug.Log($"{eventName} fired");
```

Event times are authored in samples, the same as loop points.  `ISfxReference.Events` lists what that sound will raise with each trigger time converted to seconds, which is usually more useful for lining animation up against.

A few behaviours worth knowing:

* On a looping sound, events re-arm each time it loops, unless **Remove Event Once Triggered** is ticked on that event.
* A sound that doesn't loop raises a final `ISfxReference.SFX_COMPLETE` event when it finishes, and that event appears in the `Events` list.  Looping sounds never complete, so they get neither.
* If a non-looping sound finishes with events that never fired, those are raised before the complete event, so nothing listening is left waiting.

### Import settings

Ideally, sfx stems should be imported with the following settings:
* Load In Background checked
* Load Type: Decompress on Load
* Preload Audio Data unchecked
* Compression Format: ADPCM
* Sample Rate Setting: Preserve Sample Rate

An existing preset exists to copy/paste into the folder where sfx are stored to automatically apply these settings when sfx are imported

ADPCM is roughly 3.5:1 against PCM, trivially cheap to decode, and the format the PSX SPU itself used.  With decompress on load the saving is in build size rather than memory, since the clip is decompressed when it loads either way; compressed in memory with ADPCM is the lower-RAM alternative if a module's resident set is still too large.

Preload audio data must be off for the player to manage residency at all, as described under [Voices](#voices) above, whichever provider the game uses.  Load In Background is on so a clip decompresses off the main thread: the player waits for its audio data before starting it, a frame or two at most, and a preloaded sound has it already.

## Editor tooling

### Import preset applier

An asset postprocessor ships with the package that applies an import preset to audio automatically.  When any audio file is imported, it looks for a preset in that file's folder and then in each folder above it, stopping short of the Assets root, and applies the first one it finds.  So dropping `MusicAudioImporter.preset` beside your music and `SfxAudioImporter.preset` beside your sfx is all the setup there is.  Keep one preset per folder, as the first one found wins.

This runs for every audio import in the project, not just audio belonging to the framework.

### Generating names for code

Hand-written string literals are easy to get wrong, so both provider inspectors can generate code for what they hold.  Each button opens a small window asking for an output path, a file name and a namespace, and remembers those for next time.

* **Music Asset Provider** — *Generate enum for Music Asset Provider* writes a `ulong` enum with one member per asset, each valued at that asset's name hash.  Member names are sanitised to Pascal case, so each one carries the real asset name in a doc comment.
* **SFX Asset Provider** — *Generate enum for Sfx Asset Provider* does the same for sounds.
* **SFX Asset Provider** — *Generate single class for all Sfx event data* writes one static class holding every event name across every sfx asset, plus a matching enum.
* **SFX Asset Provider** — *Generate class per Sfx for its Sfx event data* writes the same thing per asset instead, which keeps event names scoped to the sound they belong to.

Generated files are overwritten on each run and carry a "do not modify" header.

### Building bundles

Both provider inspectors have **Build bundles**, which builds a bundle per song or sound the provider lists into `StreamingAssets/Audio/Music` or `StreamingAssets/Audio/Sfx`, for the bundled providers to read — see [Where the audio comes from](#where-the-audio-comes-from).  A game playing from the build never needs it.

### Previewing in the inspector

A music asset and an sfx asset can each be played from their inspector, below the usual fields, without entering play mode.

* **Music** — *Play*, *Stop*, and *Play into the loop*, which starts four seconds before the loop's end to hear the jump back to its start without waiting for it.  A stem state dropdown and a toggle per stem switch what is audible while it plays, and the playhead shows the time and the bar, counted from 1 through the tempo sections as the loop's bars are.
* **Sfx** — *Play* and *Stop*, and the asset's events listed with their times, each lit once the playhead passes it.  As the player does, a looping sound re-arms its events each time round unless they fire once, and a sound that doesn't loop raises any it never reached, then `SfxComplete`, as it ends.

The preview plays the stems together and loops them as the players do, but on its own, not through the mixer, so a song's reverb sends and PSX Reverb aren't heard, and it plays at the clips' own volume.  Changing the asset, or selecting something else, stops it.

## Using it on its own

Nothing here needs the rest of the RPG Framework: the package depends only on RPGFramework.Hashing, and the players are plain C# objects your game creates and keeps.

**Make one music player and one sfx player for the game's life**, before anything plays.  A small first scene that builds them and then loads the next is the simplest way to be sure of that:

```csharp
using RPGFramework.Audio;
using RPGFramework.Audio.Music;
using RPGFramework.Audio.Sfx;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public sealed class Boot : MonoBehaviour
{
    [SerializeField] private MusicAssetProvider m_MusicProvider;
    [SerializeField] private SfxAssetProvider   m_SfxProvider;
    [SerializeField] private AudioMixerGroup[]  m_MusicGroups;
    [SerializeField] private AudioMixerGroup[]  m_SfxGroups;

    public static IMusicPlayer Music { get; private set; }
    public static ISfxPlayer   Sfx   { get; private set; }

    private void Awake()
    {
        Music = new UnityMusicPlayer();
        Music.SetMusicAssetProvider(m_MusicProvider);
        Music.SetStemMixerGroups(m_MusicGroups);

        Sfx = new UnitySfxPlayer();
        Sfx.SetSfxAssetProvider(m_SfxProvider);
        Sfx.SetStemMixerGroups(m_SfxGroups);

        SceneManager.LoadScene("Title");
    }
}
```

The groups are the mixer's `MusicTrack` and `SfxTrack` groups, in order.  To play from bundles instead, drop the two provider fields and make `new BundledMusicAssetProvider()` and `new BundledSfxAssetProvider()` (see [Where the audio comes from](#where-the-audio-comes-from)).  Hold the players however your game holds its services: a static as here, an object that is never destroyed, or your own dependency injection.  Each makes its own GameObject that survives scene loads, so nothing else has to.

Some suggestions for fitting it in:

- **Start from a sample's mixer.**  The Music Sample's has every group, send and exposed parameter described under [Mixer setup](#mixer-setup); copy it, and add or remove `MusicTrack` and `SfxTrack` groups to suit.
- **Keep the player's volume choices** wherever your game keeps its settings, and pass them to `SetVolume` at start-up and whenever they change.  `SetVolume` is the player's own level; a scene that wants its music quieter uses the song's volume instead (see [Song volume](#song-volume)), so the two never fight.
- **Name songs and sounds in code with a generated enum** (see [Generating names for code](#generating-names-for-code)), and **keep them as data** — in a save, or on a ScriptableObject — as their name hash, eight bytes for any name.
- **Put music aside rather than stopping it** for something short, a battle or a cutscene: `Pause` hands back a snapshot, and `ResumeAsync` carries on from it (see [Pause and resume](#pause-and-resume)).
- **Preload what a screen plays the moment it's asked**, such as its menu sounds, as it opens, and unload them as it closes, so they start on the frame they're asked for.
- **Drive animation from a sound** with its events rather than timers, so the two can't drift (see [Events](#events)).
- **Dispose the players** when your game shuts down.

## In the RPG Framework

- **The game's global installer builds both players** and binds them for every module, as the test project's does:

  ```csharp
  ISfxPlayer sfxPlayer = new UnitySfxPlayer();
  sfxPlayer.SetSfxAssetProvider(m_SfxProvider);
  sfxPlayer.SetStemMixerGroups(m_SfxMixerGroups);
  container.BindSingletonFromInstance(sfxPlayer);

  IMusicPlayer musicPlayer = new UnityMusicPlayer();
  musicPlayer.SetMusicAssetProvider(m_MusicProvider);
  musicPlayer.SetStemMixerGroups(m_MusicMixerGroups);
  container.BindSingletonFromInstance(musicPlayer);
  ```

- **The game's installer chooses the provider**, in the build or from bundles; the test project's has an **Audio From Bundles** tick.  The modules don't preload anything yet, so with bundles a song or sound opens when it's first played.
- **Field scripts play the music and sounds**: `PLAY_MUSIC`, `CROSSFADE_MUSIC`, `STOP_MUSIC`, `MUSIC_STEM_STATE`, `IS_MUSIC_PLAYING` and `PLAY_SOUND`; the song's volume with `SET_MUSIC_VOLUME`, `FADE_MUSIC_VOLUME`, `FADE_MUSIC_VOLUME_FROM` and `WAIT_FOR_MUSIC_VOLUME`, kept in Field's `MusicVolume` variable so it carries across fields and saves; and with PSX Reverb, `SET_REVERB_PRESET` and `SET_REVERB_VOLUME`.  The block editor offers each provider's songs, sounds and stem states by name.
- **The menus set the player's volume**: the Config menu as the player changes it, and the title screen from the saved settings at start-up.
- **Battle** stops the field's music and plays its victory music.
- **Menus and dialogue make their sounds through Core's audio intents** — confirm, cancel, navigate — which the game maps to its own sound effects (the test project's `GameAudioIntentPlayer`), so Core never names a sound and Audio never hears of Core.

## Not in this version

* **Content packs can't add bundles yet.**  A bundled provider reads the game's own folders; reading a content pack's beside them comes with the framework's content packs.
* **A tempo that changes gradually**, such as a ritardando, fits no list of sections, so a loop after one can't be given in bars.
* **The players run in play mode only**, through a mixer set up as described above; the inspector preview is the way to hear an asset outside it.

## Samples

Two samples ship with the package and both include the mixer asset described above, already wired up with its exposed parameters.

* **Music Sample** — a four stem track with per stem reverb sends (its mixer has the echo bus too, though the sample's stems send nothing to it), bar based looping, and three stem states to transition between.  The buttons enable and disable each other to keep the sample's own flow readable; the player takes any call at any time — playing the track already playing does nothing, and playing another cuts it.
* **Sfx Sample** — a looping sound with events, a one shot, and a looping ambience, with looping set by start/end values measured in audio samples.
