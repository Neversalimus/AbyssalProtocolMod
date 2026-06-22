# Abyssal Protocol — Content Matrix

This document is a practical routing matrix for future AI-assisted Abyssal Protocol work.
It is not a lore bible, not a full player changelog, and not a replacement for inspecting the actual local archive.

Use this file to answer four questions before editing:

1. What system owns this content?
2. Where are its C# / XML / assets / localization likely located?
3. What UI or progression surface should expose it?
4. What must be updated together so the feature is not left half-integrated?

## Ground truth order

When sources disagree:

```text
1. User-provided local archive, if explicitly current/up to date.
2. Actual file tree and file contents inside that archive.
3. Verified build result / RimWorld runtime smoke test.
4. Live GitHub and latest commits.
5. Docs/AI_ARCHITECTURE.md
6. Docs/BUILD_AND_SOURCE_LAYOUT.md
7. Docs/AI_QUICK_INDEX.md
8. Docs/RECENT_WORK.md
9. This CONTENT_MATRIX.md
10. Previous memory or old conversation context
```

If this matrix conflicts with the actual files, the actual files win.

## Current local archive snapshot

Snapshot basis: local archive inspected during the documentation pass.

```text
Source root: source/
Project: source/AbyssalProtocol.csproj
Uppercase Source/: absent
Root-level .cs in source/: 0
Real .cs under source/ excluding bin/obj: 447
Docs currently expected:
- Docs/AI_ARCHITECTURE.md
- Docs/BUILD_AND_SOURCE_LAYOUT.md
- Docs/AI_QUICK_INDEX.md
- Docs/RECENT_WORK.md
- Docs/CONTENT_MATRIX.md
- Docs/KNOWN_RISKS_AND_REGRESSIONS.md
- Docs/LOCALIZATION_AUDIT_RU.md
- Docs/LOCALIZATION_GLOSSARY_RU.md
```

Do not create root-level `.cs` files under `source/`.

## Status vocabulary

Use these statuses in future updates:

| Status | Meaning |
| --- | --- |
| Implemented | Real files exist and the system is actively wired into the mod. |
| Partial | Real files exist, but the system may still need UI, balance, polish, smoke testing, or content completion. |
| Experimental | Real files exist but are intentionally isolated, volatile, or not final architecture. |
| Planned | Design/lore target only unless actual files exist in the current archive. |
| Deprecated / Legacy | Kept for migration, cleanup, save compatibility, diagnostics, or historical compatibility. Do not expand unless explicitly needed. |

## System ownership matrix

| System / content area | Status | Primary source paths | Primary XML / defs | Assets / audio | UI exposure | Progression / gating | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Build and source layout | Implemented | `source/AbyssalProtocol.csproj`, `source/<module>/` | n/a | `Assemblies/AbyssalProtocol.dll` when build verified | n/a | n/a | Lowercase `source/`; no root `.cs`. SDK-style recursive compile is expected. |
| Core bootstrap/settings/utilities | Implemented | `source/Core/Bootstrap/`, `source/Core/Utilities/`, `source/Core/GameComponents/`, `source/Core/Misc/` | mixed | n/a | n/a | shared | Keep feature-specific code out of Core unless genuinely shared. |
| XML Def bridge / custom Defs | Implemented | `source/Defs/` | `Defs/**` | n/a | depends on system | depends on system | Add new Def/DefModExtension C# here only when XML needs new fields. |
| Shared UI framework | Implemented | `source/UI/Shared/` | n/a | `Textures/UI/**` plus procedural widgets | all custom UI | n/a | Reuse widgets/styles; do not invent parallel UI styles. Custom scroll regions should use the shared Abyssal scrollbar wrapper rather than raw vanilla scrollbars. |
| Abyssal Forge UI | Implemented | `source/UI/Forge/`, `source/Forge/` | `Defs/ThingDefs/`, `Defs/RecipeDefs/`, `Defs/ResearchProjectDefs/` | `Textures/UI/**`, forge building/item textures | Forge compact ITab + full Forge/Communion Console | residue, unlocks, recipes, boss drops | New forge content should normally surface in custom Forge UI. |
| Forge residue/sintering/progression | Implemented / Partial | `source/Forge/`, `source/Forge/Recipes/`, `source/Forge/MapComponents/`, `source/World/Buildings/Forge/`, `source/Progression/`, `source/Defs/Common/ABY_ResidueSinteringExtension.cs` | `Defs/RecipeDefs/`, `Defs/ThingDefs/`, `Defs/WorkGiverDefs/`, `Defs/PawnKindDefs/`, `Patches/ABY_ResidueSinteringCrucible_RecipeUsers.xml` | forge/crucible textures | Forge UI where relevant | residue and attunement progression | Non-boss abyssal enemy residue values should live on PawnKindDefs through `ABY_ResidueSinteringExtension`; legacy C# fallback is only for save/compatibility safety. Corpse processing/yield behavior still needs in-game verification. |
| Summoning Circle UI | Implemented | `source/UI/Summoning/`, `source/Summoning/`, `source/Summoning/ABY_SummonPreflightReport.cs` | `Defs/ThingDefs/`, `Defs/JobDefs/`, `Defs/Misc/ABY_ArrivalManifestationProfiles.xml` | `Textures/UI/**`, circle/VFX textures, sigil textures | Summoning compact ITab + full Summoning Console | sigils, modules, capacitors, instability | Important ritual actions should not be left as vanilla-only gizmos if they belong in the console. Full console now uses threat archetype tabs, a main ritual/control/preview row, and lower diagnostics columns; keep labels short and tooltip secondary text to avoid clipping. |
| Sigils / ritual activation / jobs | Implemented / Reliability-hardened | `source/Summoning/`, `source/Summoning/ABY_SummonPreflightReport.cs`, `source/Summoning/Jobs/`, `source/Summoning/Comps/`, `source/Summoning/MapComponents/MapComponent_ABY_SummonEncounterRuntime.cs`, `source/World/Buildings/Summoning/Building_AbyssalSummoningCircle.cs`, `source/World/Buildings/Summoning/Building_ABY_SigilVault.cs` | `Defs/ThingDefs/`, `Defs/JobDefs/`, `Defs/Misc/ABY_EncounterTemplates.xml`, `Defs/Misc/ABY_HordeSigil_EncounterTemplates.xml`, `Defs/Misc/ABY_BossDifficultyProfiles.xml`, `Defs/Misc/ABY_ThreatDoctrines.xml` | `Textures/`, `Sounds/ABY/ABY_AbyssSigilDrum.ogg`, `Sounds/ABY/ABY_BellmetalRookery.ogg` | Summoning Console | sigil possession, boss progression, resource cost | Delay/activation timing and pawn carrying behavior should be smoke-tested after code changes. The shared preflight report must be used by UI, direct use, and start paths; runtime lifecycle records may clear only after concrete blockers disappear, never by elapsed-time authorization. `ABY_HexgunRelaySigil` is legacy/migration-only and must stay out of Sigil Vault/active ritual routing unless deliberately reimplemented. Direct miniboss summons should use local boss-anchor support spawn with fallback, not detached edge-only packs. |
| Encounter validation / shadow planning | Implemented / Diagnostic | `source/Encounters/ABY_EncounterValidationUtility.cs`, `source/Encounters/ABY_EncounterShadowPlannerUtility.cs`, `source/Encounters/AbyssalEncounterDirectorUtility.cs`, `source/World/Buildings/Summoning/Building_AbyssalSummoningCircle.cs` | `Defs/Misc/ABY_EncounterTemplates.xml`, `Defs/Misc/ABY_ThreatDoctrines.xml`, `Defs/Misc/ABY_BossEscalationPackages.xml`, `Defs/PawnKindDefs/` | n/a | mod settings diagnostics + diagnostics window/log | none; diagnostic-only | Validator checks encounter data consistency and startup-sensitive turret module defs as diagnostic-only data. Shadow planning logs legacy-vs-directed comparisons only when enabled and must never alter real spawns. |
| Arrival manifestations / summon VFX | Implemented | `source/Summoning/VFX/`, `source/Summoning/`, `source/World/Buildings/Manifestations/`, `source/World/Buildings/Summoning/Building_AbyssalSummoningCircle.cs` | `Defs/Misc/ABY_ArrivalManifestationProfiles.xml`, `Defs/ThingDefs/` | manifestation/VFX sheets, motes | Summoning Console preview if applicable | tied to encounter type | Avoid generic portal presentation when the intended design is seam/bloom/static phase-in. Low/mid-tier direct sigils route ritual-specific pulses/VFX through `DoRitualSpecificArrivalPresentation`. |
| Protocol Nexus / custom research | Experimental / Active | `source/Experimental/ProtocolResearch/`, `source/Progression/` | `Defs/Experimental/ProtocolResearch/`, `Defs/ResearchProjectDefs/` | `Textures/UI/ABY/ProtocolResearch/` | Protocol Nexus UI | protocol segments, research gates, forge/summon relation | Treat as real active surface but expect layout/visual iteration. `ABY_PR_SaintAegisEngineering` owns current Reactor Saint reward engineering gates; avoid routing live recipes to futureReserve nodes. |
| Vanilla-style research tab | Implemented / Compact | `source/Progression/` | `Defs/ResearchProjectDefs/`, `Defs/ResearchTabDefs/` | research icons/textures | vanilla research UI + Protocol Nexus where relevant | early unlocks and indirect gates | Research is not the only progression path; summon/forge/boss gates remain central. |
| Boss shared framework | Implemented | `source/Bosses/Shared/` | `Defs/Misc/ABY_BossBarProfiles.xml`, `Defs/Misc/ABY_BossDifficultyProfiles.xml`, `Defs/Misc/ABY_BossEscalationPackages.xml`, `Defs/Misc/ABY_BossSelectionProfiles.xml` | boss bar textures, boss music | Boss Bar UI | boss phase/progression | Use shared utilities before adding boss-specific duplicate logic. |
| Boss Bar / phase UI / Aegis display / miniboss HP bars | Implemented / Fragile | `source/UI/BossBar/`, `source/Bosses/Shared/`, `source/Bosses/ReactorSaint/`, `source/Core/Utilities/ABY_AbyssalPawnClassificationUtility.cs` | `Defs/Misc/ABY_BossBarProfiles.xml`, `Defs/PawnKindDefs/` | `Textures/UI/ABY/BossBar/` | Boss Bar UI plus overhead miniboss bars | boss/miniboss state | High regression risk: clipping, phase display, chain/aegis placement, dynamic HP fill. Minibosses with `CompABY_BossTrueDeath` should use compact overhead bars instead of the full cinematic boss HUD unless deliberately promoted to a major boss. Explicit `isMiniBoss=true` classification must override legacy `role=boss` scaling values so Warden/Choir-style boss-family encounter plumbing does not suppress the overhead bar. The active draw call belongs in `AbyssalBossScreenFXGameComponent` so existing saves receive the UI without requiring a newly-added GameComponent instance. |
| Archon Beast / Archon encounter | Implemented | `source/Bosses/Archon/`, `source/Bosses/Archon/Comps/`, `source/Bosses/Archon/VFX/` | `Defs/PawnKindDefs/`, `Defs/ThingDefs/`, `Defs/HediffDefs/`, `Defs/SoundDefs/` | Archon pawn textures, portal/VFX, boss music | Boss Bar, Summoning | first boss / rupture branch | Directional sprites should use south/east/north; west is mirrored. |
| Archon of Rupture / Rupture Crown | Implemented / Partial | `source/Bosses/Rupture/`, `source/Bosses/Rupture/Comps/`, `source/Bosses/Rupture/Hediffs/` | `Defs/AbilityDefs/`, `Defs/HediffDefs/`, `Defs/ThingDefs/`, `Defs/DamageDefs/` | crown/halo/VFX/audio | Boss Bar and item/ability UI | secret/branch content | Check halo/crown visuals, ability wiring, and compatibility with boss cleanup logic. |
| Reactor Saint | Implemented / Fragile | `source/Bosses/ReactorSaint/`, `source/Bosses/ReactorSaint/Comps/`, `source/Bosses/ReactorSaint/VFX/` | `Defs/PawnKindDefs/`, `Defs/ThingDefs/`, `Defs/HediffDefs/`, `Defs/Misc/` | Reactor Saint pawn/cocoon/projectile/VFX/audio | Boss Bar + Aegis display | second major boss | High regression risk: AI targeting, Aegis state, cocoon presentation, bleeding/downed/death behavior. |
| Dominion pocket / Dominion slice | Implemented / Fragile | `source/Dominion/`, `source/Dominion/Generation/`, `source/Dominion/MapComponents/`, `source/Dominion/WorldObjects/`, `source/World/Buildings/Dominion/` | `Defs/MapGeneratorDefs/`, `Defs/TerrainDefs/`, `Defs/WorldObjectDefs/`, `Defs/ThingDefs/` | `Textures/Things/Building/DominionSlice/`, terrain/VFX textures | usually not direct UI; may have console links later | Dominion/late-game/hell dimension | High save/load and cleanup risk. Verify map transfer, pocket cleanup, collapse, heart/anchor graphics. |
| Dominion visuals/atmosphere/collapse | Implemented / Partial | `source/Dominion/VFX/`, `source/Dominion/MapComponents/` | `Defs/ThingDefs_Motes/`, `Defs/ThingDefs/` | terrain, weather, VFX, heart/anchor/platform art | n/a | Dominion presentation | Platforms must be underlays; heart/anchors must not be visually replaced by platform art. |
| Abyssal pawn classification / physiology | Implemented | `source/Core/Utilities/ABY_AbyssalPawnClassificationUtility.cs`, `source/Hediffs/ABY_AbyssalConstructPhysiologyUtility.cs`, `source/Defs/Common/ABY_AbyssalPawnClassificationExtension.cs` | `Defs/PawnKindDefs/` | pawn directional textures | Bestiary / combat UI where relevant | encounter roles, boss protection, construct bleeding rules, residue eligibility | New enemies should carry XML classification instead of adding fresh local C# name lists. Construct-like pawns should set `constructPhysiology` and `blockBloodLoss`. |
| Enemy pawn framework | Implemented | `source/Pawns/`, `source/Pawns/Comps/`, `source/Pawns/DeathActions/`, `source/Pawns/MapComponents/` | `Defs/PawnKindDefs/`, `Defs/ThingDefs/`, `Defs/HediffDefs/` | pawn directional textures | Bestiary / inspect / combat UI | encounter templates and pawn pools | AI loop guard, anti-tame/animal workflow, hostile auto behavior need careful testing. Current ordinary roster includes `ABY_CrownlessAdjudicator` as the common T4 severe-tier lane-punisher between Gate Warden/Halo Husk and T5 heavy units. |
| Generic comps / combat comps | Implemented | `source/Comps/`, `source/Comps/Properties/`, `source/Combat/Comps/` | `Defs/ThingDefs/`, `Defs/HediffDefs/` | mixed | inspect/UI when relevant | system-specific | Avoid expanding monolithic `source/Comps/` when a narrower module owns the new comp. |
| Weapon projectiles | Implemented | `source/Combat/Projectiles/Weapons/`, `source/Combat/Projectiles/Turrets/`, `source/Combat/Projectiles/Bosses/`, `source/Combat/VFX/`, `source/Combat/Verbs/` | `Defs/ThingDefs/`, `Defs/ThingDefs_Motes/`, `Defs/DamageDefs/`, `Defs/SoundDefs/` | projectile/mote/VFX/audio | combat feedback | weapon recipes/forge unlocks | Class names in XML must match compiled DLL. Rebuild required for new projectile C#. |
| Modular turrets | Implemented / Expanding | `source/Defs/Turrets/`, `source/Comps/CompAbyssalModularTurret.cs`, `source/UI/Turrets/`, `source/Combat/Projectiles/Turrets/`, `source/Combat/VFX/`, `source/Core/Misc/ABY_ModularTurretUtility.cs` | `Defs/ThingDefs/`, `Defs/Misc/*TurretModuleDef.xml`, `Defs/RecipeDefs/` | `Textures/Things/Item/TurretModules/`, turret overlay/projectile/VFX textures, sounds | turret ITab/module UI + Forge recipes | forge/residue/gating | Large growth area; each new module must wire item, module def, recipe, texture, localization, and UI stat exposure. Weapon modules may also require projectile/VFX/audio. Passive modules can use signed power draw, range/min-range, cooldown, damage, and throttled target-priority fields. |
| Apparel / armor / Aegis | Implemented / Fragile | `source/Apparel/`, `source/Apparel/Comps/`, `source/Apparel/Stats/` | `Defs/ThingDefs/`, `Defs/RecipeDefs/`, `Defs/HediffDefs/`, `Patches/ABY_ApparelAegis_*` | `Textures/Apparel/` | apparel info cards, gizmo/status | Forge recipes/unlocks | Directional overlay/body type restrictions are easy to break. Include all body types/directions when required. |
| Implants / hediffs / abilities | Implemented / Partial | `source/Hediffs/`, `source/Hediffs/Comps/`, `source/Bosses/Rupture/Comps/`, `source/Progression/` | `Defs/HediffDefs/`, `Defs/AbilityDefs/`, `Defs/RecipeDefs/` | implant/item icons | info cards, ability UI | Forge/research/boss drop gates | Need accurate stat/part efficiency presentation and surgery/recipe wiring. |
| Bestiary / lore codex | Implemented / Current roster covered | `source/UI/Bestiary/` | `Defs/ThingDefs/`, `Defs/PawnKindDefs/`, `Languages/` | pawn portraits through existing pawn textures | Bestiary window | encounter/progression rewards | Tracks current hostile PawnKind races through kind/race fallback. New hostile PawnKinds should get a Bestiary entry unless they intentionally resolve through an existing tracked race, plus EN/RU keyed text. |
| Audio/music/SFX | Implemented / Expanding | `source/Audio/`, `source/Summoning/ABY_SigilEncounterMusicUtility.cs`, `source/Bosses/Shared/ABY_BossMusicUtility.cs` | `Defs/SoundDefs/`, `Defs/SongDefs/` | `Sounds/ABY/` | combat/summon/boss feedback | encounter type / weapon def | Short SFX prefer WAV; music/ambience prefer OGG. Verify decode/load. |
| Localization | Implemented / Always required | `Docs/LOCALIZATION_GLOSSARY_RU.md` for Russian terminology | `Languages/English/`, `Languages/Russian/`, future `Languages/<lang>/` | n/a | all UI/defs | all systems | Add or update keys when adding UI text, defs, labels, descriptions, letters, alerts. For Russian, check the glossary before translating named content or UI categories. |
| Asset generation/integration | Implemented workflow | n/a | XML texPath users | `Textures/`, `SourceAssets/`, `Sounds/` | UI/game rendering | all systems | Source images may use green chromakey; final mod textures should be transparent PNG where appropriate. |
| Compatibility / hotfix layer | Implemented | `source/Compatibility/`, `source/Patches/`, `Patches/` | compatibility XML patches | n/a | n/a | modpack stability | Do not treat hotfix/compat files as primary architecture unless they are actually the owner of behavior. |
| Melee Animation compatibility data | Implemented | n/a | `WeaponTweakData/*.json` | melee weapon textures under `Textures/Things/Weapon/` | external Melee Animation editor/runtime | melee weapon `ThingDef`s | Root-level JSON data consumed by Melee Animation. Add/update one entry for each new Abyssal melee weapon to avoid missing tweak-data warnings. |
| Diagnostics/dev tools | Implemented | `source/Diagnostics/`, `source/Diagnostics/UI/` | possibly debug/dev defs | n/a | diagnostics windows/dev gizmos | dev-only | Useful for testing; should not leak into player-facing progression unless intended. |
| Legacy/migration | Implemented | `source/Legacy/` | legacy patches/old defs if present | n/a | n/a | save compatibility | Do not expand as active gameplay unless explicitly working on migration/cleanup. |

## XML category matrix

| XML folder | Current role | When to edit |
| --- | --- | --- |
| `Defs/AbilityDefs/` | Ability defs, especially Rupture/Crown-related abilities. | New ability, implant ability, boss ability. |
| `Defs/DamageDefs/` | Custom damage types. | New projectile/melee/status damage behavior. |
| `Defs/Experimental/ProtocolResearch/` | Protocol Nexus building/categories/projects/jobs. | Protocol Nexus progression/UI changes. |
| `Defs/FactionDefs/` | Abyssal faction definition. | Faction identity/diplomacy/pawn pool changes. |
| `Defs/HediffDefs/` | Implants, boss states, status effects, special conditions. | Any hediff, implant, mark, aura, disease, tether. |
| `Defs/IncidentDefs/` | Storyteller/incident entries. | New incidents or story events. |
| `Defs/JobDefs/` | Job definitions for custom jobs. | New job driver or job behavior. |
| `Defs/MapGeneratorDefs/` | Dominion slice map generation. | Dominion map layout changes. |
| `Defs/Misc/` | Profiles/templates/config defs. | Boss profiles, difficulty, encounter templates, arrival profiles, turret module defs. |
| `Defs/PawnKindDefs/` | Enemy/boss pawn kinds. | New enemies, boss pawns, escort groups, pawn stat/gear changes. |
| `Defs/RecipeDefs/` | Forge, apparel, implants, turrets, items, processing. | New craftable content or recipe balance. |
| `Defs/ResearchProjectDefs/` | Vanilla research nodes and related gates. | Research/unlock changes. |
| `Defs/ResearchTabDefs/` | Research tab config. | Research UI/tab placement. |
| `Defs/SongDefs/` | Music definitions. | Boss/sigil music changes. |
| `Defs/SoundDefs/` | SFX definitions. | Any new or changed sound. |
| `Defs/TerrainDefs/` | Dominion terrain. | Hell/Dominion map visuals/gameplay. |
| `Defs/ThingDefs/` | Main item/building/pawn/projectile defs. | Most game content. |
| `Defs/ThingDefs_Motes/` | Mote/VFX defs. | Projectiles, impacts, beams, overlays. |
| `Defs/WorkGiverDefs/` | Work givers. | New workbench/building jobs. |
| `Defs/WorldObjectDefs/` | World objects. | Dominion sites/world map content. |

## New content checklist

When adding new content, fill this mentally or directly in this file if it is a major feature.

### New weapon

```text
Defs/ThingDefs/               weapon ThingDef + projectile if needed
Defs/RecipeDefs/              recipe / forge recipe
Defs/DamageDefs/              only if new damage type is needed
Defs/SoundDefs/               fire/impact/charge sounds
Defs/ThingDefs_Motes/         muzzle/impact/beam VFX
source/Combat/Projectiles/    if custom projectile class
source/Combat/VFX/            if custom VFX utility
source/Combat/Verbs/          if custom verb
Textures/                     weapon icon/projectile/VFX
WeaponTweakData/              add/update JSON if the weapon is melee and should be compatible with Melee Animation
Sounds/                       audio
Languages/                    labels/descriptions
Forge UI / Protocol gates     check exposure/unlock
Assemblies/                   include DLL only if C# build verified
Docs                           update if new pattern/system
```

### New modular turret module

```text
Defs/Misc/                    ABY_*_TurretModuleDef.xml
Defs/ThingDefs/               module item, projectile, motes
Defs/RecipeDefs/              forge recipe
source/Defs/Turrets/          only if module data model changes
source/Combat/Projectiles/Turrets/
source/Combat/VFX/
source/UI/Turrets/            only if UI behavior changes
Textures/                     overlay/projectile/mote/icon
Sounds/                       fire/impact/charge if needed
Languages/                    module/item/projectile/recipe text
Forge UI                      recipe/unlock visibility
Turret ITab                   module socket behavior
Docs                           update if module framework changes
```

### New ordinary hostile pawn

```text
Defs/PawnKindDefs/            PawnKind with difficulty scaling, residue sintering, classification, encounter pools
Defs/ThingDefs/               race/body/stats/tools plus enemy-only weapon/projectile if needed
source/UI/Bestiary/           add entry when the race should get its own codex card
Textures/Pawn/<Enemy>/        south/east/north only; west is mirrored from east
Textures/Things/Weapon/       enemy-only weapon texture if forcedPrimaryDefName points to a new gun
Textures/Things/Projectile/   projectile texture if a new projectile Def is added
Languages/                    EN/RU DefInjected + Bestiary keyed text
Assemblies/                   include DLL only if Bestiary/source changed and build is verified
Docs                          update recent work/content matrix for substantial roster additions
```

### New boss or miniboss

```text
Defs/PawnKindDefs/            pawnkind
Defs/ThingDefs/               race/body/weapon/projectile/manifestation
Defs/HediffDefs/              phases, marks, buffs, special states
Defs/Misc/                    boss bar profile, difficulty profile, escalation package
Defs/SoundDefs/ and SongDefs/ boss SFX/music
source/Bosses/<BossName>/     boss-specific runtime
source/Bosses/Shared/         only for shared framework additions
source/UI/BossBar/            only for boss bar/Aegis/phase presentation
source/Summoning/             sigil/arrival integration
source/Pawns/                 shared AI if needed
source/Combat/                projectiles/VFX
Textures/                     pawn directions, boss bar icon, VFX
Languages/                    labels/descriptions/letters
Summoning Console             ritual preview/exposure
Bestiary                      if supported
Docs                           update architecture/recent work/content matrix
```

### New Dominion feature

```text
source/Dominion/
source/Dominion/MapComponents/
source/Dominion/VFX/
source/World/Buildings/Dominion/
Defs/ThingDefs/
Defs/ThingDefs_Motes/
Defs/MapGeneratorDefs/
Defs/TerrainDefs/
Defs/WorldObjectDefs/
Textures/Things/Building/DominionSlice/
Textures/Terrain/
Textures/Effects/
Languages/
Docs/KNOWN_RISKS_AND_REGRESSIONS.md
```

Dominion changes should be treated as high-risk because of map transfer, cleanup, save/load, and visual layer interactions.

### New UI surface

```text
source/UI/<Surface>/
source/UI/Shared/
Textures/UI/ABY/<Surface>/
Languages/
Docs/AI_ARCHITECTURE.md
Docs/AI_QUICK_INDEX.md
Docs/RECENT_WORK.md
Docs/CONTENT_MATRIX.md
```

Prefer extending existing Forge/Summoning/Protocol/BossBar surfaces over creating a new one.

## Current high-growth areas

These are likely to expand heavily and should stay structured:

| Area | Why it grows | Control strategy |
| --- | --- | --- |
| Modular turrets | many modules, projectiles, VFX, recipes | one module = predictable XML/source/asset/localization bundle |
| Forge patterns | all weapons/armor/implants/turrets pass through progression | keep Forge UI and recipes synchronized |
| Summoning sigils | encounter catalog grows with bosses/hordes/minibosses | keep Summoning Console preview/state readable |
| Protocol Nexus | research/progression categories may expand | keep categories/data-driven and avoid hardcoded layouts where possible |
| Dominion | visuals, mapgen, runtime state, crisis content | separate mapgen/runtime/VFX/world object ownership |
| Bosses | each boss adds AI/state/UI/VFX/audio/progression | use boss shared framework and profiles |

## Documentation update rule

Update this file when:

```text
- a new major content category appears;
- a new module folder is created;
- a system ownership path changes;
- a recurring integration checklist changes;
- a content batch creates a pattern future work should follow.
```

Do not update it for isolated balance or typo changes unless the matrix would become misleading.


## Modular turret localization ownership

Custom turret module definitions in `Defs/Misc/ABY_TurretModuleDefs.xml` are not standard ThingDefs. Their player-facing fields must be localized through:

```text
Languages/English/DefInjected/ABY_TurretModuleDef/
Languages/Russian/DefInjected/ABY_TurretModuleDef/
```

Module item labels/descriptions still belong to `Languages/<Lang>/DefInjected/ThingDef/`, and crafting text belongs to `Languages/<Lang>/DefInjected/RecipeDef/`. Keep all three layers synchronized so Forge cards, item info, and recipe bills do not drift.


## Optimization and release tooling

| Area | Files | Purpose | Notes |
| --- | --- | --- | --- |
| Texture budget audit | `Tools/ABY_TextureAudit.py`, `Tools/texture_budget_rules.json` | Reports texture payload, VRAM estimates, and warning/manual-review candidates | Audit-only by default. |
| Safe texture optimizer | `Tools/ABY_OptimizeTextures.py`, `Tools/texture_budget_rules.json` | Applies only safe/whitelisted auto-resize rules | Boss/hero and manual-review categories are never auto-resized. |
| Release packager | `Tools/ABY_BuildReleasePackage.py`, `Tools/texture_budget_rules.json` | Builds a clean playable release zip without `SourceAssets/`, tools, temp files, or build output | Does not compile C#. |
| In-game performance settings | `source/Core/Bootstrap/AbyssalProtocolModSettings.cs`, `source/Core/Bootstrap/ABY_PerformanceSettingsUtility.cs` | Visual intensity presets and optional Dominion ambient VFX control | Presentation-only; must not alter gameplay. |
| Performance audit window | `source/Diagnostics/ABY_PerformanceAuditUtility.cs`, `source/Diagnostics/UI/Window_ABY_PerformanceAudit.cs` | Dev/testing snapshot for map counts, Abyssal counts, and performance toggles | Open from mod settings diagnostics/performance area. |

## Runtime performance matrix — 2026-05-19

| System | Status | Source owner | XML/assets | UI exposure | Notes |
| --- | --- | --- | --- | --- | --- |
| Shared runtime target cache | Implemented | `source/Core/Runtime/ABY_RuntimeTargetCache.cs` | n/a | n/a | Reuse for broad pawn/thing targeting or scan-heavy runtime systems. Keeps gameplay validation in callers. |
| Shared VFX budget | Implemented | `source/Combat/VFX/ABY_VfxBudget.cs` | n/a | Performance settings affect it indirectly | Optional presentation only. Do not gate damage, targeting, rewards, or save/load behavior on this budget. |
| TPS optimized hot loops | Implemented / ongoing | `source/Compatibility/`, `source/Apparel/`, `source/Combat/`, `source/Bosses/Rupture/`, `source/Comps/`, `source/Dominion/` | mass enemy scan intervals in `Defs/ThingDefs/` | Forge/Summoning/shared UI should respect reduced animation | Future combat/Dominion additions should be checked against this matrix before adding new timers. |


## Remaining TPS optimization matrix — 2026-05-19

| System | Status | Source owner | XML/assets | UI exposure | Notes |
| --- | --- | --- | --- | --- | --- |
| Thing-ID runtime lookup cache | Implemented | `source/Core/Runtime/ABY_RuntimeTargetCache.cs` | n/a | n/a | Replaces repeated delayed-target `AllThings` scans. Cache can be stale briefly; callers must still validate spawned/destroyed state. |
| Projectile trail VFX budgeting | Implemented | `source/Combat/Projectiles/Weapons/`, `source/Combat/VFX/ABY_VfxBudget.cs` | projectile textures/flecks unchanged | Performance settings only | Presentation-only gate for repeated trail/spark/arc ticks. Damage and impact logic must remain outside the budget. |
| Reactor Saint projectile presentation budget | Implemented | `source/Bosses/ReactorSaint/VFX/ABY_ReactorSaintProjectileVfxUtility.cs` | Reactor Saint projectile visuals unchanged | Performance settings only | Uses shared VFX budget rather than an isolated local budget. |
| Anti-tame / animal workflow throttling | Implemented | `source/Pawns/MapComponents/`, `source/Pawns/ABY_AntiTameUtility.cs`, `source/Compatibility/ABY_LargeModpackHotfixBUtility.cs` | n/a | n/a | Longer intervals and runtime caches reduce baseline compatibility-tax. |
| Protocol Nexus UI cache | Implemented | `source/Experimental/ProtocolResearch/ABY_ProtocolResearchUtility.cs`, `Window_AbyssalProtocolNexus.cs` | n/a | Protocol Nexus | Cached project/category/header data prevents repeated sorting/filtering in draw paths. |
| Dominion ambient/edge/collapse VFX budget | Implemented | `source/Dominion/MapComponents/` | Dominion VFX assets unchanged | Performance settings only | Optional spectacle is reduced under budget pressure; reward/extraction gameplay must not depend on skipped visuals. |

## Runtime safety utility — hidden faction hostility

| System | Source owner | Purpose | Integration notes |
|---|---|---|---|
| Hidden faction hostility safety | `source/Core/Runtime/ABY_FactionHostilityUtility.cs` | Prevents red `Faction ... has null relation with PlayerColony` errors when ABY hidden/generated factions are checked by target scans, boss aggression, turrets, auras, or projectiles. | Use `SafeHostileTo(...)` in ABY hot paths instead of direct vanilla `HostileTo(...)` whenever ABY pawns/factions or generated hidden factions can be involved. |

## Modular turret threat/aggro integration — 2026-05-19

| System | Status | Source owner | XML/assets | UI exposure | Notes |
| --- | --- | --- | --- | --- | --- |
| Modular turret combat-building aggro | Implemented | `source/Core/Runtime/ABY_RuntimeTargetCache.cs`, `source/Encounters/AbyssalThreatPawnUtility.cs`, `source/Pawns/ABY_AbyssalMonsterBrain.cs` | `Defs/ThingDefs/ABY_ModularTurrets.xml` | Modular turret install UI/gizmos unchanged | Abyssal enemies now see player modular turrets with installed main weapon modules as hostile combat buildings. |
| Ranged Abyssal building targeting | Implemented | `source/Comps/CompHexgunThrallShooter.cs`, `source/Comps/CompABY_RiftSapperShooter.cs`, `source/Comps/CompABY_SiegeIdolSiegeShooter.cs` | Enemy pawn XML unchanged | n/a | Hexgun-style shooters, Rift Sappers, and Siege Idols can consider cached combat buildings without reintroducing broad `AllThings` scans. |

## Structure targeting safety — 2026-05-19

| System | Status | Source owner | XML/assets | UI exposure | Notes |
| --- | --- | --- | --- | --- | --- |
| Hidden/passive utility structure filter | Implemented | `source/Encounters/AbyssalThreatPawnUtility.cs` | n/a | n/a | Filters hidden conduits/cables/wires/invisible passive utility buildings out of Abyssal monster, boss, and special structure-damage targeting while preserving walls, doors, barricades, barriers, and turrets as valid targets. |
- Protocol Nexus decode now applies worker Intellectual-based speed through Building_ABY_ProtocolNexus.NotifyDecodeWorkTick(Pawn) and ABY_ProtocolResearchUtility.ResolveDecodeWorkPerTick(Pawn).

## 2026-05-20 — Projectile safety integration note

| System | Status | Source owner | XML/assets | UI exposure | Gating | Integration / regression note |
| --- | --- | --- | --- | --- | --- | --- |
| Projectile impact safety | Implemented / compatibility layer | `source/Combat/Projectiles/ABY_ProjectileImpactSafetyUtility.cs`, `source/Combat/VFX/ABY_ProjectileProcUtility.cs`, custom projectile classes under `source/Combat/Projectiles/` | Projectile `ThingDef`s in `Defs/ThingDefs/`, VFX in `Defs/ThingDefs_Motes/`, sounds in `Defs/SoundDefs/` | none | weapon/turret/boss-specific | All custom projectile `Impact(...)` overrides that invoke vanilla impact/damage should use the shared safety utility, especially in weapon, turret, and boss projectile classes. |


## Passive modular turret modules — 2026-05-20

| Area | Status | Source / XML | Assets | UI exposure | Notes |
| --- | --- | --- | --- | --- | --- |
| 12 passive turret module set | Implemented | `source/Defs/Turrets/ABY_TurretModuleDef.cs`, `source/Comps/CompAbyssalModularTurret.cs`, `Defs/Misc/ABY_TurretModuleDefs.xml`, `Defs/ThingDefs/ABY_TurretModules.xml`, `Defs/RecipeDefs/ABY_ModularTurretRecipes.xml` | `Textures/Things/Item/TurretModules/ABY_TurretModule_*.png` | Forge Turret Systems cards, turret ITab, item info cards | Passive module tiers are module-owned, not chassis-owned. The current chassis stays a platform; installed passive modules modify runtime stats and targeting behavior. |

## Passive turret aegis modules — 2026-05-20
| System | Status | Source | Defs/assets | UI exposure | Notes |
| --- | --- | --- | --- | --- | --- |
| Passive turret aegis modules | Implemented / needs smoke test | `source/Comps/CompAbyssalModularTurret.cs`, `source/Defs/Turrets/ABY_TurretModuleDef.cs`, `source/UI/Turrets/ITab_AbyssalTurretModules.cs`, `source/Comps/CompABY_TurretModuleInfoCard.cs` | `Defs/Misc/ABY_TurretModuleDefs.xml`, `Defs/ThingDefs/ABY_TurretModules.xml`, `Defs/RecipeDefs/ABY_ModularTurretRecipes.xml`, `Textures/Things/Item/TurretModules/` | turret inspect string, ITab stat text, module info card | Shield modules add `turretShieldMax`, recharge-per-tick and recharge-delay fields. They absorb incoming damage before damage multipliers and recharge only while operational. |

### Modular turret Aegis passive modules

- Source owner: `source/Comps/CompAbyssalModularTurret.cs`.
- Def owner: `Defs/Misc/ABY_TurretModuleDefs.xml` and matching module ThingDefs/recipes.
- Localization owner: `Languages/*/Keyed/ABY_ModularTurrets_Strings.xml`.
- Integration rule: only one Aegis/passive shield module is allowed per turret; the status gizmo is read-only and uses the shared `Gizmo_ABY_AegisStatus` presentation.


### T5 passive turret module expansion

- System: Modular turret modules / Forge TurretSystems.
- Files: `Defs/Misc/ABY_T5PassiveTurretModuleDefs.xml`, `Defs/ThingDefs/ABY_T5PassiveTurretModules.xml`, `Defs/RecipeDefs/ABY_T5PassiveTurretModuleRecipes.xml`.
- Assets: `Textures/Things/Item/TurretModules/ABY_TurretModule_DominionVerdictLens.png`, `ABY_TurretModule_CrownOverpressureChoir.png`, `ABY_TurretModule_SepulcherFailSafeMantle.png`.
- Role coverage: T5 passive targeting, firing cadence, and chassis survival. These are intentionally not Aegis modules because Aegis stacking is blocked by the turret runtime.

## Crown Interdictor T4 melee weapon — 2026-06-22

| System | Status | Source ownership | Defs/assets | UI exposure | Gating/progression | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| Crown Interdictor | Implemented / needs runtime smoke test | `source/Combat/Comps/CompABY_CrownInterdictor.cs`, `source/Combat/DamageWorkers/DamageWorker_ABY_CrownInterdictorTrigger.cs` | `Defs/ThingDefs/ABY_CrownInterdictor.xml`, `Defs/DamageDefs/ABY_CrownInterdictor_DamageDefs.xml`, `Defs/HediffDefs/ABY_CrownInterdictor_Hediffs.xml`, `Textures/Things/Weapon/ABY_CrownInterdictor*.png`, `WeaponTweakData/ABY_CrownInterdictor_neversalimus.abyssalprotocol.json` | Existing Abyssal Forge weapon pattern and normal item InfoCard | `ABY_PR_CrownLogicDecoding`, 3400 Forge residue, Dominion Crown Shard, Crowned Core Fragment | T4 priority-target interceptor. Weapon-owned saved state records a writ on the first confirmed hit and resolves an Edict Lock on the second hit against the same living hostile pawn within three seconds. Normal targets receive a short lock plus flinch; bosses/minibosses receive a reduced debuff only. A shared Authority Scar blocks immediate squad re-locking. |

## Crown Scission Array T5 fast melee weapon — 2026-06-22

| System | Status | Source ownership | Defs/assets | UI exposure | Gating/progression | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| Crown Scission Array | Implemented / needs runtime smoke test | `source/Combat/Comps/CompABY_CrownScissionArray.cs`, `source/Combat/DamageWorkers/DamageWorker_ABY_CrownScissionEcho.cs` | `Defs/ThingDefs/ABY_CrownScissionArray.xml`, `Defs/DamageDefs/ABY_CrownScissionArray_DamageDefs.xml`, `Textures/Things/Weapon/ABY_CrownScissionArray*.png`, `WeaponTweakData/ABY_CrownScissionArray_neversalimus.abyssalprotocol.json` | Existing Abyssal Forge weapon pattern and normal item InfoCard | `ABY_PR_CrownLogicDecoding`, 5400 Forge residue, Reactor Saint Core, Crowned Core fragments, Dominion Crown Shard | T5 fast melee pack-reaper. A weapon-owned saved state stores up to three short-lived Echo charges after direct hostile kills. The next confirmed melee hit spends one charge on one other nearby living hostile pawn for a separate Cut hit. The target search is a bounded local radial lookup only; echoes cannot target the primary pawn, buildings or corpses, and cannot chain. |

## Dominion Breach Driver T5 melee weapon — 2026-06-22

| System | Status | Source ownership | Defs/assets | UI exposure | Gating/progression | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| Dominion Breach Driver | Implemented / needs runtime smoke test | `source/Combat/Comps/CompABY_DominionBreachDriver.cs`, `source/Combat/DamageWorkers/DamageWorker_ABY_DominionBreachSequence.cs` | `Defs/ThingDefs/ABY_DominionBreachDriver.xml`, `Defs/DamageDefs/ABY_DominionBreachDriver_DamageDefs.xml`, `Textures/Things/Weapon/ABY_DominionBreachDriver*.png`, `WeaponTweakData/ABY_DominionBreachDriver_neversalimus.abyssalprotocol.json` | Existing Abyssal Forge weapon pattern and normal item InfoCard | `ABY_PR_DominionSurvivalFrames`, 5200 Forge residue, Rift Butcher Severance Core, Crowned Core fragments, Dominion Crown Shard | T5 single-target pressure weapon. A weapon-owned saved sequence records three confirmed hits on one living pawn within seven seconds, then applies a separate high-penetration Blunt verdict hit. No map scans, no static combat cache, and no sequence progression on buildings/corpses. |

## Reactor Choir Minigun — 2026-05-23

| System | Status | Source ownership | Defs/assets | UI exposure | Gating/progression | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| Reactor Choir Minigun | Implemented / needs runtime smoke test | `source/Combat/Projectiles/Weapons/Projectile_ReactorChoirPlasmaSlug.cs`, `source/Combat/VFX/ReactorChoirMinigunVfxUtility.cs` | `Defs/ThingDefs/ABY_ReactorChoirMinigun.xml`, `Defs/HediffDefs/ABY_ReactorChoirMinigun_Hediffs.xml`, `Defs/ThingDefs_Motes/ABY_ReactorChoirMinigun_Motes.xml`, `Defs/SoundDefs/ABY_ReactorChoirMinigun_Sounds.xml`, `Textures/Things/Weapon/ABY_ReactorChoirMinigun.png`, `Textures/Things/Projectile/ABY_ReactorChoirPlasmaSlug.png`, `Textures/Things/VFX/ReactorChoirMinigun/` | Forge weapon pattern through existing Abyssal Forge UI | `ABY_PR_SaintAegisEngineering`, 5400 Forge residue, Reactor Saint Core, Horde Fragments | Heavy T5 sustained plasma weapon. Projectile uses budgeted muzzle/impact VFX and stacking thermal saturation; no map-wide scans or per-tick beam damage. Existing Ultra Plasma clips are reused through new SoundDefs until bespoke SFX are produced. |

## Dominion Gravplate T5 armor — 2026-05-23

| System | Status | Source ownership | Defs/assets | UI exposure | Gating/progression | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| Dominion Gravplate shell + helm | Implemented / needs runtime smoke test | Existing `source/Apparel/` hover + armor Aegis systems; no new C# | `Defs/ThingDefs/ABY_AbyssalGravplatePrototype_Placeholder.xml`, `Defs/RecipeDefs/ABY_AbyssalGravplatePrototype_Recipes.xml`, existing textures under `Textures/Things/Apparel/` and `Textures/Things/Item/Apparel/` | Abyssal Forge armor pattern, apparel info cards, worn Aegis gizmo/status, hover VFX while drafted | `ABY_DominionGateBootstrapping`, `ABY_PR_DominionSurvivalFrames`, 4200 Forge residue, Crowned Core Fragment / Dominion Crown Shard material gates | Reuses existing hover-apparel and armor Aegis runtime. The shell is intended as mobile T5 Dominion assault armor: lower Aegis capacity than Crowned Core plate, faster recharge, strong drafted mobility, and no external shield stacking. |

## Crafting requirement ownership

Crafting requirement balance spans both `RecipeDef` files and `ThingDef.costList + recipeMaker` items. Future economy audits must inspect both paths; many weapons and implants are generated from ThingDef recipeMaker rather than standalone RecipeDefs.

## Rift Butcher post-horde miniboss — 2026-05-22

| System | Status | Source | Defs/assets | UI exposure | Notes |
| --- | --- | --- | --- | --- | --- |
| Rift Butcher miniboss gate | Implemented / needs runtime smoke test | `source/Comps/CompABY_RiftButcherCombat.cs`, `source/Progression/ABY_HordeAndButcherProgressionGameComponent.cs`, `source/UI/Summoning/AbyssalSummoningConsoleUtility.cs`, `source/Summoning/AbyssalCircleCapacitorRitualUtility.cs`, `source/World/Buildings/Summoning/Building_AbyssalSummoningCircle.cs` | `Defs/ThingDefs/ABY_RiftButcher_Content.xml`, `Defs/PawnKindDefs/ABY_RiftButcher_PawnKinds.xml`, `Defs/HediffDefs/ABY_RiftButcher_Hediffs.xml`, `Defs/RecipeDefs/ABY_RiftButcher_Recipes.xml`, `Defs/Misc/ABY_BossDifficultyProfiles.xml`, `Defs/Misc/ABY_EncounterTemplates.xml`, `Defs/Misc/ABY_ThreatDoctrines.xml`, `Textures/Pawn/RiftButcher/`, `Textures/Things/Item/ABY_RiftButcher*` | Summoning Console ritual list, compact miniboss overhead HP bar via `CompABY_BossTrueDeath`, Sigil Vault staging | Sits after first Horde Gate clear and before Dominion Gate. Dominion Sigil crafting now requires `ABY_RiftButcherSeveranceCore` so the encounter cannot be skipped through residue alone. The summon now uses `ABY_BossProfile_RiftButcher`, `rift_butcher_escort`, and local boss-anchor escort spawning. |

## Runtime hardening matrix — 2026-05-22

| System | Status | Source ownership | Asset/Def impact | UI exposure | Gating/progression | Notes |
|---|---|---|---|---|---|---|
| Quantized material helper | Implemented | `source/Core/Utilities/ABY_MaterialCacheUtility.cs` | none | none | none | Use for pulse-driven draw/VFX material creation instead of direct `MaterialPool.MatFrom` calls with changing colors. |
| Safe spawn/transfer helper | Implemented / hardened | `source/Core/Utilities/ABY_SafeSpawnUtility.cs` | none | none | encounter/runtime safety | No `map.Center` fallback with `WipeMode.Vanish`; callers must handle failure/retry. |
| Horde/breach target-scan hardening | Implemented | `source/Comps/CompABY_BreachDirective.cs`, `source/Core/Runtime/ABY_RuntimeTargetCache.cs` | none | none | horde/encounter runtime | Avoid per-pawn `AllThings` scans; prefer bounded or cached target lists. |
| Specter Lash impact fallback | Implemented | `source/Combat/Projectiles/Weapons/Projectile_SpecterLashAnchor.cs` | existing projectile defs unchanged | combat VFX stream | weapon runtime | Impact fallback now scans radial cells rather than whole map thing lists. |


## Summon threat rehearsal dev gizmo

| System | Ownership / files | Runtime exposure | Notes |
|---|---|---|---|
| Summon threat rehearsal / preflight reliability dev gizmo | `source/Diagnostics/ABY_SummonThreatRehearsalUtility.cs`, `source/Summoning/ABY_SummonPreflightReport.cs`, hooked from `source/World/Buildings/Summoning/Building_AbyssalSummoningCircle.cs` | Dev Mode gizmo on Abyssal Summoning Circle only | Logs mode-aware payload routes, target cells, T1/T2 estimates, director allocations, boss escort profiles, horde plan summaries, Dominion runtime readiness, capacitor status, presentation routing, and a non-mutating coherent-preflight pass for every active ritual. Fallback directed plans are labeled explicitly. Force-start options consume no sigil and bypass progression/capacitor gates for diagnostics. If another encounter is active, a confirmation dialog is required before the dev-only bypass allows an overlapping non-singleton encounter. |

### Summoning Console detail routing

- Main Summoning Console: archetype tabs, ritual cards, compact preview, invocation controls, readiness/capacitor/module telemetry.
- Ritual dossier: expanded forecast, readiness breakdown, reward routing, side effects, and long horde/Dominion telemetry.
- New ritual or sigil categories should expose short combat identity on the main screen and detailed operational text through the dossier path.

## Summoning Console UI ownership update

- Primary file: `source/UI/Summoning/Window_AbyssalSummoningConsole.cs`.
- Main screen ownership: threat archetype tabs, ritual card list, selected ritual action card, compact circle infrastructure callout, and invocation actions.
- Long-form ritual information ownership: Summoning ritual dossier window nested in the same source file.
- Circle slot management ownership: Circle Infrastructure window nested in `Window_AbyssalSummoningConsole.cs`, simplified into one compact slot-manager screen with capacitor lattice, stabilizer ring, and effect summary sections.
- Localization ownership: `Languages/English/Keyed/ABY_SummoningConsoleRedesign_Strings.xml` and `Languages/Russian/Keyed/ABY_SummoningConsoleRedesign_Strings.xml` for redesign-specific labels.
- Safety note: keep selected-ritual actions inside the right action card and technical readiness/capacitor/stabilizer information inside the lower tabbed drawer.


## Summoning Circle infrastructure UI route — 2026-05-24

| System | Status | Source ownership | UI exposure | Notes |
| --- | --- | --- | --- | --- |
| Circle infrastructure window | Implemented / needs runtime smoke test | `source/UI/Summoning/Window_AbyssalSummoningConsole.cs` | Selected ritual card button: Circle Infrastructure / Open circle slots | Moves capacitor lattice and stabilizer ring management out of the main console into an explicit compact slot-management window similar in intent to turret module management, while preserving existing install/remove slot methods. Readiness remains in the main console/dossier rather than becoming a tab in this window. |
