# RPGFramework.Audio
Audio functionality for the RPG Framework

Audio is played using Unity's built in audio systems.  The samples contain a mixer asset that has 16 channels reserved for playing music and 16 channels for sound effects, however this is just an example, a mixer could have 2 channels, it could have 200 channels, 16 and 16 just seemed like a reasonable number that would cover the vast majority of use cases.

## Music

The 16 channels give the option of playing music via stems instead of a bounced track, where each stem can send to a realtime reverb bus for live processing of reverb with varying send amounts per stem.

Music can also have stems fade in and out should something happen in game where a transition would be preferred instead of starting a different track.

Music can be looped by specifying the tempo, beats per bar, and the start/end bar to loop

Pausing music does not prevent a different track from playing. For example, pause music ID 1, play music ID 2, then when wanting to return to music A, just call Stop() then Play(1) and it will resume from where it was paused.
If you want a previously paused music to start from scratch, you can call ClearPausedMusic() before calling Play and it will ensure the track starts from the beginning.

Ideally, music stems should be imported with the following settings:
Load In Background checked
Load Type: Compressed in Memory
Preload Audio Data unchecked
Compression Format: Vorbis
Quality: 60-70
Sample Rate Setting: Preserve Sample Rate

An existing preset exists to copy/paste into the folder where music is stored to automatically apply these settings when music is imported

## Sfx

Sound effects behave similarly to music, however since a sfx won't have a tempo/bpm, they can be looped by specifying start/end time in audio samples

Events can also be specified in the sfx asset, with a name and time to raise the event.
For example, if you have an enemy death sfx and you want to tie animationn starting to a sudden change in the sfx like an explosion, raise an event at that point in the sfx asset and listen for it in code

Ideally, sfx stems should be imported with the following settings:
Load In Background unchecked
Load Type: Decompress on Load
Preload Audio Data checked
Compression Format: PCM
Sample Rate Setting: Preserve Sample Rate

An existing preset exists to copy/paste into the folder where sfx are stored to automatically apply these settings when sfx are imported