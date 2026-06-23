## 2026-06-23 — Item classification storage and repair compatibility

- Added `Patches/ABY_ItemClassification_StorageAndRepairCompatibility.xml`, which explicitly assigns vanilla `Weapons` or `Apparel` categories to every Abyssal weapon and apparel `ThingDef`.
- This is a metadata compatibility pass for storage frameworks and repair utilities that inspect direct `thingCategories` instead of inherited base-parent categories.
- The pass intentionally leaves `techLevel`, tradeability, `destroyOnDrop`, quality behavior, and enemy-only item policy unchanged.
- No C# source or assembly changed; XML validation and direct category coverage checks are required before release.

## 2026-05-24 — Combat targeting and Dominion lookup cleanup

- Replaced LINQ/order-by target selection in turret projectile impact paths with bounded manual top-N insertion for Null Arc, Rift Flak, and Sanctified Prism projectiles.
- Routed those projectile target scans through `ABY_RuntimeTargetCache.CombatTargetPawnsFor` instead of direct `mapPawns.AllPawnsSpawned` enumeration on impact.
- Reduced support-aura scan cost for Choir Engine and Null Priest by reading `ABY_RuntimeTargetCache.SpawnedLivingPawnsFor` instead of each comp independently touching `mapPawns.AllPawnsSpawned`.
- Cached Dominion Slice encounter resolution in the anchor and heart buildings, matching the earlier VFX MapComponent resolver pass.
- Added active-encounter cache invalidation when portal waves start/reset and replaced Null Bolt impact def lookup with `ABY_DefCache`.
- Build verified by direct Roslyn compile against bundled RimWorld/Unity/Harmony/.NET Framework-style references. XML parse validation passed. RimWorld runtime smoke testing is still required.

Touched files:
source/Combat/Projectiles/Turrets/Projectile_ABY_TurretNullArcPulse.cs
source/Combat/Projectiles/Turrets/Projectile_ABY_TurretRiftFlakSeed.cs
source/Combat/Projectiles/Turrets/Projectile_ABY_TurretSanctifiedPrismBolt.cs
source/Combat/Projectiles/Weapons/Projectile_NullBolt.cs
source/Comps/CompABY_ChoirEngineAura.cs
source/Comps/CompABY_NullPriestAura.cs
source/World/Buildings/Dominion/Building_ABY_DominionSliceAnchor.cs
source/World/Buildings/Dominion/Building_ABY_DominionSliceHeart.cs
source/Core/GameComponents/MapComponent_AbyssalPortalWave.cs
Docs/KNOWN_RISKS_AND_REGRESSIONS.md
Docs/RECENT_WORK.md

## 2026-05-24 — Dash hot-path and progression notification hardening

- Removed the per-`PatherTick` `Map.GetComponent<T>()` lookup from abyssal dash freeze checks by routing `ABY_AbyssalDashRuntime.IsDashing` through a static active-pawn id registry maintained by `MapComponent_ABY_AbyssalDashRuntime`.
- Kept dash ownership and ticking in the map component, but made Harmony path/job guards read a zero-allocation `HashSet<int>` instead of scanning map components on every pawn path tick.
- Hardened first Rift Butcher kill progression so letter-stack failures cannot propagate through `GameComponentTick`, while preserving the saved processed pawn id list and adding a runtime `HashSet<int>` lookup index.
- Reduced Dominion pocket telemetry allocation pressure by making `HasAnyPlayerPawnsOnMap` and `GetPocketPlayerCount` scan pawns directly instead of creating temporary pawn lists.
- Build verified by direct Roslyn compile against bundled RimWorld/Unity/Harmony/.NET Framework-style references. RimWorld runtime smoke testing is still required.

Touched files:
source/Core/Misc/ABY_AbyssalDashRuntime.cs
source/Core/GameComponents/MapComponent_ABY_AbyssalDashRuntime.cs
source/Progression/ABY_HordeAndButcherProgressionGameComponent.cs
source/Dominion/AbyssalDominionPocketUtility.cs
Docs/KNOWN_RISKS_AND_REGRESSIONS.md
Docs/RECENT_WORK.md

## 2026-05-23 — Summoning Console redesign visual hotfix

- Tightened the full Summoning Console redesign after in-game screenshot review.
- Fixed the player-facing console title so the Summoning Circle no longer displays the Forge-style “infernal communion console” title in English/Russian.
- Simplified threat archetype tabs to single-line labels with counts moved into tooltips to avoid clipped lower text at normal UI scale.
- Replaced vanilla checkbox glyphs in the invocation control panel with compact styled On/Off buttons so reduced effects, overchannel, and emergency dump no longer render as oversized red/green marks.
- Reclassified Rift Butcher under Node Entities instead of Archon-Class to match its miniboss role.
- XML validation passed and C# build was verified by direct Roslyn compile against bundled RimWorld/Unity/Harmony references. RimWorld runtime smoke testing is still required.

Touched files:
source/UI/Summoning/Window_AbyssalSummoningConsole.cs
Languages/English/Keyed/ABY_Strings.xml
Languages/Russian/Keyed/ABY_Strings.xml
Languages/English/Keyed/ABY_SummoningConsoleRedesign_Strings.xml
Languages/Russian/Keyed/ABY_SummoningConsoleRedesign_Strings.xml
Docs/AI_ARCHITECTURE.md
Docs/CONTENT_MATRIX.md
Docs/KNOWN_RISKS_AND_REGRESSIONS.md
Docs/RECENT_WORK.md

## 2026-05-23 — Summon threat rehearsal dev gizmo

- Added a dev-only `DEV: threat rehearsal` Summoning Circle gizmo that opens a FloatMenu for summon rehearsal and force-start testing.
- Rehearsal logs ritual id, summon mode, resolved PawnKind, unlock/readiness/capacitor state, predicted arrival cell where available, T1/T2 scaling, boss escort profiles, horde plan summaries, Dominion runtime notes, and selected presentation route.
- Force-start entries run the selected ritual without consuming a sigil and bypass progression/capacitor gates, while still requiring a usable spawned circle/map and normal encounter safety checks.
- This is a diagnostics/testing aid only; no player-facing Summoning Console category UI was implemented in this pass.

Touched files:
source/World/Buildings/Summoning/Building_AbyssalSummoningCircle.cs
source/Diagnostics/ABY_SummonThreatRehearsalUtility.cs

## 2026-05-23 — Sigil routing presentation and miniboss escort pass

- Hid the retired `ABY_HexgunRelaySigil` from active sigil storage/use paths by making the legacy ThingDef inert and removing it from the Sigil Vault accepted list while preserving save migration into ember hound sigils.
- Added a real `ABY_BossProfile_RiftButcher` with `rift_butcher_escort` encounter pool routing, doctrine/template coverage, and pool membership on existing abyssal warforms.
- Reworked direct miniboss support spawning to prefer local boss-anchor escort placement with edge fallback, improving encounter cohesion for Choir Engine, Warden of Ash, and Rift Butcher without changing Archon/Reactor manifestation bosses.
- Added ritual-specific arrival presentation pulses/VFX for unstable breach, ember hunt, Warden of Ash, Choir Engine, and Rift Butcher so lower-tier sigils no longer share one generic spawn feel.
- Rewrote EN/RU lore letters for active sigil categories and added missing Archon/Dominion completion-key routing where needed.
- Build verified by direct Roslyn compile against bundled RimWorld/Unity/Harmony/.NET Framework-style references. XML parse validation passed. RimWorld runtime smoke testing is still required.

Changed areas:

```text
Assemblies/AbyssalProtocol.dll
Assemblies/AbyssalProtocol.pdb
source/World/Buildings/Summoning/Building_AbyssalSummoningCircle.cs
source/World/Buildings/Summoning/Building_ABY_SigilVault.cs
Defs/Misc/ABY_BossDifficultyProfiles.xml
Defs/Misc/ABY_EncounterTemplates.xml
Defs/Misc/ABY_ThreatDoctrines.xml
Defs/PawnKindDefs/* abyssal escort pool memberships
Defs/ThingDefs/ABY_HexgunThrall_Content.xml
Defs/ThingDefs/ABY_Items.xml
Defs/ThingDefs/ABY_DominionCrisis_Content.xml
Languages/English/** sigil letter/localization keys
Languages/Russian/** sigil letter/localization keys
Docs/CONTENT_MATRIX.md
Docs/KNOWN_RISKS_AND_REGRESSIONS.md
Docs/RECENT_WORK.md
```

## 2026-05-23 — Static cache memory-retention hardening

- Hardened `ABY_ResidueSinteringConsoleUtility` so Forge crucible status caching no longer uses `Map` as a static dictionary key. The cache now keys by `map.uniqueID`, validates cached focus crucibles against the live map, and periodically removes entries for maps no longer present in `Find.Maps`.
- Hardened `ABY_LogThrottleUtility` with normalized bounded keys, expired/stale entry pruning, a maximum tracked-key cap, and a best-effort `Clear()` path so repeated compatibility/runtime warnings cannot grow an unbounded static dictionary during long modpack sessions.
- Build verified by direct Roslyn compile against bundled RimWorld/Unity/Harmony/.NET Framework-style references. This is a memory-retention risk reduction pass, not a gameplay/content change. RimWorld runtime smoke testing is still required.

Changed areas:

```text
Assemblies/AbyssalProtocol.dll
Assemblies/AbyssalProtocol.pdb
source/Core/Utilities/ABY_LogThrottleUtility.cs
source/UI/Forge/ABY_ResidueSinteringConsoleUtility.cs
Docs/KNOWN_RISKS_AND_REGRESSIONS.md
Docs/RECENT_WORK.md
```

## 2026-05-23 — Dominion Gravplate T5 armor reprototype

- Reworked the unused `ABY_AbyssalGravplatePrototype` armor line into a playable Tier V Dominion Gravplate shell + helm with Abyssal Forge recipes, active `ABY_PR_DominionSurvivalFrames` Protocol Nexus gating, EN/RU DefInjected text, and updated glossary terms.
- The shell now uses the existing armor Aegis runtime (`Apparel_ABY_ArmorAegis`) in addition to the existing hover presentation path: lower capacity than Crowned Core plate, faster recharge, no outgoing-fire block, no shield stacking with external shield belts, and EMP drain vulnerability.
- Balance intent: mobile late-Dominion assault/survival armor. It sits beside Crowned Core plate rather than replacing it: less raw/capacity-focused than Crowned Core, but much faster while drafted through the gravplate hover lattice.
- C# build not required: the patch only reuses already compiled apparel Aegis/hover systems and changes XML/localization/docs. XML parse validation passed; RimWorld runtime smoke testing is still required.

Changed areas:

```text
Defs/ThingDefs/ABY_AbyssalGravplatePrototype_Placeholder.xml
Defs/RecipeDefs/ABY_AbyssalGravplatePrototype_Recipes.xml
Defs/Experimental/ProtocolResearch/ABY_ProtocolResearchProjects.xml
Languages/English/DefInjected/ThingDef/ABY_AbyssalGravplatePrototype.xml
Languages/English/DefInjected/RecipeDef/ABY_AbyssalGravplatePrototype_Recipes.xml
Languages/English/Keyed/ABY_ApparelAegis_Strings.xml
Languages/Russian/DefInjected/ThingDef/ABY_AbyssalGravplatePrototype.xml
Languages/Russian/DefInjected/ThingDef/ABY_RU_Audit_Missing_ThingDef.xml
Languages/Russian/DefInjected/RecipeDef/ABY_AbyssalGravplatePrototype_Recipes.xml
Languages/Russian/Keyed/ABY_ApparelAegis_Strings.xml
Docs/CONTENT_MATRIX.md
Docs/LOCALIZATION_GLOSSARY_RU.md
Docs/RECENT_WORK.md
```

## 2026-05-23 — Weapon icon and ground footprint normalization

- Normalized all current Abyssal weapon presentation assets that use weapon/item equipment textures: transparent PNG bounds were trimmed/re-padded so the visible weapon fills the texture predictably without green/chromakey or fake transparency.
- Added explicit `uiIconScale` values to 31 weapon ThingDefs so equipped-weapon gizmos, vanilla info cards, and inventory/gear icons no longer collapse into tiny thin silhouettes.
- Recalibrated weapon `graphicData.drawSize` values against the normalized texture canvases for ground sprites, fixing flattened or over-stretched weapons while keeping `ABY_CrownReactorMultilance` draw size unchanged because its beam alignment is draw-size sensitive.
- This was XML + texture work only; no C# files or assemblies were changed. XML parse validation passed; RimWorld runtime visual smoke testing is still required.

Changed areas:

```text
Defs/ThingDefs/ABY_*.xml weapon defs
Textures/Things/Weapon/*.png weapon assets
Textures/Things/Item/Equipment/WeaponRanged/*.png weapon assets
Docs/KNOWN_RISKS_AND_REGRESSIONS.md
Docs/RECENT_WORK.md
```

## 2026-05-23 — Reactor Choir Minigun integration

- Added the T5 `ABY_ReactorChoirMinigun` as a Saint-engineering heavy plasma suppression weapon with Abyssal Forge craftability, Protocol Nexus gate `ABY_PR_SaintAegisEngineering`, Horde Fragment/Reactor Saint Core material gating, and EN/RU DefInjected text.
- Added optimized transparent weapon, projectile, compact muzzle flash, and vent-burst textures extracted from chromakey sources; final mod PNGs are sized for RimWorld use and not shipped with green backgrounds.
- Added `Projectile_ReactorChoirPlasmaSlug` and `ReactorChoirMinigunVfxUtility` so every slug spawns the compact muzzle flash, budgeted light travel/impact feedback, and a threshold thermal-saturation vent burst without per-tick beam damage or map-wide scans.
- Added `ABY_ReactorChoirThermalSaturation` as the stacking heat-softening hediff and reused existing Ultra Plasma audio clips through new lower-volume Reactor Choir SoundDefs instead of adding unverified new SFX binaries.
- Build verified by direct Roslyn compile against bundled RimWorld/Unity/Harmony/.NET Framework-style references. Standard `dotnet build` still fails in this sandbox because the .NET Framework 4.7.2 targeting pack is unavailable. Runtime smoke testing in RimWorld is still required.

Changed areas:

```text
Assemblies/AbyssalProtocol.dll
source/Combat/Projectiles/Weapons/Projectile_ReactorChoirPlasmaSlug.cs
source/Combat/VFX/ReactorChoirMinigunVfxUtility.cs
Defs/ThingDefs/ABY_ReactorChoirMinigun.xml
Defs/HediffDefs/ABY_ReactorChoirMinigun_Hediffs.xml
Defs/ThingDefs_Motes/ABY_ReactorChoirMinigun_Motes.xml
Defs/SoundDefs/ABY_ReactorChoirMinigun_Sounds.xml
Textures/Things/Weapon/ABY_ReactorChoirMinigun.png
Textures/Things/Projectile/ABY_ReactorChoirPlasmaSlug.png
Textures/Things/VFX/ReactorChoirMinigun/ABY_ReactorChoirMuzzleFlash_01.png
Textures/Things/VFX/ReactorChoirMinigun/ABY_ReactorChoirVentBurst_01.png
Languages/English/DefInjected/ThingDef/ABY_ReactorChoirMinigun.xml
Languages/Russian/DefInjected/ThingDef/ABY_ReactorChoirMinigun.xml
Languages/English/DefInjected/HediffDef/ABY_ReactorChoirMinigun_Hediffs.xml
Languages/Russian/DefInjected/HediffDef/ABY_ReactorChoirMinigun_Hediffs.xml
Docs/CONTENT_MATRIX.md
Docs/RECENT_WORK.md
```

## 2026-05-23 — Crown Reactor Multilance Four-Rail Verdict pass

- Reworked `Thing_CrownReactorBeamSequence` from four identical damage pulses into the Four-Rail Verdict sequence: acquisition/lock, shield-system shear, short overline penetration, and capped crown-verdict execution.
- Preserved the faster dot-based charge presentation and invisible sequence-controller fallback so the weapon no longer holds visible alignment artifacts on-screen.
- Added bounded smart retargeting when the original target dies mid-sequence, limited to nearby hostile targets around the original impact cell.
- Added a short overline secondary hit path behind the target and a small one-time final rupture pulse for wasted final shots; both are bounded and do not use map-wide scans or per-tick damage.
- Added `ABY_CrownReactorChargeDot.png` as a compact transparent charge-dot texture.
- Build verified by direct Roslyn compile against bundled RimWorld/Unity/Harmony/.NET Framework-style references. Runtime smoke testing in RimWorld is still required.

Changed areas:

```text
Assemblies/AbyssalProtocol.dll
Assemblies/AbyssalProtocol.pdb
source/Combat/VFX/Thing_CrownReactorBeamSequence.cs
Defs/ThingDefs/ABY_CrownReactorMultilance.xml
Textures/Things/Projectile/ABY_CrownReactorChargeDot.png
Textures/Things/Projectile/ABY_CrownReactorBeamSequence_Invisible.png
Languages/English/DefInjected/ThingDef/ABY_CrownReactorMultilance.xml
Languages/Russian/DefInjected/ThingDef/ABY_CrownReactorMultilance.xml
Docs/CROWN_REACTOR_MULTILANCE.md
Docs/KNOWN_RISKS_AND_REGRESSIONS.md
Docs/RECENT_WORK.md
```

## 2026-05-23 — Protocol Nexus authority pass 2A and Saint Aegis Engineering

- Added the active Protocol Nexus project `ABY_PR_SaintAegisEngineering` for Reactor Saint reward engineering, Saint Aegis protection, condensation cells, Saint-core implants, Vesper/Ultra Plasma weapons, and reactor-grade Forge patterns.
- Re-routed current Forge unlock extensions through explicit Protocol Nexus gates instead of residue/name inference, including the previous authority baseline for all current Forge unlocks.
- Refined T4/T5 routing: implemented turret modules remain on active `ABY_PR_ModularTurretInterface`, `ABY_PR_BreachLockdownSystems`, or Dominion material authority where appropriate; current playable content still avoids futureReserve nodes such as `ABY_PR_ApexWeaponry` and `ABY_PR_CrownfireSepulcherCalibration`.
- Included the material-cache static-startup fix because the supplied archive did not contain it; `ABY_MaterialCacheUtility` no longer touches `Find.TickManager` during static constructor material creation.
- Build verified by direct Roslyn compile against bundled RimWorld/Unity/Harmony/.NET Framework-style references. Runtime smoke testing in RimWorld is still required.

Changed areas:

```text
Assemblies/AbyssalProtocol.dll
Assemblies/AbyssalProtocol.pdb
source/Core/Utilities/ABY_MaterialCacheUtility.cs
Defs/Experimental/ProtocolResearch/ABY_ProtocolResearchProjects.xml
Defs/RecipeDefs/
Defs/ThingDefs/
Docs/CONTENT_MATRIX.md
Docs/KNOWN_RISKS_AND_REGRESSIONS.md
Docs/RECENT_WORK.md
```

## 2026-05-22 — Accessory stacking and encounter routing balance pass

- Connected `ABY_HaloHusk` to late encounter routing as an intentionally rare Final Gate easter-egg elite: it can appear in Reactor Saint escort / Dominion support / Dominion wave pools, but keeps `maxPlanCount` 1, `allowFutureAutoEscalation` false, and an extremely low selection weight.
- Raised boss escort fallback budgets without changing boss/miniboss `budgetCost`: Warden, Archon Beast, Reactor Saint, and Archon of Rupture now have slightly more room for corrected escort units after the enemy range/value passes.
- Rebalanced backpack, boot, glove, and vambrace stat offsets so accessory slots no longer stack full movement, hauling, work, shooting, and melee bonuses all at once.
- Raised accessory residue recipe costs moderately to slow colony-wide mass stacking while preserving each piece as earned Forge progression.
- Softened early/mid weapon outliers: Rift Carbine now has slightly slower cadence and a higher Forge unlock threshold, Sigil Repeater has a higher unlock/value, and Phalanx Driver has a higher MarketValue.
- Adjusted non-boss frontline budget values for Ember Hound, Breach Brute, and Gate Warden.
- XML-only change; C# build not required.

## 2026-05-22 — Enemy budget and player value balance pass

- Applied an XML-only balance/value pass after the enemy range role pass.
- Updated combatPower/budgetCost for non-boss ranged and elite hostile roles only: Hexgun Thrall, Rift Sapper, Null Priest, Rift Sniper, Halo Husk, and Siege Idol/Siege Idol Escort.
- Deliberately did not use budgetCost as a boss/miniboss balance lever for Reactor Saint, Archon Beast, Warden of Ash, or Rift Butcher; those should remain balanced through summon costs, escorts, phase logic, shields/HP, cooldowns, rewards, and runtime encounter design.
- Added or raised MarketValue/economy pressure for high-impact player weapons: Ultra Plasma Rifle, Oblivion Choir, Crownspike Rail, Vesper Lance, Gatebreaker Spiker, Null Marksman Rifle, Rift Carbine, and Hex Pistol.
- Reworked Ultra Plasma Rifle cost away from excessive bulk steel/plasteel and toward a more appropriate post-Saint mix of residue, spacer components, uranium, gold, and reduced bulk materials.
- Shortened Hex Pistol range to keep it as a sidearm instead of a cheap universal 30-cell weapon.
- Raised Saint Aegis Carapace MarketValue to better reflect its armor and Aegis shield utility.
- Abyssal Attunement was intentionally left unchanged because its levels are expensive and residue-earned; future tuning should only happen after real progression tests, not as a blind nerf.
- C# build not required; XML parse should be checked when packaging.

# Abyssal Protocol — Recent Work Notes

## 2026-05-23 — Add Crownless Adjudicator T4 hostile prototype

- Added `ABY_CrownlessAdjudicator` as a common T4 ordinary support/marksman monster to thicken the previously thin severe-tier roster without touching boss/miniboss balance.
- Added XML-only enemy race/PawnKind content plus an enemy-only `ABY_EdictLance` and `ABY_EdictLanceBolt` using existing abyssal ranged AI/shooter comps, so no new combat framework was introduced.
- Routed the unit into severe/late pools: Archon escort, Reactor Saint escort, Dominion wave, Dominion gate support, and Horde Sigil wave.
- Added south/east/north pawn textures only, weapon/projectile icons, EN/RU localization, and a Bestiary entry.
- Build verified by direct Roslyn compile against bundled RimWorld/Unity/Harmony/.NET Framework-style references. Runtime smoke testing in RimWorld is still required.

Changed areas:

```text
Assemblies/AbyssalProtocol.dll
Assemblies/AbyssalProtocol.pdb
source/UI/Bestiary/ABY_BestiaryGameComponent.cs
Defs/ThingDefs/ABY_CrownlessAdjudicator_Content.xml
Defs/PawnKindDefs/ABY_CrownlessAdjudicator_PawnKinds.xml
Textures/Pawn/CrownlessAdjudicator/
Textures/Things/Weapon/ABY_EdictLance.png
Textures/Things/Projectile/ABY_EdictLanceBolt.png
Languages/English/DefInjected/ThingDef/ABY_CrownlessAdjudicator_Content.xml
Languages/Russian/DefInjected/ThingDef/ABY_CrownlessAdjudicator_Content.xml
Languages/English/DefInjected/PawnKindDef/ABY_CrownlessAdjudicator_PawnKinds.xml
Languages/Russian/DefInjected/PawnKindDef/ABY_CrownlessAdjudicator_PawnKinds.xml
Languages/English/Keyed/ABY_Bestiary_Strings.xml
Languages/Russian/Keyed/ABY_Bestiary_Strings.xml
Docs/CONTENT_MATRIX.md
Docs/RECENT_WORK.md
```

## 2026-05-22 — Complete current monster Bestiary coverage
- Added missing Bestiary tracking entries for `ABY_HaloHusk`, `ABY_RiftButcher`, and `ABY_ArchonOfRupture` so all current hostile PawnKind races resolve into the threat codex through kind or race fallback.
- Added English and Russian Bestiary localization for Halo Husk, Rift Butcher, Reliquary Archon Beast, and Archon of Rupture.
- Escort PawnKinds such as `ABY_BreachBruteEscort` and `ABY_SiegeIdolEscort` remain intentionally resolved through their tracked race entries instead of separate duplicate codex cards.
- Build verified by direct Roslyn compile against bundled RimWorld/Unity/Harmony/.NET Framework-style references. Runtime smoke testing in RimWorld is still required.

Changed areas:

```text
Assemblies/AbyssalProtocol.dll
source/UI/Bestiary/ABY_BestiaryGameComponent.cs
Languages/English/Keyed/ABY_Bestiary_Strings.xml
Languages/Russian/Keyed/ABY_Bestiary_Strings.xml
Docs/CONTENT_MATRIX.md
Docs/RECENT_WORK.md
```

## 2026-05-22 — Harden horde portal placement around power grids
- Added horde/portal placement guards so imp portals and command gate nodes no longer spawn on hidden conduits, power buildings, blueprints, frames, or other building-category things that can be vanished by `WipeMode.Vanish`.
- Broadened horde perimeter building checks to treat neutral/unfactioned power-network utilities as sensitive map infrastructure, not empty ground.
- Updated abyssal hostile building targeting so custom abyssal breach logic ignores non-combat power utilities such as generators, batteries, conduits, solar, wind and watermill power while still allowing turrets, doors, barriers and walls as tactical targets.
- This prevents future horde encounters from silently cutting hard-to-see power grids or intentionally selecting generators as custom breach targets. Existing saves with already-missing conduits still need the broken power-net segment rebuilt manually.
- Build verified by direct Roslyn compile against bundled RimWorld/Unity/Harmony/.NET Framework-style references. Runtime smoke testing in RimWorld is still required.

Changed areas:

```text
Assemblies/AbyssalProtocol.dll
source/Core/GameComponents/MapComponent_AbyssalPortalWave.cs
source/Encounters/AbyssalThreatPawnUtility.cs
Docs/KNOWN_RISKS_AND_REGRESSIONS.md
Docs/RECENT_WORK.md
```

## 2026-05-22 — Fix Forge implant subfilter placement
- Corrected Forge implant subfilter routing so `Cohort Sync Subnode` appears under Brain, optic/sight implants appear under Eyes, and stomach implants appear under Organs instead of falling back to Body.
- Added explicit identity overrides before broad text matching to avoid description/summary keywords misclassifying implant recipes.
- Restored Forge craftability for `ABY_InfernalEye` by adding the Abyssal Forge recipe user and bionic unfinished-item definition to its `recipeMaker`.
- Build verified by direct Roslyn compile against bundled RimWorld/Unity/Harmony/.NET Framework-style references. Runtime smoke testing in RimWorld is still required.

Changed areas:

```text
Assemblies/AbyssalProtocol.dll
Assemblies/AbyssalProtocol.pdb
source/UI/Forge/Window_AbyssalForgeConsole.cs
Defs/ThingDefs/ABY_InfernalEye.xml
Docs/KNOWN_RISKS_AND_REGRESSIONS.md
Docs/RECENT_WORK.md
```

## 2026-05-21 — Fix miniboss classification for overhead HP bars
- Fixed `ABY_AbyssalPawnClassificationUtility.IsMajorBoss` so explicit `ABY_AbyssalPawnClassificationExtension.isMiniBoss=true` wins over legacy difficulty-scaling `role=boss` values.
- This specifically unblocks Warden of Ash and Choir Engine from the compact overhead HP-bar renderer: they still use boss-family encounter plumbing, but UI systems no longer filter them out as major bosses.
- No XML role changes were made, avoiding encounter-template/pool side effects.
- Build verified by direct Roslyn compile against bundled RimWorld/Unity/Harmony/.NET Framework-style references. Runtime smoke testing in RimWorld is still required.

Changed areas:

```text
Assemblies/AbyssalProtocol.dll
source/Core/Utilities/ABY_AbyssalPawnClassificationUtility.cs
Docs/CONTENT_MATRIX.md
Docs/KNOWN_RISKS_AND_REGRESSIONS.md
Docs/RECENT_WORK.md
```

## 2026-05-21 — Fix miniboss HP bars on existing saves
- Moved the active miniboss HP-bar draw call into the long-lived `AbyssalBossScreenFXGameComponent` OnGUI path so saves created before the new miniboss GameComponent still display bars after a DLL update.
- Kept `GameComponent_ABY_MiniBossHealthBars` as a save-compatibility fallback shell only; it does not double-draw when the main boss UI component exists.
- Improved miniboss overhead bar contrast and placement by reading `PawnKindDef.lifeStages.bodyGraphicData.drawSize` for large sprites such as Choir Engine.
- Build verified by direct Roslyn compile against bundled RimWorld/Unity/Harmony/.NET Framework-style references. Runtime smoke testing in RimWorld is still required.

Changed areas:

```text
Assemblies/AbyssalProtocol.dll
source/Bosses/Shared/AbyssalBossScreenFXGameComponent.cs
source/UI/BossBar/ABY_MiniBossHealthBarRenderer.cs
source/UI/BossBar/GameComponent_ABY_MiniBossHealthBars.cs
Docs/
```

## 2026-05-21 — Add lightweight miniboss custom-HP bars
- Added a compact overhead health bar path for abyssal minibosses that use `CompABY_BossTrueDeath` custom HP but should not occupy the full cinematic boss bar.
- Warden of Ash and Choir Engine are detected through the shared abyssal pawn classification helper and keep their separate custom HP readable in combat.
- Added a mod setting toggle for miniboss health bars; the bars also respect the global boss-bar enable switch and health-number visibility setting.
- Updated EN/RU localization and documentation so future UI work treats miniboss HP bars as part of the existing BossBar UI surface, not a separate parallel HUD.

Changed areas:

```text
Assemblies/AbyssalProtocol.dll
source/UI/BossBar/
source/Core/Bootstrap/
source/Core/Utilities/
Languages/English/Keyed/ABY_BossBar_Strings.xml
Languages/Russian/Keyed/ABY_BossBar_Strings.xml
Docs/
```

Build verified by direct Roslyn compile against bundled RimWorld/Unity/Harmony/.NET Framework-style references. Runtime smoke testing in RimWorld is still required.

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
Real .cs files under source/ excluding bin/obj: 447
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

## 2026-05-21 — Encounter validator root hardening and turret module diagnostics

- Reworked `ABY_EncounterValidationUtility` from one monolithic startup scan into staged diagnostic validation so individual pool, pawn-kind, doctrine, escalation or turret-module scan failures cannot collapse the whole report into a generic `NullReferenceException`.
- Added safe DefDatabase access, safe pawn scaling extension inspection, safe def-name formatting and verbose-only fallback logging for unexpected diagnostic-stage exceptions.
- Added explicit `ABY_TurretModuleDef` validation because the recent passive/aegis turret module expansion increased the chance that malformed module XML could surface only during startup diagnostics.
- The validator remains diagnostic-only: it reports concrete data issues but does not rewrite defs, block encounters, alter turret mechanics or hide gameplay failures.
- Build verified with direct local Roslyn compile against bundled RimWorld/Unity/Harmony libraries. Runtime smoke testing in-game is still required.

## 2026-05-21 — Cross-save runtime target cache hardening
- Hardened `ABY_RuntimeTargetCache` against map `uniqueID` reuse across save switches by binding each cache entry to the actual `Map` instance, not just the numeric ID.
- Startup diagnostics now clears runtime target caches on game finalization so stale pawns/buildings from a previously loaded save cannot be reused by turrets or combat helpers.
- Modular turret runtime burst targets are no longer restored from saves; turrets reacquire targets after load instead of carrying serialized pawn references that may be stale or partially initialized.
- This addresses reports of modular turrets firing at empty cells and killing an apparently invisible `ABY_EmberHound` after switching to a different save.

## 2026-05-21 — VFX budget and Dominion reference hygiene hardening
- Hardened `ABY_VfxBudget` against cross-save map `uniqueID` reuse by binding budget entries to actual `Map` instances, resetting windows when game ticks move backwards, and clearing all VFX budget state during game finalization.
- Replaced Dominion slice encounter per-tick `RemoveAll` lambdas with reverse `for` cleanup loops to avoid closure allocation during active encounters.
- Throttled Dominion slice `RestoreReferencesFromMap` fallback scans so full `AllThings` recovery runs on load/forced recovery or short fallback intervals instead of every tick while references are incomplete.
- Added deterministic armor Aegis selection tie-breaking so equal-capacity Aegis apparel chooses by recharge quality and stable def name instead of worn-list order.
- Build verified with direct local Roslyn compile against bundled RimWorld/Unity/Harmony libraries. Runtime smoke testing in-game is still required.

## 2026-05-21 — Miniboss HP bar map projection fix
- Replaced the lightweight miniboss HP bar's raw `Camera.WorldToScreenPoint` projection with RimWorld's `GenMapUI.LabelDrawPosFor` projection.
- This fixes the bar drifting toward a fixed screen/map position while the camera pans or UI scale changes; the overhead bar should now stay attached to the visible miniboss map label position.
- Reduced the large-pawn vertical offset clamp so Choir Engine's oversized graphic does not push the bar excessively far away from the sprite.
- Build verified with direct local Roslyn compile against bundled RimWorld/Unity/Harmony libraries. Standard `dotnet build` is still not verified in this sandbox because the .NET Framework 4.7.2 targeting pack is unavailable. Runtime smoke testing in-game is still required on the user's save/video scenario.

## 2026-05-22 — T5 passive turret module expansion

- Added three Dominion-tier passive turret modules: Dominion Verdict Lens, Crown Overpressure Choir, and Sepulcher Fail-Safe Mantle.
- The new modules fill T5 passive build roles without adding additional Aegis stacking: target adjudication, overpressure firing cadence, and chassis survival/recovery.
- The patch also keeps the turret module tier rebalance self-contained when applied over older archives: T5 now includes Crowncoil Gauss Minigun, Sepulcher Rail Core, Crownfire Rocket Choir, Crown Aegis Matrix, and the three new passive modules.
- XML/assets/docs only; no C# or assembly rebuild required.

## 2026-05-22 — Crafting requirement economy pass

- Rebalanced craft requirements so player-facing craftables consistently require Abyssal Residue plus vanilla resources.
- Reduced boss-drop pressure across implant grids: boss drops now gate signature weapons, brain/heart/spine/torso/arm pieces, Aegis/shield modules, and apex turret/weapon rewards instead of every ordinary slot.
- Added missing residue costs to high-tier Dominion/Horde implants and added Crafting skill gates to Rift Blade and Rift Carbine.
- Lowered excessive HordeFragment usage on Litany Grinder, Cohort Halberd, and Phalanx Driver.
- Added light CrownedCore/DominionShard gates to high-end turret modules and special late weapons without making every T5 passive consume multiple crown shards.

## 2026-05-22 — Rift Butcher post-horde miniboss integration

- Added Rift Butcher as the post-Horde / pre-Dominion miniboss gate with real pawn defs, pawn kind, transparent directional textures, sigil, reward core, hediffs, summoning console exposure, capacitor profile, sigil vault support, and progression tracking.
- Rift Butcher mechanics are C#-owned by `CompABY_RiftButcherCombat`: startup carapace, hook snare, short rift dash, severance sweep, low-health execution focus, and small threshold reinforcements.
- First Horde Gate containment is now recorded by `ABY_HordeAndButcherProgressionGameComponent`; Rift Butcher routing requires that recorded horde clear, and Dominion Gate routing requires the first Rift Butcher kill plus the new Severance Core crafting ingredient.
- The pawn uses the existing hover-apparel presentation path extended to support pawn-level `ABY_HoverArmorExtension`, keeping the hover/tether VFX centralized rather than adding a parallel pawn draw system.
- Build verification status is recorded in the patch handoff response.

## 2026-05-22 — Horde power-net recovery hardening

- Added `ABY_PowerNetRecoveryUtility` to force vanilla `PowerNetManager` to rebuild a map's power graph from the actual spawned power comps after rare horde/portal desyncs.
- Horde and ember portal waves now nudge a throttled power-net rebuild when a wave starts, when portals/command gates are opened or destroyed, and when the wave resets.
- Added a Summoning Circle dev gizmo, `DEV: rebuild power nets`, for save recovery when visible conduits remain connected but RimWorld behaves as if the map has two stale power networks.
- Choir Engine relay pulses and death bursts no longer EMP ordinary generators, batteries, conduits or other non-combat power utilities; suppression remains focused on turrets and hostile mechanoids.
- Build verification status is recorded in the patch handoff response. Runtime smoke testing should check the isolated Harmony+DLC horde save where one power net showed deficit while another showed large excess.

## 2026-05-22 — Safer horde power-net recovery compatibility pass

- Softened `ABY_PowerNetRecoveryUtility` for public modpack compatibility: automatic horde recovery now performs only a vanilla `PowerNetManager` graph rebuild and overlay refresh.
- Moved the intrusive global `CompPower.TryManualReconnect(false)` pass behind the Summoning Circle dev-only manual recovery gizmo instead of running it automatically during portal waves.
- Removed automatic recovery from every individual portal-open and command-gate-spawn event; horde/ember waves now queue a delayed soft recovery at wave start and wave reset/collapse points only.
- This keeps the emergency tool for affected saves while reducing the chance of resetting intentional disconnect/reconnect states from other power-system mods.
- Build verification status is recorded in the patch handoff response. Runtime smoke testing should still include the isolated Harmony+DLC horde save and at least one large modpack with power-related mods before workshop release.

## 2026-05-22 — Boss portal retry and no-downed hot-path hardening

- Confirmed the horde power-grid safety and safe power-net recovery fixes are present in the supplied local archive before applying this patch.
- Added throttled retry state to `Building_AbyssalRupturePortal` so a blocked boss release no longer scans radial cells every tick while enemies occupy the portal perimeter. After repeated blocked attempts it expands the search radius and retries less often instead of busy-looping.
- Added save-safe label migration for rupture portal boss labels: legacy hardcoded `Archon of Rupture` labels are resolved through the localized `ABY_BossName` key on load.
- Hardened `CompABY_BossNoDowned` so damage and tick callbacks cannot run duplicate no-downed recovery in the same tick, and so persistent downed states retry on a short cadence rather than dirtying health caches every tick.
- Changed `AbyssalBossNoDownedUtility` to batch injury/clamp changes and call health cache/state refresh at most once per recovery pass plus one fallback refresh, instead of per-heal-pass.
- Added Harmony priorities to boss true-death health/death patches so boss suppression runs early for death/downed prefixes and late for `ShouldBeDead`/`ShouldBeDowned` postfix result correction.
- Build verified by direct Roslyn compile against bundled RimWorld/Unity/Harmony libraries.

## 2026-05-22 — Full runtime hardening audit pass

- Added `ABY_MaterialCacheUtility` and routed Abyssal material creation through a quantized shared cache so pulse-driven draw colors no longer create unbounded `MaterialPool` variants in VFX/building/projectile/apparel draw paths.
- Hardened `ABY_SafeSpawnUtility`: safe spawns no longer fall back to `map.Center` with `WipeMode.Vanish`; spawn cells now reject pawns, buildings, blueprints, frames and other building-category blockers before spawning.
- Hardened pawn transfer so a missing safe destination aborts before the pawn is despawned from the source map.
- Reworked `CompABY_BreachDirective` target acquisition to scan colonist buildings instead of `map.listerThings.AllThings` in horde/breach hot paths.
- Reworked `Projectile_SpecterLashAnchor` impact target fallback to scan radial cells around impact instead of every thing on the map.
- Hardened imp and rupture portals against blocked spawn cells: blocked imp spawns retry instead of consuming wave count, and rupture boss spawning uses safe spawn checks plus wider fallback radius.
- Made delayed horde power-net recovery state save/load-safe and cleared power/material runtime caches during game finalization.
- Fixed null-safe Oblivion Choir scar load cleanup and translated the Russian Rift Butcher label.
- Build verified by direct Roslyn compile against bundled RimWorld/Unity/Harmony libraries. Standard `dotnet build` still fails in sandbox because .NET Framework 4.7.2 targeting pack is unavailable. Runtime smoke testing in RimWorld is still required.

## 2026-05-22 — Manifestation and Dominion transfer safety follow-up

- Hardened hostile arrival manifestations so temporary manifestation buildings are spawned through `ABY_SafeSpawnUtility` and can no longer wipe buildings, blueprints, frames, pawns, or other building-category infrastructure on otherwise standable cells.
- Hardened `CompABY_BreachArrival` so blocked arrival VFX no longer falls back to spawning on the pawn's own position; if no safe cell exists, the visual manifestation is skipped instead of forcing an unsafe spawn.
- Hardened manifested hostile pack spawning so individual pawns require safe cells and no longer fall back to the requested root cell when the area is blocked.
- Hardened Dominion pocket transfer/return fallback paths to use safe spawn predicates and rollback-safe restore attempts instead of direct `GenSpawn.Spawn(..., WipeMode.Vanish)` on minimally checked cells.
- Made Dominion slice reference-restore throttle state save/load-aware by scribing `nextReferenceRestoreTick`.
- Re-encoded the two Sigil song OGG files as pure Vorbis audio-only streams, removing embedded Theora cover/video streams that could confuse Unity/RimWorld audio import.
- Build verified by direct Roslyn compile against bundled RimWorld/Unity/Harmony libraries. Runtime smoke testing in-game is still required.
## 2026-05-22 — Reward and implant gate consistency balance pass

- Removed the duplicate Hexgun Thrall butcher residue route so it no longer pays both killed leavings and butcher-product residue.
- Raised the Ember Hound sigil residue ingredient to reduce early residue-positive summon farming while keeping the encounter rewarding.
- Added light trophy-material gates to strong Horde/Archon/Choir/Reactor/Dominion implant lines that previously relied only on residue and vanilla materials.
- Raised Dominion-tier implant market values so late-body stacking is reflected more honestly in colony wealth and reward valuation.
- Raised the Vesper Lance forge unlock residue threshold so the post-Saint precision weapon remains a premium reward rather than an immediate universal answer.
- XML/docs only; no C# or assembly rebuild required.


## 2026-05-22 — C# balance constant sync

- Synchronized hardcoded C# encounter fallback budgets with the recent XML enemy range/budget and escort balance passes.
- Updated Summoning Circle pending escort fallback values for Warden of Ash, Archon Beast, Archon of Rupture, and Reactor Saint so ritual previews/shadow escort budgets no longer fall back to stale lower values.
- Updated Reactor Saint, Archon Beast manifestation/portal, and Rupture Portal escort calls to use the same post-pass escort budgets as XML.
- Updated Reliquary Archon escort fallback to remain heavier than the normal Archon escort after the normal fallback was raised.
- Updated T1 summon threat constants for Thrall, Sapper, Zealot, Priest, and Sniper so forecast/shadow threat math matches the revised pawn budget values.
- Build verified by direct Roslyn compile against bundled RimWorld/Unity/Harmony libraries. Standard `dotnet build` still requires the .NET Framework 4.7.2 targeting pack in this sandbox.


## 2026-05-23 — Crown Reactor Multilance barrel-locked VFX refinement

- Reintroduced the T5 Crown Reactor Multilance weapon content into the current archive: ThingDef, beam-sequence ethereal thing, verb, texture, projectile beam segment, and EN/RU DefInjected text.
- Tightened the multilance visual profile so charge bars now begin at the barrel bank rather than floating too far forward, and the firing origin is constrained closer to the barrel tips.
- Reduced charge and discharge beam widths substantially and compressed the four lane offsets so both the warmup rails and the fired beams track the actual four barrels much more closely instead of rendering oversized cyan slabs.
- Kept the damage/runtime model unchanged: four discrete damage pulses, no per-tick beam damage, no map-wide scans.
- Build verified by direct Roslyn compile against bundled RimWorld/Unity/Harmony libraries. Runtime smoke testing in RimWorld is still required.


## 2026-05-23 — Fix Crown Reactor raw sequence sprite regression

- Fixed `ABY_CrownReactorBeamSequence` XML so the transient beam controller uses a `MoteBase`-style def instead of a standalone map-mesh thing.
- Replaced the sequence def's visible beam texture with a 1x1 transparent safety texture so any fallback/raw Thing graphic draw is invisible and cannot render as a horizontal beam sprite on the pawn or target.
- Kept the custom `Thing_CrownReactorBeamSequence.DrawAt` beam rendering and four-pulse damage logic unchanged.
- XML/asset-only fix; C# build not required. Runtime smoke testing in RimWorld is still required.

## 2026-05-23 — Finalize Crownless Adjudicator pawn art and hide duplicated weapon draw

- Replaced Crownless Adjudicator pawn sprites with the final chromakey-extracted transparent South/East/North art.
- Kept the Edict Lance as the functional forced enemy weapon, but made its world/equipment graphic transparent because the finalized pawn art already contains the lance.
- Bestiary continues to use the east-facing Crownless Adjudicator portrait through `Pawn/CrownlessAdjudicator/ABY_CrownlessAdjudicator_east`.
- Build verified after the combined content + asset pass.

## Summoning Console ritual dossier follow-up

- Added a dedicated Summoning Console ritual dossier window opened from the invocation control panel.
- The main console now stays compact while the dossier exposes the expanded ritual forecast, reward routing, side effects, readiness breakdown, and active ritual telemetry in a separate scrollable window.
- This follows the redesign rule that dense ritual data should be available on demand instead of permanently occupying the primary ritual selection screen.

## 2026-05-24 — Summoning Console layout cleanup pass

- Reworked the redesigned Summoning Console from a crowded three-column layout into a two-column primary layout: ritual selection on the left and a selected-ritual action card on the right.
- Moved invocation controls, risk state, blocker text, dossier/codex/jump actions, and the main invocation button into the selected ritual card to avoid the previous narrow middle-column overlap.
- Replaced the always-visible three-panel lower infrastructure area with a single tabbed infrastructure drawer: Readiness, Capacitors, and Stabilizers.
- Kept the ritual dossier window as the long-form information route and kept the primary console focused on selection and action.
- Build verified by direct Roslyn compile against bundled RimWorld/Unity/Harmony libraries. Runtime smoke testing in RimWorld is still required.


## 2026-05-24 — Move Summoning Circle slots to infrastructure window

- Removed the lower permanent infrastructure drawer from the main Summoning Console primary layout.
- Added a compact Circle Infrastructure callout to the selected-ritual action card showing capacitor/stabilizer counts and current support/pattern summary.
- Added a dedicated Circle Infrastructure window for Readiness, Capacitor lattice, and Stabilizer ring management, reusing the existing slot install/remove panels.
- Main Summoning Console is now focused on ritual selection and action; ritual dossier remains the long-form ritual data route; infrastructure window owns circle slot management.
- Build verified by direct Roslyn compile against bundled RimWorld/Unity/Harmony libraries. Runtime smoke testing in RimWorld is still required.


## 2026-05-24 — Simplify Summoning Circle infrastructure window

- Simplified the dedicated Circle Infrastructure window into a compact one-screen slot manager.
- Removed the Readiness / Capacitors / Stabilizers tab strip from that window; readiness remains available through the main console and ritual dossier routes.
- The window now shows capacitor lattice rows, stabilizer ring rows, and a short effect summary with tooltip-backed details.
- Kept the existing install/remove slot row logic so behavior stays consistent with the earlier implementation.
- Build verified by direct Roslyn compile against bundled RimWorld/Unity/Harmony libraries. Runtime smoke testing in RimWorld is still required.

## 2026-05-24 — Circle Infrastructure window fit polish

- Extended the compact Circle Infrastructure window and removed the bottom action area pressure so the stabilizer rows and Effect summary no longer clip at common UI scales.
- Kept the window as a focused slot manager: capacitor lattice, stabilizer ring, and short effect summary only.
- Replaced raw capacitor/stat tooltip output with player-facing explanatory text.


## 2026-05-24 — Harden progression milestones and runtime caches

- Added explicit runtime-state clearing for the static abyssal dash active-pawn id cache during game initialization to prevent stale dash state after returning to menu or loading another save in the same RimWorld session.
- Hardened First Boss and Reactor Saint kill progression tracking to mirror the Rift Butcher fix: save-compatible processed pawn lists are preserved, runtime `HashSet` lookups are rebuilt after load, and milestone letters are isolated from `GameComponentTick` failures.
- Wrapped progression recap letters so broken `LetterStack`/localization behavior cannot invalidate already-recorded boss milestone state.
- Added a short-lived active-encounter query cache and switched repeated portal def lookups to `ABY_DefCache` for Summoning Console/readiness paths.
- Added a shared Dominion Slice encounter resolver so ambient VFX MapComponents avoid repeated per-tick `Map.GetComponent<MapComponent_DominionSliceEncounter>()` scans on maps without an active slice encounter.
- Build verified by direct Roslyn compile against bundled RimWorld/Unity/Harmony libraries. Runtime smoke testing in RimWorld is still required.

## 2026-05-24 — Optimize modular turret scoring and Dominion crisis lookups

- Reduced modular turret targeting score pressure by building a single filtered candidate buffer per scan and reusing it for line/cluster priority scoring instead of rescanning all spawned pawns for every candidate.
- Added bounded expensive line-of-sight checks for line/cluster targeting bonuses so large late-game waves cannot create unbounded targeting spikes during acquisition.
- Added a cached Dominion crisis resolver and routed Dominion Gate/Anchor tick, inspect, console, and destruction paths through it instead of repeated direct `Map.GetComponent<MapComponent_DominionCrisis>()` calls.
- Removed the `Projectile_AshenScatterShell` impact-time `.ToList()` allocation by iterating radial cells directly.
- Cached Gate Warden escort anchor defs through `ABY_DefCache` so repeated escort scans no longer call `DefDatabase.GetNamedSilentFail` for the same anchor names.
- Build verified by direct Roslyn compile against bundled RimWorld/Unity/Harmony libraries. Runtime smoke testing in RimWorld is still required.

## 2026-05-24 — Harden UI scroll and letter delivery paths

- Added `ABY_LetterUtility.TryReceiveLetter` and routed direct `Find.LetterStack.ReceiveLetter` calls through it so broken `LetterStack`, target data, or localization cannot escape runtime progression, Dominion, Forge, summoning, or guidance transitions.
- Wrapped Abyssal custom scroll views in `try/finally` so `EndAbyssalScrollView` is called even if a card, tooltip, localization line, or third-party UI patch throws during draw.
- Reduced hover armor draw-path allocation by reusing a `MaterialPropertyBlock` for alpha draws instead of allocating a new block for every VFX draw call.
- Updated Aortic Chain Harrower combat scanning to use the cached Dominion Slice encounter resolver, `ABY_DefCache`, and runtime pawn cache instead of repeated `Map.GetComponent`, `DefDatabase`, and direct spawned-pawn scans.
- Build verified by direct Roslyn compile against bundled RimWorld/Unity/Harmony libraries. Runtime smoke testing in RimWorld is still required.
## 2026-05-24 — Letter utility recursion hotfix

- Fixed `ABY_LetterUtility.TryReceiveLetter` so it calls `Find.LetterStack.ReceiveLetter` instead of recursively calling itself.
- Kept the safe-letter wrapper pattern, but removed the P1 stack-overflow risk introduced during UI/letter hardening.
- Hardened turret module ITab scroll cleanup by resetting text/GUI state inside scroll `finally` paths.


## 2026-05-24 — Harden residual UI state and letter wrapper cleanup

- Added centralized UI state reset coverage for vanilla `Widgets.BeginScrollView` / `EndScrollView` windows in mod settings and diagnostics panels.
- Made `AbyssalStyledWidgets.EndAbyssalScrollView` fail-safe by resetting text/GUI state from its own `finally` block after scrollbar cleanup.
- Wrapped remaining `GUI.matrix` rotation draw helpers in `try/finally` so matrix/color state cannot leak after draw exceptions.
- Removed redundant `Find.LetterStack` pre-checks before `ABY_LetterUtility.TryReceiveLetter`; direct `Find.LetterStack.ReceiveLetter` usage remains isolated inside the safe wrapper.
- Build verified by direct Roslyn compile against bundled RimWorld/Unity/Harmony libraries. Runtime smoke testing in RimWorld is still required.

## 2026-05-24 — Reduce remaining support-comp scan pressure

- Updated Choir Engine Relay infrastructure and proximity checks to avoid temporary turret/other target lists, sorting, and direct full pawn scans during relay pulses.
- Routed Gate Warden escort threat selection, Halo Step proximity/hostile collection, and Harvester Essence interference checks through `ABY_RuntimeTargetCache.CombatTargetPawnsFor` while preserving per-call validation.
- Switched Harvester Essence hediff lookup to `ABY_DefCache` for consistency with other runtime comp paths.
- Build not verified in this environment; modified-file syntax was checked against the previous assembly and XML parse checks passed. Full DLL rebuild is still required before runtime testing.


## 2026-06-22 — Summoning Reliability Foundation

- Added `ABY_SummonPreflightReport` as the single side-effect-free readiness authority shared by direct sigil validation, the circle start transaction, Summoning Console blocker text, ritual dossier diagnostics, and the dev reliability pass. The report provides exact gates for unlock, circle state, power, interaction/focus cells, active encounter, capacitor authorization, sigil/operator state, and deferred arrival routing.
- Added `MapComponent_ABY_SummonEncounterRuntime`, a save-backed one-encounter-per-map lifecycle record with `Preparing`, `Active`, and terminal states. The circle begins preparation only after normal start gates pass and activates the record only after a concrete encounter route actually begins.
- Consolidated player-facing active encounter blockers in `AbyssalBossSummonUtility`: runtime lifecycle, Dominion crisis, active horde wave, portal/manifestation/command structures, and any live combat-capable Abyssal pawn. The exact blocker is short-cached for custom UI so the console does not full-scan the map every OnGUI pass.
- Added a state-based lifecycle watchdog. It can clear a blocked runtime record only after the map has no concrete Abyssal encounter signals for a short grace period. It does not permit a fresh summon after an arbitrary two-day timer, so a real boss, portal, horde or Dominion crisis remains authoritative.
- Added a ritual-dossier **Copy diagnostic report** action and EN/RU localization. Reports include circle state, selected ritual, preflight entries, runtime lifecycle, concrete blocker, horde/Dominion state, and installed modules.
- Extended the Dev Mode threat rehearsal gizmo with a non-mutating preflight reliability pass over all active rituals.
- Build verified by direct Roslyn compile against bundled RimWorld/Unity/Harmony/.NET Framework-style libraries. XML parsing and RimWorld runtime smoke testing are tracked separately; runtime smoke testing is still required.

Touched files:

```text
Assemblies/AbyssalProtocol.dll
Assemblies/AbyssalProtocol.pdb
source/Bosses/Shared/AbyssalBossSummonUtility.cs
source/World/Buildings/Summoning/Building_AbyssalSummoningCircle.cs
source/Summoning/ABY_SigilUseValidator.cs
source/Summoning/ABY_SummonPreflightReport.cs
source/Summoning/MapComponents/MapComponent_ABY_SummonEncounterRuntime.cs
source/UI/Summoning/AbyssalSummoningConsoleUtility.cs
source/UI/Summoning/Window_AbyssalSummoningConsole.cs
source/Diagnostics/ABY_SummonThreatRehearsalUtility.cs
Languages/English/Keyed/ABY_SummoningReliability_Strings.xml
Languages/Russian/Keyed/ABY_SummoningReliability_Strings.xml
Docs/AI_ARCHITECTURE.md
Docs/BUILD_AND_SOURCE_LAYOUT.md
Docs/AI_QUICK_INDEX.md
Docs/RECENT_WORK.md
Docs/CONTENT_MATRIX.md
Docs/KNOWN_RISKS_AND_REGRESSIONS.md
```
