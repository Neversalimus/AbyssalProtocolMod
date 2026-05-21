# Abyssal Protocol — Equipment, Implant and Reward Balance Reference

_Last updated: 2026-05-21._

## Purpose

This is the central balance source document for future work on **player-facing equipment, implants, modular turret modules and Forge reward items**.

Use it when changing:

- ranged weapons;
- melee weapons;
- apparel, armor, helmets, belts, packs, boots, gloves, vambraces and special body gear;
- implants and implant surgery recipes;
- modular turret modules;
- Forge reward items, cells, capacitors and progression materials;
- combat-stat visibility in InfoCards and Forge UI.

This document is a **reference and balancing contract**, not a replacement for inspecting the actual XML/C# files. If the current archive, live GitHub or runtime behavior disagrees with this file, inspect the real files first and update this document if the balance target changed.

## Ground truth and workflow

Use this order:

```text
1. User-provided latest local archive, if explicitly current.
2. Actual file contents in that archive.
3. Live GitHub repository and latest commits.
4. Verified build / RimWorld runtime smoke test.
5. This balance reference.
6. Older chat memory or previous audit output.
```

Before every equipment or implant task:

1. Check live GitHub and latest commits.
2. Inspect the current local archive.
3. Check this document.
4. Check `Docs/CONTENT_MATRIX.md`, `Docs/AI_QUICK_INDEX.md`, and `Docs/RECENT_WORK.md`.
5. Make only the requested balance/content changes.
6. Update this document when numbers, tiers, item roles, or balance policy change.

## Global balance philosophy

Abyssal Protocol is planned around **9 total tiers**, but the currently introduced playable equipment should mostly cover **T1 through T5**.

The most important rule:

> **T1 is not early-game. T1 is approximately vanilla endgame.**

A T1 Abyssal weapon or armor piece should feel comparable to vanilla charge / marine / cataphract / monosword-level content, but with an abyssal role identity and higher progression cost through residue, bosses, sigils or Forge gates.

## Vanilla baseline anchors

Use these as sanity checks, not as exact copies:

| Vanilla anchor | Important baseline |
| --- | --- |
| Charge rifle | 16 damage, 35% AP, 3-shot burst, 27.9 range, 14.12 raw DPS, Crafting 7, 45,000 work |
| Monosword | 25 damage, 90% AP, 2.0s cooldown on main attacks, about 11.45 normal-quality listed DPS |
| Marine armor | 106% sharp, 45% blunt, 54% heat, covers torso/neck/arms/legs, Crafting 7, 60,000 work |
| Cataphract armor | 120% sharp, 50% blunt, 60% heat, covers torso/neck/arms/legs, Crafting 8, 75,000 work |

## Canonical tier model

| Tier | Current status | Intended power role | Notes |
| --- | --- | --- | --- |
| T1 — Signal | Implemented | Vanilla-endgame equivalent | Charge rifle / monosword / marine-cataphract reference band. |
| T2 — Breach | Implemented | 1.15–1.30× T1 in role | Better role clarity, not raw universal superiority. |
| T3 — Archon | Implemented | 1.35–1.55× T1 in role | Miniboss / Archon-side rewards, specialized heavy or support gear. |
| T4 — Reactor | Implemented | 1.60–1.85× T1 in role | Reactor/Saint, anti-boss, heavy armor, strong turret modules. |
| T5 — Dominion | Implemented / expanding | 1.90–2.20× T1 in role | Current upper playable band; should not consume future T6–T9 headroom. |
| T6 — Crown | Reserved / preview only | 2.30–2.60× T1 in role | Do not fully spend this band while only T1–T5 are meant to be introduced. |
| T7 — Metadomain | Planned | 2.70–3.10× T1 in role | Future crisis tier. |
| T8 — Apex | Planned | 3.20–3.70× T1 in role | Future post-Dominion apex. |
| T9 — Final Gate | Planned | 4.00×+ only when heavily gated | Final/post-endgame, should be rare and role-limited. |

## Forge residue bands in current XML

These are current operational bands, not necessarily final 9-tier thresholds.

| Residue gate | Interpreted band |
| ---: | --- |
| 0–150 | T1 / Signal |
| 151–500 | T2 / Breach |
| 501–1000 | T3 / Archon |
| 1001–2000 | T4 / Reactor |
| 2001–5000 | T5 / Dominion |
| 5001+ | T6+ preview / reserved / special reward |

## UI and stat-card visibility rules

Some stats round poorly in RimWorld cards. For future balance:

- `ShootingAccuracyPawn`, `MeleeHitChance`, and `MeleeDodgeChance` should not be set to positive values below **+0.10** when the bonus is intended to be visible to the player.
- Small hidden calculations can exist in C#, but player-facing equipment bonuses should not show as `0.0`.
- If a weapon's real damage comes from C# effects, expose it through a combat-profile InfoCard block and Forge detail text instead of hiding the real output behind low projectile damage.
- Do not write technical balance explanations into lore descriptions. Use InfoCard stats, Forge details, tooltips and keyed UI strings.

## Ranged weapon policy

### Role-based range bands

| Range | Role |
| ---: | --- |
| 30–32 | sidearm, scatter, suppression, close utility |
| 33–36 | carbine, repeater, tether, anchor-mid |
| 37–40 | main rifle, heavy rifle, launcher |
| 41–45 | marksman, lance, anti-armor spiker |
| 47–52 | siege, apex rail, extreme precision |

Avoid returning all weapons to the old 25–35 range blob. Range should express combat role.

### Canonical ranged weapon table

`Current range` is the value parsed from this local archive. `Canonical target range` is the intended role-pass value and should be preserved if a later archive has already applied it.

| DefName | Scope | Band | Residue | Role | Current range | Canonical target range | Damage | Damage type | AP | Cooldown | Warmup | Burst | Source |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| ABY_SigilRepeater | Player | T1 Signal | 50 | starter repeater | 24.9 | 31 | 7 | Bullet | 28% | 2.35 | 0.7 | 6 | Defs/ThingDefs/ABY_SigilRepeater.xml |
| ABY_HexPistol | Player | T1 Signal | 90 | sidearm / officer pistol | 21.9 | 30 | 12 | Bullet | 32% | 1.2 | 0.65 | 2 | Defs/ThingDefs/ABY_HexPistol.xml |
| ABY_AshenPike | Player | T1 Signal | 150 | early anti-armor pike | 29.9 | 36 | 34 | Bullet | 52% | 2.35 | 1.35 | 1 | Defs/ThingDefs/ABY_AshenPike.xml |
| ABY_AshenScattergun | Player | T1 Signal | 150 | close assault / swarm burst | 15.9 | 30 | 44 | Bullet | 38% | 2.95 | 1.2 | 1 | Defs/ThingDefs/ABY_AshenScattergun.xml |
| ABY_RiftNeedler | Player | T1 Signal | 150 | accurate needle carbine | 27.9 | 33 | 8 | Bullet | 34% | 1.95 | 0.82 | 4 | Defs/ThingDefs/ABY_RiftNeedler.xml |
| ABY_RiftCarbine | Player | T2 Breach | 250 | general main rifle | 28.9 | 35 | 15 | Burn | 48% | 1.5 | 1.1 | 3 | Defs/ThingDefs/ABY_Weapons.xml |
| ABY_NullMarksmanRifle | Player | T2 Breach | 275 | marksman anti-armor | 33.9 | 43 | 28 | Bullet | 75% | 2.2 | 1.45 | 1 | Defs/ThingDefs/ABY_NullMarksmanRifle.xml |
| ABY_NullCantor | Player | T2 Breach | 350 | specialist carbine | 27.9 | 34 | 14 | Burn | 72% | 1.75 | 0.85 | 2 | Defs/ThingDefs/ABY_NullCantor.xml |
| ABY_CanticleDriver | Player | T2 Breach | 400 | support rifle | 30.9 | 36 | 20 | Blunt | 38% | 2.2 | 1.4 | 2 | Defs/ThingDefs/ABY_ChoirRewards.xml |
| ABY_NullDisruptor | Player | T2 Breach | 400 | EMP / utility | 24.9 | 32 | 18 | EMP | 0% | 2.25 | 1.05 | 1 | Defs/ThingDefs/ABY_NullDisruptor.xml |
| ABY_AnchorSpiker | Player | T2 Breach | 425 | control harpoon | 29.9 | 35 | 30 | Bullet | 82% | 2.65 | 1.35 | 1 | Defs/ThingDefs/ABY_AnchorSpiker.xml |
| ABY_VesperLance | Player | T3 Archon | 600 | precision lance | 34.9 | 45 | 34 | Burn | 115% | 2.35 | 1.25 | 1 | Defs/ThingDefs/ABY_VesperLance.xml |
| ABY_AshChoirLauncher | Player | T3 Archon | 850 | heavy launcher / area pressure | 26.9 | 38 | 36 | Bomb | 45% | 3.15 | 1.55 | 1 | Defs/ThingDefs/ABY_AshChoirLauncher.xml |
| ABY_LitanyGrinder | Player | T3 Archon | 1000 | suppression volume-fire | 25.9 | 32 | 7 | Bullet | 24% | 4.8 | 2.05 | 16 | Defs/ThingDefs/ABY_LitanyGrinder.xml |
| ABY_PhalanxDriver | Player | T4 Reactor | 1500 | heavy rifle / line gun | 30.9 | 39 | 18 | Bullet | 78% | 2.6 | 1.25 | 3 | Defs/ThingDefs/ABY_PhalanxDriver.xml |
| ABY_GatebreakerSpiker | Player | T4 Reactor | 1800 | anti-boss spiker | 34.5 | 42 | 30 | Burn | 135% | 3.8 | 1.65 | 3 | Defs/ThingDefs/ABY_GatebreakerSpiker.xml |
| ABY_SpecterLashProjector | Player | T5 Dominion | 3000 | tether lock weapon | 26.9 | 36 | 26 | Burn | 90% | 2.5 | 1.25 | 1 | Defs/ThingDefs/ABY_SpecterLash.xml |
| ABY_CrownshardStormcaster | Player | T5 Dominion | 3500 | storm field / area denial | 36 | 41 | 12 | Burn | 45% | 4.85 | 2.15 | 1 | Defs/ThingDefs/ABY_CrownshardStormcaster.xml |
| ABY_CrownspikeRail | Player | T5 Dominion | 3500 | apex rail | 60 | 52 | 72 | Bullet | 155% | 3.45 | 2.7 | 1 | Defs/ThingDefs/ABY_CrownspikeRail.xml |
| ABY_UltraPlasmaRifle | Player | T5 Dominion | 5000 | late main rifle | 28.9 | 40 | 22 | Burn | 110% | 2.35 | 1.2 | 3 | Defs/ThingDefs/ABY_UltraPlasmaRifle.xml |
| ABY_BreachCannon | Player | T6+ reserved / preview | 7000 | siege / breach gun | 38.9 | 47 | 96 | Bullet | 130% | 4.65 | 2.65 | 1 | Defs/ThingDefs/ABY_BreachCannon.xml |
| ABY_OblivionChoir | Player | T6+ reserved / preview | 10000 | resonance apex weapon | 35.9 | 44 | 34 | Burn | 96% | 4.95 | 2.8 | 1 | Defs/ThingDefs/ABY_OblivionChoir.xml |
| ABY_BreachSpikeProjector | Enemy | Ungated / reward / unknown |  | enemy sapper projector | 18.5 |  | 9 | Burn | 22% | 2.65 | 1.34 | 1 | Defs/ThingDefs/ABY_RiftSapper_Content.xml |
| ABY_Hexgun | Enemy | Ungated / reward / unknown |  | enemy thrall gun | 27.9 |  | 11 | Burn | 22% | 1.65 | 0.95 | 3 | Defs/ThingDefs/ABY_HexgunThrall_Content.xml |

## Melee weapon policy

Melee gear should not simply escalate raw DPS. It should split into:

- light precision blades;
- fast sidearms;
- reach / polearms;
- breaching blunt weapons;
- anti-boss heavy melee;
- future Crown/Dominion melee with explicit risk or cost.

T1 melee should sit near vanilla monosword quality, not accidentally exceed future tiers. Later melee must not be weaker than early sidearms unless it has a very clear utility role.

| DefName | Band | Residue | Role | Best dmg | Best cd | Best AP | Tools | Source |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| ABY_RiftBlade | T1 Signal | 50 | T1 monosword-adjacent blade | 24 | 1.65 | 58% | blade: 24/1.65s/58% AP | Defs/ThingDefs/ABY_Weapons.xml |
| ABY_RiftDagger | T1 Signal | 65 | T1 fast sidearm / surgical melee | 14 | 1 | 42% | rift edge: 14/1s/42% AP; piercing tip: 12/1.05s/48% AP; pommel: 6/1.1s/6% AP | Defs/ThingDefs/ABY_RiftDagger.xml |
| ABY_NullbrandGlaive | T2 Breach | 450 | T2 reach / anti-armor glaive | 30 | 1.85 | 58% | nullbrand cleave: 30/1.85s/58% AP; hooking spike: 24/1.62s/66% AP; weighted haft: 15/1.75s/18% AP | Defs/ThingDefs/ABY_NullbrandGlaive.xml |
| ABY_GatebreakerMaul | T3 Archon | 575 | T3 breaching blunt weapon | 48 | 2.65 | 72% | reactor maul head: 48/2.65s/72% AP; breaching spike: 18/2.55s/48% AP; reinforced haft: 12/2.05s/12% AP | Defs/ThingDefs/ABY_GatebreakerMaul.xml |
| ABY_CohortHalberd | T4 Reactor | 1250 | T4 polearm / mixed reach weapon | 34 | 2.05 | 68% | hook blade: 34/2.05s/68% AP; thrust tine: 30/1.85s/82% AP; counterweight: 11/2.1s/0% AP | Defs/ThingDefs/ABY_CohortHalberd.xml |

## Apparel and armor policy

### Armor budget targets

| Band | Torso armor target | Helmet target | Mobility notes |
| --- | --- | --- | --- |
| T1 | Marine to low-cataphract: 100–110 sharp / 42–48 blunt / 55–65 heat | 105–115 sharp / 45–50 blunt / 58–65 heat | Moderate movement cost is acceptable. |
| T2 | Slightly above T1 or specialized support | 110–120 sharp if combat helm | Accessories should be role bonuses, not full armor. |
| T3 | 115–125 sharp equivalent or special defensive mechanic | 95–110 sharp depending role | Saint/Aegis gear should have actual Aegis if named as such. |
| T4 | 120–135 sharp or heavy role armor | 100–115 sharp | Hover mobility must be capped; avoid passive +3.0 c/s. |
| T5 | 132–145 sharp for current apex armor | 110–125 sharp | Strong, expensive, but not final T9. |
| T6+ | Reserved | Reserved | Do not introduce freely craftable T6+ armor while T1–T5 are the intended playable scope. |

### Apparel stat-budget rules

- Avoid stacking too many `+0.10 ShootingAccuracyPawn` sources across helmet + gloves + harness + artifact.
- Accuracy should primarily live on helmets, targeting slings and dedicated targeting gear.
- Gloves/vambraces should prefer manipulation, melee handling, reload/aim delay or role utility.
- Boots/greaves should carry move speed/carrying capacity, but do not stack with hover into runaway mobility.
- Packs should focus on carrying capacity, work/economy support and maybe minor movement.
- Psychic/research gear should trade combat power for knowledge/support value.
- Belts and artifact slots should be unique and not simply duplicate helmet/glove accuracy.

### Apparel source table

| DefName | Band | Residue | Layers | Groups | Sharp % | Blunt % | Heat % | Work | Visible offsets / mechanics | Aegis | Source |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| ABY_NullAcolyteCowl | T2 Breach | 300 | Overhead | FullHead | 48 | 22 | 58 | 1900 | ShootingAccuracyPawn +0.1; PsychicSensitivity -0.08; ResearchSpeed +0.03 |  | Defs/ThingDefs/ABY_NullAcolyteApparel.xml |
| ABY_NullProcessionVeil | T2 Breach | 325 | EyeCover | Eyes | 2 | 1 | 4 | 1200 | PsychicSensitivity -0.05; ResearchSpeed +0.02; ShootingAccuracyPawn -0.1 |  | Defs/ThingDefs/ABY_SetAccessoryApparel.xml |
| ABY_NullAcolyteVestment | T2 Breach | 350 | Shell | Torso,Shoulders,Arms,Legs,Neck | 46 | 22 | 68 | 4200 | MoveSpeed -0.01; PsychicSensitivity -0.06; ResearchSpeed +0.05 |  | Defs/ThingDefs/ABY_NullAcolyteApparel.xml |
| ABY_NullCantorFocusSling | T2 Breach | 350 | Belt | Torso | 2 | 2 | 4 | 1800 | ShootingAccuracyPawn +0.1; ResearchSpeed +0.03; PsychicSensitivity -0.03 |  | Defs/ThingDefs/ABY_SetAccessoryApparel.xml |
| ABY_GatebreakerAnchorHarness | T4 Reactor | 1800 | Belt | Torso | 3 | 4 | 5 | 2600 | ShootingAccuracyPawn +0.1; MoveSpeed -0.02 |  | Defs/ThingDefs/ABY_SetAccessoryApparel.xml |
| ABY_AbyssalGravplateHelmPrototype | Ungated / reward / unknown |  | Overhead | FullHead | 100 | 52 | 90 | 2800 | ShootingAccuracyPawn +0.1 |  | Defs/ThingDefs/ABY_AbyssalGravplatePrototype_Placeholder.xml |
| ABY_AbyssalGravplatePrototype | Ungated / reward / unknown |  | Shell | Torso,Shoulders,Arms,Neck | 118 | 62 | 102 | 6000 | MoveSpeed -0.04; drafted hover +3.0 |  | Defs/ThingDefs/ABY_AbyssalGravplatePrototype_Placeholder.xml |
| ABY_AshboundFieldPack | Ungated / reward / unknown |  | Belt | Waist | 5 | 4 | 8 | 1400 | MoveSpeed +0.08; CarryingCapacity +20; WorkSpeedGlobal +0.04; MeleeDodgeChance -0.1 |  | Defs/ThingDefs/ABY_BackpackApparel.xml |
| ABY_AshboundTreadBoots | Ungated / reward / unknown |  | Belt | Legs | 4 | 3 | 6 | 950 | MoveSpeed +0.08; CarryingCapacity +8 |  | Defs/ThingDefs/ABY_Boots.xml |
| ABY_AshenGripGloves | Ungated / reward / unknown |  | OnSkin | Hands | 4 | 2 | 6 | 850 | GeneralLaborSpeed +0.04; MeleeHitChance +0.02 |  | Defs/ThingDefs/ABY_Gloves.xml |
| ABY_AshenVambraces | Ungated / reward / unknown |  | Belt | Arms | 5 | 3 | 6 | 1050 | GeneralLaborSpeed +0.03; MeleeHitChance +0.02 |  | Defs/ThingDefs/ABY_Vambraces.xml |
| ABY_CrownAuthorityVambraces | Ungated / reward / unknown |  | Belt | Arms | 11 | 8 | 15 | 3350 | GeneralLaborSpeed +0.06; ShootingAccuracyPawn +0.1; MeleeHitChance +0.04; MeleeDodgeChance +0.1; PsychicSensitivity +0.06 |  | Defs/ThingDefs/ABY_Vambraces.xml |
| ABY_CrownConduitPack | Ungated / reward / unknown |  | Belt | Waist | 10 | 8 | 18 | 4200 | MoveSpeed +0.22; CarryingCapacity +55; WorkSpeedGlobal +0.08; MeleeDodgeChance -0.1; PsychicSensitivity +0.1 |  | Defs/ThingDefs/ABY_BackpackApparel.xml |
| ABY_CrownOfRupture | Ungated / reward / unknown |  | Belt | Waist | 8 | 12 | 20 | 50000 | MoveSpeed +0.08; ShootingAccuracyPawn +0.1; MeleeDodgeChance +0.1 |  | Defs/ThingDefs/ABY_RuptureCrown_Items.xml |
| ABY_CrownedCoreHelm | Ungated / reward / unknown |  | Overhead | FullHead | 108 | 56 | 94 | 3100 | ShootingAccuracyPawn +0.1 |  | Defs/ThingDefs/ABY_CrownedCoreArmor.xml |
| ABY_CrownedCorePlate | Ungated / reward / unknown |  | Shell | Torso,Shoulders,Arms,Neck | 132 | 70 | 116 | 7200 | MoveSpeed -0.06 |  | Defs/ThingDefs/ABY_CrownedCoreArmor.xml |
| ABY_CrownpathSabatons | Ungated / reward / unknown |  | Belt | Legs | 11 | 8 | 15 | 3200 | MoveSpeed +0.2; CarryingCapacity +22; PsychicSensitivity +0.06 |  | Defs/ThingDefs/ABY_Boots.xml |
| ABY_CrownsealGauntlets | Ungated / reward / unknown |  | OnSkin | Hands | 9 | 5 | 14 | 2850 | GeneralLaborSpeed +0.07; ShootingAccuracyPawn +0.1; MeleeHitChance +0.04 |  | Defs/ThingDefs/ABY_Gloves.xml |
| ABY_GatebreakerCarapace | Ungated / reward / unknown |  | Shell | Torso,Shoulders,Arms,Legs,Neck | 118 | 62 | 110 | 6600 | MoveSpeed -0.1; PsychicSensitivity -0.06; drafted hover +3.0 |  | Defs/ThingDefs/ABY_GatebreakerArmor.xml |
| ABY_GatebreakerHelm | Ungated / reward / unknown |  | Overhead | FullHead | 98 | 50 | 88 | 3000 | ShootingAccuracyPawn +0.1; PsychicSensitivity -0.05 |  | Defs/ThingDefs/ABY_GatebreakerArmor.xml |
| ABY_InfernalCombatFrame | Ungated / reward / unknown |  | Shell | Torso,Shoulders,Arms,Neck | 70 | 36 | 54 | 3200 | MoveSpeed -0.03 |  | Defs/ThingDefs/ABY_InfernalCombatFrame.xml |
| ABY_RiftConduitGloves | Ungated / reward / unknown |  | OnSkin | Hands | 6 | 4 | 10 | 1550 | GeneralLaborSpeed +0.05; ShootingAccuracyPawn +0.1; MeleeHitChance +0.03 |  | Defs/ThingDefs/ABY_Gloves.xml |
| ABY_RiftHelm | Ungated / reward / unknown |  | Overhead | FullHead | 62 | 30 | 50 | 1600 | ShootingAccuracyPawn +0.1 |  | Defs/ThingDefs/ABY_RiftHelm.xml |
| ABY_RiftRelayPack | Ungated / reward / unknown |  | Belt | Waist | 8 | 6 | 10 | 2400 | MoveSpeed +0.15; CarryingCapacity +35; WorkSpeedGlobal +0.06; MeleeDodgeChance -0.1; PsychicSensitivity +0.05 |  | Defs/ThingDefs/ABY_BackpackApparel.xml |
| ABY_RiftVectorVambraces | Ungated / reward / unknown |  | Belt | Arms | 7 | 5 | 10 | 1850 | GeneralLaborSpeed +0.04; ShootingAccuracyPawn +0.1; MeleeHitChance +0.03; PsychicSensitivity +0.03 |  | Defs/ThingDefs/ABY_Vambraces.xml |
| ABY_RiftstepGreaves | Ungated / reward / unknown |  | Belt | Legs | 7 | 5 | 10 | 1750 | MoveSpeed +0.14; CarryingCapacity +14; PsychicSensitivity +0.03 |  | Defs/ThingDefs/ABY_Boots.xml |
| ABY_SaintAegisCarapace | Ungated / reward / unknown |  | Shell | Torso,Shoulders,Arms,Legs,Neck | 104 | 54 | 92 | 5800 | MoveSpeed -0.08 |  | Defs/ThingDefs/ABY_ReactorSaint_Armor.xml |
| ABY_VesperHaloHelm | Ungated / reward / unknown |  | Overhead | FullHead | 86 | 44 | 74 | 2200 | ShootingAccuracyPawn +0.1 |  | Defs/ThingDefs/ABY_ReactorSaint_Armor.xml |

## Implant policy

Implants must remain exciting but cannot silently invalidate apparel, weapons, skills and pawn specialization.

### Implant budget targets

| Band | Implant power expectation |
| --- | --- |
| T1 | One clear body-function upgrade, usually +0.10 visible combat stat or +0.20–0.35 body capacity, with small drawback if broad. |
| T2 | Stronger specialization or two moderate bonuses; avoid universal super-soldier packages. |
| T3 | Miniboss/core reward implants; can combine combat + support but should require rare core material. |
| T4 | Reactor/Saint implants; strong but still role-bound. |
| T5 | Dominion implants; can be broad, but should not make every pawn identical. |
| T6+ | Reserved for future crisis-tier body rewriting. |

### Implant display rules

- If `ShootingAccuracyPawn`, `MeleeHitChance`, or `MeleeDodgeChance` is meant to be visible, use **+0.10 minimum**.
- If a very small value is mechanically necessary, consider converting it into a different visible stat or hide it behind C# where it does not create a bad card line.
- Do not stack too many universal combat stats on one implant; prefer body-slot identity.

### Implant source table

| Hediff | Item | Band | Residue | Visible effects | Install recipe | Recipe source | Hediff source |
| --- | --- | --- | --- | --- | --- | --- | --- |
| ABY_InfernalEye_Implant | ABY_InfernalEye | T1 Signal | 100 | ShootingAccuracyPawn +0.1; MeleeHitChance +0.02; Sight +0.2 | ABY_InstallInfernalEye | Defs/RecipeDefs/ABY_ImplantRecipes.xml | Defs/HediffDefs/ABY_Implants.xml |
| ABY_AshenAnchorNode_Implant | ABY_AshenAnchorNode | T1 Signal | 150 | ResearchSpeed +0.12; GlobalLearningFactor +0.08; RestFallRateFactor x1.05; Consciousness +0.06 | ABY_InstallAshenAnchorNode | Defs/RecipeDefs/ABY_AshenImplantRecipes.xml | Defs/HediffDefs/ABY_AshenImplants_Hediffs.xml |
| ABY_AshenLiverLattice_Implant | ABY_AshenLiverLattice | T1 Signal | 150 | ImmunityGainSpeed +0.08; BloodFiltration +0.35 | ABY_InstallAshenLiverLattice | Defs/RecipeDefs/ABY_AshenImplantRecipes.xml | Defs/HediffDefs/ABY_AshenImplants_Hediffs.xml |
| ABY_CinderMandibleSeal_Implant | ABY_CinderMandibleSeal | T1 Signal | 150 | PainShockThreshold +0.15; MeleeHitChance +0.05; SocialImpact -0.1; Beauty -0.3 | ABY_InstallCinderMandibleSeal | Defs/RecipeDefs/ABY_AshenImplantRecipes.xml | Defs/HediffDefs/ABY_AshenImplants_Hediffs.xml |
| ABY_EmberLungArray_Implant | ABY_EmberLungArray | T1 Signal | 150 | MoveSpeed +0.05; Breathing +0.22 | ABY_InstallEmberLungArray | Defs/RecipeDefs/ABY_AshenImplantRecipes.xml | Defs/HediffDefs/ABY_AshenImplants_Hediffs.xml |
| ABY_ArchonTendonSpine_Implant | ABY_ArchonTendonSpine | T2 Breach | 300 | MoveSpeed +0.12; MeleeDodgeChance +0.1; PainShockThreshold +0.08; CarryingCapacity +12; Moving +0.12 | ABY_InstallArchonTendonSpine | Defs/RecipeDefs/ABY_HeraldImplantRecipes.xml | Defs/HediffDefs/ABY_Implants.xml |
| ABY_HeraldCarapaceMesh_Implant | ABY_HeraldCarapaceMesh | T2 Breach | 300 | ArmorRating_Sharp +0.18; ArmorRating_Blunt +0.1; ArmorRating_Heat +0.2; PawnBeauty -0.4; MoveSpeed -0.02 | ABY_InstallHeraldCarapaceMesh | Defs/RecipeDefs/ABY_HeraldImplantRecipes.xml | Defs/HediffDefs/ABY_Implants.xml |
| ABY_HeraldEye_Implant | ABY_HeraldEye | T2 Breach | 300 | ShootingAccuracyPawn +0.1; MeleeHitChance +0.03; MeleeDodgeChance +0.1; Sight +0.3 | ABY_InstallHeraldEye | Defs/RecipeDefs/ABY_HeraldImplantRecipes.xml | Defs/HediffDefs/ABY_Implants.xml |
| ABY_CanticleSubcoreNode_Implant | ABY_CanticleSubcoreNode | T2 Breach | 400 | ResearchSpeed +0.1; GlobalLearningFactor +0.08; AimingDelayFactor -0.03; RestFallRateFactor x1.05; Consciousness +0.08 | ABY_InstallCanticleSubcoreNode | Defs/RecipeDefs/ABY_ChoirRewards_Recipes.xml | Defs/HediffDefs/ABY_ChoirRewards_Hediffs.xml |
| ABY_ChoirSinkKidney_Implant | ABY_ChoirSinkKidney | T2 Breach | 400 | ImmunityGainSpeed +0.08; ArmorRating_Heat +0.1; PainShockThreshold +0.02; BloodFiltration +0.22 | ABY_InstallChoirSinkKidney | Defs/RecipeDefs/ABY_ChoirRewards_Recipes.xml | Defs/HediffDefs/ABY_ChoirRewards_Hediffs.xml |
| ABY_HarmonicMesh_Implant | ABY_HarmonicMesh | T2 Breach | 400 | ArmorRating_Sharp +0.12; ArmorRating_Blunt +0.08; ArmorRating_Heat +0.14; PawnBeauty -0.2; MoveSpeed -0.01 | ABY_InstallHarmonicMesh | Defs/RecipeDefs/ABY_ChoirRewards_Recipes.xml | Defs/HediffDefs/ABY_ChoirRewards_Hediffs.xml |
| ABY_ResonanceServoArm_Implant | ABY_ResonanceServoArm | T2 Breach | 400 | ShootingAccuracyPawn +0.1; AimingDelayFactor -0.04; MeleeHitChance +0.01; Manipulation +0.1 | ABY_InstallResonanceServoArm | Defs/RecipeDefs/ABY_ChoirRewards_Recipes.xml | Defs/HediffDefs/ABY_ChoirRewards_Hediffs.xml |
| ABY_AegisSinkKidney_Implant | ABY_AegisSinkKidney | T3 Archon | 600 | ImmunityGainSpeed +0.14; ArmorRating_Heat +0.22; PainShockThreshold +0.05; BloodFiltration +0.32 | ABY_InstallAegisSinkKidney | Defs/RecipeDefs/ABY_ReactorSaint_ImplantRecipes.xml | Defs/HediffDefs/ABY_ReactorSaint_Implants_Hediffs.xml |
| ABY_HaloSubcoreNode_Implant | ABY_HaloSubcoreNode | T3 Archon | 600 | ResearchSpeed +0.2; GlobalLearningFactor +0.1; AimingDelayFactor -0.05; RestFallRateFactor x1.07; Consciousness +0.1 | ABY_InstallHaloSubcoreNode | Defs/RecipeDefs/ABY_ReactorSaint_ImplantRecipes.xml | Defs/HediffDefs/ABY_ReactorSaint_Implants_Hediffs.xml |
| ABY_SaintReactorHeart_Implant | ABY_SaintReactorHeart | T3 Archon | 600 | MoveSpeed +0.04; PainShockThreshold +0.1; ArmorRating_Heat +0.1; BloodPumping +0.32; Consciousness +0.07 | ABY_InstallSaintReactorHeart | Defs/RecipeDefs/ABY_ReactorSaint_ImplantRecipes.xml | Defs/HediffDefs/ABY_ReactorSaint_Implants_Hediffs.xml |
| ABY_VesperServoArm_Implant | ABY_VesperServoArm | T3 Archon | 600 | ShootingAccuracyPawn +0.1; AimingDelayFactor -0.08; MeleeHitChance +0.02; Manipulation +0.14 | ABY_InstallVesperServoArm | Defs/RecipeDefs/ABY_ReactorSaint_ImplantRecipes.xml | Defs/HediffDefs/ABY_ReactorSaint_Implants_Hediffs.xml |
| ABY_BoundClawArray_Implant | ABY_BoundClawArray | T3 Archon | 1000 | MeleeHitChance +0.06; MeleeDodgeChance +0.1; PainShockThreshold +0.04; PawnBeauty -0.3; Manipulation +0.08 | ABY_InstallBoundClawArray | Defs/RecipeDefs/ABY_HordeImplantRecipes.xml | Defs/HediffDefs/ABY_HordeImplants_Hediffs.xml |
| ABY_BreachTendonWeave_Implant | ABY_BreachTendonWeave | T3 Archon | 1000 | MoveSpeed +0.08; MeleeDodgeChance +0.1; Moving +0.12 | ABY_InstallBreachTendonWeave | Defs/RecipeDefs/ABY_HordeImplantRecipes.xml | Defs/HediffDefs/ABY_HordeImplants_Hediffs.xml |
| ABY_CohortSyncSubnode_Implant | ABY_CohortSyncSubnode | T3 Archon | 1000 | MentalBreakThreshold +0.08; SocialImpact +0.1; ShootingAccuracyPawn +0.1; RestFallRateFactor x1.03; Consciousness +0.04; Hearing +0.25 | ABY_InstallCohortSyncSubnode | Defs/RecipeDefs/ABY_HordeImplantRecipes.xml | Defs/HediffDefs/ABY_HordeImplants_Hediffs.xml |
| ABY_NullChorusCollar_Implant | ABY_NullChorusCollar | T3 Archon | 1000 | MentalBreakThreshold +0.1; PainShockThreshold +0.12; PsychicSensitivity -0.1; PawnBeauty -0.1 | ABY_InstallNullChorusCollar | Defs/RecipeDefs/ABY_HordeImplantRecipes.xml | Defs/HediffDefs/ABY_HordeImplants_Hediffs.xml |
| ABY_CrownCortexSubnode_Implant | ABY_CrownCortexSubnode | T5 Dominion | 3500 | ResearchSpeed +0.19; GlobalLearningFactor +0.13; AimingDelayFactor -0.06; PsychicSensitivity +0.1; RestFallRateFactor x1.1; Consciousness +0.1 | ABY_InstallCrownCortexSubnode | Defs/RecipeDefs/ABY_DominionImplantRecipes.xml | Defs/HediffDefs/ABY_DominionImplants_Hediffs.xml |
| ABY_DominionPulseHeart_Implant | ABY_DominionPulseHeart | T5 Dominion | 3500 | MoveSpeed +0.06; PainShockThreshold +0.13; ArmorRating_Heat +0.1; RestFallRateFactor x1.05; BloodPumping +0.33; Consciousness +0.06 | ABY_InstallDominionPulseHeart | Defs/RecipeDefs/ABY_DominionImplantRecipes.xml | Defs/HediffDefs/ABY_DominionImplants_Hediffs.xml |
| ABY_LawwovenCarapaceMesh_Implant | ABY_LawwovenCarapaceMesh | T5 Dominion | 3500 | ArmorRating_Sharp +0.28; ArmorRating_Blunt +0.18; ArmorRating_Heat +0.25; PainShockThreshold +0.05; MoveSpeed -0.03; PawnBeauty -0.55 | ABY_InstallLawwovenCarapaceMesh | Defs/RecipeDefs/ABY_DominionImplantRecipes.xml | Defs/HediffDefs/ABY_DominionImplants_Hediffs.xml |
| ABY_VerdictTendonSpine_Implant | ABY_VerdictTendonSpine | T5 Dominion | 3500 | MoveSpeed +0.13; CarryingCapacity +25; MeleeDodgeChance +0.1; PainShockThreshold +0.06; PawnBeauty -0.25; Moving +0.18 | ABY_InstallVerdictTendonSpine | Defs/RecipeDefs/ABY_DominionImplantRecipes.xml | Defs/HediffDefs/ABY_DominionImplants_Hediffs.xml |

## Modular turret module policy

Turret modules are equipment-like progression and should obey tier readability too.

### Module budget principles

- Main weapon modules define turret role: anti-light, anti-armor, mortar, lance, flak, harpoon, rail, rocket.
- Passive modules should be installable systems, not hidden global buffs.
- Each passive module should have one primary identity: cooling, targeting, power, close-quarters, anti-swarm, execution, shield burn, stabilizer, emergency heat, aegis.
- Avoid per-tick map scans. Targeting modules should use existing throttled scan paths.
- Modules should be exposed through Forge UI and InfoCards with explicit effects.

### Turret module source table

| Module def | Tier | Slot | Role | Range | Min range | Cooldown ticks | Burst | Extra power | ThingDef | Source |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| ABY_TMD_RiftNeedlerCore | 1 | MainWeapon | anti-light burst fire | 28.5 |  | 150 | 3 |  | ABY_TurretModule_RiftNeedlerCore | Defs/Misc/ABY_TurretModuleDefs.xml |
| ABY_TMD_BlackoutPowerRegulator | 1 | Passive | grid load governor |  |  |  |  | -180 | ABY_TurretModule_BlackoutPowerRegulator | Defs/Misc/ABY_TurretModuleDefs.xml |
| ABY_TMD_CoolingLattice | 1 | Passive | cadence stabilizer |  |  |  |  | 70 | ABY_TurretModule_CoolingLattice | Defs/Misc/ABY_TurretModuleDefs.xml |
| ABY_TMD_TargetingSigil | 1 | Passive | range and tracking |  |  |  |  | 80 | ABY_TurretModule_TargetingSigil | Defs/Misc/ABY_TurretModuleDefs.xml |
| ABY_TMD_ChoirArcEmitter | 2 | Auxiliary | crowd pressure | 30 |  | 420 | 1 | 130 | ABY_TurretModule_ChoirArcEmitter | Defs/Misc/ABY_TurretModuleDefs.xml |
| ABY_TMD_AshChoirRepeaterCore | 2 | MainWeapon | anti-swarm ash repeater | 30.5 |  | 210 | 5 | 180 | ABY_TurretModule_AshChoirRepeaterCore | Defs/Misc/ABY_TurretModuleDefs.xml |
| ABY_TMD_PlasmaLanceCore | 2 | MainWeapon | anti-armor lance | 33.5 |  | 260 | 1 | 120 | ABY_TurretModule_PlasmaLanceCore | Defs/Misc/ABY_TurretModuleDefs.xml |
| ABY_TMD_BreachAegisRelay | 2 | Passive | breach aegis buffer |  |  |  |  | 220 | ABY_TurretModule_BreachAegisRelay | Defs/Misc/ABY_TurretModuleDefs.xml |
| ABY_TMD_LongChoirLens | 2 | Passive | long-range sighting |  |  |  |  | 150 | ABY_TurretModule_LongChoirLens | Defs/Misc/ABY_TurretModuleDefs.xml |
| ABY_TMD_OverpressureCycleGovernor | 2 | Passive | cycle overpressure |  |  |  |  | 220 | ABY_TurretModule_OverpressureCycleGovernor | Defs/Misc/ABY_TurretModuleDefs.xml |
| ABY_TMD_ResidueCapacitor | 2 | Passive | charge buffer |  |  |  |  | 120 | ABY_TurretModule_ResidueCapacitor | Defs/Misc/ABY_TurretModuleDefs.xml |
| ABY_TMD_AbyssalHarpoonProjector | 3 | MainWeapon | elite lockdown | 32 | 4 | 420 | 1 | 340 | ABY_TurretModule_AbyssalHarpoonProjector | Defs/Misc/ABY_TurretModuleDefs.xml |
| ABY_TMD_CinderMortarCore | 3 | MainWeapon | indirect cinder fire | 40 | 8 | 336 | 1 | 320 | ABY_TurretModule_CinderMortarCore | Defs/Misc/ABY_TurretModuleDefs.xml |
| ABY_TMD_NullArcDischarger | 3 | MainWeapon | EMP suppression | 36 | 0 | 348 | 1 | 380 | ABY_TurretModule_NullArcDischarger | Defs/Misc/ABY_TurretModuleDefs.xml |
| ABY_TMD_RiftFlakBloom | 3 | MainWeapon | anti-cluster flak bloom | 31.5 | 3 | 288 | 1 | 350 | ABY_TurretModule_RiftFlakBloom | Defs/Misc/ABY_TurretModuleDefs.xml |
| ABY_TMD_SanctifiedPrismEmitter | 3 | MainWeapon | line-control refraction | 37 | 0 | 360 | 1 | 420 | ABY_TurretModule_SanctifiedPrismEmitter | Defs/Misc/ABY_TurretModuleDefs.xml |
| ABY_TMD_VesperLanceArray | 3 | MainWeapon | precision sanction lance | 40.5 | 0 | 312 | 1 | 260 | ABY_TurretModule_VesperLanceArray | Defs/Misc/ABY_TurretModuleDefs.xml |
| ABY_TMD_AbyssalThreatPrioritizer | 3 | Passive | elite target scoring |  |  |  |  | 180 | ABY_TurretModule_AbyssalThreatPrioritizer | Defs/Misc/ABY_TurretModuleDefs.xml |
| ABY_TMD_AntiSwarmPatternScanner | 3 | Passive | cluster target scoring |  |  |  |  | 170 | ABY_TurretModule_AntiSwarmPatternScanner | Defs/Misc/ABY_TurretModuleDefs.xml |
| ABY_TMD_CloseQuartersInterlock | 3 | Passive | minimum range control |  |  |  |  | 120 | ABY_TurretModule_CloseQuartersInterlock | Defs/Misc/ABY_TurretModuleDefs.xml |
| ABY_TMD_ExecutionLogicCore | 3 | Passive | wounded target execution |  |  |  |  | 190 | ABY_TurretModule_ExecutionLogicCore | Defs/Misc/ABY_TurretModuleDefs.xml |
| ABY_TMD_CrowncoilGaussMinigun | 4 | MainWeapon | crowncoil suppression | 35 | 5 | 420 | 18 | 720 | ABY_TurretModule_CrowncoilGaussMinigun | Defs/Misc/ABY_TurretModuleDefs.xml |
| ABY_TMD_CrownfireRocketChoir | 4 | MainWeapon | guided rocket saturation | 38 | 7 | 660 | 1 | 820 | ABY_TurretModule_CrownfireRocketChoir | Defs/Misc/ABY_CrownfireRocketChoir_TurretModuleDef.xml |
| ABY_TMD_SepulcherRailCore | 4 | MainWeapon | anti-elite rail shot | 52 | 8 | 720 | 1 | 520 | ABY_TurretModule_SepulcherRailCore | Defs/Misc/ABY_TurretModuleDefs.xml |
| ABY_TMD_CrownAegisMatrix | 4 | Passive | crown aegis shield |  |  |  |  | 620 | ABY_TurretModule_CrownAegisMatrix | Defs/Misc/ABY_TurretModuleDefs.xml |
| ABY_TMD_EmergencyHeatDump | 4 | Passive | thermal recovery |  |  |  |  | 420 | ABY_TurretModule_EmergencyHeatDump | Defs/Misc/ABY_TurretModuleDefs.xml |
| ABY_TMD_SanctifiedStabilizerPlate | 4 | Passive | chassis hardening |  |  |  |  | 240 | ABY_TurretModule_SanctifiedStabilizerPlate | Defs/Misc/ABY_TurretModuleDefs.xml |
| ABY_TMD_ShieldBurnCapacitor | 4 | Passive | anti-shield targeting |  |  |  |  | 360 | ABY_TurretModule_ShieldBurnCapacitor | Defs/Misc/ABY_TurretModuleDefs.xml |

## Other Forge reward and progression-controlled items

These are not weapons/apparel/implants, but still affect balance and progression. Keep them in this document so future source work does not treat them as free utility.

| DefName | Forge category | Band | Residue | Label | Source |
| --- | --- | --- | --- | --- | --- |
| ABY_TurretModule_RiftNeedlerCore | TurretSystems | T1 Signal | 80 | rift needler core | Defs/ThingDefs/ABY_TurretModules.xml |
| ABY_AshboundCapacitorModule | Core | T1 Signal | 100 | ashbound capacitor module | Defs/ThingDefs/ABY_CircleCapacitors.xml |
| ABY_TurretModule_CoolingLattice | TurretSystems | T1 Signal | 110 | cooling lattice | Defs/ThingDefs/ABY_TurretModules.xml |
| ABY_TurretModule_BlackoutPowerRegulator | TurretSystems | T1 Signal | 130 | blackout power regulator | Defs/ThingDefs/ABY_TurretModules.xml |
| ABY_TurretModule_TargetingSigil | TurretSystems | T1 Signal | 140 | targeting sigil | Defs/ThingDefs/ABY_TurretModules.xml |
| ABY_TurretModule_ResidueCapacitor | TurretSystems | T2 Breach | 170 | residue capacitor | Defs/ThingDefs/ABY_TurretModules.xml |
| ABY_TurretModule_PlasmaLanceCore | TurretSystems | T2 Breach | 190 | plasma lance core | Defs/ThingDefs/ABY_TurretModules.xml |
| ABY_TurretModule_OverpressureCycleGovernor | TurretSystems | T2 Breach | 240 | overpressure cycle governor | Defs/ThingDefs/ABY_TurretModules.xml |
| ABY_TurretModule_ChoirArcEmitter | TurretSystems | T2 Breach | 260 | choir arc emitter | Defs/ThingDefs/ABY_TurretModules.xml |
| ABY_TurretModule_BreachAegisRelay | TurretSystems | T2 Breach | 280 | breach aegis relay | Defs/ThingDefs/ABY_TurretModules.xml |
| ABY_TurretModule_LongChoirLens | TurretSystems | T2 Breach | 280 | long choir lens | Defs/ThingDefs/ABY_TurretModules.xml |
| ABY_RiftCapacitorModule | Core | T2 Breach | 300 | rift capacitor module | Defs/ThingDefs/ABY_CircleCapacitors.xml |
| ABY_TurretModule_AshChoirRepeaterCore | TurretSystems | T2 Breach | 320 | ash choir repeater core | Defs/ThingDefs/ABY_TurretModules.xml |
| ABY_TurretModule_CloseQuartersInterlock | TurretSystems | T2 Breach | 460 | close-quarters interlock | Defs/ThingDefs/ABY_TurretModules.xml |
| ABY_CrownCondenserModule | Core | T2 Breach | 500 | crown condenser module | Defs/ThingDefs/ABY_CircleCapacitors.xml |
| ABY_TurretModule_AntiSwarmPatternScanner | TurretSystems | T3 Archon | 580 | anti-swarm pattern scanner | Defs/ThingDefs/ABY_TurretModules.xml |
| ABY_SaintCondensationCell | Core | T3 Archon | 600 | saint condensation cell | Defs/ThingDefs/ABY_SaintCondensationCell.xml |
| ABY_TurretModule_AbyssalThreatPrioritizer | TurretSystems | T3 Archon | 620 | abyssal threat prioritizer | Defs/ThingDefs/ABY_TurretModules.xml |
| ABY_TurretModule_ExecutionLogicCore | TurretSystems | T3 Archon | 620 | execution logic core | Defs/ThingDefs/ABY_TurretModules.xml |
| ABY_TurretModule_VesperLanceArray | TurretSystems | T3 Archon | 680 | vesper lance array | Defs/ThingDefs/ABY_TurretModules.xml |
| ABY_TurretModule_CinderMortarCore | TurretSystems | T3 Archon | 820 | cinder mortar core | Defs/ThingDefs/ABY_TurretModules.xml |
| ABY_TurretModule_RiftFlakBloom | TurretSystems | T3 Archon | 920 | rift flak bloom | Defs/ThingDefs/ABY_TurretModules.xml |
| ABY_TurretModule_NullArcDischarger | TurretSystems | T3 Archon | 960 | null-arc discharger | Defs/ThingDefs/ABY_TurretModules.xml |
| ABY_TurretModule_AbyssalHarpoonProjector | TurretSystems | T4 Reactor | 1040 | abyssal harpoon projector | Defs/ThingDefs/ABY_TurretModules.xml |
| ABY_TurretModule_SanctifiedPrismEmitter | TurretSystems | T4 Reactor | 1120 | sanctified prism emitter | Defs/ThingDefs/ABY_TurretModules.xml |
| ABY_TurretModule_SanctifiedStabilizerPlate | TurretSystems | T4 Reactor | 1150 | sanctified stabilizer plate | Defs/ThingDefs/ABY_TurretModules.xml |
| ABY_TurretModule_ShieldBurnCapacitor | TurretSystems | T4 Reactor | 1200 | shield-burn capacitor | Defs/ThingDefs/ABY_TurretModules.xml |
| ABY_TurretModule_CrownAegisMatrix | TurretSystems | T4 Reactor | 1250 | crown aegis matrix | Defs/ThingDefs/ABY_TurretModules.xml |
| ABY_TurretModule_EmergencyHeatDump | TurretSystems | T4 Reactor | 1250 | emergency heat dump | Defs/ThingDefs/ABY_TurretModules.xml |
| ABY_TurretModule_CrowncoilGaussMinigun | TurretSystems | T4 Reactor | 1280 | crowncoil gauss minigun | Defs/ThingDefs/ABY_TurretModules.xml |
| ABY_TurretModule_CrownfireRocketChoir | TurretSystems | T4 Reactor | 1500 | crownfire rocket choir | Defs/ThingDefs/ABY_CrownfireRocketChoir.xml |
| ABY_TurretModule_SepulcherRailCore | TurretSystems | T4 Reactor | 1500 | sepulcher rail core | Defs/ThingDefs/ABY_TurretModules.xml |
| ABY_OblivionChoirCell | Herald | T6+ reserved / preview | 10000 | choir cell | Defs/ThingDefs/ABY_OblivionChoir.xml |

## Special C# damage visibility policy

Some weapons are intentionally weak in raw projectile XML because their real output is C# driven. These must always have player-facing UI disclosure:

| Weapon | Required UI disclosure |
| --- | --- |
| `ABY_SpecterLashProjector` | Base anchor impact plus tether pulse count, pulse damage, AP, max maintained lock package and break conditions. |
| `ABY_CrownshardStormcaster` | Seed impact plus storm node pulse damage, interval, radius, target cap, duration and dense-target multiplier. |
| `ABY_OblivionChoir` | Core impact, branch arc damage/interval/target cap, resonance severity, final collapse and detonation rules. |

Do not balance these solely by XML DPS. Use an effective-role budget and show the damage profile in InfoCard and Forge UI.

## Crafting, work and skill gate policy

Endgame-equivalent gear should not be cheap in work just because it requires residue.

| Band | Suggested Crafting gate | Suggested work scale |
| --- | ---: | ---: |
| T1 | 7–8 | near vanilla endgame: roughly 20k–60k depending item size |
| T2 | 8–9 | 30k–70k |
| T3 | 10 | 40k–90k |
| T4 | 11–12 | 60k–120k |
| T5 | 13–14 | 90k–170k |
| T6+ | 15+ or special nonstandard gate | only if future tier is intentionally playable |

Surgery recipes should also have explicit medicine/skill expectations when the implant is powerful enough to define a pawn build.

## Current high-risk balance points to preserve

- T1 must remain vanilla-endgame equivalent; do not nerf T1 below charge/marine baseline unless it is explicitly pre-T1 utility.
- Do not let T1 melee exceed T3/T4 melee by accident.
- Keep player weapon ranges role-based: do not collapse everything back to around 30.
- Do not reintroduce passive hover bonuses around `+3.0` c/s unless it becomes an active cooldown-based movement system.
- Do not add “Aegis” to item names unless the item has actual Aegis behavior or the name is clearly metaphorical.
- Do not add small positive combat accuracy/dodge offsets below +0.10 if they appear on item/implant cards.
- Do not hide C# damage layers from the player.
- Do not add new Forge rewards without category, residue gate, recipe, InfoCard clarity and UI exposure.

## Files to inspect by content type

| Task | Start here |
| --- | --- |
| Ranged weapon XML stats | `Defs/ThingDefs/ABY_*.xml` weapon files, especially `ABY_Weapons.xml`, specific weapon files, and projectile sibling defs |
| Melee weapon XML stats | `Defs/ThingDefs/ABY_Weapons.xml`, `ABY_RiftDagger.xml`, `ABY_NullbrandGlaive.xml`, `ABY_GatebreakerMaul.xml`, `ABY_CohortHalberd.xml` |
| Special weapon C# damage | `source/Combat/`, especially special projectile/game component files and `ABY_SpecialWeaponDamageInfoUtility` |
| Forge weapon/apparel/implant exposure | `source/Forge/`, `source/UI/Forge/`, `Defs/RecipeDefs/`, `Defs/ThingDefs/` |
| Apparel XML | `Defs/ThingDefs/ABY_*Armor*.xml`, `ABY_*Apparel*.xml`, `ABY_Gloves.xml`, `ABY_Vambraces.xml`, `ABY_Boots.xml`, `ABY_BackpackApparel.xml` |
| Aegis armor | `source/Apparel/`, `Patches/ABY_ApparelAegis_ArmorPatches.xml`, armor ThingDefs with `DefModExtension_ABY_ApparelAegis` |
| Implants | `Defs/ThingDefs/*Implant*.xml`, `Defs/HediffDefs/*Implant*.xml`, `Defs/RecipeDefs/*Implant*.xml`, `source/UI/Health/` if card display is involved |
| Turret modules | `Defs/Misc/ABY_TurretModuleDefs.xml`, `Defs/ThingDefs/ABY_TurretModules.xml`, `source/Comps/CompAbyssalModularTurret.cs`, `source/Defs/Turrets/` |
| Balance reference maintenance | `Docs/EQUIPMENT_BALANCE_REFERENCE.md`, `Docs/CONTENT_MATRIX.md`, `Docs/AI_QUICK_INDEX.md`, `Docs/RECENT_WORK.md` |


## Implant slot coverage expansion — 2026-05-21

This pass completes the current implant grid for the introduced T1-T5 range of the future 9-tier progression model.

Canonical implant slot grid used by the project:

- Brain
- Eye
- Spine
- Heart
- Lung
- Kidney
- Liver
- Stomach
- Arm
- Leg
- Torso
- Jaw
- Neck

Rules preserved by this pass:

- T1 is kept around vanilla endgame / early abyssal baseline.
- T2-T5 increase by role and slot without making every slot a pure DPS upgrade.
- Positive `ShootingAccuracyPawn`, `MeleeHitChance`, and `MeleeDodgeChance` entries must be at least `0.10` so RimWorld stat cards do not show misleading `0.0` values.
- Brain, Torso and Neck entries are treated as auxiliary implants where possible; major organs and limbs use added body part behavior.
- New implants use existing Forge unlock infrastructure under category `Implants`.
- New craftable implant items must have matching `HediffDef`, surgery `RecipeDef`, `spawnThingOnRemoved`, `CompProperties_ABY_ImplantInfoCard`, and a real `Textures/Things/Implant/*.png` asset.

New coverage added:

| Tier | Added missing slots |
|---|---|
| T1 Signal | Spine, Heart, Kidney, Stomach, Arm, Leg, Torso, Neck |
| T2 Breach | Heart, Lung, Liver, Stomach, Leg, Jaw, Neck |
| T3 Archon | Eye, Spine, Lung, Liver, Stomach, Torso, Jaw |
| T4 Reactor | Brain, Eye, Spine, Heart, Lung, Kidney, Liver, Stomach, Arm, Leg, Torso, Jaw, Neck |
| T5 Dominion | Eye, Lung, Kidney, Liver, Stomach, Arm, Leg, Jaw, Neck |

New ownership files:

- `Defs/ThingDefs/ABY_ImplantTierExpansion.xml`
- `Defs/HediffDefs/ABY_ImplantTierExpansion_Hediffs.xml`
- `Defs/RecipeDefs/ABY_ImplantTierExpansion_Recipes.xml`
- `Textures/Things/Implant/ABY_*.png` for each added implant.

Existing safety fixes included:

- Raised player-facing positive `MeleeHitChance` implant entries below `0.10` to `0.10`.
- Added missing `spawnThingOnRemoved` entries to the Ashen implant hediffs so removed implants return their body-part item consistently.
