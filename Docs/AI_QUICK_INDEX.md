# Abyssal Protocol — AI Quick Index

This is the fast navigation index for future AI-assisted work on the Abyssal Protocol RimWorld mod.
Use it before editing files, but never treat it as a replacement for inspecting the actual local archive or live repository.

## Ground truth order

When sources disagree, use this order:

```text
1. User-provided local archive, if the user says it is current/up to date.
2. Live GitHub repository and latest commits.
3. Actual file tree and file contents in the working copy.
4. Build result and RimWorld runtime test result.
5. Docs/AI_ARCHITECTURE.md and Docs/BUILD_AND_SOURCE_LAYOUT.md.
6. Prior memory or older conversation context.
```

Current source convention:

```text
source/                 lowercase only
source/AbyssalProtocol.csproj
source/<module folders>
```

Do not create uppercase `Source/`. Do not place `.cs` files directly under `source/` root.

## First files to inspect by task

| User task / symptom | Start here | Also check |
| --- | --- | --- |
| Forge UI, Forge console, pattern browser, clipped text | `source/UI/Forge/` | `source/UI/Shared/`, `source/Forge/`, `Defs/RecipeDefs/`, `Defs/ThingDefs/`, `Languages/` |
| Forge progression, residue, attunement, unlocks | `source/Forge/`, `source/Progression/` | `source/UI/Forge/`, `Defs/ThingDefs/`, `Defs/RecipeDefs/`, `Languages/` |
| Summoning Circle UI / Summoning Console | `source/UI/Summoning/` | `source/Summoning/`, `source/UI/Shared/`, `Defs/ThingDefs/`, `Defs/JobDefs/`, `Languages/` |
| Sigil use, ritual activation, circle modules/capacitors | `source/Summoning/` | `source/Defs/Summoning/`, `source/UI/Summoning/`, `Defs/ThingDefs/`, `Defs/RecipeDefs/` |
| Protocol Nexus / custom research ring | `source/Experimental/ProtocolResearch/` | `Defs/Experimental/ProtocolResearch/`, `Textures/UI/ABY/ProtocolResearch/`, `source/Progression/` |
| Boss bar, boss phase text, Aegis bar/chains | `source/UI/BossBar/` | `source/Bosses/Shared/`, `source/Bosses/ReactorSaint/`, `Defs/Misc/`, `Textures/UI/ABY/BossBar/` |
| Archon Beast / Archon of Rupture behavior | `source/Bosses/Archon/` | `source/Bosses/Shared/`, `source/Pawns/`, `Defs/PawnKindDefs/`, `Defs/ThingDefs/`, `source/UI/BossBar/` |
| Reactor Saint behavior, AI, cocoon, Aegis | `source/Bosses/ReactorSaint/` | `source/Bosses/Shared/`, `source/UI/BossBar/`, `source/Combat/`, `Defs/ThingDefs/`, `Defs/PawnKindDefs/` |
| Rupture-specific crown/halo/secret boss logic | `source/Bosses/Rupture/` | `source/Bosses/Archon/`, `source/Bosses/Shared/`, `source/UI/BossBar/` |
| Dominion pocket/slice/hell dimension | `source/Dominion/` | `source/World/`, `Defs/MapGeneratorDefs/`, `Defs/TerrainDefs/`, `Defs/ThingDefs/`, `Textures/Terrain/`, `Textures/Things/Building/DominionSlice/` |
| Dominion visuals, atmosphere, collapse, flow, void edge | `source/Dominion/VFX/`, `source/Dominion/MapComponents/` | `Defs/ThingDefs_Motes/`, `Textures/Effects/`, `Textures/Things/Building/DominionSlice/` |
| Modular turret behavior, modules, sockets, targeting | `source/Comps/CompAbyssalModularTurret.cs`, `source/UI/Turrets/`, `source/Defs/Turrets/` | `Defs/Misc/ABY_TurretModuleDefs.xml`, `Defs/ThingDefs/ABY_TurretModules.xml`, `Defs/RecipeDefs/ABY_ModularTurretRecipes.xml` |
| Turret projectile or weapon module VFX | `source/Combat/VFX/`, `source/Combat/Projectiles/` | `Defs/ThingDefs/ABY_ModularTurret_Projectiles.xml`, `Defs/ThingDefs_Motes/`, `Textures/Things/Projectile/`, `Textures/Effects/` |
| Pawn AI, hostile behavior, pathing, anti-tame/animal workflow | `source/Pawns/`, `source/Patches/` | `source/Comps/`, `Defs/PawnKindDefs/`, `Defs/ThingDefs/` |
| Pawn death drops / true death / no downed state | `source/Pawns/DeathActions/`, `source/Bosses/Shared/` | `Defs/ThingDefs/`, `Defs/PawnKindDefs/`, `source/UI/BossBar/` |
| Apparel, armor Aegis, hover armor, body type restrictions | `source/Apparel/` | `source/Defs/Apparel/`, `Defs/ThingDefs/`, `Textures/Things/Apparel/`, `Languages/` |
| Implants, hediff comps, implant info cards | `source/Hediffs/` | `Defs/HediffDefs/`, `source/UI/Forge/`, `Languages/` |
| Harmony patch issues | `source/Patches/`, feature-specific module | `source/Core/Bootstrap/`, latest build output |
| Dev tools / diagnostics / stability reports | `source/Diagnostics/` | `source/Core/Utilities/`, `source/Legacy/` |
| Legacy save migration / old cleanup | `source/Legacy/` | `source/Diagnostics/`, save/load behavior |
| Sound/SFX issue | `source/Audio/`, `Defs/SoundDefs/`, `Sounds/ABY/` | Relevant weapon/projectile/incident XML and SFX pipeline rules |
| Asset path or missing texture | Actual XML `texPath` first | `Textures/`, `SourceAssets/Generated/`, asset-generation rules |
| Localization/missing key / Russian terminology | `Docs/LOCALIZATION_GLOSSARY_RU.md`, `Languages/English/`, `Languages/Russian/` | Source string keys, XML labels/descriptions, DefInjected, Keyed UI strings |

## Russian localization workflow

Before editing Russian localization, inspect:

```text
Docs/LOCALIZATION_GLOSSARY_RU.md
Languages/English/
Languages/Russian/
source/ files that emit the affected Keyed strings
Defs/ files that own the affected DefInjected keys
```


For modular turret localization specifically, also inspect:

```text
Defs/Misc/ABY_TurretModuleDefs.xml
Languages/English/DefInjected/ABY_TurretModuleDef/
Languages/Russian/DefInjected/ABY_TurretModuleDef/
Languages/<Lang>/DefInjected/ThingDef/ABY_ModularTurrets.xml
Languages/<Lang>/DefInjected/RecipeDef/ABY_ModularTurretRecipes.xml
```

Do not leave tactical role/effect summaries as raw English in Russian Forge cards.

Do not machine-translate item names directly from English. Use the glossary for canonical forms such as `Рифт-клинок`, `Рифт-карабин`, `Хор Забвения`, `Панцирь святого носителя Эгиды`, `Сигила угольных гончих`, and Russian plural forms for requirement counts.

## Common integration checklist

For any new gameplay content:

```text
1. Locate the owning module in source/.
2. Locate XML defs and texture/sound paths before editing.
3. Check whether the content belongs in Forge UI, Summoning UI, Protocol Nexus, boss bar, bestiary, or turret UI.
4. Add/modify full source files, not snippets.
5. Add/modify full XML files or targeted patches.
6. For Russian text, check `Docs/LOCALIZATION_GLOSSARY_RU.md` before translating names, categories, UI labels, or descriptions.
7. Add localization keys if any player-facing text is introduced.
8. Add real assets, not mockups, when assets are required.
9. If C# changed, build and include Assemblies/AbyssalProtocol.dll only if build succeeds.
10. Check whether architecture docs or recent-work docs must be updated.
11. Include a commit title and commit description in the final response.
```

## Where new files should go

```text
General mod bootstrap/settings        -> source/Core/Bootstrap/
Low-level generic utility             -> source/Core/Utilities/
Feature-specific utility              -> the feature module, not Core
Custom Def / DefModExtension          -> source/Defs/<area>/
Forge runtime                         -> source/Forge/
Forge UI                              -> source/UI/Forge/
Summoning runtime                     -> source/Summoning/
Summoning UI                          -> source/UI/Summoning/
Protocol Nexus / custom research      -> source/Experimental/ProtocolResearch/
Boss-specific logic                   -> source/Bosses/<BossName>/
Shared boss logic                     -> source/Bosses/Shared/
Boss bar UI                           -> source/UI/BossBar/
Dominion runtime/generation           -> source/Dominion/
Dominion world/building classes        -> source/World/ or source/Dominion/ if tightly owned
Weapon/projectile/VFX logic           -> source/Combat/
Pawn AI/comps/death actions           -> source/Pawns/
General thing comps                   -> source/Comps/
Apparel/armor behavior                -> source/Apparel/
Implant/Hediff behavior               -> source/Hediffs/
Harmony patches                       -> source/Patches/ or module-specific patch file
Diagnostics/dev windows               -> source/Diagnostics/
Legacy migrations                     -> source/Legacy/
Audio helpers                         -> source/Audio/
```

## High-risk systems

Treat these as high-risk and inspect dependencies before editing:

```text
Dominion pocket/slice runtime
Boss cleanup, true death, and save/load state
Reactor Saint AI/Aegis/presentation
Boss bar layout and text clipping
Forge/Summoning custom UI layout
Protocol Nexus tier/category mapping
Modular turret runtime and module definitions
XML class references to custom C# classes
Texture/sound paths used by XML
```

## Documentation update rule

Update `Docs/AI_ARCHITECTURE.md`, `Docs/BUILD_AND_SOURCE_LAYOUT.md`, `Docs/AI_QUICK_INDEX.md`, or `Docs/RECENT_WORK.md` when a change makes them outdated.

Do not update docs for tiny isolated fixes unless not updating would mislead future work.

When no docs are updated, final response should include a short note such as:

```text
Architecture docs not changed: this was an isolated XML/asset/balance fix and did not affect system ownership or source layout.
```

## Optimization / release packaging / performance settings

For texture budget and release packaging tasks, look first at:

```text
Tools/texture_budget_rules.json
Tools/ABY_TextureAudit.py
Tools/ABY_OptimizeTextures.py
Tools/ABY_BuildReleasePackage.py
Docs/TEXTURE_BUDGET.md
Docs/RELEASE_PACKAGING.md
```

For in-game visual intensity, low-end performance settings, and the performance audit window, look first at:

```text
source/Core/Bootstrap/AbyssalProtocolModSettings.cs
source/Core/Bootstrap/AbyssalProtocolMod.cs
source/Core/Bootstrap/ABY_VisualIntensity.cs
source/Core/Bootstrap/ABY_PerformanceSettingsUtility.cs
source/Diagnostics/ABY_PerformanceAuditUtility.cs
source/Diagnostics/UI/Window_ABY_PerformanceAudit.cs
source/Dominion/MapComponents/
```

The performance audit window is opened from Abyssal Protocol mod settings, diagnostics/performance area. It is a dev/testing aid, not player progression UI.

## Performance/TPS quick routing — 2026-05-19

For TPS, stutter, scan-loop, VFX density, or large-encounter performance tasks, inspect these first:

| Symptom / task | Start here | Also check |
| --- | --- | --- |
| Repeated pawn/thing/map scans | `source/Core/Runtime/ABY_RuntimeTargetCache.cs` | callers in `source/Compatibility/`, `source/Comps/`, `source/Combat/`, `source/Bosses/`, `source/Apparel/` |
| Optional VFX overload / beams / trails / Dominion ambience | `source/Combat/VFX/ABY_VfxBudget.cs` | `source/Combat/MapComponents/`, `source/Dominion/MapComponents/`, projectile VFX utilities |
| Modular turret targeting TPS | `source/Comps/CompAbyssalModularTurret.cs` | `source/Comps/Properties/CompProperties_AbyssalModularTurret.cs`, `Defs/ThingDefs/ABY_ModularTurrets.xml` |
| Dominion maintenance spikes | `source/Dominion/MapComponents/MapComponent_ABY_DominionAtmosphere.cs` | Dominion generation and weather/VFX map components |
| Specter Lash stream stutter | `source/Combat/MapComponents/SpecterLashStreamGameComponent.cs` | `source/Combat/VFX/HarmonyPatch_SpecterLashProjector.cs`, related mote defs |

