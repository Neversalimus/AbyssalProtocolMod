# Abyssal Protocol — starter RimWorld mod scaffold

This archive contains a first-pass playable framework for the mod idea:
- custom summoning circle workbench
- craftable residue
- craftable Archon Sigil
- C# use-effect that spawns a hostile boss pawn on the current map
- starter weapons
- starter implant
- placeholder art

## Build
1. Put the mod folder inside `RimWorld/Mods/`.
2. Open `Source/AbyssalProtocol.csproj` in Visual Studio.
3. Fix the `HintPath` values so they point to your local RimWorld managed DLL folder.
4. Build in Release or Debug; the DLL is configured to output to `Assemblies/`.
5. Enable the mod in RimWorld.

## What is intentionally simple in this starter pack
- Boss spawn uses a hostile vanilla humanlike pawnkind (`SpaceSoldier`) and then customizes its gear.
- No custom boss phases yet.
- No custom faction yet.
- Art is placeholder style, made for rapid prototyping.

## Best next steps
- swap `pawnKindDefName` to a dedicated custom pawnkind
- add a boss apparel set
- add 2-3 more sigils
- move boss tuning into Defs or ModSettings
- add an IncidentWorker if you want storyteller integration later
