# Abyssal Protocol — AI Architecture Map

This document is a navigation map for future AI-assisted work on the Abyssal Protocol RimWorld mod.
It is not a design pitch and not a replacement for source inspection. It exists to reduce wrong-file edits,
make future patches easier to review, and keep systems separated after the source modularization pass.

## Non-negotiable workflow

Before changing code, XML, assets, sounds, UI, or balance:

1. Check the live GitHub repository and recent commits.
2. If the user provides a local archive and explicitly says it is up to date, treat that archive as the working base.
3. Inspect the actual file paths before editing.
4. Preserve existing class names and namespaces unless the task explicitly requires a rename.
5. Do not add new `.cs` files directly under `source/` root.
6. Do not create an uppercase `Source/` directory. The current project uses lowercase `source/`.
7. For C# changes, always include the full changed source files and a compiled `Assemblies/AbyssalProtocol.dll` only when the build was actually verified.
8. For asset/XML/audio-only changes, do not rebuild the assembly unless C# changed.

## Current source root rule

The source root must remain clean:

```text
source/
  AbyssalProtocol.csproj
  <module folders only>
```

There should be no root-level `.cs` files in `source/`.
All source files belong in module folders.

## Top-level module map

### `source/Core/`

Project bootstrap, mod settings, startup hooks, generic utilities, and low-level shared game/map components.
Use this for code that is genuinely foundational and not owned by a more specific gameplay system.

Typical content:

- mod entry and settings
- startup/static initialization
- safe spawn helpers
- general map/game components
- non-UI shared utilities

Avoid putting feature-specific logic here just because it is reused once.

### `source/Defs/`

C# Def classes and DefModExtensions used by XML.
This is the bridge between XML and runtime behavior.

Typical content:

- custom `Def` classes
- `DefModExtension` classes
- XML-driven profiles
- config containers for turrets, summoning, Dominion, boss bar, apparel, and common systems

When adding XML fields that need C# support, first check whether an existing DefModExtension fits.

### `source/UI/`

All custom Abyssal UI surfaces and shared UI primitives.
Treat this as a first-class system, not temporary overlay code.

Subareas:

- `UI/Shared/` — reusable styled widgets, layout helpers, colors, textures, buttons, text utilities, and the shared procedural Abyssal scroll view/scrollbar wrapper.
- `UI/Forge/` — Abyssal Forge compact tab and full Forge/Communion UI support.
- `UI/Summoning/` — Summoning Circle compact tab and full Summoning Console support.
- `UI/BossBar/` — boss bar renderer, boss bar profile defs, phase entries, Aegis overlay renderer.
- `UI/Bestiary/` — bestiary/codex UI and reward presentation.
- `UI/Turrets/` — modular turret UI, ITabs, module socket windows.
- `UI/Gizmos/` — custom command/gizmo rendering and presentation helpers.

Rule: new Forge, Summoning, ritual, progression, reward, or unlock interaction should be evaluated for custom UI integration before falling back to vanilla-only gizmos or inspect strings.

### `source/Summoning/`

Summoning Circle logic, sigil validation, ritual workers, manifestation profiles, gate animation, arrival VFX, summon jobs, and circle map components.

Typical content:

- ritual activation flow
- sigil validation
- arrival manifestation utilities
- circle instability/capacitor/stability logic
- summoning VFX/audio helpers
- summoning job drivers and comps

When changing summon behavior, also check:

- `Defs/` for ritual/summon-related XML bindings
- `UI/Summoning/` for player-facing presentation
- `Progression/` for unlock gates
- `Audio/` if encounter music or activation sounds are affected

### `source/Forge/`

Abyssal Forge progression, residue sintering, forge map components, forge recipes, and forge-specific things.

Typical content:

- residue processing logic
- forge progress utility
- forge progression map component
- custom recipe workers
- forge-produced special things

When adding new craftable abyssal content, also check:

- `Defs/` for DefModExtensions and XML-driven unlocks
- `UI/Forge/` for pattern/reward presentation
- `Progression/` for gates
- relevant `ThingDefs`, `RecipeDefs`, `ResearchProjectDefs`, and localization keys

### `source/Progression/`

Reward gates, lore progression, first-loop guidance, protocol research gate helpers, Herald analysis, and recap utilities.

Typical content:

- first boss progression
- early guidance/tutorial whisper systems
- Protocol Nexus progress state helpers
- Herald fragment analysis
- unlock/gating helpers used by Forge/Summoning/UI

This module should answer: what has the player earned, unlocked, learned, or triggered?

### `source/Experimental/ProtocolResearch/`

Protocol Nexus / custom research prototype layer.
This is currently separated as experimental because it is a custom research/protocol system rather than vanilla research only.

Typical content:

- Protocol research defs
- Protocol Nexus building
- Protocol Nexus window
- decode job driver and work giver
- utility logic for custom protocol projects

Rule: do not treat this as throwaway code if it is active in the current archive. Inspect before changing Protocol Nexus UI or progression.

### `source/Bosses/`

Boss-specific and shared boss systems.

Subareas:

- `Bosses/Shared/` — boss music, selection, presentation, true death, shared cleanup, boss utilities.
- `Bosses/Archon/` — Archon Beast / Archon of Rupture related portal, phase, cleanup, comps, and state logic.
- `Bosses/ReactorSaint/` — Reactor Saint AI, progression, cocoon/presentation, projectiles/VFX support, Aegis-related boss logic.
- `Bosses/Rupture/` — Rupture-specific crown, abilities, state, or presentation logic.

When editing a boss, also check:

- `UI/BossBar/` for bar, phase, Aegis, and selection presentation
- `Combat/Projectiles/` for boss projectile classes
- `Pawns/` and `Comps/` for pawn comps and death behavior
- XML `PawnKindDefs`, `ThingDefs`, `HediffDefs`, `AbilityDefs`, `SoundDefs`, and localization

### `source/Dominion/`

Dominion pocket/slice/hell-dimension systems.

Subareas:

- generation
- world objects
- buildings and anchors through related `World/Buildings/Dominion/`
- pocket runtime session/state
- atmosphere, weather, music, VFX
- map components and cleanup logic

This is a high-risk runtime state area. For Dominion changes, always think about save/load, map cleanup, pawn transfer, exit flow, boss cleanup, and invalid map references.

### `source/World/`

World and map objects, custom buildings, skyfaller/vessel bases, place workers, and special things that are not owned by a more specific module.

Typical content:

- custom buildings
- place workers
- skyfaller/vessel base classes
- world objects
- Dominion buildings when placed under `World/Buildings/Dominion/`

When changing building behavior, also check XML `ThingDefs`, graphic paths, comps, UI tabs, and draw layers.

### `source/Combat/`

Weapons, projectiles, verbs, VFX helpers, damage workers, combat comps, and map/game components for combat effects.

Subareas:

- `Combat/Projectiles/` — custom projectile classes, including boss and turret projectiles.
- `Combat/Verbs/` — custom verb behavior.
- `Combat/DamageWorkers/` — custom damage behavior.
- `Combat/VFX/` — combat visual feedback helpers.
- `Combat/Comps/` — combat-related thing comps.
- `Combat/MapComponents/` / `Combat/GameComponents/` — combat runtime state trackers.

When adding a weapon/turret/projectile, also check XML projectile defs, weapon defs, sound defs, texture paths, and Forge integration.

### `source/Apparel/`

Armor/apparel runtime systems, Aegis apparel logic, hover armor rendering, body type restrictions, apparel stats, and apparel comps.

Typical content:

- apparel Aegis feedback
- hover armor rendering and stat injection
- body type restrictions
- apparel comp properties and stats

When changing armor, also check apparel XML, pawn body type assumptions, texture directions, and stat offsets.

### `source/Pawns/`

Pawn AI, monster role utilities, auto-hostility, lord helpers, death actions, animated pawn body comps, and pawn map components.

Typical content:

- abyssal monster brain/ranged brain
- job loop guard
- anti-tame/anti-animal workflow
- pawn comps
- boss/monster death actions
- pawn-related map components

When changing pawn behavior, inspect ThinkTree/XML references and avoid breaking boss pawn special cases.

### `source/Comps/`

General-purpose ThingComp and CompProperties classes not clearly owned by a narrower module.
This is a shared behavior bucket.

Before adding a new comp here, ask whether it belongs in `Summoning/Comps`, `Pawns/Comps`, `Combat/Comps`, `Apparel/Comps`, or another feature folder instead.

### `source/Hediffs/`

Hediff classes, HediffComp classes, implant info card support, disease/immunity helpers, tether hediffs, and construct physiology helpers.

When adding implants or status effects, also check:

- `Defs/HediffDefs/`
- `UI/Forge/` or `UI/Shared/` for info presentation
- `Patches/Hediffs/` for Harmony behavior
- localization keys

### `source/Audio/`

Sound utility and C# sound gating, especially for charge/sustainer behavior.

Use this only for runtime audio logic. Audio assets and SoundDefs live outside source.

### `source/Encounters/`

Encounter director, threat pawn helper logic, and telemetry components.
Use this for encounter-level orchestration that is not owned by a specific boss, summon ritual, or Dominion system.

### `source/Compatibility/`

Compatibility and hotfix code for large modpacks or external interactions.
Do not put core gameplay here.

### `source/Patches/`

Harmony patches that are broad or cross-system.

Subareas:

- `Patches/Hediffs/`
- `Patches/Pawns/`

Rule: prefer clear patch names and keep patch logic small. If a patch grows into a feature, move feature logic into the owning module and keep only the Harmony hook here.

### `source/Diagnostics/`

Dev tools, stability diagnostics, Harmony patch reports, test immortality logic, and diagnostic UI.

Diagnostics should not be required for normal gameplay progression unless explicitly designed as a debug-only system.

### `source/Legacy/`

Migration and legacy cleanup only.
Do not add new gameplay here.
Do not expand legacy systems unless fixing migration from old saves or old def names.

## Cross-system dependency guide

### Forge content changes usually touch

- `source/Forge/`
- `source/UI/Forge/`
- `source/Progression/`
- `source/Defs/`
- XML `ThingDefs`, `RecipeDefs`, `ResearchProjectDefs`, `HediffDefs` as relevant
- Textures and localization

### Summoning content changes usually touch

- `source/Summoning/`
- `source/UI/Summoning/`
- `source/Progression/`
- `source/Defs/Summoning/`
- XML incident/ritual/sigil/thing defs
- VFX/audio/localization

### Boss changes usually touch

- `source/Bosses/<BossName>/`
- `source/Bosses/Shared/`
- `source/UI/BossBar/`
- `source/Pawns/`
- `source/Combat/Projectiles/`
- XML pawnkind/thing/ability/hediff/sound defs
- boss textures and localization

### Dominion changes usually touch

- `source/Dominion/`
- `source/World/Buildings/Dominion/`
- `source/UI/` only if player-facing status is changed
- XML terrain/building/weather/world object defs
- map cleanup and save/load logic

### Modular turret changes usually touch

- `source/Combat/Projectiles/`
- `source/Combat/VFX/`
- `source/UI/Turrets/`
- `source/Defs/Turrets/`
- `source/World/Things/` or `World/Buildings/` if needed
- XML turret module defs, item defs, projectile defs, mote defs, recipes, sound defs
- textures and localization

## High-risk areas

Treat these as regression-sensitive:

1. Forge UI layout, scrollbars, clipped labels, button skins.
2. Summoning UI layout, ritual readiness, duplicated preview text.
3. Protocol Nexus UI ring/socket/layer placement.
4. Boss bar and Reactor Saint Aegis display.
5. Dominion runtime/save/load/map cleanup.
6. Boss death/true death/downed state cleanup.
7. Modular turret projectile classes referenced from XML.
8. Def-driven profile systems where XML names must match C# lookup logic.
9. Texture path and directional sprite conventions.
10. Localization keys for new UI, messages, letters, gizmos, and inspect strings.

## Runtime smoke test after C# refactors

After any structural C# refactor or large feature change, do at least this:

```text
- Build Release successfully.
- Launch RimWorld with the mod enabled.
- Confirm no red errors on mod load.
- Open Abyssal Forge compact tab and full console.
- Open Summoning Circle compact tab and full console.
- Open Protocol Nexus if present in the current build.
- Spawn or trigger a basic abyssal enemy.
- Dev-spawn Archon and Reactor Saint if the task touched bosses/combat/UI.
- Check boss bar appears and updates.
- Check at least one modular turret projectile class loads if turret code changed.
```

## Implementation discipline

When preparing a patch:

- Use repository-relative paths exactly.
- Include full changed `.cs` files, not snippets.
- Include new XML/assets/sounds in final mod paths.
- Do not include `source/bin/` or `source/obj/`.
- Do not include dev-only `Libraries/`.
- Mention whether build was verified.
- Include a commit title and commit description.

## Release optimization and performance tools

The project now has a repository-side release optimization layer and an in-game performance diagnostics layer.

Repository-side tools live in:

```text
Tools/texture_budget_rules.json
Tools/ABY_TextureAudit.py
Tools/ABY_OptimizeTextures.py
Tools/ABY_BuildReleasePackage.py
```

Documentation:

```text
Docs/TEXTURE_BUDGET.md
Docs/RELEASE_PACKAGING.md
```

Runtime performance/settings code lives in:

```text
source/Core/Bootstrap/ABY_VisualIntensity.cs
source/Core/Bootstrap/ABY_PerformanceSettingsUtility.cs
source/Diagnostics/ABY_PerformanceAuditUtility.cs
source/Diagnostics/UI/Window_ABY_PerformanceAudit.cs
```

The in-game visual intensity settings are presentation-only. They must not change gameplay rewards, encounter composition, AI, boss progression, or save-critical logic. They may reduce optional ambient Dominion VFX, UI animation, map presentation effects, title cards, weather intensity, and VFX interval density.

Do not implement runtime texture downscaling inside RimWorld unless there is a strong, tested reason. Prefer build-time texture budget scripts and pre-optimized PNGs.

## Runtime performance systems — 2026-05-19

A shared TPS optimization layer now exists and should be reused before adding any new recurring map/pawn scans:

- `source/Core/Runtime/ABY_RuntimeTargetCache.cs` owns low-frequency per-map caches for spawned living pawns, combat target pawns, and def-scoped spawned things.
- `source/Combat/VFX/ABY_VfxBudget.cs` owns per-map soft budgets for optional combat, Dominion, and decorative VFX.

Rules for future AI work:

1. Do not add new `Find.Maps`, `map.mapPawns.AllPawnsSpawned`, `map.listerThings.AllThings`, or broad `AllDesignations` loops on short intervals unless there is no narrower source.
2. Prefer `ABY_RuntimeTargetCache` for target selection, aura scans, turret scans, halo/crown tracking, hover-apparel tracking, and compatibility repair loops.
3. Optional flecks, motes, beams, trails, Dominion ambience, and decorative UI/gameplay effects should pass through `ABY_VfxBudget` or existing performance settings.
4. Gameplay effects, damage, targeting validity, and save/load state must not depend on the visual budget; only optional presentation should be skipped.


## Remaining TPS optimization layer — 2026-05-19

The second TPS pass extends the shared performance architecture instead of adding isolated throttles.

Additional ownership notes:

- `source/Core/Runtime/ABY_RuntimeTargetCache.cs` now also owns a throttled per-map thing-ID cache. Use `TryFindThingById` for delayed beam/projectile/stream targets instead of scanning `map.listerThings.AllThings` in every lookup.
- `source/Combat/VFX/ABY_VfxBudget.cs` is now the default gate for optional projectile trails, Reactor Saint projectile presentation, Dominion ambient/void-edge/collapse spectacle, and high-frequency combat decorative effects.
- `source/Pawns/MapComponents/MapComponent_ABY_AntiTameGuard.cs`, `source/Pawns/MapComponents/MapComponent_ABY_AntiAnimalWorkflowV3.cs`, `source/Pawns/ABY_AntiTameUtility.cs`, and `source/Compatibility/ABY_LargeModpackHotfixBUtility.cs` use slower intervals and cached pawn/portal lookups. Future anti-tame or modpack-compatibility repairs should follow this pattern.
- `source/Experimental/ProtocolResearch/ABY_ProtocolResearchUtility.cs` owns cached Protocol Nexus project/category/ThingDef lists. `Window_AbyssalProtocolNexus.cs` should avoid sorting/filtering all protocol projects every OnGUI frame and should use these cached lists.
- Projectile `Tick()` methods under `source/Combat/Projectiles/Weapons/` may still own their gameplay logic, but optional trail/spark/arc presentation should be gated before spawning flecks/motes.

Regression rule: when adding a new recurring runtime effect, first decide whether it belongs to gameplay state, target selection, or optional presentation. Gameplay state must remain deterministic and ungated by visual budgets; target selection should use runtime caches where possible; optional presentation should use performance settings and/or `ABY_VfxBudget`.

## Hidden faction hostility safety — 2026-05-19

Hidden or generated Abyssal encounter factions can lack a normal relation row with `PlayerColony` in existing saves or mid-encounter generated state. Vanilla `Faction.HostileTo`, `Pawn.HostileTo`, and `RelationWith` log red errors when called in that state.

Shared helper:

```text
source/Core/Runtime/ABY_FactionHostilityUtility.cs
```

Use this helper in Abyssal hot paths where one side can be `ABY_AbyssalHost`, an ABY pawn kind, a boss, a projectile instigator, a modular turret target, an aura target, or a compatibility/anti-tame guard.

Rules:

1. Prefer `ABY_FactionHostilityUtility.SafeHostileTo(...)` over direct `HostileTo(...)` for ABY target selection and AoE logic.
2. `ABY_AbyssalHost` is treated as hostile to non-Abyssal factions without calling vanilla `RelationWith` when the relation row is missing.
3. Missing non-Abyssal relation rows fall back conservatively instead of spamming red errors in per-tick target scans.
4. Do not reintroduce direct hidden-faction `HostileTo` calls into turret, boss, projectile, aura, or runtime-cache code.

## Modular turret threat targeting — 2026-05-19

Player-owned Abyssal modular turrets are not vanilla `Building_Turret` classes; their weapon behavior is owned by `source/Comps/CompAbyssalModularTurret.cs`. Because of that, vanilla and generic Abyssal pawn targeting can miss them unless the ABY runtime cache and threat utility expose them as combat buildings.

Ownership notes:

- `source/Core/Runtime/ABY_RuntimeTargetCache.cs` now also caches player-owned combat buildings: vanilla turrets and modular turrets with a main weapon module installed.
- `source/Encounters/AbyssalThreatPawnUtility.cs` owns shared hostile combat-building validation and selection helpers.
- `source/Pawns/ABY_AbyssalMonsterBrain.cs` may turn stale or missing hostile-pawn jobs into tactical jobs against nearby hostile combat buildings.
- `source/Comps/CompHexgunThrallShooter.cs`, `source/Comps/CompABY_RiftSapperShooter.cs`, and `source/Comps/CompABY_SiegeIdolSiegeShooter.cs` should use runtime cached pawns/buildings rather than fresh broad map scans.

Regression rule: if a new player defense building is functionally a turret but not a `Building_Turret`, add it to the runtime combat-building path or expose it through an equivalent helper. Do not rely only on vanilla hostile-pawn targeting for Abyssal enemies.

## Hidden/passive structure target filtering — 2026-05-19

Abyssal target selection must distinguish real tactical structures from passive utility overlays. Hidden conduits, hidden cables, invisible wires, and similar utility infrastructure can be indestructible or not meaningful as combat targets.

Shared ownership:

- `source/Encounters/AbyssalThreatPawnUtility.cs` owns building target validation through `IsValidHostileBuildingTarget(...)` and `ShouldIgnoreAsHostileBuildingTarget(...)`.
- Breach, boss, projectile splash, and monster AI code should call the shared utility before assigning an attack job or applying special anti-structure damage.

Valid structure targets include combat turrets, Abyssal modular turrets with main weapons, doors, real walls, barricades, sandbags, barriers, and other visible/destroyable tactical blockers. Hidden/invisible/conduit/cable/wire utility structures are filtered out unless explicitly combat-capable.
