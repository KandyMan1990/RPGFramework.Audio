# RPGFramework.Audio
Audio functionality for the RPG Framework

Audio is played using Unity's built in audio systems.  16 channels are reserved for playing music, 16 channels for sound effects.

## Music

The 16 channels give the option of playing music via stems instead of a bounced track, where each stem can send to a realtime reverb bus for live processing of reverb with varying send amounts per stem.

Music can also have stems fade in and out should something happen in game where a transition would be preferred instead of starting a different track.

Music can be looped by specifying the tempo, beats per bar, and the start/end bar to loop

## Sfx

Sound effects behave similarly to music, however since a sfx won't have a tempo/bpm, they can be looped by specifying start/end time in audio samples

Events can also be specified in the sfx asset, with a name and time to raise the event.
For example, if you have an enemy death sfx and you want to tie animationn starting to a sudden change in the sfx like an explosion, raise an event at that point in the sfx asset and listen for it in code