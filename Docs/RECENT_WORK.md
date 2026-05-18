# Abyssal Protocol — Recent Work Notes


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

