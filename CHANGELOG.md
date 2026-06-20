# Changelog

All notable changes to this package will be documented in this file.

## [1.0.1] - 2026-June-21

- Fix race condition when releasing very short audio clips back to the pool

## [1.0.0] - 2026-May-1

### Added

- Initial release of CreativeArcana Audio.
- Added centralized audio playback through `IAudioService`.
- Added support for playing audio by `AudioEntry`, `AudioId`, and generated enum IDs.
- Added audio source pooling support using `CreativeArcana.Factory`.
- Added mixer channel support for Master, Music, SFX, Voice, UI, and Ambient.
- Added fade in, fade out, crossfade, pause, resume, and stop controls.
- Added per-sound volume, pitch, loop, channel, 2D/3D position, and follow-target options.
- Added channel volume, mute, ducking, and volume multiplier controls.
- Added scene-bound and persistent-across-scenes audio lifetime support.
- Added multiple clip play modes: Single, Random, Random No Repeat, Sequential, and Shuffle.
- Added PlayerPrefs-based channel volume save/load support.
- Added DOTween-powered audio transitions.
- Added sample scene with audio channel tests, play mode examples, audio settings panel, and scene reload testing.
