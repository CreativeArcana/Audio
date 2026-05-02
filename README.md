# Creative Arcana Audio

Creative Arcana Audio is a modular Unity audio package built on top of Unity's built-in audio system.

It provides a centralized `AudioService` for playing, stopping, pausing, resuming, crossfading, mixing, ducking, saving/loading channel volumes, and managing 2D/3D audio across scenes.

The package is designed for projects that use Unity `AudioClip`, `AudioSource`, `AudioMixer`, and `AudioMixerGroup`.

---

## Features

- Centralized audio playback through `IAudioService`
- Play sounds by `AudioEntry`, `AudioId`, or generated enum IDs
- Audio source pooling through `CreativeArcana.Factory`
- Required mixer channel support: Master, Music, SFX, Voice, UI, Ambient
- Fade in, fade out, crossfade, pause, resume, and stop controls
- Per-sound volume, pitch, loop, channel, 2D/3D position, and follow-target controls
- Channel volume, mute, ducking, and volume multiplier controls
- Scene-bound and persistent-across-scenes audio lifetime support
- Multiple clip playback modes: Single, Random, Random No Repeat, Sequential, Shuffle
- PlayerPrefs-based channel volume save/load support
- DOTween-powered transitions

---

## Dependencies

This package requires:

- Unity Audio System
- DOTween
- CreativeArcana.Factory

---

## Required AudioMixer Setup

Your Unity `AudioMixer` must contain the following mixer groups:
```txt
Master
Music
SFX
Voice
UI
Ambient
```
The `AudioService` expects these groups to exist and maps them to the following `AudioChannel` values:

```csharp
public enum AudioChannel
{
  Master = 0,
  Music = 1,
  SFX = 2,
  UI = 3,
  Voice = 4,
  Ambient = 5,
}
```
The mixer must also expose the volume parameters used by the package, such as:

```txt
MasterVolume
MusicVolume
SFXVolume
VoiceVolume
UIVolume
AmbientVolume
```
The exact exposed parameter names should match the constants used in your package, for example:

```csharp
AudioMixerParameterNames.MASTER_VOLUME
AudioMixerParameterNames.MUSIC_VOLUME
AudioMixerParameterNames.SFX_VOLUME
AudioMixerParameterNames.VOICE_VOLUME
AudioMixerParameterNames.UI_VOLUME
AudioMixerParameterNames.AMBIENT_VOLUME
```
---

## Quick Start

### 1. Install Dependencies

Install or import:

- DOTween
- CreativeArcana.Factory

Make sure DOTween is initialized in your Unity project.

---

### 2. Create an AudioMixer

Create a Unity `AudioMixer` with these groups:

```txt
Master
Music
SFX
Voice
UI
Ambient
```
Expose the required volume parameters for each group.

---

### 3. Create AudioEntry Assets

Create `AudioEntry` ScriptableObjects from:

```txt
Create > CreativeArcana > Audio > Data > AudioEntry
```
Each `AudioEntry` contains:

- `AudioId`
- Audio clips
- Playback mode
- Audio channel
- Audio lifetime
- Volume range
- Pitch range
- Loop setting
- 2D/3D spatial settings
- Distance and rolloff settings

Example configuration:

```txt
Id: Main_Menu_Music
Channel: Music
Lifetime: PersistentAcrossScenes
Playback Mode: Single
Loop: true
Spatial: false
```
---

### 4. Create an AudioLibrary

Create an `AudioLibrary` and add your `AudioEntry` assets to it.

The library is used by `AudioService` to resolve sounds by `AudioId`.

---

### 5. Create the AudioService

The constructor requires only:

```csharp
public AudioService(
  AudioMixer audioMixer,
  AudioLibrary library,
  IPoolFactory<IAudioSourceController> sourceControllerFactory)
```
Example:

```csharp
IAudioService audioService = new AudioService(
  audioMixer,
  audioLibrary,
  audioSourceControllerFactory
);
```
The service internally creates:

- `DuckingManager`
- `AudioClipSelector`
- `Mixer group controllers`
- `Mixer group mapping`
- `Saved channel volume loading`
- `Scene load listener`

---

### 6. Play Audio

Play by `AudioEntry`:

```csharp
AudioHandle handle = audioService.Play(audioEntry);
```
Play with fade in:

```csharp
AudioHandle handle = audioService.Play(audioEntry, fadeInDuration: 0.5f);
```
Play by `AudioId`:

```csharp
AudioHandle handle = audioService.Play(new AudioId("Main_Menu_Music"));
```
---

## Generated Audio ID Enum

For easier usage, the package includes an editor code generator.

The generator can create:

```txt
YourLibraryNameIds.g.cs
YourLibraryNameIdsExtensions.g.cs
```
Example generated enum:

```csharp
public enum GameAudioIds
{
  Main_Menu_Music,
  Button_Click,
  Explosion,
  Player_Footstep
}
```
Example generated extension usage:

```csharp
audioService.Play(GameAudioIds.Button_Click);
```
Crossfade with generated enum:

```csharp
musicHandle = audioService.CrossFadePlay(
  musicHandle,
  GameAudioIds.Battle_Music,
  fadeInDuration: 1f,
  fadeOutDuration: 1f
);
```
To use this feature:

1. Select your `AudioLibrary` asset.
2. Enable `Auto Generate On Library Changes`, or click `Generate`.
3. Choose an output folder inside `Assets`.
4. Use the generated enum and extension methods in your code.

---

## Crossfade

Crossfade allows you to start a new sound while fading out the previous one.

Crossfade by `AudioEntry`:

```csharp
AudioHandle newHandle = audioService.CrossFadePlay(
  from: oldHandle,
  to: newMusicEntry,
  fadeInDuration: 1f,
  fadeOutDuration: 1f
);
```
Crossfade by `AudioId`:

```csharp
AudioHandle newHandle = audioService.CrossFadePlay(
  from: oldHandle,
  to: new AudioId("Battle_Music"),
  fadeInDuration: 1f,
  fadeOutDuration: 1f
);
```
Crossfade using generated enum:

```csharp
AudioHandle newHandle = audioService.CrossFadePlay(
  oldHandle,
  GameAudioIds.Battle_Music,
  fadeInDuration: 1f,
  fadeOutDuration: 1f
);
```
---

## Audio Lifetime

Each `AudioEntry` has an `AudioLifetime` value:

```csharp
public enum AudioLifetime
{
  SceneBound,
  PersistentAcrossScenes
}
```
### SceneBound

The sound is automatically stopped when a new scene is loaded.

Useful for:

- Scene ambience
- Level-specific SFX
- Local environmental sounds

### PersistentAcrossScenes

The sound continues playing across scene loads.

Useful for:

- Main menu music
- Global background music
- Long-running ambience
- Persistent UI audio

You can also change lifetime at runtime:

```csharp
audioService.SetLifetime(handle, AudioLifetime.PersistentAcrossScenes);
```
Scene-bound auto-stop can be enabled or disabled:

```csharp
audioService.SetAutoStopSceneBoundAudio(true);
audioService.SetAutoStopSceneBoundAudio(false);
```
Manually stop only scene-bound audio:

```csharp
audioService.StopSceneBoundAudio(fadeOutDuration: 0.5f);
```
---

## Pause and Resume

Pause one sound:

```csharp
audioService.Pause(handle, fadeOutDuration: 0.25f);
```
Resume one sound:

```csharp
audioService.Resume(handle, fadeInDuration: 0.25f);
```
Pause all sounds:

```csharp
audioService.PauseAll();
```
Resume all sounds:

```csharp
audioService.ResumeAll();
```
Pause a channel:

```csharp
audioService.PauseAll(AudioChannel.Music, fadeOutDuration: 0.5f);
```
Resume a channel:

```csharp
audioService.ResumeAll(AudioChannel.Music, fadeInDuration: 0.5f);
```
When a sound is paused, its ducking effect is also paused.

---

## Stop Audio

Stop one sound:

```csharp
audioService.Stop(handle);
```
Stop with fade out:

```csharp
audioService.Stop(handle, fadeOutDuration: 0.5f);
```
Stop all sounds:

```csharp
audioService.StopAll();
```
Stop all sounds in a channel:

```csharp
audioService.StopAll(AudioChannel.SFX);
```
---

## Channel Control

Set channel volume multiplier:

```csharp
audioService.SetChannelVolumeMultiplier(AudioChannel.Music, 0.5f, fadeDuration: 0.25f);
```
Get effective channel volume:

```csharp
float volume = audioService.GetChannelEffectiveVolume(AudioChannel.Music);
```
Mute or unmute a channel:

```csharp
audioService.MuteChannel(AudioChannel.Music, true);
audioService.MuteChannel(AudioChannel.Music, false);
```
Check mute state:

```csharp
bool muted = audioService.IsChannelMuted(AudioChannel.Music);
```
---

## Save and Load Channel Volume

Set base volume:
```csharp
audioService.SetChannelBaseVolume(AudioChannel.Music, 0.75f);
````
Save volume value to `PlayerPrefs`:

```csharp
audioService.SaveChannelVolume(AudioChannel.Music);
````
Load volume value from `PlayerPrefs`:

```csharp
float loadedVolume = audioService.LoadChannelVolume(AudioChannel.Music);
````
`SaveChannelVolume` and `LoadChannelVolume` only save and load the stored value using `PlayerPrefs`.

They do not automatically apply the loaded value to the mixer.

If you want to persist a changed value manually, save it after setting the channel volume:
```csharp
audioService.SetChannelBaseVolume(AudioChannel.Music, 0.75f);
audioService.SaveChannelVolume(AudioChannel.Music);
````
When `AudioService` initializes, it automatically loads saved mixer volumes from `PlayerPrefs` and applies them to the mixer group controllers.

---

## 2D and 3D Audio

Set a playing sound to 2D:

```csharp
audioService.Set2D(handle);
```
Set a playing sound to 3D:

```csharp
audioService.Set3D(handle, minDistance: 1f, maxDistance: 500f);
```
Set world position:

```csharp
audioService.SetPosition(handle, transform.position);
```
Make audio follow a target:

```csharp
audioService.SetFollowTarget(handle, targetTransform);
```
---

## Cleanup

When the audio system is no longer needed:

```csharp
audioService.Dispose();
```
Disposing the service:

- Stops active sounds
- Releases source controllers back to the pool
- Releases ducking states
- Disposes mixer group controllers
- Clears clip selector state
- Unsubscribes from scene load events

---

## Notes

This package is designed for Unity's built-in audio system.

It is not intended for:

- FMOD
- Wwise
- External audio middleware

See `Documentation.md` for more details.
