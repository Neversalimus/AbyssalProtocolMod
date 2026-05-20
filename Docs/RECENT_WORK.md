## 2026-05-21 — Encounter validation hardening and turret Aegis gizmo

Fixed a load-time encounter validation warning path and added a read-only Aegis status gizmo for player modular turrets that have passive shield modules installed.

Changed behavior:
- `ABY_EncounterValidationUtility` now runs startup validation in isolated stages and uses guarded DefDatabase access, so a null or temporarily unavailable validation input reports a scoped warning instead of collapsing into the generic `Encounter validation failed: NullReferenceException` message.
- English/Russian `ABY_BreakCrown_*` keyed entries no longer contain empty translation values, removing a likely English translation-data error while preserving the secret mechanic being silent in code.
- `CompAbyssalModularTurret` now exposes a styled read-only `Gizmo_ABY_AegisStatus` card for player-owned turrets with passive Aegis modules, showing charge, state, restart delay and recharge tooltip.
- Added localized English/Russian turret Aegis gizmo strings.

Changed areas:

```text
Assemblies/AbyssalProtocol.dll
source/Encounters/ABY_EncounterValidationUtility.cs
source/Comps/CompAbyssalModularTurret.cs
Languages/English/Keyed/ABY_BreakCrown_Strings.xml
Languages/Russian/Keyed/ABY_BreakCrown_Strings.xml
Languages/English/Keyed/ABY_ModularTurrets_Strings.xml
Languages/Russian/Keyed/ABY_ModularTurrets_Strings.xml
Docs/CONTENT_MATRIX.md
Docs/KNOWN_RISKS_AND_REGRESSIONS.md
Docs/RECENT_WORK.md
```

Build verified with direct Roslyn compile against bundled RimWorld/Unity/Harmony/.NET Framework-style libraries. Runtime smoke testing in RimWorld is still required.


## 2026-05-21 — Runtime safety and large-modpack performance hardening

Applied a focused C# hardening pass for audit-reported runtime risks without changing content balance, XML defs, textures, or UI layout.

Changed behavior:
- `MapComponent_ABY_OblivionChoirScar` now caps active scars at 24, prunes expired scars before adding a new one, resolves saved instigators through `ABY_RuntimeTargetCache`, and stores faction/abyssal fallback data so lingering scars do not become factionless friendly-fire zones after the source pawn dies.
- `AbyssalEncounterDirectorUtility.GetCandidates` now caches candidate lists by encounter pool, base tier, allowed tier, and current difficulty profile, avoiding repeated full `PawnKindDef` scans in large modpacks. The case-insensitive list helper no longer allocates lowercase copies inside the comparison loop.
- `MapComponent_AbyssalProgressionHotfix.MoveThingSafely` now wraps despawn/spawn relocation in rollback logic so sigils or fogged portals are not lost if `GenSpawn.Spawn` throws after `DeSpawn`.
- Direct `ABY_ProtocolResearchGateUtility.IsDecoded("")` now fails closed while `IsDecodedForForge` still treats recipes with no protocol requirement as ungated.
- Dominion pocket victory sessions now track `victoryAchievedTick`; after a grace window, runtime maintenance attempts a safe auto-return for stuck pawns or finalizes reward/cleanup for orphaned victory pockets instead of extending extraction forever.

Changed areas:

```text
Assemblies/AbyssalProtocol.dll
source/Core/GameComponents/MapComponent_ABY_OblivionChoirScar.cs
source/Encounters/AbyssalEncounterDirectorUtility.cs
source/Compatibility/MapComponent_AbyssalProgressionHotfix.cs
source/Progression/ABY_ProtocolResearchGateUtility.cs
source/Dominion/ABY_DominionPocketSession.cs
source/Dominion/ABY_DominionPocketRuntimeGameComponent.cs
source/Dominion/AbyssalDominionPocketUtility.cs
source/Dominion/AbyssalDominionPocketSafeUtility.cs
source/Dominion/MapComponents/MapComponent_DominionSliceEncounter.cs
Docs/KNOWN_RISKS_AND_REGRESSIONS.md
Docs/RECENT_WORK.md
```

Build verified with direct Roslyn compile against bundled RimWorld/Unity/Harmony/.NET Framework-style libraries. The rebuilt assembly references `mscorlib` / `System.Core`, not .NET 9 reference assemblies. Runtime smoke testing in RimWorld is still required.


## 2026-05-21 — Execution Logic Core passive turret module

Added a Tier III passive turret targeting module that biases existing modular turret target scans toward wounded hostile pawns. The implementation uses additive scoring during the existing throttled scan path, not a new per-tick map scan.

Touched areas:
- `source/Defs/Turrets/ABY_TurretModuleDef.cs`
- `source/Comps/CompAbyssalModularTurret.cs`
- `Defs/Misc/ABY_TurretModuleDefs.xml`
- `Defs/ThingDefs/ABY_TurretModules.xml`
- `Defs/RecipeDefs/ABY_ModularTurretRecipes.xml`
- turret module localization and item icon assets

Follow-up smoke test: verify Forge visibility, installation into passive slots, and that wounded-but-still-valid hostile pawns are preferred without causing scan spikes.

# Abyssal Protocol — Recent Work Notes

## 2026-05-20 — Emergency rebuild for residue sintering extension load errors
- Rebuilt `Assemblies/AbyssalProtocol.dll` from the current `source/` tree after runtime logs showed repeated XML load failures for pawn kind defs referencing `AbyssalProtocol.ABY_ResidueSinteringExtension`.
- The C# source file already existed at `source/Defs/Common/ABY_ResidueSinteringExtension.cs`, but the shipped assembly did not expose the type, causing many red `Could not find type named AbyssalProtocol.ABY_ResidueSinteringExtension` errors during Def loading.
- The rebuild was performed with direct Roslyn compile against bundled RimWorld/Unity/Harmony/.NET Framework-style libraries, not .NET 9 reference assemblies.
- No gameplay XML, residue values, Forge UI behavior, or progression thresholds were changed; this is an assembly/source synchronization fix.

Changed areas:

```text
Assemblies/AbyssalProtocol.dll
Docs/KNOWN_RISKS_AND_REGRESSIONS.md
Docs/RECENT_WORK.md
```

Build verified by direct Roslyn compile and by confirming the rebuilt assembly metadata contains `ABY_ResidueSinteringExtension`. Runtime smoke testing in RimWorld is still required.


## 2026-05-20 — Large modpack predator and Melee Animation compatibility pass
- Disabled vanilla predator/food-hunting behavior on abyssal summoned animal-style hostile pawns and animal-style bosses by making the affected races hungerless and non-predatory: Ember Hound, Rift Imp, Archon Beast, Reliquary Archon Beast, and Archon of Rupture.
- This prevents summoned abyssal entities from creating vanilla predator letters such as `An ember hound is hunting <colonist> for food!` during horde/sigil combat. Their hostile behavior should remain driven by lords, think trees, comps, and encounter logic rather than food needs.
- Added root-level `WeaponTweakData/` JSON files for all currently detected Abyssal melee weapons so Melee Animation no longer reports missing tweak data for Abyssal Protocol.
- No C# files were changed in this pass; DLL rebuild is not required.

## 2026-05-20 — Emergency rebuild for monster info-card icon regression
- The previous monster info-card icon patch accidentally shipped a DLL compiled against .NET 9 reference assemblies. RimWorld/Mono then failed type discovery with `ReflectionTypeLoadException`, causing many XML class/type lookup red errors at load.
- Rebuilt `Assemblies/AbyssalProtocol.dll` against the bundled RimWorld/Unity/Harmony libraries and .NET Framework-style `mscorlib`/`System.Core` references only.
- Replaced the runtime icon normalizer with a no-op compatibility stub and moved monster info-card icon sizing into explicit ThingDef `uiIconScale` / `uiIconOffset` XML values.
- This keeps the visual fix while removing the risky startup/static-constructor path.

Changed areas:

```text
Assemblies/AbyssalProtocol.dll
source/UI/Shared/ABY_MonsterInfoCardIconNormalizer.cs
Defs/ThingDefs/ABY_* hostile pawn XML files
Docs/BUILD_AND_SOURCE_LAYOUT.md
Docs/KNOWN_RISKS_AND_REGRESSIONS.md
Docs/RECENT_WORK.md
```

Build verified with direct Roslyn compile using bundled `Libraries/mscorlib.dll`, `Libraries/System.dll`, `Libraries/System.Core.dll`, RimWorld `Assembly-CSharp.dll`, Unity modules, and Harmony. The rebuilt assembly references `mscorlib 4.0.0.0` / `System.Core 4.0.0.0`, not `System.Runtime 9.0.0.0`.

## 2026-05-19 — Monster info-card icon normalization
- Added a runtime normalization pass for hostile Abyssal pawn info-card icons so oversized pawn portraits no longer overlap or crowd the monster name in the vanilla info card header.
- All hostile Abyssal pawn ThingDefs now receive a unified `uiIconScale` based on their actual draw size, with a stricter ceiling for exceptionally large boss profiles.
- The normalization is automatic and Def-driven, so newly added hostile Abyssal monsters inherit the same clean info-card presentation without per-def hand tuning.

Changed area:

```text
source/UI/Shared/ABY_MonsterInfoCardIconNormalizer.cs
```

Keep this runtime pass if new monster sprites are added. Do not reintroduce per-def random icon scaling unless a specific creature genuinely needs a bespoke exception.

## 2026-05-19 — Performance audit breakdown pass
- Performance audit window now separates map summary, Abyssal entity breakdown, horde/portal state, Dominion state, and component presence.
- Added top-count diagnostics for Abyssal PawnKinds, factions, pawn states, ThingDefs, thing categories, and corpse inner pawn kinds.
- Added portal/horde internals snapshot for queued portal requests, used portal cells, front anchors, next portal timing, command gate status, and horde watchdog timing.
- Clarified Dominion diagnostics: component presence is not treated as active Dominion Hell. Audit now reports whether the map is marked as Dominion, whether a session exists, why pocket detection is true/false, and whether ambient VFX can actually run on the current map.


## UI hot-path pass — Forge/Summoning console caching

A low-risk custom UI performance pass was applied to avoid repeating expensive list work every OnGUI event while preserving the current fragile layout/styling work.

Changed areas:

```text
source/UI/Forge/Window_AbyssalForgeConsole.cs
source/UI/Summoning/Window_AbyssalSummoningConsole.cs
source/UI/Shared/AbyssalStyledWidgets.cs
```

Key behavior:

- Forge pattern browser now uses a dirty-key cache for category/subfilter/status/search/residue/tick-bucket state instead of rebuilding filtered/sorted pattern lists every frame.
- Forge material/status evaluation remains informational and uses cached statuses with a small refresh budget per pass, avoiding a full expensive refresh spike after cache expiry.
- Summoning Console ritual list retrieval is cached for short intervals and invalidated by ritual/Dominion/capacitor state changes.
- Decorative shared UI accent animations now draw only during `EventType.Repaint`, avoiding non-render OnGUI work.

Do not replace this with a large UI refactor unless profiling proves it is needed. Keep tab hover/pressed behavior, shared Abyssal scrollbars, SafeLabel clipping protection, and current Enhanced/Classic layout rules intact. If a status appears delayed by up to a short cache interval, remember Forge material availability is informational only; vanilla bills remain the authoritative crafting path.



## 2026-05-19 — Turret localization and non-technical description cleanup

A follow-up localization pass removed remaining English turret module text from the Russian Forge/turret UI and rewrote player-facing module descriptions so they describe weapons, effects, and lore rather than implementation details.

Important details:

```text
- Custom turret module fields are localized through Languages/<Lang>/DefInjected/ABY_TurretModuleDef/ABY_TurretModuleDefs.xml.
- Modular turret ThingDef and RecipeDef localization now uses diegetic weapon/module descriptions instead of Slot/Role/Effect boilerplate in item descriptions.
- Oblivion Choir and Breach Cannon descriptions were cleaned so they no longer mention animated projectiles, mod implementation, reload internals, or technical projectile behavior.
- English base descriptions were also cleaned to avoid reintroducing technical text through fallback language data.
- C# was not changed; this pass is XML/docs-only and does not require a DLL rebuild.
```

Future localization work must scan both English and Russian player-facing descriptions for implementation words such as `mod`, `projectile is animated`, `runtime`, `def`, `save/load`, and similar technical wording. Descriptions should be weapon/lore-facing unless the string is explicitly a dev/debug setting.


## 2026-05-19 — Glossary-driven Russian editorial localization pass

A broad Russian localization editorial pass was applied after the dedicated glossary was added. It focused on natural Russian player-facing text, compact Forge labels, and consistent Abyssal terminology rather than simple load-safe machine translation.

Important details:

```text
- Forge-sensitive labels were shortened where possible: рифт-конденсатор, пепельный конденсатор, коронный конденсатор, грубый/резонансный/коронный стабилизатор.
- Reported weapon/resource terms are kept aligned with the glossary: Рифт-клинок, Рифт-карабин, Хор Забвения, Сигила угольных гончих, Панцирь святого носителя Эгиды.
- Dominion crisis/progression strings now avoid the old `Великий адский портал` wording and use `Великие инфернальные врата`.
- Machine-transliterated UI and DefInjected text such as веил, слинг, харнесс, пайплайн, кэши, scanline/sweep was cleaned up.
- C# was not changed; this pass is XML/docs-only and does not require DLL rebuild.
```

Future Russian localization work should not rely only on XML/duplicate-key validation. It must also run a terminology/editorial scan and check Forge/Summoning visual length risks.



## Forge Russian UI localization follow-up

A Forge console localization follow-up was applied on 2026-05-19 after in-game Russian UI review. It fixed remaining hard-coded English text in the Forge pattern browser/search/status/subfilter/selected-pattern panels and added C# fallback-safe keyed translation helpers for those labels.

Important details:

```text
- Forge subfilters now use Keyed translations instead of raw internal English IDs.
- Forge status chips use Russian plural-aware requirement text: 1 требование, 2-4 требования, 5+ требований, including 11-14 требований.
- The visible bad Russian terms reported from the Forge UI pass were corrected in DefInjected and Keyed localization files.
- Shorter Russian item labels should be preferred in tight Forge cards to avoid overlap in the selected-pattern and browser panels.
```

Future Forge UI changes should keep all player-facing labels behind Keyed translation helpers, not hard-coded English literals, even when the English string is only used as a fallback.


## Russian localization audit and load-error cleanup

A Russian localization audit pass was applied on 2026-05-19 against the user-provided local archive. The pass removed duplicate flat Russian language keys, removed an orphan CrownshardStormcaster RecipeDef translation in both EN/RU, added missing Russian DefInjected coverage for audited gameplay defs, and translated visible Russian UI/label/job-string English leftovers.

Validation after the pass:

```text
XML parse errors: 0
Russian duplicate flat keys: 0
Russian orphan DefInjected keys: 0
English orphan DefInjected keys: 0
Audited missing Russian DefInjected fields: 0
Visible Russian Latin label/title/jobString leftovers: 0
```

Future content batches that add Defs should run the same localization checks before packaging: duplicate flat language keys, orphan DefInjected defNames, missing Russian label/description/jobString fields, and Latin leftovers in visible Russian labels.

## Recent UI work — shared Abyssal scrollbars

A shared procedural Abyssal scrollbar wrapper now lives in `source/UI/Shared/AbyssalStyledWidgets.cs`. It deliberately does **not** modify `GUI.skin` globally and does not require scrollbar PNG assets. It overlays a narrow obsidian/brass/ember scrollbar on top of normal RimWorld scroll behavior so wheel/drag behavior remains safe while the visual style matches Abyssal custom UI.

Applied to current custom scroll regions in:

```text
source/UI/Forge/Window_AbyssalForgeConsole.cs
source/UI/Summoning/Window_AbyssalSummoningConsole.cs
source/Experimental/ProtocolResearch/Window_AbyssalProtocolNexus.cs
source/UI/Bestiary/Window_ABY_BestiaryCodex.cs
source/UI/Turrets/ITab_AbyssalTurretModules.cs
source/UI/BossBar/Window_ABY_BossBarCalibration.cs
```

Future custom Abyssal windows should prefer `AbyssalStyledWidgets.BeginAbyssalScrollView(...)` / `EndAbyssalScrollView(...)` or `DrawAbyssalVerticalScrollbar(...)` over raw `Widgets.BeginScrollView` when the scrollbar is visible to the player. Avoid global scrollbar skin changes.

### Forge communion/attunement gauge polish

The Forge status panel now uses a more deliberate industrial segmented gauge style for the two important state bars instead of generic flat bars.

```text
Communion/unlock progress -> ember-toned segmented industrial gauge
Attunement progress       -> pale-gold segmented industrial gauge
```

This is still procedural C# UI, not texture-driven. Keep the bars readable first: centered labels on a dark capsule that does **not** shrink vertically inside 20–24px bars, restrained glow, a dark trough, visible black gaps between segments, and brass/ember framing. Avoid flat full-width orange fills, noisy fantasy rune bars, oversized animated effects, or putting long labels directly on the segmented fill without a readable text backing. The compact Forge bills tab attunement bar should visually match the main Forge console.

Recent Forge regression fix: the `Pattern records` / `Next milestones` block must use wrapped height with extra padding for milestone lines and upcoming pattern lines. Do not hardcode 22px rows for wrapped Tiny text; it causes second lines to be clipped at common UI scales.


This document is a compact working-memory ledger for future AI-assisted development.
It is not a player-facing changelog and not a substitute for Git history.
Use it to avoid repeating old work, confusing current systems with older backups, or missing recent architectural decisions.

## Maintenance rules

Update this file after substantial work, especially when:

```text
- a major system is added, moved, removed, or stabilized;
- C# source ownership changes;
- a large UI pass lands;
- a boss/Dominion/progression/runtime system changes materially;
- a new asset/audio/content pipeline decision becomes important for future work;
- a recurring bug is fixed and should not be reintroduced;
- a large batch of content is integrated.
```

Do not update this file for tiny isolated value changes unless the change is easy to forget and likely to matter later.

When sources disagree, local user-provided up-to-date archives win over live GitHub, then actual files win over this document.

## Current snapshot

Status as of the local archive inspected on 2026-05-18:

```text
C# source root: source/
Project file: source/AbyssalProtocol.csproj
Root-level .cs files in source/: 0
Real .cs files under source/ excluding bin/obj: 407
Architecture docs present: yes
Build docs present: yes
```

The project uses lowercase `source/`. Do not recreate uppercase `Source/` or place `.cs` files directly in `source/` root.

## Recent structural work

### Source modularization

The old flat source layout has been replaced by module folders under lowercase `source/`.

Important consequences:

```text
- Future C# patches must preserve module placement.
- New files must go into the narrowest relevant module folder.
- Do not add root-level `.cs` files.
- Do not rename namespaces just because a file moved folders.
```

Primary documentation:

```text
Docs/AI_ARCHITECTURE.md
Docs/BUILD_AND_SOURCE_LAYOUT.md
Docs/AI_QUICK_INDEX.md
```

### AI documentation support

The project now has AI-oriented maintenance rules and documentation:

```text
Docs/AI_ARCHITECTURE.md          system/module map
Docs/BUILD_AND_SOURCE_LAYOUT.md  source/build/casing rules
Docs/AI_QUICK_INDEX.md           fast lookup table for future edits
Docs/RECENT_WORK.md              this recent-work ledger
```

When future changes alter architecture or system ownership, update these docs in the same patch.

## Recent UI and progression work

### Protocol Nexus / custom research UI

Protocol Nexus exists as an active custom/experimental research-progression surface.

Key locations:

```text
source/Experimental/ProtocolResearch/
Defs/Experimental/ProtocolResearch/
Textures/UI/ABY/ProtocolResearch/
source/Progression/
```

Important assets currently present include:

```text
Textures/UI/ABY/ProtocolResearch/ABY_LargeResearchRing.png
Textures/UI/ABY/ProtocolResearch/ABY_LargeResearchRing_SelectedSocketHalo.png
Textures/UI/ABY/ProtocolResearch/ABY_NexusWindowBackground.png
Textures/UI/ABY/ProtocolResearch/ABY_SmallCategoryRingFrame.png
```

Do not treat Protocol Nexus as dead code without checking the current archive.

### Forge browser UX safety pass

Recent Forge browser work keeps material availability informational only. The Forge UI may show resource shortage status chips, but Add Bill must not be disabled solely because a custom material availability check thinks ingredients are missing. RimWorld's vanilla bill/workgiver flow should resolve material shortages after a bill is queued.

Also keep the pattern browser compact: the standalone Pattern Browser section title was removed so search, subfilters, status chips, and visible cards sit higher in the narrow left column.

The selected pattern panel now has its own internal scroll region while its action footer stays fixed. Do not reintroduce fixed-height truncation for long pattern descriptions or long ingredient lists. The Forge header has been restyled for a stronger infernal-tech presentation, but title/subtitle readability remains the priority.

### Custom UI remains first-class

Forge, Summoning, Protocol Nexus, Boss Bar, Bestiary, and Turret UI should be treated as real project infrastructure.

Key locations:

```text
source/UI/Shared/
source/UI/Forge/
source/UI/Summoning/
source/UI/BossBar/
source/UI/Bestiary/
source/UI/Turrets/
source/Experimental/ProtocolResearch/
```

New important player-facing actions should be checked for integration into existing custom UI before falling back to vanilla-only gizmos or hidden inspect strings.

## Recent combat and turret work

### Modular turret system expanded

The modular turret system is now a large active subsystem, not a prototype.

Key locations:

```text
source/Comps/CompAbyssalModularTurret.cs
source/Defs/Turrets/
source/UI/Turrets/
source/Combat/VFX/
Defs/Misc/ABY_TurretModuleDefs.xml
Defs/Misc/ABY_CrownfireRocketChoir_TurretModuleDef.xml
Defs/ThingDefs/ABY_ModularTurrets.xml
Defs/ThingDefs/ABY_TurretModules.xml
Defs/ThingDefs/ABY_ModularTurret_Projectiles.xml
Defs/RecipeDefs/ABY_ModularTurretRecipes.xml
```

Recent/active module families visible in the current archive include:

```text
Rift Needler Core
Plasma Lance Core
Ash Choir Repeater Core
Sepulcher Rail Core
Vesper Lance Array
Cinder Mortar Core
Null-Arc Discharger
Abyssal Harpoon Projector
Rift Flak Bloom
Sanctified Prism Emitter
Crowncoil Gauss Minigun
Crownfire Rocket Choir
Choir Arc Emitter
```

When adding turret modules, check all of these layers:

```text
1. module def
2. item def
3. recipe / Forge unlock
4. projectile def
5. mote/VFX defs
6. source projectile/VFX logic
7. textures and optional sound defs
8. turret UI / info cards
9. localization
```

## Recent Dominion work

Dominion Slice/Pocket is a high-risk active runtime system with map generation, atmosphere, VFX, world objects, rewards, and cleanup flows.

Key locations:

```text
source/Dominion/
source/Dominion/Generation/
source/Dominion/MapComponents/
source/Dominion/VFX/
source/Dominion/WorldObjects/
source/World/
Defs/MapGeneratorDefs/
Defs/TerrainDefs/
Defs/WorldObjectDefs/
Defs/ThingDefs_Motes/
Textures/Terrain/
Textures/Things/Building/DominionSlice/
```

Recent design direction to preserve:

```text
- Dominion/hell should not feel sterile.
- Heart and anchor graphics should remain visible above platform underlays.
- Platforms are underlays, not replacements for heart/anchor identity art.
- Side architecture, ruins, edge structures, and atmospheric VFX are part of the Dominion readability pass.
- Spawn presentation should feel like Dominion seam emergence, not a generic portal.
```

Always think about save/load, map cleanup, pawn transfer, boss cleanup, and invalid map references when touching Dominion runtime.

## Recent boss-related reminders

### Boss bar and Aegis presentation

Boss bar layout and Aegis overlay are sensitive UI areas.

Key locations:

```text
source/UI/BossBar/
source/Bosses/Shared/
source/Bosses/ReactorSaint/
Defs/Misc/
Textures/UI/ABY/BossBar/
```

Known direction:

```text
- Aegis should read as a separate shield state connected to the boss bar, not as random cramped text.
- Text clipping is a recurring risk.
- Reactor Saint Aegis and boss phase state must not visually desync.
- Boss bars should remain scalable/relocatable and readable.
```

### Reactor Saint

Reactor Saint remains a complex high-risk boss with AI, Aegis, presentation/cocoon, projectile/VFX, progression, and boss bar dependencies.

Key locations:

```text
source/Bosses/ReactorSaint/
source/Bosses/Shared/
source/UI/BossBar/
source/Combat/
Defs/PawnKindDefs/
Defs/ThingDefs/
Defs/ThingDefs_Motes/
```

## Recent source and build rules

C# changes require:

```text
- full changed source files in source/<module>/
- no root-level source files
- Release build verification before including Assemblies/AbyssalProtocol.dll
- no claim of build success unless actually built
```

Asset/XML/audio-only changes should not rebuild DLL unless C# changed.

## Recurring risks to avoid

```text
- relying on old GitHub state when the user provides a newer local archive;
- creating uppercase Source/ beside lowercase source/;
- adding .cs files directly under source/;
- forgetting custom UI integration for Forge/Summoning/Protocol Nexus content;
- editing XML class references without checking actual C# class names;
- changing boss or Dominion runtime without considering save/load cleanup;
- packaging source/bin, source/obj, or Libraries/ into user-facing delta zips;
- claiming build verification without a real build;
- replacing production assets with concept/mockup outputs;
- adding West pawn textures when RimWorld should mirror East.
```

## Next recommended passes

Recommended next work after this documentation pass:

```text
1. UI regression pass: Forge, Summoning, Protocol Nexus, Boss Bar, Turret UI.
2. Early progression pass: first 2-3 hours, Residue loop, first summons, Forge unlock clarity.
3. Runtime stability pass: Dominion cleanup, boss death/true-death, save/load, map transfer.
4. Content scale pass: prepare Forge/Summoning data structures for hundreds of items/patterns.
5. Changelog/release readiness pass: About metadata, player-facing changelog, versioning.
```

- Gauge labels were moved out of the fill area in Forge status displays: communion uses a right-aligned "current band" caption above the upper bar, while attunement uses a right-aligned tier caption below the lower bar. Keep the bars themselves visually clean. The attunement bar should use 50 discrete segments to match the 50-tier system.

## 2026-05-19 — Russian localization glossary

Added `Docs/LOCALIZATION_GLOSSARY_RU.md` as the canonical Russian terminology guide for Abyssal Protocol localization work.

Important decisions captured:

```text
- `Oblivion Choir` is a weapon/proper name: `Хор Забвения`. Do not classify it as a pawn/enemy.
- Rift equipment should use compact hyphenated forms such as `Рифт-клинок` and `Рифт-карабин`.
- `Sigil` is feminine in Russian: `сигила`, `эту сигилу`, `сигила угольных гончих`.
- Requirement counts need Russian plural handling: `1 требование`, `2-4 требования`, `5+/11-14 требований`.
- Cramped Forge card labels should prefer readable compact forms such as `пепельный конденсатор`.
```

Future Russian localization passes must check the glossary before changing `Languages/Russian/`, DefInjected labels/descriptions, Keyed UI strings, or C# player-facing strings.

## 2026-05-19 — Russian turret localization hardening pass

A follow-up pass made turret module localization robust against raw custom-def fields leaking into Forge cards and tooltips.

Important details:

```text
- Turret module `ABY_TurretModuleDef` DefInjected fields and mirrored Keyed role/effect entries were refreshed so existing Forge/tooltips have localized data without changing C# runtime code in this patch.
- Forge turret cards no longer need raw Slot:/Role: prefixes and should stay compact for Abyssal Forge layout.
- Turret tooltips no longer expose projectile def names as player-facing implementation details.
- Russian turret badge labels are intentionally short: ОСН., ВСП., ПАСС., КОРПУС, СИСТ.
```
## 2026-05-19 — Russian localization final cleanup follow-up

After the turret pass, remaining malformed Russian machine-translation fragments were cleaned in horde reward text, Choir Engine ritual text, Forge milestone UI, UI style settings, and first-sigil guidance. This follow-up remained XML/docs/localization-only: no C# source or DLL changes were made.


## 2026-05-19 — Startup-safe log throttle rebuild

Fixed a red startup error path where `ABY_LargeModpackCompatPatches` could trip through `ABY_LogThrottleUtility.CanLog` during very early `StaticConstructorOnStartup` loading.

Important details:

```text
- `ABY_LogThrottleUtility` now avoids Verse string helpers inside the throttle gate.
- Settings access is isolated behind `SafeSuppressRepeatedWarnings()` so missing early mod settings cannot escape into static constructors.
- Tick access stays behind `SafeTicks()` because `Find.TickManager` can dereference unavailable game state during early loading.
- The DLL was rebuilt from the updated source and should replace older localization-patch DLLs that may be out of sync with source.
```

## 2026-05-19 — Turret localization bridge and lore-description cleanup

- Fixed modular turret Forge/UI text that continued to show raw English `ABY_TurretModuleDef` labels, roles and effect summaries in Russian mode.
- `ABY_TurretModuleDef` now exposes localized label/role/effect properties backed by `ABY_TurretModuleLabel_*`, `ABY_TurretModuleRole_*` and `ABY_TurretModuleEffect_*` Keyed entries, because relying only on custom DefInjected fields was not enough for the Forge cards/tooltips.
- Removed the projectile line from the modular turret detailed tooltip and info card so player-facing turret text no longer exposes implementation projectile defs.
- Rewrote a batch of weapon/apparel/recipe descriptions to remove tier/progression/prototype/projectile wording and keep the text in lore/gameplay tone.
- Runtime smoke test still required in-game, especially in Abyssal Forge > Turret Systems.

## 2026-05-19 — Optimization tooling and in-game performance controls

Added repository-side optimization tools:

- `Tools/texture_budget_rules.json`
- `Tools/ABY_TextureAudit.py`
- `Tools/ABY_OptimizeTextures.py`
- `Tools/ABY_BuildReleasePackage.py`

Added docs:

- `Docs/TEXTURE_BUDGET.md`
- `Docs/RELEASE_PACKAGING.md`

Added in-game visual intensity presets under mod settings:

- Full
- Reduced
- Minimal

The presets reduce optional presentation load without changing gameplay. Dominion ambient VFX components now respect the performance settings by scaling intensity/intervals or disabling optional ambient visuals in Minimal mode.

Added a performance audit window at:

```text
source/Diagnostics/UI/Window_ABY_PerformanceAudit.cs
```

It is opened from Abyssal Protocol mod settings via the diagnostics/performance button and reports current visual settings, map counts, Abyssal thing/pawn counts, and key Dominion component presence.

## 2026-05-19 — TPS runtime cache and VFX budget pass

- Added `ABY_RuntimeTargetCache` as the shared per-map low-frequency pawn/thing cache for hot runtime systems.
- Added `ABY_VfxBudget` as the shared soft budget for optional combat, Dominion, and decorative VFX.
- Reduced broad scans in progression hotfix, hover armor, Rift Blade dash, Rupture halo, threat targeting, Specter Lash streams, and modular turrets.
- Raised XML scan intervals for several mass enemy abilities and modular turrets.
- Chunked Dominion atmosphere maintenance to avoid full-map maintenance spikes.
- Routed Dominion flow/collapse/reward visuals and Specter Lash beam segments through the VFX budget.
- UI shared animation now respects reduced/minimal performance settings more aggressively.
- Build verified with local Roslyn compile against bundled RimWorld/Unity/Harmony libraries.


## 2026-05-19 — Remaining TPS optimization layer

- Extended `ABY_RuntimeTargetCache` with a throttled thing-ID lookup cache for delayed target resolution without repeated `AllThings` scans.
- Reused shared combat target caches in Abyssal monster brain logic, implant ability targeting, friendly-fire checks, Oblivion Choir resonance, and Crownfire micro-target selection.
- Routed additional projectile trails, Reactor Saint projectile presentation, Dominion ambient/void-edge/collapse spectacle, and repeated combat decorative effects through `ABY_VfxBudget`.
- Reduced baseline compatibility-tax from anti-tame and anti-animal workflow components by increasing scan intervals and using cached pawn/portal lookups.
- Cached Protocol Nexus project/category/header data to reduce repeated sorting and summary work in UI draw paths.
- Build verified with direct local Roslyn compile against bundled RimWorld/Unity/Harmony libraries. Runtime smoke test still needs in-game validation.

## 2026-05-19 — Hidden faction relation red-error fix

- Added `ABY_FactionHostilityUtility` as a relation-safe hostility helper for hidden/generated Abyssal factions.
- Replaced high-risk direct `HostileTo` calls in modular turrets, boss targeting/aggression, abyssal threat utility, projectile splash checks, aura checks, anti-tame workflows, and several boss/ability systems.
- Fixed red errors where `ABY_AbyssalHost` or blank generated hidden factions could have no relation row with `PlayerColony` during turret scans or Reactor Saint arrival aggression.
- Build verified with direct local Roslyn compile against bundled RimWorld/Unity/Harmony libraries. Runtime smoke test still needs in-game validation.

## 2026-05-19 — Modular turret aggro fix

- Extended `ABY_RuntimeTargetCache` with a cached combat-building list for player-owned vanilla turrets and Abyssal modular turrets with installed main weapon modules.
- Added shared threat helpers for hostile combat-building selection in `AbyssalThreatPawnUtility`.
- Updated `ABY_AbyssalMonsterBrain` so Abyssal pawns can create tactical melee/reposition/hold jobs against hostile combat buildings when appropriate.
- Updated Hexgun-style shooters, Rift Sappers, and Siege Idols to consider cached combat buildings and cached pawn lists without returning to broad `AllThings` scans.
- Build verified with direct local Roslyn compile against bundled RimWorld/Unity/Harmony libraries. Runtime smoke test still needs in-game validation.

## 2026-05-19 — Hidden utility structure targeting guard

- Centralized hidden/passive structure filtering in `AbyssalThreatPawnUtility`.
- Breach directive target selection now uses the shared hostile building validator instead of accepting all player-home unfactioned structures.
- Reactor Saint melee structure-crush and several structure splash bonus paths now ignore hidden/invisible/conduit/cable/wire utility buildings.
- Build verified with direct local Roslyn compile against bundled RimWorld/Unity/Harmony libraries. Runtime smoke test still needs in-game validation.
- Fixed Rupture phase HP polling to use a 10-tick cache, localized Rupture Sentence player messages, made Protocol Nexus decode speed scale with Intellectual skill, cached Abyssal faction resolution for large modpacks, and added guarded impact handling for Choir Arc / Sepulcher Rail projectiles when external combat stacks throw during damage resolution.

## 2026-05-20 — Broad projectile impact safety pass

A broad compatibility pass extended `ABY_ProjectileImpactSafetyUtility` from two known offenders to the whole custom projectile family under `source/Combat/Projectiles/`.

Important details:

```text
- Every custom projectile override that calls `base.Impact(hitThing, blockedByShield)` now routes the vanilla damage pipeline through `TryRunBaseImpact(...)`.
- Direct `Thing.TakeDamage(...)` calls inside projectile post-impact logic were replaced with `TryApplyDamage(...)`.
- Projectile-triggered `GenExplosion.DoExplosion(...)` calls are wrapped with throttled post-impact safety handling.
- `ABY_ProjectileProcUtility` now routes its damage helper through the same safety utility and catches external hediff/proc exceptions.
```

This is a large-modpack compatibility layer, not a gameplay rebalance. It is meant to prevent external combat stacks such as CombatAI/Yayo/MVCF/Hospitality/HAR/VEF from turning Abyssal projectile impacts into repeated red `Exception ticking projectile` logs. Do not replace it with silent empty `catch` blocks; keep throttled warnings so real regressions remain visible.

Validation after this pass: direct local Roslyn compile against bundled RimWorld/Unity/Harmony libraries succeeded. Runtime smoke testing in the target modpack is still required.

## 2026-05-20 — Residue sintering XML ownership and safe cache cleanup

- Added `ABY_ResidueSinteringExtension` as the XML-owned way to assign corpse-to-residue values for non-boss abyssal pawn kinds.
- Updated current sinterable enemy `PawnKindDef` files to carry explicit residue values, including Aortic Chain Harrower and Halo Husk so future content audits do not depend on a hardcoded C# table.
- Kept the old residue lookup table as a legacy fallback so existing saves and older XML remain safe.
- Cached Forge, Residue, and Attunement def lookups in `AbyssalForgeProgressUtility` to avoid repeated DefDatabase calls in Forge UI/progression paths.
- Cached and warning-throttled the Dominion `Map.generatorDef` reflection fallback; the sterile map component and world site def remain the primary safe detection paths.
- Build verified with direct local Roslyn compile against bundled RimWorld/Unity/Harmony libraries. Runtime smoke testing in-game is still required.

## 2026-05-20 — Abyssal pawn classification and physiology centralization

- Added `ABY_AbyssalPawnClassificationExtension` so `PawnKindDef`/race XML can mark abyssal pawns, bosses, minibosses, Dominion entities, and construct-physiology pawns without spreading new hardcoded C# lists through gameplay systems.
- Added `ABY_AbyssalPawnClassificationUtility` as the shared runtime helper for abyssal pawn checks, boss/miniboss protection, construct physiology, and BloodLoss blocking.
- Routed residue sintering boss protection, construct physiology checks, and Harvester corpse eligibility through the shared classification helper while keeping legacy name/component fallbacks for save and compatibility safety.
- Extended `ABY_DefCache` with typed negative caches for PawnKindDef, SoundDef, ResearchProjectDef, RecipeDef, FactionDef, TerrainDef, and MapGeneratorDef in addition to the existing Hediff/Thing/Song caches.
- Marked current non-boss abyssal enemies, bosses/minibosses, and construct-like enemies with explicit classification extensions in `Defs/PawnKindDefs/`.
- Build verified with direct local Roslyn compile against bundled RimWorld/Unity/Harmony libraries. Runtime smoke testing in-game is still required.

Note: this pass deliberately did not mass-migrate authored T1/Dominion/fallback wave compositions to generic auto-pools. Those hardcoded spawn lists affect balance and encounter pacing and should be migrated through a separate encounter-template/playtest pass rather than hidden inside a low-risk classification cleanup.

## 2026-05-20 — Encounter validation and shadow-planning diagnostics

A low-risk encounter architecture preparation pass added a validator/shadow-mode layer without changing live encounter composition.

Changed behavior:

```text
- Encounter data validation can run at startup and from the diagnostics/settings UI.
- Validation checks templates, doctrines, pawn pools, role counts, budget values, difficulty refs, boss profile refs, and escalation package refs.
- Shadow planning can be enabled manually to log legacy/authored pack vs directed-plan comparisons.
- Shadow planning is diagnostic-only; actual spawned waves remain unchanged.
```

Key files:

```text
source/Encounters/ABY_EncounterValidationUtility.cs
source/Encounters/ABY_EncounterShadowPlannerUtility.cs
source/Core/Bootstrap/AbyssalProtocolModSettings.cs
source/Core/Bootstrap/AbyssalProtocolMod.cs
source/Diagnostics/ABY_StabilityDiagnosticsUtility.cs
source/World/Buildings/Summoning/Building_AbyssalSummoningCircle.cs
```

Do not use shadow-mode output as automatic authorization to migrate T1, Dominion, or boss escort waves. It is a comparison tool for future playtest-driven encounter migration.

## 2026-05-20 — Dominion pocket music post-load warning guard

- Hardened `ABY_DominionPocketMusicGameComponent` so loading directly inside an active Dominion pocket gives RimWorld's music manager a short post-load grace window before attempting/logging forced hell-track start failures.
- Replaced the immediate first-attempt warning with repeated-failure tracking; Dominion pocket music now warns only after several failed start attempts outside the grace window while continuing to retry quietly.
- This is a log-noise and load-order safety fix only: it does not change Dominion combat, map transfer, encounter logic, or the selected hell-pocket song.
- Build verified with direct local Roslyn compile against bundled RimWorld/Unity/Harmony libraries. Runtime smoke testing in-game is still required.

## 2026-05-20 — Projectile base-impact warning demotion

- Refined `ABY_ProjectileImpactSafetyUtility` for the recurring `Projectile_HexgunBurst base impact` / external combat-stack `NullReferenceException` pattern seen in large combat modpacks.
- Expected `NullReferenceException` failures thrown inside vanilla/external `base.Impact(...)` are now treated as non-fatal suppressed base-impact events and logged as throttled messages instead of stack-trace warnings.
- Added a pre-base-impact projectile validity guard so already-destroyed/despawned projectiles skip base impact cleanly instead of feeding invalid state into patched combat stacks.
- Log messages now include projectile def and launcher def context without dumping a full warning stack for this expected interop path.
- This is a log-noise and compatibility hardening pass only: successful projectile impacts, damage, VFX, sounds, and post-impact logic are unchanged.
- Build verified with direct local Roslyn compile against bundled RimWorld/Unity/Harmony libraries. Runtime smoke testing in-game is still required.

## 2026-05-20 — Deferred Dominion UI action guard

- Added a deferred UI action game component and Dominion pocket UI action helper.
- Dominion pocket enter/jump/return commands from gizmos, compact tab and Summoning Console now queue the actual map-transfer/collapse work by one Unity frame instead of executing directly inside the IMGUI click event.
- This is intended to prevent heavy modpack UI overlays from leaving RimWorld's mouse-position scroll stack unbalanced after returning from Dominion pocket maps.
- Also hardened Abyssal settings/diagnostics/performance scroll views with try/finally EndScrollView guards.
- Gameplay flow is intended to remain unchanged: the same action still runs, only outside the current IMGUI draw frame.

## 2026-05-20 — Forge tier rail and badge readability pass

- Added lightweight code-drawn tier rails to Forge pattern cards so large pattern lists can communicate progression tier without adding new texture assets or another filter row.
- Added compact tier badges to selected pattern details and upcoming-pattern rows.
- Tier labels are derived from existing Forge residue thresholds: Signal, Breach, Archon, Reactor, Dominion, and Crown.
- Added EN/RU Forge tier localization keys and tooltip text.
- This is a UI readability pass only: no recipes, unlock thresholds, residue economy, crafting behavior, assets, or gameplay balance were changed.
- Build verified with direct local Roslyn compile against bundled RimWorld/Unity/Harmony libraries. Runtime smoke testing in-game is still required.


## 2026-05-20 — Passive modular turret module expansion

- Added a 12-icon passive turret module sheet integration: existing Cooling Lattice, Targeting Sigil, and Residue Capacitor received new transparent item icons, and nine new passive modules were added across Signal, Breach, Integration, and Crown tiers.
- Added new passive module defs, item ThingDefs, Forge recipes, EN/RU module/item/recipe localization, and Keyed Forge/turret UI strings for Blackout Power Regulator, Overpressure Cycle Governor, Long Choir Lens, Close-Quarters Interlock, Abyssal Threat Prioritizer, Anti-Swarm Pattern Scanner, Shield-Burn Capacitor, Sanctified Stabilizer Plate, and Emergency Heat Dump.
- Extended `ABY_TurretModuleDef` and `CompAbyssalModularTurret` so these passives are real mechanics rather than fake descriptions: negative module power draw can reduce total chassis draw, minimum range offsets affect main/aux weapons, passive target-priority hints are evaluated during the existing throttled target scan, cluster targeting uses the cached combat pawn list, incoming damage multipliers can harden the chassis, and cooldown recovery supports fractional per-tick heat dump behavior.
- Updated turret module info cards and ITab stat text so new passive effects and negative/positive power deltas are player-readable.
- Build verified with direct local Roslyn compile against bundled RimWorld/Unity/Harmony libraries. Runtime smoke testing in-game is still required, especially Forge > Turret Systems and installed passive module stat summaries.

## 2026-05-20 — Passive turret aegis modules and icon optimization
- Added two passive shield modules for modular turrets: Breach Aegis Relay (tier 2) and Crown Aegis Matrix (tier 4).
- Passive aegis modules add a real turret shield pool, recharge delay, recharge rate and inspect/ITab/stat-card exposure instead of using only incoming damage reduction.
- The generated passive turret module icons were reduced from 512x512 to optimized 256x256 PNGs for UI/item use.
- Runtime smoke test still required in-game: install shield modules, damage a powered chassis, verify aegis absorption/recharge and UI display.
