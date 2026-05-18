# Abyssal Protocol — Known Risks and Regressions

This document tracks recurring technical, visual, UI, runtime, and workflow risks in Abyssal Protocol.
It exists to prevent future AI-assisted patches from reintroducing bugs that were already found, fixed, or identified as likely.

This is not a blame log. It is a safety checklist.

## Ground truth order

When this file conflicts with current files:

```text
1. User-provided local archive, if explicitly current/up to date.
2. Actual file tree and file contents inside that archive.
3. Verified build result / RimWorld runtime smoke test.
4. Live GitHub and latest commits.
5. Docs/AI_ARCHITECTURE.md
6. Docs/BUILD_AND_SOURCE_LAYOUT.md
7. Docs/AI_QUICK_INDEX.md
8. Docs/RECENT_WORK.md
9. Docs/CONTENT_MATRIX.md
10. This risk document
11. Previous memory or old conversation context
```

Actual code and assets win over this document.

## Severity scale

| Severity | Meaning |
| --- | --- |
| P0 | Can break load, compile, save/load, runtime stability, or core progression. |
| P1 | Serious gameplay/UI regression, broken encounter behavior, wrong content gating, or severe visual issue. |
| P2 | Noticeable quality, balance, usability, or presentation issue. |
| P3 | Polish issue, minor inconsistency, or future maintainability risk. |

## Source/build/package risks

| Risk | Severity | Area | Symptoms | Prevention / check |
| --- | --- | --- | --- | --- |
| Reintroducing root-level `.cs` files | P0 | `source/` | Build confusion, duplicate files, user deletes root source files | Keep `source/` root clean; put every `.cs` in module folders. |
| Creating uppercase `Source/` | P0 | repository layout | Case-sensitive duplicate path on GitHub/Linux; local Windows confusion | Use lowercase `source/` only. |
| Shipping DLL without matching source | P0 | patch packaging | Future development loses source parity | Any C# change must include full changed `.cs` files and DLL only if build verified. |
| Claiming build success without actual build | P0 | workflow | False confidence, broken mod package | Only state Build verified after real `dotnet build` success. |
| Including `source/bin/` or `source/obj/` in delta zips | P2 | packaging | Dirty archives, confusing generated code | Exclude build artifacts from user-facing zips. |
| Ignoring local archive priority when user says it is current | P1 | workflow | Work based on stale GitHub state | Use local archive first when explicitly current/up to date. |
| Using docs as authority over actual code | P1 | workflow | Wrong path or stale assumption | Docs are maps; actual archive files win. |
| Forgetting commit summary | P2 | workflow | Harder continuation/history | Always include commit title + description after file changes. |

## C# / RimWorld API risks

| Risk | Severity | Area | Symptoms | Prevention / check |
| --- | --- | --- | --- | --- |
| Wrong override access modifier | P0 | C# compile | Compile error on `Tick`, `DrawAt`, etc. | Match base method access exactly. |
| Wrong RimWorld version signature | P0 | C# compile/runtime | Compile errors or methods not called | Check current RimWorld 1.6 signatures before editing death actions, comps, incidents, jobs. |
| Invalid `Find.Game` / API assumptions | P0 | C# compile | Compile error | Inspect actual references and existing utilities first. |
| Missing `UnityEngine` imports | P0 | C# compile | `Mathf`, `Texture2D`, `Rect`, etc. missing | Add proper using only where needed. |
| New XML class not in DLL | P0 | XML/C# | RimWorld load error: class not found | Rebuild DLL and include full source when adding new class. |
| XML custom fields without C# support | P0/P1 | XML defs | XML load warnings/errors or ignored data | Add/verify DefModExtension or Def class. |
| Overusing shared/global utilities for one feature | P2 | architecture | tangled dependencies | Put feature logic in narrowest module; shared only when genuinely reused. |

## XML/Def risks

| Risk | Severity | Area | Symptoms | Prevention / check |
| --- | --- | --- | --- | --- |
| Invalid RimWorld XML fields | P0/P1 | Defs | Red XML errors at load | Check existing valid patterns; avoid guessed fields. |
| Duplicate XML fields | P0/P1 | Defs | XML errors or unpredictable behavior | Audit changed defs for duplicate nodes, especially complex comps/properties. |
| Def name mismatch between XML and code | P0/P1 | Defs/C# | Null refs, missing content, broken UI | Search exact defName and class names before editing. |
| `texPath` mismatch | P1 | assets/XML | Missing texture pink squares | Verify exact asset path without extension. |
| SoundDef `clipPath` with multiple/broken assets | P1 | audio/XML | AudioClip load errors or wrong sound | Ensure only intended valid final file exists for a clipPath. |
| Patch operation targeting old path | P1 | Patches | Patch failure or silent no-op | Check current XML structure before patching. |
| Research/status docs claiming implemented when only planned | P2 | docs/design | Confusing future work | Separate Implemented / Partial / Planned in docs and responses. |

## UI risks

| Risk | Severity | Area | Symptoms | Prevention / check |
| --- | --- | --- | --- | --- |
| Treating custom UI as temporary overlay | P1 | Forge/Summoning/Protocol/BossBar | New content hidden in vanilla-only gizmos or inspect text | Check whether content belongs in custom console first. |
| Clipped text / cut lower letters | P1 | UI | Labels look broken, descenders clipped | Use existing UI safety/text helpers; test at target sizes. |
| Scrollbar/content overlap | P1 | Forge/Protocol/Summoning UI | Browser/card content under scrollbar | Reserve scrollbar space and test long lists. |
| Button style regression | P1 | UI/Shared | Custom buttons revert to vanilla/unstyled or inconsistent states | Reuse `source/UI/Shared/` styling and existing button state assets. |
| Overloaded circular/ring UI | P1/P2 | Protocol Nexus/Summoning | Content does not fit, unreadable at scale | Prefer category/filter/detail panels over stuffing dozens of items into ring area. |
| Static UI with large occupied area | P2 | Summoning circle UI/Protocol Nexus | Looks pretty but functionally weak | Add meaningful state, preview, requirement, and selection feedback. |
| Long text without wrapping/control | P2 | UI | Overflows/cuts in localized strings | Use wrapped text and constrained card layouts. |
| Too much animation/motion | P2 | UI/VFX | Distracting or unreadable | Use restrained animation; keep state/action readable. |

## Boss bar risks

| Risk | Severity | Area | Symptoms | Prevention / check |
| --- | --- | --- | --- | --- |
| Aegis bar placement wrong | P1 | Reactor Saint boss bar | Aegis appears inside/beside instead of under boss bar | Compare against intended layout; test with shield up/down. |
| Aegis label clipping | P1 | Boss bar | Text top/bottom eaten | Use safe text rects and avoid tight dark backplates. |
| Chain visuals not aligned | P2 | Boss bar | Chains do not connect Aegis to HP bar | Validate start/end anchors visually. |
| Phase display stale/wrong | P1 | Boss state/UI | Phase 3 then reverts to Phase 2, or wrong phase text | Source phase from canonical boss state; avoid cached stale display state. |
| HP numeric updates but fill does not | P1 | Boss bar | Numbers change while bar stays static | Verify fill fraction source and repaint/update path. |
| Boss selection overlay too large/small | P2 | boss selection | awkward clickable area or debug feel | Use Def-driven boss selection profiles. |

## Reactor Saint risks

| Risk | Severity | Area | Symptoms | Prevention / check |
| --- | --- | --- | --- | --- |
| Reactor Saint bleeding | P1 | pawn defs/health/comps | Boss bleeds despite previous fix | Re-check race/hediff/comps/health setup after pawn edits. |
| Boss can be downed instead of true death/controlled death | P1 | boss comps | fight enters invalid state | Keep boss no-downed/true-death comps and death workers intact. |
| Pawn generation age/life stage errors | P0/P1 | pawn defs | red errors during dev spawn | Verify life stages and race props for custom boss pawns. |
| AI freezes around many colonists | P1 | boss AI | Saint stands still instead of melee/building response | Regression-test dense pawn scenarios and building-priority targeting. |
| Building targeting hits hidden utility wires/cables | P1 | AI targeting | boss wastes attacks on hidden conduits | Preserve filtering for hidden conduit/wire-like buildings. |
| Cocoon invisible or wrong launch direction | P1 | presentation/buildings/VFX | arrival/launch presentation broken | Test cocoon fall, release, upward launch, ocean/deep water edge cases. |
| Escort spawning missing at difficulty gates | P2 | encounter scaling | expected escorts absent | Check escalation/difficulty profiles and release spawn logic. |
| Hit-event microstutter | P2 | performance | stutter when Saint hits pawns | Profile hit VFX/damage feedback/tick loops if touched. |

## Archon / Rupture risks

| Risk | Severity | Area | Symptoms | Prevention / check |
| --- | --- | --- | --- | --- |
| Wrong directional pawn textures | P1 | pawn sprites | east/west movement holes, wrong facing | Use south/east/north only; west is mirrored from east. |
| Adding separate west texture for VOID-style pawn | P1 | pawn sprites | facing/rendering confusion | Do not add west unless project explicitly changes convention. |
| Archon phase/portal cleanup leaks | P1 | encounter runtime | portals/pawns/state remains after boss | Check cleanup utilities and true-death handling. |
| Rupture branch accidentally broken by Archon fixes | P1 | boss progression | secret/branch boss does not trigger | Test Archon and Rupture paths separately. |
| Crown/halo visuals too large/misaligned | P2 | Rupture visuals | unreadable or floating wrong | Validate at in-game scale and with pawn directions. |

## Dominion risks

| Risk | Severity | Area | Symptoms | Prevention / check |
| --- | --- | --- | --- | --- |
| Dominion pocket/slice save/load instability | P0/P1 | maps/world/runtime | lost pawns, broken maps, stuck transitions | Test save/load during entry, active slice, exit, cleanup. |
| Map cleanup not complete | P1 | Dominion runtime | orphan maps/world objects/components | Verify cleanup/deinit guards and world object removal. |
| Heart/anchor graphics replaced by platform | P1 | Dominion visuals | only platforms visible; real objects hidden | Platforms must be underlays; heart/anchors render above. |
| Side architecture not spawning | P2 | mapgen | sterile empty hell map | Verify gen step, placement rules, density, and max-zoom readability. |
| Too many VFX/ticks in Dominion | P1/P2 | performance | TPS/FPS drops in hell map | Keep ambient VFX restrained; avoid per-cell expensive ticks. |
| Spawn presentation looks like generic portal | P2 | presentation | wrong fantasy feel | Prefer Dominion seam emergence where intended. |
| Terrain too contrasty/noisy | P2 | visuals | unreadable pawns/items | Terrain should be low-contrast, tileable, and not fight silhouettes. |

## Forge / progression risks

| Risk | Severity | Area | Symptoms | Prevention / check |
| --- | --- | --- | --- | --- |
| Forge unlock duplicated with research in confusing way | P1/P2 | progression/UI | player does not know whether Forge or research unlocks content | Make requirement blocks show research + forge + boss/sigil gates clearly. |
| Attunement tier starts wrong | P1 | Forge progression | tier bonuses applied too early or not shown | Verify initial tier and thresholds after changing progression. |
| Pattern browser scales poorly | P2 | Forge UI | dozens/hundreds of entries become unusable | Maintain categories, filters, search/subfilters, selected detail panel. |
| Sintering corpse recipe yield wrong | P1/P2 | Forge/recipes | always 1 residue or strange vanilla behavior | Verify custom recipe worker/building behavior in-game. |
| Dev gizmo text missing/localization key shown | P2 | UI/localization | `ABY_*` key displayed | Add translation keys and test visible labels. |

## Summoning / sigil risks

| Risk | Severity | Area | Symptoms | Prevention / check |
| --- | --- | --- | --- | --- |
| Sigil activation timing mismatch | P1 | summoning jobs/VFX | VFX fires before pawn waits/activation completes | Preserve carry/wait/activate sequence. |
| Beam/VFX offset from circle center | P1/P2 | VFX | activation beam appears above/beside circle | Test in-game with actual circle footprint and overlay coordinates. |
| Old animation not removed | P2 | VFX/assets/XML | obsolete VFX still plays alongside new effect | Search old mote/texture/def references and remove/wire correctly. |
| Horde composition includes bosses/minibosses when not intended | P1 | encounter templates | horde contains invalid units | Keep horde pools separate from boss/miniboss pools unless explicitly designed. |
| Summoning Console duplicate descriptions | P2 | UI text | repeated ritual preview/consequence blocks | Keep preview concise and avoid duplicated sections. |
| Instability effects unclear | P2 | UI/gameplay | player cannot tell risk/cause | Expose instability state and consequences in Summoning UI. |

## Modular turret risks

| Risk | Severity | Area | Symptoms | Prevention / check |
| --- | --- | --- | --- | --- |
| Projectile class missing after XML load | P0 | turret/projectile C# | XML class load error | Rebuild DLL and include source when adding class. |
| Projectile origin not matching visual barrel | P1/P2 | turret VFX | shots emerge from wrong point | Use synchronized burst index/offsets and test rotation. |
| Charge/discharge overlay draw order wrong | P2 | turret visuals | overlay under/over wrong layer | Check modular turret overlay draw order. |
| LOS behavior wrong for indirect/direct modules | P1 | targeting | mortar requires LOS or direct gun shoots through walls | Use per-module `targetRequiresLineOfSight` patterns. |
| Turret module UI not updated for new module | P2 | UI | module works but player cannot understand/install it | Check `source/UI/Turrets/` and Forge recipe exposure. |
| Texture sizes too large | P2 | optimization | VRAM bloat | Downsize only when readability is preserved; prefer lossless optimization first. |

## Apparel / pawn graphics risks

| Risk | Severity | Area | Symptoms | Prevention / check |
| --- | --- | --- | --- | --- |
| Apparel body type missing | P1 | apparel textures | invisible/wrong armor on body type | Check Male/Female/Thin/Fat/Hulk where relevant. |
| Apparel direction holes | P1 | apparel sprites | gaps or wrong orientation east/west/north/south | Inspect all directional overlays in-game. |
| Realistic humanoid proportions from generated assets | P2 | assets | looks unlike RimWorld pawn | Use simplified RimWorld pawn proportions for humanoids. |
| Green chromakey left in final asset | P1 | assets | green background in-game | Remove chromakey and export true alpha PNG. |
| Fake checkerboard transparency | P1 | source asset | extraction unusable | Use solid #00FF00 source or real alpha final. |
| Excessive smoke/fire baked into static sprites | P2 | readability | silhouette hidden at game scale | Keep VFX separate from static pawn/building asset when possible. |

## Audio risks

| Risk | Severity | Area | Symptoms | Prevention / check |
| --- | --- | --- | --- | --- |
| Broken OGG/WAV payload | P1 | audio loading | RimWorld AudioClip errors | Verify file decodes before packaging. |
| Too many iterations on one SFX | P3 | workflow | time sink, slow broad coverage | Stop after 2–3 rounds and change strategy if needed. |
| Energy weapon sounds too firearm-like | P2 | style | wrong identity | Use energy-first mix; save firearm-like results for ballistic/industrial future use. |
| Short SFX stored as OGG without reason | P3 | audio format | less ideal pipeline | Prefer WAV for short weapon/UI/VFX sounds under ~2 seconds. |
| Binary GitHub upload not verified | P1 | workflow | corrupted audio/texture committed | Prefer delta zip or verify binary after direct upload. |

## Performance risks

| Risk | Severity | Area | Symptoms | Prevention / check |
| --- | --- | --- | --- | --- |
| Expensive per-tick loops over all pawns/buildings | P1 | runtime | TPS drops on large maps | Use throttling, map components carefully, cache where safe. |
| Excessive VFX spawning | P1/P2 | combat/boss/Dominion | FPS/TPS drop during fights | Cap VFX, use restrained motes, avoid redundant spawn loops. |
| UI work in OnGUI too heavy | P2 | UI | stutter when windows/boss bar open | Cache textures/layout where safe; avoid expensive searches every frame. |
| Large unoptimized textures | P2 | VRAM/loading | memory bloat | Lossless optimize by default; downscale only after readability check. |
| Presentation scaffolding left active | P2 | boss/Dominion | unnecessary runtime overhead | Remove or gate dev/test presentation systems. |

## Localization/text risks

| Risk | Severity | Area | Symptoms | Prevention / check |
| --- | --- | --- | --- | --- |
| Missing translation keys | P2 | UI/Defs | raw `ABY_*` keys shown | Add keys under `Languages/English/` and test visible UI. |
| Inconsistent tone with lore docs | P2/P3 | descriptions | generic demon/fantasy feel | Use techno-infernal, ritual-industrial tone. |
| Text says implemented when content is planned | P2 | docs/UI | player/dev confusion | Mark planned/partial/implemented clearly. |
| Too much lore in small UI cards | P2 | UI | unreadable or cluttered | Put concise gameplay requirements in UI; keep long lore in codex/descriptions. |

## Documentation risks

| Risk | Severity | Area | Symptoms | Prevention / check |
| --- | --- | --- | --- | --- |
| AI docs becoming stale | P2 | docs/workflow | future patches route to wrong files | Update docs when architecture/system ownership changes. |
| RECENT_WORK not updated after large pass | P2 | docs/workflow | future chat forgets important recent decision | Update after major UI/boss/Dominion/content/refactor work. |
| CONTENT_MATRIX missing new content category | P2 | docs/workflow | future additions half-integrated | Update matrix when new content category/framework appears. |
| This file grows into a noisy changelog | P3 | docs/workflow | risk list becomes unreadable | Keep only recurring/structural risks; use RECENT_WORK for recent events. |

## Pre-patch checklist

Before producing an integration patch:

```text
1. Confirm whether the user says the local archive is current.
2. Inspect actual file paths in the archive.
3. Check Docs/AI_QUICK_INDEX.md for where to look first.
4. Check Docs/RECENT_WORK.md for recent decisions.
5. Check this risk file for known regression zones.
6. For C# changes, build if possible and include DLL only if verified.
7. For XML changes, check class names, defNames, duplicate fields, texPaths.
8. For assets, verify final paths, alpha/chromakey handling, and optimization.
9. For UI changes, check Forge/Summoning/Protocol/BossBar custom surfaces.
10. Update architecture/docs if the change affects system ownership, layout, or recurring risk.
```

## Runtime smoke-test checklist after high-risk changes

Use when possible:

```text
- Mod loads without red XML/class errors.
- Forge opens and pattern browser/requirements render.
- Summoning Circle opens and ritual preview/activation works.
- Protocol Nexus opens and selected ring/socket highlight works.
- Boss bar displays correct HP/phase/Aegis state.
- Archon and Reactor Saint can be dev-spawned without generation errors.
- Reactor Saint does not bleed or enter invalid downed state.
- A modular turret with custom projectile fires without class/VFX errors.
- Dominion slice entry/exit/cleanup works after save/load.
- No missing textures, green backgrounds, raw translation keys, or audio decode errors.
```

## Maintenance rule

Update this file when:

```text
- a recurring bug is discovered or fixed;
- a new high-risk system is added;
- a repeated AI mistake appears;
- a regression checklist item becomes obsolete;
- source layout/build workflow changes;
- asset/audio/UI pipeline rules change.
```

Do not update for isolated harmless edits unless the risk knowledge would be lost.
