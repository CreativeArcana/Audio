# Creative Arcana Audio Documentation

## Overview

Creative Arcana Audio is a Unity audio management package that provides a service-based architecture for handling audio
playback, mixer control, scene lifetime behavior, crossfading, ducking, save/load volume settings, and 2D/3D audio.

The main entry point is:

```csharp
IAudioService
```

The main implementation is:

```csharp
AudioService
```

The system is built around Unity's native audio stack:

- `AudioClip`
- `AudioSource`
- `AudioMixer`
- `AudioMixerGroup`

---

# Core Concepts

## AudioService

`AudioService` is the central runtime class responsible for:

- Playing audio
- Stopping audio
- Pausing audio
- Resuming audio
- Crossfading audio
- Managing active audio handles
- Managing source controller pooling
- Applying ducking
- Controlling mixer channels
- Saving and loading channel volumes
- Handling scene-bound audio
- Managing 2D and 3D audio behavior

Constructor:

```csharp
public AudioService(
    AudioMixer audioMixer,
    AudioLibrary library,
    IPoolFactory<IAudioSourceController> sourceControllerFactory)
```

Internally, the service initializes:

```csharp
_sourceControllerFactory.ApplyInitialPreWarm();

InitializeAudioMixerGroupControllers();
InitializeAudioMixerGroups();
LoadAndSetAudioMixerVolumes();

_duckingManager = new DuckingManager(SetChannelDuckingMultiplier);
_clipSelector = new AudioClipSelector();

SceneManager.sceneLoaded += OnSceneLoad;
```

This means you do not manually pass `IDuckingManager` or `IAudioClipSelector` to the constructor. They are created
internally by the service.

---

# Required AudioMixer Structure

The package expects your Unity `AudioMixer` to contain exactly these logical mixer groups:

```txt
Master
Music
SFX
Voice
UI
Ambient
```

These groups map to:

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

During initialization, `AudioService` creates an `AudioMixerGroupController` for each channel:

```csharp
_mixerGroupControllers[AudioChannel.Master]
_mixerGroupControllers[AudioChannel.Music]
_mixerGroupControllers[AudioChannel.SFX]
_mixerGroupControllers[AudioChannel.UI]
_mixerGroupControllers[AudioChannel.Voice]
_mixerGroupControllers[AudioChannel.Ambient]
```

If a required mixer group is missing, the service logs an error:

```csharp
[AudioService] MixerGroup is null for channel: ...
```

## Required Exposed Parameters

Each mixer group should expose its volume parameter.

Typical parameters:

```txt
MasterVolume
MusicVolume
SFXVolume
UIVolume
VoiceVolume
AmbientVolume
```

These should match your internal constants:

```csharp
AudioMixerParameterNames.MASTER_VOLUME
AudioMixerParameterNames.MUSIC_VOLUME
AudioMixerParameterNames.SFX_VOLUME
AudioMixerParameterNames.UI_VOLUME
AudioMixerParameterNames.VOICE_VOLUME
AudioMixerParameterNames.AMBIENT_VOLUME
```

---

# AudioEntry

`AudioEntry` is a ScriptableObject that describes a playable sound.

Create menu:

```txt
Create > CreativeArcana > Audio > Data > AudioEntry
```

Definition:

```csharp
[CreateAssetMenu(menuName = "CreativeArcana/Audio/Data/AudioEntry", fileName = "AudioEntry")]
public class AudioEntry : ScriptableObject
```

## Main Fields

```csharp
[SerializeField] private AudioId _id;
[SerializeField] private AudioClip[] _clips;
[SerializeField] private AudioPlaybackMode _playbackMode;
[SerializeField] private AudioChannel _audioChannel = AudioChannel.Master;
[SerializeField] private AudioLifetime _lifetime = AudioLifetime.SceneBound;
```

## Volume and Pitch

```csharp
[SerializeField] private float _minVolume = 1f;
[SerializeField] private float _maxVolume = 1f;
[SerializeField] private float _minPitch = 1f;
[SerializeField] private float _maxPitch = 1f;
```

Runtime methods:

```csharp
public float GetVolume()
{
    return Random.Range(_minVolume, _maxVolume);
}

public float GetPitch()
{
    return Random.Range(_minPitch, _maxPitch);
}
```

This allows each playback to have slightly different volume and pitch.

## Playback Options

```csharp
[SerializeField] private bool _loop;
[SerializeField] private bool _spatial;
```

## 3D Settings

```csharp
[SerializeField] private float _spatialBlend = 1f;
[SerializeField] private float _dopplerLevel = 1f;
[SerializeField] private int _spread;
[SerializeField] private AudioRolloffMode _rolloffMode = AudioRolloffMode.Logarithmic;
[SerializeField] private float _minDistance = 1f;
[SerializeField] private float _maxDistance = 500f;
```

Safe distance properties:

```csharp
public float MinDistance => Mathf.Max(0.01f, _minDistance);
public float MaxDistance => Mathf.Max(MinDistance, _maxDistance);
```

## Automatic AudioId Assignment

In the Unity Editor, if an `AudioEntry` has an empty ID, it automatically uses the asset file name:

```csharp
if (string.IsNullOrWhiteSpace(_id.Value))
{
    var path = UnityEditor.AssetDatabase.GetAssetPath(this);
    var fileName = System.IO.Path.GetFileNameWithoutExtension(path);
    _id.SetId(fileName);
}
```

This makes asset naming important and helps keep IDs consistent.

---

# AudioPlaybackMode

`AudioEntry` supports multiple clip selection modes:

```csharp
public enum AudioPlaybackMode
{
    Single,
    Random,
    RandomNoRepeat,
    Sequential,
    Shuffle
}
```

## Single

Always plays the first clip:

```txt
AudioClip[0]
```

## Random

Chooses a random clip.

## RandomNoRepeat

Chooses a random clip but avoids playing the same clip twice in a row.

## Sequential

Plays clips in order and loops back to the beginning.

## Shuffle

Plays every clip once in a shuffled order, then reshuffles.

It also avoids the previous cycle's last clip being the next cycle's first clip when possible.

---

# AudioLibrary

`AudioLibrary` stores a list of `AudioEntry` assets.

`AudioService` uses it to resolve audio by `AudioId`:

```csharp
public AudioHandle Play(AudioId audioId, float fadeInDuration = 0, DuckingProfile duckingProfile = default)
{
    var entry = _library.Get(audioId);
    
    if (entry == null)
    return AudioHandle.Invalid;
    
    return Play(entry, fadeInDuration, duckingProfile);
}
```

---

# Generated Audio IDs

The package includes an editor tool that can generate enum-based IDs from an `AudioLibrary`.

This improves usage by replacing string-based or manually created IDs with strongly typed enum values.

## Generated Files

For a library named:

```txt
GameAudio
```

The generator creates:

```txt
GameAudioIds.g.cs
GameAudioIdsExtensions.g.cs
```

The files are generated inside the selected output folder.

## Generated Enum Example

```csharp
namespace CreativeArcana.Audio
{
    public enum GameAudioIds
    {
        Button_Click,
        Main_Menu_Music,
        Player_Footstep
    }
}
```

## Generated Mapper Example

The generator also creates extension methods:

```csharp
public static AudioId ToAudioId(this GameAudioIds value)
```

Example usage:

```csharp
AudioId id = GameAudioIds.Button_Click.ToAudioId();
```

## Generated Play Extension

The generated extension allows this:

```csharp
audioService.Play(GameAudioIds.Button_Click);
```

Instead of:

```csharp
audioService.Play(new AudioId("Button_Click"));
```

## Generated CrossFadePlay Extension

The generated extension also supports crossfade:

```csharp
audioService.CrossFadePlay(
    oldHandle,
    GameAudioIds.Battle_Music,
    fadeInDuration: 1f,
    fadeOutDuration: 1f
);
```

## How to Generate

1. Select an `AudioLibrary` asset.
2. In the Inspector, find the `Code Generation` section.
3. Enable:

```txt
Auto Generate On Library Changes
```

or click:

```txt
Generate
```

4. Select an output folder inside the `Assets` folder.
5. The generator creates `.g.cs` files.

## Important Notes

Generated files include:

```csharp
// <auto-generated />
// This file is generated. Do not modify manually.
```

Do not edit generated files directly. Change the `AudioLibrary` entries and regenerate instead.

---

# Playing Audio

## Play by AudioEntry

```csharp
AudioHandle handle = audioService.Play(audioEntry);
```

With fade in:

```csharp
AudioHandle handle = audioService.Play(audioEntry, fadeInDuration: 0.5f);
```

With ducking profile:

```csharp
AudioHandle handle = audioService.Play(
    audioEntry,
    fadeInDuration: 0.5f,
    duckingProfile
);
```

## Play by AudioId

```csharp
AudioHandle handle = audioService.Play(new AudioId("Explosion"));
```

## Play by Generated Enum

```csharp
AudioHandle handle = audioService.Play(GameAudioIds.Explosion);
```

---

# AudioHandle

Every successful play request returns an `AudioHandle`.

```csharp
AudioHandle handle = audioService.Play(audioEntry);
```

The handle is used to control that specific audio instance:

```csharp
audioService.Stop(handle);
audioService.Pause(handle);
audioService.Resume(handle);
audioService.SetVolume(handle, 0.75f);
audioService.SetPitch(handle, 1.2f);
```

If playback fails, the service returns:

```csharp
AudioHandle.Invalid
```

Common failure cases:

- Entry is null
- No valid clip exists
- Source controller factory returns null
- Source controller fails to play

---

# Crossfade

Crossfade starts a new sound and fades out an existing one.

## Crossfade by AudioEntry

```csharp
AudioHandle newHandle = audioService.CrossFadePlay(
    from: currentMusicHandle,
    to: nextMusicEntry,
    fadeInDuration: 1f,
    fadeOutDuration: 1f
);
```

## Crossfade by AudioId

```csharp
AudioHandle newHandle = audioService.CrossFadePlay(
    from: currentMusicHandle,
    to: new AudioId("Battle_Music"),
    fadeInDuration: 1f,
    fadeOutDuration: 1f
);
````

## Crossfade by Generated Enum

```csharp
AudioHandle newHandle = audioService.CrossFadePlay(
    currentMusicHandle,
    GameAudioIds.Battle_Music,
    fadeInDuration: 1f,
    fadeOutDuration: 1f
);
```

## Behavior

Internally:

```csharp
var newHandle = Play(to, fadeInDuration, duckingProfile);

if (!newHandle.IsValid)
    return AudioHandle.Invalid;

Stop(from, fadeOutDuration);

return newHandle;
```

The new sound must start successfully before the old sound is stopped.

---

# Stopping Audio

## Stop One Sound

```csharp
audioService.Stop(handle);
```

With fade out:

```csharp
audioService.Stop(handle, fadeOutDuration: 0.5f);
```

Ducking is released when the source actually ends, not immediately when fade out starts.

## Stop All Sounds

```csharp
audioService.StopAll();
```

With fade:

```csharp
audioService.StopAll(fadeOutDuration: 0.5f);
```

## Stop All Sounds in a Channel

```csharp
audioService.StopAll(AudioChannel.SFX);
```

With fade:

```csharp
audioService.StopAll(AudioChannel.SFX, fadeOutDuration: 0.25f);
```

---

# Pause and Resume

## Pause One Sound

```csharp
audioService.Pause(handle);
```

With fade out:

```csharp
audioService.Pause(handle, fadeOutDuration: 0.25f);
```

When a sound is paused, ducking is paused too:

```csharp
_duckingManager.Pause(handle.Id);
```

## Resume One Sound

```csharp
audioService.Resume(handle);
```

With fade in:

```csharp
audioService.Resume(handle, fadeInDuration: 0.25f);
```

When resumed, ducking is restored:

```csharp
_duckingManager.Resume(handle.Id);
```

## Pause All

```csharp
audioService.PauseAll();
```

## Resume All

```csharp
audioService.ResumeAll();
```

## Pause All in Channel

```csharp
audioService.PauseAll(AudioChannel.Music);
```

## Resume All in Channel

```csharp
audioService.ResumeAll(AudioChannel.Music);
```

---

# Per-Sound Controls

## Set Volume

```csharp
audioService.SetVolume(handle, 0.75f);
```

## Set Pitch

```csharp
audioService.SetPitch(handle, 1.2f);
```

## Set Loop

```csharp
audioService.SetLoop(handle, true);
```

## Change Channel

```csharp
audioService.SetChannel(handle, AudioChannel.SFX);
````

## Check Playing State

```csharp
bool isPlaying = audioService.IsPlaying(handle);
```

---

# Channel Controls

The package supports channel-level control through `AudioMixerGroupController`.

## Set Channel Volume Multiplier

```csharp
audioService.SetChannelVolumeMultiplier(
    AudioChannel.Music,
    multiplier: 0.5f,
    fadeDuration: 0.25f
);
```

The multiplier is clamped between `0` and `1`.

## Get Effective Channel Volume

```csharp
float effectiveVolume = audioService.GetChannelEffectiveVolume(AudioChannel.Music);
```

The effective volume may include:

- Base volume
- Volume multiplier
- Ducking multiplier
- Mute state

## Mute Channel

```csharp
audioService.MuteChannel(AudioChannel.Music, true);
```

## Unmute Channel

```csharp
audioService.MuteChannel(AudioChannel.Music, false);
```

## Check Channel Mute State

```csharp
bool isMuted = audioService.IsChannelMuted(AudioChannel.Music);
```

---

# Save and Load

The package supports saving and loading channel base volumes.

## Set Base Volume

```csharp
audioService.SetChannelBaseVolume(AudioChannel.Music, 0.8f);
```

## Save Channel Volume

```csharp
audioService.SaveChannelVolume(AudioChannel.Music);
```

## Load Channel Volume

```csharp
float volume = audioService.LoadChannelVolume(AudioChannel.Music);
```

## Automatic Load on Initialization

When `AudioService` initializes, it loads saved volumes for every mixer group controller:

```csharp
private void LoadAndSetAudioMixerVolumes()
{
    foreach (var mixerGroup in _mixerGroupControllers.Values)
    {
        var loadedVolume = mixerGroup.LoadVolume();
        mixerGroup.SetBaseVolume(loadedVolume);
    }
}
```

This makes it suitable for user settings menus.

Example settings flow:

```csharp
audioService.SetChannelBaseVolume(AudioChannel.Music, musicSlider.value);
audioService.SaveChannelVolume(AudioChannel.Music);
```

On the next application start or service initialization, the saved value is restored.

---

# Audio Lifetime and Scene Loading

Each `AudioEntry` has a lifetime:

```csharp
public enum AudioLifetime
{
    SceneBound,
    PersistentAcrossScenes
}
```

## SceneBound

Scene-bound audio is stopped automatically when a new scene loads.

Useful for:

- Level ambience
- Scene-specific music
- Local environmental loops
- Temporary scene SFX

## PersistentAcrossScenes

Persistent audio continues playing across scene changes.

Useful for:

- Global music
- Main menu music
- Loading screen music
- Persistent ambience

## Runtime Lifetime Change

You can change a playing sound's lifetime:

```csharp
audioService.SetLifetime(handle, AudioLifetime.PersistentAcrossScenes);
```

## Manual Scene-Bound Stop

```csharp
audioService.StopSceneBoundAudio(fadeOutDuration: 0.5f);
```

## Auto Stop Scene-Bound Audio

By default:

```csharp
_autoStopSceneBoundAudio = true;
```

The service listens to scene loading:

```csharp
SceneManager.sceneLoaded += OnSceneLoad;
```

And stops scene-bound audio:

```csharp
private void OnSceneLoad(Scene scene, LoadSceneMode sceneMode)
{
    if (_autoStopSceneBoundAudio)
    {
        StopSceneBoundAudio();
    }
}
```

You can enable or disable this behavior:

```csharp
audioService.SetAutoStopSceneBoundAudio(true);
audioService.SetAutoStopSceneBoundAudio(false);
```

---

# 2D and 3D Audio

The service supports runtime 2D/3D audio control.

## Set 2D

```csharp
audioService.Set2D(handle);
```

## Set 3D

```csharp
audioService.Set3D(
    handle,
    minDistance: 1f,
    maxDistance: 500f
);
```

## Set Position

```csharp
audioService.SetPosition(handle, worldPosition);
```

## Follow Target

```csharp
audioService.SetFollowTarget(handle, targetTransform);
```

This is useful for sounds attached to moving objects, such as:

- Characters
- Vehicles
- Projectiles
- Interactable objects
- Environmental emitters

---

# Ducking

Ducking is handled by `DuckingManager`.

The service creates it internally:

```csharp
_duckingManager = new DuckingManager(SetChannelDuckingMultiplier);
```

Ducking affects channel volume through:

```csharp
SetChannelDuckingMultiplier(AudioChannel channel, float multiplier, float fadeDuration = 0)
```

Example playback with ducking:

```csharp
AudioHandle handle = audioService.Play(
    voiceLineEntry,
    fadeInDuration: 0.2f,
    duckingProfile
);
```

Common use cases:

- Lower music while voice plays
- Lower ambient sounds during dialogue
- Lower background audio during important UI sounds

Ducking is released when audio ends:

```csharp
_duckingManager.Release(id);
```

When audio is paused:

```csharp
_duckingManager.Pause(id);
```

When audio is resumed:

```csharp
_duckingManager.Resume(id);
```

---

# Audio Source Pooling

`AudioService` uses:

```csharp
IPoolFactory<IAudioSourceController>
```

Source controllers are requested when audio starts:

```csharp
var sourceController = _sourceControllerFactory.Get();
```

And released when audio ends:

```csharp
_sourceControllerFactory.Release(activeAudio.SourceController);
```

The service also prewarms the pool during initialization:

```csharp
_sourceControllerFactory.ApplyInitialPreWarm();
```

This helps reduce runtime allocation and improves audio playback performance.

---

# ActiveAudio

`ActiveAudio` is an internal runtime wrapper.

It stores:

```csharp
IAudioSourceController SourceController
AudioLifetime Lifetime
```

Definition:

```csharp
internal sealed class ActiveAudio
{
    public IAudioSourceController SourceController => _sourceController;
    public AudioLifetime Lifetime => _lifetime;
    
    private readonly IAudioSourceController _sourceController;
    private AudioLifetime _lifetime;
}
```

It allows the service to track whether a playing sound should stop on scene load or persist across scenes.

---

# Disposal

`AudioService` implements `IDisposable`.

Call this when the service is no longer needed:

```csharp
audioService.Dispose();
```

Dispose behavior:

- Prevents further use of the service
- Unsubscribes from source controller events
- Releases ducking states
- Stops active sources
- Releases source controllers back to the pool
- Clears active audio dictionary
- Disposes ducking manager if disposable
- Disposes mixer group controllers if disposable
- Clears mixer group references
- Clears clip selector state
- Unsubscribes from `SceneManager.sceneLoaded`

After disposal, calling methods like `Play` will throw:

```csharp
ObjectDisposedException
```

---

# Recommended Usage Pattern

A typical project setup may look like this:

```csharp
public sealed class GameAudioBootstrapper : MonoBehaviour
{
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private AudioLibrary audioLibrary;
    
    private IAudioService _audioService;
    
    private void Awake()
    {
        var sourceFactory = CreateAudioSourceControllerFactory();
        
        _audioService = new AudioService(
            audioMixer,
            audioLibrary,
            sourceFactory
        );
    }
    
    private void Start()
    {
        _audioService.Play(GameAudioIds.Main_Menu_Music);
    }
    
    private void OnDestroy()
    {
        if (_audioService is IDisposable disposable) 
            disposable.Dispose();
    }
}
```

---

# Best Practices

## Use Generated IDs

Prefer:

```csharp
audioService.Play(GameAudioIds.Button_Click);
```
Instead of:

```csharp
audioService.Play(new AudioId("Button_Click"));
```
Generated IDs reduce typo-related bugs.

## Use Persistent Lifetime for Global Music

For music that should continue across scenes:

```txt
Lifetime: PersistentAcrossScenes
```
## Use SceneBound for Level Audio

For level-specific audio:

```txt
Lifetime: SceneBound
```
## Use Crossfade for Music Transitions

```csharp
musicHandle = audioService.CrossFadePlay(
    musicHandle,
    GameAudioIds.Battle_Music,
    1f,
    1f
);
```
## Save User Volume Settings

```csharp
audioService.SetChannelBaseVolume(AudioChannel.Music, slider.value);
audioService.SaveChannelVolume(AudioChannel.Music);
```
## Use Channels Consistently

Recommended usage:

```txt
Music -> Background music
SFX -> Gameplay sound effects
Voice -> Dialogue and voice lines
UI -> Buttons and menus
Ambient -> Environment loops
Master -> Global control
```
---

# Limitations

This package is designed for Unity's built-in audio system.

It does not directly support:

- FMOD
- Wwise
- External audio middleware

It also requires the expected mixer groups and exposed mixer parameters to exist before runtime.