# RPGFramework.Audio
Audio functionality for the RPG Framework

Requires Unity 6000.0 or newer. It references Unity, Unity.Mathematics and RPGFramework.Hashing, and nothing else — Hashing is a single static class with no dependencies of its own, so this package can still be dropped into a project that has none of the rest of the framework. It drives its own per frame update from a component on the GameObject each player creates for its audio sources, so there is no update manager to wire up.

Audio is played using Unity's built in audio systems.  The samples contain a mixer asset that has 16 channels reserved for playing music and 16 channels for sound effects, however this is just an example, a mixer could have 2 channels, it could have 200 channels, 16 and 16 just seemed like a reasonable number that would cover the vast majority of use cases.

When it comes to looping, be it music or sfx, there should be trailing sound after the loop point in case Unity's audio system overruns the buffer size when processing, e.g. if Unity processes audio in say 64 byte chunks but your audio clip isn't divisible by 64, it could cause noise/pops/clicks or it could cause Unity to think the audio has finished playing and stop looping.  For music, I've found an additional bar of music generally covers the overflow, and for sfx, at least 1 second past the loop point has stopped any errors occurring.  Neither player sets `AudioSource.loop`; both watch the playhead and seek it back, and that detection only happens once per mixer buffer, so the tail in the audio is what covers the overshoot.

## Assets are addressed by name, not by list position

Every call that asks for a track or a sound takes a `ulong` — the FNV-1a 64 hash of the asset's own name:

```csharp
m_MusicPlayer.PlayAsync(Fnv1a64.Hash("Overworld")).FireAndForget();
m_SfxPlayer.Play(Fnv1a64.Hash("Sword_Hit"));
```

Each provider indexes its list by that hash when it is enabled.  These used to be list indices, which meant inserting or reordering an entry silently repointed every caller with nothing to report it — the wrong sound just played.  A name survives reordering, and a rename fails loudly at the call site instead.  Renaming an asset is now the thing that breaks callers.

Music stem states are named and hashed the same way.  `MusicAsset.NO_STATE_NAMED` (zero) means "the first state the asset lists", which is what `PlayAsync` defaults to for a caller that doesn't care about layering.

Rather than hashing string literals everywhere, both provider inspectors can generate an enum of their contents — see [Editor tooling](#editor-tooling) below.

## Mixer setup

The players expect a particular mixer graph, and it is worth setting up before writing any code:

* Each music stem plays on its own mixer group (`MusicTrack0` … `MusicTrack15`), and those groups are children of a **Music** bus.
* Each stem group sends a percentage of its output to a **MusicReverbSend** bus, through an exposed parameter named `{GroupName}_Send`, e.g. `MusicTrack0_Send`.
* **MusicReverbSend** feeds a **Reverb** bus, which is wet only.
* So Music is the dry path and MusicReverbSend is the wet path — two parallel signals, not a bus and one of its sends.
* Sfx is arranged identically, with `Sfx`, `SfxReverbSend` and `SfxTrack{N}_Send`.

`Music`, `MusicReverbSend`, `Sfx`, `SfxReverbSend` and every `{GroupName}_Send` must be exposed on the mixer.  Anything that isn't exposed logs an error naming the parameter when the player tries to read or write it.

`SetVolume` writes the same dB to both the dry bus and the reverb send bus, which keeps the dry/wet ratio constant as volume changes.  Attenuating only the dry bus would leave the reverb ringing on its own channel.

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

Both players implement `IDisposable` explicitly, so disposing means casting: `((IDisposable)musicPlayer).Dispose()`.  That stops everything playing and destroys the player GameObject.

The package does not guard against being used before it is configured — there are no null-provider or mixer-group checks.  Those always pass once it is wired up correctly, and a mistake fails on the first frame of the first run.

## Music

The 16 channels give the option of playing music via stems instead of a bounced track, where each stem can send to a realtime reverb bus for live processing of reverb with varying send amounts per stem.

A track with more stems than there are music mixer groups throws.  That is deliberate: music channels are a fixed part of setting the player up, so too few of them is a setup mistake that should fail immediately rather than quietly play the track with its top layers missing.  Sfx behaves differently — see below.

### Stem states

Music can also have stems fade in and out should something happen in game where a transition would be preferred instead of starting a different track.

Which stems are audible is authored on the asset as a list of named **stem states** — a name plus one tick per stem, in track order.  A caller asks for a state by name, so what "combat" sounds like is decided on the asset rather than by whatever is asking:

```csharp
await musicPlayer.SetStemStateFadeAsync(Fnv1a64.Hash("Combat"), 2f);
musicPlayer.SetStemStateImmediate(Fnv1a64.Hash("Exploration"));
```

A zero fade length routes to the immediate path, so one call covers both.  `PlayAsync` takes an optional state hash too, so a track can start already layered, along with an optional fade in time.

Two things happen automatically when an asset loads, in builds as well as in the editor:

* An asset that declares no state gets one named `Default` with every stem audible, so an asset can be played without a state having been authored first.
* States resize with the track list, and a stem added later arrives audible in every state.  Adding a stem means wanting to hear it; untick it in the states that shouldn't have it.

Two states sharing a name makes one of them unreachable, so the asset warns about duplicate names while authoring.

### Looping

Music can be looped by specifying the tempo, time signature, and the start/end bar to loop. The time signature is beats per bar plus the note that gets the beat, so compound signatures such as 6/8 or 12/8 give the correct bar length rather than being approximated in 4/4. BPM is read as quarter notes per minute, which is what a DAW reports, so a 6/8 bar at 120 BPM is 1.5 seconds.

Loop points are authored as bars, so changing the time signature of an existing asset moves where those bars land in the audio.  The first bar is bar 1, and the end bar must come after the start bar.  An asset marked to loop with a BPM, beats per bar, or bar range that can't produce a loop logs a warning naming the asset and plays through without looping.

A track that doesn't loop costs nothing per frame — the update component is only enabled while there is a loop point to watch.

### Pause and resume

Pausing music does not prevent a different track from playing. For example, pause music "Overworld", play music "Battle", then when wanting to return to "Overworld", just call `StopAsync()` then `PlayAsync(Fnv1a64.Hash("Overworld"))` and it will resume from where it was paused.
If you want a previously paused music to start from scratch, you can call `ClearPausedMusic()` before calling play and it will ensure the track starts from the beginning.

Only one paused position is remembered, so pausing a second track replaces the first.  Playing something else does not discard it — the paused track stays waiting until it is played again or cleared.

### Import settings

Ideally, music stems should be imported with the following settings:
* Load In Background checked
* Load Type: Compressed in Memory
* Preload Audio Data unchecked
* Compression Format: Vorbis
* Quality: 60-70 (the shipped preset uses 70)
* Sample Rate Setting: Preserve Sample Rate

An existing preset exists to copy/paste into the folder where music is stored to automatically apply these settings when music is imported

The player loads a stem's sample data before scheduling it and releases it when the track stops, but only for clips that were imported with Preload Audio Data off.  A stem imported without its preset gets Unity's default of preload on, and the player then leaves it alone entirely — it stays resident rather than silently failing to reload.

## Sfx

Sound effects behave similarly to music, however since a sfx won't have a tempo/bpm, they can be looped by specifying start/end time in audio samples

`Play` returns an `ISfxReference` for that one playing sound, which is what `Pause`, `Resume` and `Stop` take.  `PauseAll`, `ResumeAll` and `StopAll` act on everything currently playing.

### Voices

Sfx voices are a pool, and running out of them is a normal runtime condition rather than an error.  A sound with more stems than there are voices plays as many as it can, and a sound arriving when every voice is busy evicts the oldest playing sound.  This is the opposite of how music handles a shortage of channels, on purpose: a missing music layer is far more noticeable than a missing sfx voice.

Sample data is loaded when a sound is about to play and released when nothing is playing it, so what stays resident is what is currently audible.  As with music, that only applies to clips imported with Preload Audio Data off.

### Events

Events can also be specified in the sfx asset, with a name and time to raise the event.
For example, if you have an enemy death sfx and you want to tie animation starting to a sudden change in the sfx like an explosion, raise an event at that point in the sfx asset and listen for it in code

```csharp
ISfxReference sfxReference = sfxPlayer.Play(Fnv1a64.Hash("Enemy_Death"));
sfxReference.OnEvent += (eventName, source) => Debug.Log($"{eventName} fired");
```

Event times are authored in samples, the same as loop points.  `ISfxReference.Events` lists what that sound will raise with each trigger time converted to seconds, which is usually more useful for lining animation up against.

A few behaviours worth knowing:

* On a looping sound, events re-arm each time it loops, unless **Remove Event Once Triggered** is ticked on that event.
* A sound that doesn't loop raises a final `SfxReference.SFX_COMPLETE` event when it finishes, and that event appears in the `Events` list.  Looping sounds never complete, so they get neither.
* If a non-looping sound finishes with events that never fired, those are raised before the complete event, so nothing listening is left waiting.

### Import settings

Ideally, sfx stems should be imported with the following settings:
* Load In Background unchecked
* Load Type: Decompress on Load
* Preload Audio Data unchecked
* Compression Format: ADPCM
* Sample Rate Setting: Preserve Sample Rate

An existing preset exists to copy/paste into the folder where sfx are stored to automatically apply these settings when sfx are imported

ADPCM is roughly 3.5:1 against PCM, trivially cheap to decode, and the format the PS1 SPU itself used.  With decompress on load the saving is in build size rather than memory, since the clip is decompressed when it loads either way; compressed in memory with ADPCM is the lower-RAM alternative if a module's resident set is still too large.

Preload audio data must be off for the player to manage residency at all, as described under [Voices](#voices) above.  Load In Background stays off deliberately: the player loads a clip immediately before scheduling it, and that load only blocks until the data is ready while this is unchecked.  Turning it on would let a sound start before its samples had arrived.

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

## Samples

Two samples ship with the package and both include the mixer asset described above, already wired up with its exposed parameters.

* **Music Sample** — a four stem track with per stem reverb sends, bar based looping, and three stem states to transition between.  The buttons enable and disable each other to show the order the system expects; that sequencing is the sample's, not the player's, as calling play while something is already playing can give strange results.
* **Sfx Sample** — a looping sound with events, a one shot, and a looping ambience, with looping set by start/end values measured in audio samples.
