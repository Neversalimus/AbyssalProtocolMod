# Abyssal Protocol

**Abyssal Protocol** is a summon-driven techno-infernal endgame expansion for **RimWorld 1.6**.

It is built around controlled hostile rituals, abyssal infrastructure, custom infernal UI, boss-driven progression, and reward-gated escalation. Instead of passively waiting for a stronger faction or simply researching better gear, the colony deliberately opens dangerous contact with the Abyss, survives what answers, and turns the remains into weapons, implants, sigils, and deeper access.

---

## Current Project State

**Stage:** Late active alpha / public testing candidate  
**Supported RimWorld version:** 1.6  
**Release status:** Repository build; no formal 1.0 release yet

Abyssal Protocol is no longer a small prototype or a narrow vertical slice. The current repository build contains a substantial playable framework with custom C# systems, XML defs, custom UI, assets, audio, localization, multiple hostile encounter layers, and a growing progression ecosystem.

At the same time, this is still **not** a content-complete 1.0 release. Balance, late-game coverage, modpack compatibility, save/load edge cases, Dominion runtime behavior, and encounter polish are still actively developing.

Use it as an ambitious alpha-stage endgame mod, not as a finished stable release.

---

## Core Gameplay Identity

Abyssal Protocol is designed as a **summon-driven infernal progression mod**.

The intended loop is:

1. Unlock forbidden abyssal access.
2. Build ritual and forge infrastructure.
3. Gather residue, sigils, and progression materials.
4. Trigger controlled hostile summons, manifestations, and domain breaches.
5. Survive abyssal entities, elites, minibosses, and bosses.
6. Convert rewards into stronger weapons, implants, artifacts, and deeper infernal-tech permission.

The target tone is:

**infernal, industrial, ritualized, hostile, high-tech, and boss-driven.**

This is not a generic fantasy demon faction. The mod is closer to forbidden ultra-tech ritual warfare: machines that behave like altars, symbols that act like executable protocols, and enemies that feel like infrastructure from a hostile dimension.

---

## Implemented Now

### Core infrastructure and UI

- **Abyssal Summoning Circle**
- **Abyssal Forge**
- Compact tabs plus full custom abyssal consoles
- Styled abyssal UI framework and button presentation
- Powered summon infrastructure
- Summon activation and hostile manifestation pipeline
- Sigil handling and **Sigil Vault** support
- Circle modules, capacitors, and instability systems
- Forge reward / pattern progression support

### Hostile encounter content

- **Rift Imp**
- **Ember Hound**
- **Hexgun Thrall**
- **Chain Zealot**
- **Rift Sniper**
- **Null Priest**
- **Warden of Ash**
- **Choir Engine**
- **Archon Beast** progression content
- **Reactor Saint** progression content
- Portal-driven hostile escalation and manifestation support
- Dominion / pocket-map related encounter systems under active stabilization

### Player-facing combat and reward content

- **Rift Carbine**
- **Rift Blade**
- **Ultra Plasma Rifle**
- **Specter Lash**
- **Vesper Lance**
- **Ashen Pike**
- **Ashen Scattergun**
- Modular turret-related content and targeting integration
- Implant content
- Herald-side content
- **Rupture Crown** item / hediff / ability systems
- Custom projectiles, VFX, sounds, and reward drops

### Repository and content pipeline

- XML defs
- C# source code
- Compiled assembly for the repository build
- Textures and effect assets
- Sound defs and audio assets
- EN/RU localization
- Patch-side integration files
- Project documentation for future AI-assisted development

---

## Recent Stabilization Focus

Recent work has been focused less on raw content expansion and more on making the mod behave better in real games and heavy modpacks.

Current stabilization areas include:

- Hostile target filtering for hidden / utility structures
- Modular turret threat targeting
- Hidden faction hostility and relation safety
- Dominion combat relation and gate UI regressions
- Startup-safe log throttling
- TPS runtime scan reduction
- VFX budget and texture-size optimization
- Custom UI hot-path optimization
- Forge readability and queue/browser UX cleanup
- Monster info-card icon normalization
- Russian localization cleanup and lore-facing description passes

This means the current alpha is already past the pure prototype stage, but the project is still in a testing-heavy phase.

---

## Implemented vs Still Expanding

### Already real in the current repository build

- Summoning infrastructure
- Forge and Summoning custom UI
- Residue and sigil-driven progression hooks
- Sigil Vault support
- Circle module / capacitor / instability systems
- Hostile manifestation framework
- Multiple abyssal unit types beyond the original first slice
- Miniboss and boss-side content expansion
- Several custom weapons and projectile pipelines
- Implant, Herald, and Rupture Crown systems
- Dominion-related runtime and encounter work
- Custom source code, textures, sounds, patches, and localization

### Still actively expanding

- Overall balance and encounter tuning
- First-hours progression clarity
- Broader ritual and multi-tier progression coverage
- More enemies, minibosses, and bosses beyond the current alpha spread
- Stronger late-game and post-boss escalation
- More breadth across weapons, armor, implants, and rewards
- Save/load edge-case hardening
- Large modpack compatibility testing
- Additional UI polish and systemic cleanup

---

## Testing Expectations

Recommended testing profile:

1. **Clean smoke test**  
   RimWorld + Harmony + Abyssal Protocol. Use this to check loading, red errors, core UI, basic summoning, Forge behavior, save/load, and boss lifecycle.

2. **Progression modpack stress test**  
   Use a full progression pack to test compatibility, balance distortion, targeting edge cases, TPS, turret interactions, and whether Abyssal rewards still matter beside other endgame mods.

Do not judge balance from a single large modpack alone. Full modpacks are excellent stress tests, but they can hide whether a problem belongs to Abyssal Protocol itself or to an external compatibility interaction.

---

## Known Alpha Caveats

Expect possible issues around:

- Large modpack compatibility
- Edge-case targeting behavior from unusual buildings or utility structures
- Boss state cleanup and save/load transitions
- Dominion pocket / gate runtime behavior
- UI scaling and text readability regressions
- Late-game balance against very strong progression mods
- Encounter pacing and reward economy tuning

Bug reports are most useful when they include:

- the full error log
- mod list or load order context
- whether the issue appears in a clean profile
- whether it survives save/load
- screenshots or short clips for UI, targeting, and boss behavior bugs

---

## Repository Layout

Main project areas:

- `About/` — RimWorld mod metadata
- `Assemblies/` — compiled mod assembly for the repository build
- `Defs/` — XML defs
- `Patches/` — patch operations and compatibility XML
- `Languages/` — localization
- `Textures/` — in-game textures and UI assets
- `Sounds/` — audio assets and sound paths
- `source/` — C# source project
- `Docs/` — architecture, build, content, regression, and recent-work documentation
- `Tools/` — helper scripts and pipeline utilities

---

## Status Summary

**Abyssal Protocol is best described as a late active alpha / public testing candidate.**

It already contains a real summon-driven gameplay foundation, multiple hostile content layers, bespoke UI infrastructure, custom audiovisual identity, boss-facing progression, and a growing reward ecosystem.

It is playable as a substantial alpha-stage endgame mod, but it is not yet a finished 1.0 release. The current development priority is stabilization, compatibility, progression clarity, performance, and polish.
