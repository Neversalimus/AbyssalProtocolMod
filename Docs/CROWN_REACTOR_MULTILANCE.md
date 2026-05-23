# Crown Reactor Multilance

## Status

Implemented in the main `AbyssalProtocol.dll` source/build path.

## Ownership

```text
Defs/ThingDefs/ABY_CrownReactorMultilance.xml
source/Combat/Verbs/Verb_CrownReactorMultilance.cs
source/Combat/VFX/Thing_CrownReactorBeamSequence.cs
Textures/Things/Weapon/ABY_CrownReactorMultilance.png
Textures/Things/Projectile/ABY_CrownReactorBeamSegment.png
Languages/English/DefInjected/ThingDef/ABY_CrownReactorMultilance.xml
Languages/Russian/DefInjected/ThingDef/ABY_CrownReactorMultilance.xml
Assemblies/AbyssalProtocol.dll
```

## Gameplay role

T5 post-Saint / Crown-Reactor heavy weapon. The weapon is intentionally slower than normal late-game guns and uses a four-rail sequence instead of vanilla burst projectile spam.

## Runtime design

`Verb_CrownReactorMultilance` spawns one transient `Thing_CrownReactorBeamSequence` after warmup. The sequence:

- charges four rails in order;
- emits four long visual beams in order;
- applies four controlled damage pulses, one per rail;
- does not deal per-tick beam damage;
- does not perform map-wide scans;
- uses cached/quantized materials through `ABY_MaterialCacheUtility`.

This keeps the weapon presentation-heavy while respecting the recent runtime hot-path safety passes.

## Progression and Forge exposure

The weapon is craftable at `ABY_AbyssalForge` and exposed to the custom Forge progression through `DefModExtension_AbyssalForgeUnlock`:

```text
requiredResidue: 5200
category: Herald
requiredProtocolResearchDefName: ABY_PR_CrownLogicDecoding
```

It consumes Reactor Saint and Crown/Dominion materials so it remains a late reward rather than a generic research unlock.

## Asset notes

The weapon and beam source generations used green chromakey. Final repository PNGs use real alpha transparency and optimized PNG compression.
