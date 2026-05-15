PlasmaLance_Fire.wav

HOTFIX IMPORTANT:
- Delete the old file before testing:
  Sounds/ABY/Weapons/PlasmaLance_Fire.ogg
- RimWorld scans audio files during mod loading. If the broken OGG remains in the folder, it can still throw the same AudioClip load exception even if this WAV exists.

Why WAV:
- The previous OGG payload kept failing in RimWorld's RuntimeAudioClipLoader.
- This replacement is conservative PCM WAV: mono, 44.1 kHz, 16-bit signed PCM.

SoundDef:
- No XML change is required.
- The existing clipPath stays:
  ABY/Weapons/PlasmaLance_Fire
- RimWorld should resolve that clip path to PlasmaLance_Fire.wav once the broken OGG is removed.

Source notes:
- Derived from ABY_PlasmaLance_EnergyBodyBlend_08_IonShearDischarge_10Body, selected by the user for Plasma Lance Core integration.
- Built from user-provided CC0/OpenGameArt source layers and procedural non-tonal energy layers during the Plasma Lance SFX pass.
- No Doom/game/movie samples used.
- No Stable Audio source used in the final selected EnergyBodyBlend pass.

Build note:
- Sound-file-only hotfix. Build not verified.
