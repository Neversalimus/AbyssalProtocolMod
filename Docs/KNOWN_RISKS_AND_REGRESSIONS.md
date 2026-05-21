
## Modular turret passive target scoring must stay on throttled scans

When adding passive modules that alter target choice, keep the logic inside the existing modular turret target scoring path. Do not add per-tick map-wide pawn scans for passive targeting modules. Execution-style wounded targeting, cluster targeting and shield/mech prioritization should remain additive scoring hints evaluated only during the existing target scan interval.

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


## Localization and player-facing text risks

| Risk | Severity | Area | Symptoms | Prevention / check |
| --- | --- | --- | --- | --- |
| Technical implementation text in item/weapon descriptions | P2 | localization / DefInjected / XML descriptions | Players see lines about mods, runtime, defs, save/load, animated projectiles, projectile internals, or framework behavior | Descriptions should be weapon/lore-facing. Keep technical wording only in dev/debug settings or internal comments. Scan both English base defs and Russian localization before packaging. |
| Custom turret module fields not localized | P2 | modular turrets / Forge UI | Forge cards show English `Slot`, `Role`, `Effect`, `Primary gun module`, or raw internal tactical roles in Russian mode | Localize `ABY_TurretModuleDef` fields through `Languages/<Lang>/DefInjected/ABY_TurretModuleDef/ABY_TurretModuleDefs.xml`; also cover ThingDef/RecipeDef text for module items. |
| Long turret/Forge labels overflowing cards | P2 | Forge UI / turret modules | Pattern cards and selected panel clip or overlap text | Prefer short labels in tight cards and move long lore to descriptions/tooltips. Use glossary short forms where possible. |


## Large modpack compatibility risks

| Risk | Severity | Area | Symptoms | Prevention / check |
| --- | --- | --- | --- | --- |
| Abyssal summoned predators use vanilla food-hunting behavior | P1 | Pawn race XML / hostile encounters | Letters such as `An ember hound is hunting <colonist> for food!`; summoned enemies momentarily behave like animals hunting meals instead of encounter hostiles | Do not mark temporary hostile abyssal summon races as vanilla predators. Keep animal-style summoned combatants hungerless/non-predatory unless a real ecological animal is intentionally added. |
| Missing Melee Animation weapon tweak data | P2 | External mod compatibility / melee weapons | `[MeleeAnim] neversalimus.abyssalprotocol has ... missing weapon tweak data` warning and awkward or unsupported melee animation placement | Add one `WeaponTweakData/*.json` entry for each new Abyssal melee `ThingDef`; inspect the weapon texture and tune offsets/rotation in Melee Animation's editor if exact hand placement matters. |

## Source/build/package risks

| Risk | Severity | Area | Symptoms | Prevention / check |
| --- | --- | --- | --- | --- |
| Reintroducing root-level `.cs` files | P0 | `source/` | Build confusion, duplicate files, user deletes root source files | Keep `source/` root clean; put every `.cs` in module folders. |
| Creating uppercase `Source/` | P0 | repository layout | Case-sensitive duplicate path on GitHub/Linux; local Windows confusion | Use lowercase `source/` only. |
| Shipping DLL without matching source | P0 | patch packaging | Future development loses source parity | Any C# change must include full changed `.cs` files and DLL only if build verified. |
| Claiming build success without actual build | P0 | workflow | False confidence, broken mod package | Only state Build verified after real `dotnet build` success. |
| Compiling `AbyssalProtocol.dll` against .NET Core/.NET 9 reference assemblies | P0 | build/runtime | RimWorld load shows `ReflectionTypeLoadException`, then many `Could not find type named AbyssalProtocol...` XML errors | For emergency Roslyn builds, use bundled .NET Framework-style references: `mscorlib.dll`, `System.dll`, `System.Core.dll`, RimWorld `Assembly-CSharp.dll`, Unity modules, and Harmony. Verify assembly refs do not include `System.Runtime, Version=9.0.0.0`. |
| Including `source/bin/` or `source/obj/` in delta zips | P2 | packaging | Dirty archives, confusing generated code | Exclude build artifacts from user-facing zips. |
| Routing miniboss custom HP through the full boss HUD by default | P2 | UI/combat readability | Miniboss fights feel like major boss encounters, or custom HP remains unreadable if no full boss profile exists | Keep minibosses on compact overhead bars unless they are intentionally promoted to major boss status; continue reading HP from `CompABY_BossTrueDeath`. |
| Adding miniboss HP bars only through a new `GameComponent` | P1 | UI/save compatibility | Existing saves do not display the new bars because the new component was never instantiated for that save | Route the live draw call through an existing long-lived component such as `AbyssalBossScreenFXGameComponent`; keep new components as fallback shells unless save migration is implemented. |
| Letting legacy `role=boss` override explicit miniboss classification | P1 | UI/classification | Warden/Choir or future minibosses are filtered out of overhead HP bars or treated as major bosses despite `isMiniBoss=true` | In `ABY_AbyssalPawnClassificationUtility`, explicit `ABY_AbyssalPawnClassificationExtension.isMiniBoss` must win over older difficulty-scaling role strings unless `isBoss=true` is also explicitly set. |
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
| Tab hover wash/white rectangle regression | P2 | UI/Shared / Forge categories | Category/tab hover shows a pale rectangular overlay that does not match Abyssal skins | Do not draw the generic `BaseContent.WhiteTex` hover wash over tab-style buttons; rely on tab hover textures and custom content tinting. |
| Overloaded circular/ring UI | P1/P2 | Protocol Nexus/Summoning | Content does not fit, unreadable at scale | Prefer category/filter/detail panels over stuffing dozens of items into ring area. |
| Static UI with large occupied area | P2 | Summoning circle UI/Protocol Nexus | Looks pretty but functionally weak | Add meaningful state, preview, requirement, and selection feedback. |
| Long text without wrapping/control | P2 | UI | Overflows/cuts in localized strings | Use wrapped text and constrained card layouts. |
| Too much animation/motion | P2 | UI/VFX | Distracting or unreadable | Use restrained animation; keep state/action readable. |
| Forge communion/attunement bars regress to generic or noisy styling | P3 | Forge UI | Important state bars either look out of place or become harder to read | Keep the procedural industrial segmented gauge style: dark trough, visible black segment gaps, restrained brass/ember framing, centered labels on a dark readable capsule, subtle animation only. Do not use `rect.ContractedBy(12f)` for labels inside 20–24px bars because it collapses label height. |
| Forge Pattern records / Next milestones clipped wrapping | P2 | Forge UI | `Next pattern` or blocker lines lose their second line or overlap following rows | Use wrapped height with padding for Tiny text and advance Y from measured height; avoid hardcoded 22px rows for wrapped milestone/upcoming pattern labels. |
| Hard-coded English in custom UI filters/status chips | P2 | Forge/Summoning/Protocol UI localization | Russian UI shows `Search`, `Selected pattern`, `Needs resources`, raw category names, or awkward mixed-language chips | Put all player-facing text behind Keyed translation helpers; scan custom UI source for literal labels after adding filters/search/status chips. |
| Over-aggressive custom UI cache invalidation | P2 | Forge/Summoning UI performance | stale ritual lists, stale material badges, or selected pattern not updating after filters/resource changes | Cache only derived lists/statuses, keep player actions immediate, include category/subfilter/status/search/residue/state keys, and refresh material/status data on a short budgeted interval rather than every OnGUI event. |

## Boss bar risks

| Risk | Severity | Area | Symptoms | Prevention / check |
| --- | --- | --- | --- | --- |
| Aegis bar placement wrong | P1 | Reactor Saint boss bar | Aegis appears inside/beside instead of under boss bar | Compare against intended layout; test with shield up/down. |
| Aegis label clipping | P1 | Boss bar | Text top/bottom eaten | Use safe text rects and avoid tight dark backplates. |
| Chain visuals not aligned | P2 | Boss bar | Chains do not connect Aegis to HP bar | Validate start/end anchors visually. |
| Phase display stale/wrong | P1 | Boss state/UI | Phase 3 then reverts to Phase 2, or wrong phase text | Source phase from canonical boss state; avoid cached stale display state. |
| HP numeric updates but fill does not | P1 | Boss bar | Numbers change while bar stays static | Verify fill fraction source and repaint/update path. |
| Boss selection overlay too large/small | P2 | boss selection | awkward clickable area or debug feel | Use Def-driven boss selection profiles. |
| Monster info-card header icon too large or inconsistent | P2 | pawn info card / presentation | Monster icon overlaps the name or looks wildly different between hostile Abyssal pawns | Preserve the hostile Abyssal runtime `uiIconScale` normalization pass and regression-test a small, medium, and very large monster info card after sprite or drawSize changes. |

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
| Map.generatorDef reflection fallback | P3 | Dominion map detection/compatibility | Dominion slice detection relies only on site def/component if RimWorld renames the private field | Keep explicit sterile map component marking as the primary path; reflection is cached and warning-throttled fallback only. Test after RimWorld version bumps. |

## Forge / progression risks

| Risk | Severity | Area | Symptoms | Prevention / check |
| --- | --- | --- | --- | --- |
| Forge unlock duplicated with research in confusing way | P1/P2 | progression/UI | player does not know whether Forge or research unlocks content | Make requirement blocks show research + forge + boss/sigil gates clearly. |
| Attunement tier starts wrong | P1 | Forge progression | tier bonuses applied too early or not shown | Verify initial tier and thresholds after changing progression. |
| Pattern browser scales poorly | P2 | Forge UI | dozens/hundreds of entries become unusable | Maintain categories, filters, search/subfilters, selected detail panel. |
| Selected Forge pattern panel clips content | P2 | Forge UI | long pattern descriptions, requirement lists, or research blockers are cut off | Keep the selected pattern body scrollable and leave the action/footer area fixed. |
| Custom Forge material checks block bill creation | P1 | Forge UI/bills | player has or can obtain materials but Add Bill is disabled by Forge UI | Never gate Add Bill solely on custom material availability; let RimWorld vanilla bills resolve resources. Use material status only as informational UI. |
| Sintering corpse recipe yield wrong | P1/P2 | Forge/recipes | always 1 residue or strange vanilla behavior | Verify custom recipe worker/building behavior in-game. |
| New abyssal enemy missing sintering value | P2 | Forge/recipes/XML ownership | corpse cannot be processed for residue even though enemy is intended as a normal abyssal unit | Add `AbyssalProtocol.ABY_ResidueSinteringExtension` to the new non-boss `PawnKindDef`; do not expand the legacy C# fallback table unless maintaining old saves/old XML. |
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
| Raw vanilla scrollbars in Abyssal custom UI | P3 | UI/style | gray scrollbar visually breaks Forge/Summoning/Nexus style | Use `AbyssalStyledWidgets.BeginAbyssalScrollView` / `EndAbyssalScrollView` or `DrawAbyssalVerticalScrollbar`; do not modify `GUI.skin` globally. |
| Large unoptimized textures | P2 | VRAM/loading | memory bloat | Lossless optimize by default; downscale only after readability check. |
| Presentation scaffolding left active | P2 | boss/Dominion | unnecessary runtime overhead | Remove or gate dev/test presentation systems. |

## Localization/text risks

| Risk | Severity | Area | Symptoms | Prevention / check |
| --- | --- | --- | --- | --- |
| Missing translation keys | P2 | UI/Defs | raw `ABY_*` keys shown | Add keys under `Languages/English/` and `Languages/Russian/` and test visible UI. |
| Duplicate flat language keys | P1 | localization | RimWorld language report shows duplicate or broken translation data, often when same defName exists in multiple DefInjected folders | Scan all `Languages/<lang>/DefInjected/**` keys as a flat set before packaging; avoid duplicate PawnKind/ThingDef label entries with the same key. |
| Orphan DefInjected keys for removed/renamed defs | P1 | localization/XML | RimWorld language report warns about translation errors even when XML parses | For every DefInjected key, verify the defName exists in the matching Def type; remove stale keys when defs are renamed or deleted. |
| English leftovers in Russian visible labels | P2 | localization/UI | Russian UI still shows English names like boss titles, difficulty names, horde labels, or recipe job strings | Run a Latin-text scan over Russian label/title/header/button/jobString values after content batches. |
| Machine-translated Russian names / glossary drift | P2 | localization/UI/Defs | Awkward or wrong names such as `Рифт Бладе`, `Рифт Карбине`, `Забвение хоровой`, `святой эгида панцирь`, or malformed sigil labels | Check `Docs/LOCALIZATION_GLOSSARY_RU.md` before editing Russian text; update the glossary and localization together when terminology changes. |
| Russian plural forms in UI counts | P2 | localization/UI | Wrong endings such as `1 требований`, `2 требований`, or cramped requirement counters | Use Russian plural-aware helper logic for numeric UI counts: 1 требование, 2-4 требования, 5+/11-14 требований. |
| Inconsistent tone with lore docs | P2/P3 | descriptions | generic demon/fantasy feel | Use techno-infernal, ritual-industrial tone. |
| Text says implemented when content is planned | P2 | docs/UI | player/dev confusion | Mark planned/partial/implemented clearly. |
| Too much lore in small UI cards | P2 | UI | unreadable or cluttered | Put concise gameplay requirements in UI; keep long lore in codex/descriptions. |


## Russian localization risks

| Risk | Severity | Area | Symptoms | Prevention / check |
| --- | --- | --- | --- | --- |
| Treating Russian localization as only XML validation | P1 | localization/UI | No load errors, but visible text is ugly, too long, or machine-translated | Check `Docs/LOCALIZATION_GLOSSARY_RU.md`, run bad-phrase scans, and review Forge/Summoning UI length. |
| Long lore names in Forge cards | P2 | Forge UI/localization | Browser cards and selected-pattern panels look crowded or clipped | Use compact labels in `label`/recipe rows and keep full names in descriptions/tooltips. |
| Raw English/transliterated terms in Russian mode | P2 | localization | `веил`, `слинг`, `харнесс`, `пайплайн`, `кэши`, `scanline/sweep` appear in-game | Translate as natural Russian terms and re-run Latin/transliteration scan before packaging. |
| Wrong category for named content | P2 | localization/content | Weapon names get translated like pawns or vice versa | Check actual Def type first; `Oblivion Choir` is a weapon/proper name: `Хор Забвения`. |


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
6. For Russian localization, check Docs/LOCALIZATION_GLOSSARY_RU.md before changing terminology.
7. For C# changes, build if possible and include DLL only if verified.
8. For XML changes, check class names, defNames, duplicate fields, texPaths.
9. For assets, verify final paths, alpha/chromakey handling, and optimization.
10. For UI changes, check Forge/Summoning/Protocol/BossBar custom surfaces.
11. Update architecture/docs if the change affects system ownership, layout, or recurring risk.
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

## Russian turret localization: custom Def fields can leak raw English

Custom `ABY_TurretModuleDef` fields such as `role` and `effectSummary` appear directly in Forge cards and turret tooltips if missing localization data. Keep both `Languages/<Lang>/DefInjected/ABY_TurretModuleDef/` custom-field translations and mirrored `ABY_TurretModuleRole_<defName>` / `ABY_TurretModuleEffect_<defName>` Keyed entries in sync. A future C# hardening pass may route these accessors through keyed lookup, but this patch intentionally leaves runtime code unchanged.

Player-facing descriptions must describe the weapon or lore. Do not mention implementation terms such as runtime streams, save/load storage, projectile animation, prototype plumbing, def names, or feature kill-switches in item descriptions or Forge tooltips.


## Static constructor logging must be startup-safe

`[StaticConstructorOnStartup]` classes run while RimWorld is still building play data. Do not let optional compatibility patches, Harmony guards, or diagnostics call logging helpers that assume `Find.TickManager`, `Current.Game`, or mod settings are fully initialized.

Checklist for future logging changes:

```text
- Do not use Verse string helpers inside low-level log throttle gates when plain `string.IsNullOrEmpty` is enough.
- Wrap settings access separately from tick access.
- Prefer silence over red errors when diagnostics fail during early startup.
- If a C# source fix already exists, make sure `Assemblies/AbyssalProtocol.dll` is rebuilt from the same source before packaging.
```

## 2026-05-19 — Custom turret Def localization regression

- Risk: custom `ABY_TurretModuleDef` fields such as `role` and `effectSummary` may still appear as raw English in Forge UI if code reads the raw field directly.
- Rule: UI code must use localized wrapper properties (`LocalizedLabel`, `LocalizedLabelCap`, `RoleLabel`, `EffectSummary`) or explicit Keyed lookups, not raw `label`, `role`, or `effectSummary`.
- Do not add player-facing turret text that mentions `projectile`, runtime behavior, save/load, kill switches, module defs, or other implementation details.
- Forge-visible descriptions should be short enough for cards and should use lore/gameplay phrasing rather than progression/tier labels.

## Optimization / packaging risks

| Risk | Severity | Area | Symptoms | Prevention / check |
| --- | --- | --- | --- | --- |
| Runtime texture downscale temptation | P2 | performance/assets | Memory spikes, cache confusion, first-use lag | Prefer build-time texture budget tools and pre-optimized PNGs; do not downscale Unity textures at runtime by default. |
| Release package includes dev/source assets | P1 | packaging | Workshop zip becomes huge or includes chromakey/source art | Use `Tools/ABY_BuildReleasePackage.py`; exclude `SourceAssets/`, `Tools/`, `BuildOutput/`, source art formats, and caches. |
| Texture optimizer touches boss/hero sprites automatically | P1 | assets | Bosses become blurry or lose silhouette | Keep boss/hero groups manual-review only in `texture_budget_rules.json`. |
| Performance preset changes gameplay | P0/P1 | settings/runtime | Reduced/Minimal changes rewards, AI, boss logic, encounter composition | Visual intensity presets must only affect optional presentation/VFX/UI motion/weather density. |
| Dominion ambient VFX disabled too aggressively | P2 | Dominion visuals | Slice feels sterile in Minimal/Reduced mode | Minimal may disable optional ambient VFX; Reduced should keep lighter ambient visuals with longer intervals. |

## Diagnostics interpretation risks
| Risk | Severity | Area | Symptom | Mitigation |
|---|---|---|---|---|
| Misreading MapComponent presence as active Dominion Hell | P1 | Diagnostics / Dominion | Empty or horde-test map shows Dominion atmosphere/slice/crisis components as `present`, causing false suspicion of active Dominion Hell | Treat component presence as normal; use performance audit's Dominion state section: `marked`, `session`, `pocket reason`, `slice active`, and ambient active reason. |
| Raw Abyssal thing counts are too broad without breakdown | P1 | Diagnostics / horde testing | Horde tests show high `Abyssal things`/`Abyssal pawns` counts but do not identify whether they are live pawns, corpses, portals, buildings, or leftover state | Use top PawnKind/ThingDef/category breakdown and portal-wave snapshot before attempting cleanup or balance fixes. |

## 2026-05-19 — TPS scan-loop regression guard

Recurring risk: adding independent short-interval map/pawn/thing scans to many systems will recreate late-game TPS loss during horde, turret, boss, and Dominion encounters.

Regression rules:

- Do not add new 12–60 tick `AllPawnsSpawned`, `AllThings`, `Find.Maps`, or full-map cell sweeps without checking whether `ABY_RuntimeTargetCache`, `ThingsOfDef`, or chunked processing can be used instead.
- Do not spawn optional projectile trails, beam segments, Dominion flows, hover sparks, or decorative UI/gameplay motes without checking visual intensity and/or `ABY_VfxBudget`.
- Dominion map maintenance must stay chunked; avoid restoring one-pass full-map cleanup in runtime ticks.
- Modular turrets should keep cached target retention and staggered scan intervals; avoid per-tick reacquisition for every turret.


## 2026-05-19 — Remaining TPS optimization regression checklist

Potential regressions introduced by the remaining optimization layer:

| Area | What could regress | In-game check |
| --- | --- | --- |
| Runtime thing-ID cache | Delayed beam/stream target lookups may resolve stale or missing targets for up to the cache interval after destruction/despawn. | Fire Specter Lash / delayed projectile effects at pawns and buildings, then destroy/despawn targets; verify no red errors and beams stop or retarget cleanly. |
| Projectile VFX budget | Trails, sparks, choir arcs, plasma pulses, and rocket micro-target visuals may become too sparse in Reduced/Minimal. | Test Rift Carbine, Ultra Plasma, Hexgun, Null Bolt, Ashen Pike, Choir Arc, Siege Idol shell, Crownfire carrier, and Oblivion Choir Core on Full/Reduced/Minimal. Damage must remain unchanged. |
| Reactor Saint projectile VFX | Saint volleys may look quieter because local VFX budget was replaced by shared combat budget. | Spawn Reactor Saint, observe salvos on Full/Reduced/Minimal; confirm gameplay damage/targeting still happens and no projectile VFX errors appear. |
| Anti-tame guard intervals | Tame/train/slaughter designations on abyssal monsters may persist a few seconds longer than before. | Try taming/training/slaughter designations on abyssal monsters; verify designations are removed without red errors and without blocking normal non-abyssal animals. |
| Large modpack portal hotfix | Cached hostile portal def-name list may miss unusual defs if another mod dynamically creates defs after load. | Start horde/portal encounters with large modpack active; verify special portals are not prematurely collapsed and orphaned hostile portals still hard-stop correctly. |
| Dominion ambient/edge/collapse budget | Minimal/Reduced modes may hide too much collapse/extraction/reward guidance. | Enter Dominion Slice, trigger collapse/reward/extraction phases, compare Full vs Reduced vs Minimal; ensure the map remains readable and guidance is still understandable. |
| Protocol Nexus UI cache | Header summary or project/category counts could lag until the refresh interval. | Open Protocol Nexus, start/finish decode, change project selection/category; verify buttons, locks, progress, and summary update within a short interval and no stale action is possible. |
| Abyssal monster brain cache | Enemy retargeting may lag slightly if combat target cache has just refreshed. | Spawn groups of abyssal monsters, down/kill visible colonist targets, verify enemies retarget without standing idle for long. |
| Implant ability friendly-fire cache | Friendly-fire checks may briefly use cached pawn lists. | Use implant abilities near moving friendly pawns; verify friendly-fire avoidance is still acceptable and no obvious self-blocking happens. |

Rules after testing:

- If a visual looks too quiet but gameplay works, tune `ABY_VfxBudget` category costs or per-effect spend frequency rather than removing the budget gate.
- If targeting feels sluggish, reduce the relevant cache interval before reverting to direct `AllPawnsSpawned`/`AllThings` scans.
- If UI state lags, invalidate the relevant Protocol Nexus cache on state change instead of moving sorting/filtering back into `DoWindowContents`.

## 2026-05-19 — Hidden faction relation safety regression

Observed runtime regression after the TPS targeting-cache pass:

- `Faction ... has null relation with PlayerColony. Returning dummy relation.`
- stack traces from modular turret target scans and boss aggression target selection.

Cause:

- Hidden/generated encounter factions such as `ABY_AbyssalHost` can exist in saves without a normal relation row to `PlayerColony`.
- Calling vanilla `Faction.HostileTo` / `Pawn.HostileTo` / `RelationWith` from frequent target scans logs a red error before returning a dummy relation.

Regression rules:

- New Abyssal target-selection, projectile splash, aura, boss, turret, anti-tame, or compatibility code should prefer `ABY_FactionHostilityUtility.SafeHostileTo(...)` over direct `HostileTo(...)` when one side may be an Abyssal pawn/faction.
- Direct vanilla hostility checks are still acceptable for purely vanilla actors with guaranteed normal relations, but do not use them in ABY hidden-faction hot paths.
- If red relation errors return, inspect the stack trace and replace the exact hot-path hostility check rather than reverting runtime target caching.

In-game checks:

- Spawn Reactor Saint from cocoon and verify boss aggression starts without relation red errors.
- Place modular turrets and spawn Abyssal Host pawns; turrets should acquire and fire without relation red errors.
- Test Choir/Null/Halo/Warden AoE effects and turret projectile impacts; effects should hit enemies and avoid allies without relation red errors.

## 2026-05-19 — Modular turret aggro regression

Observed behavior:

- Abyssal monsters could ignore player modular turrets after weapon modules were installed.

Cause:

- The modular turret is a custom comp-driven building, not necessarily a vanilla `Building_Turret` target from the perspective of every pawn AI path.
- The TPS cache pass made many target selectors pawn-cache first, so enemies that did not have dedicated building targeting could continue to prefer only colonist pawns.

Regression rules:

- Combat-capable modular turrets must be included in `ABY_RuntimeTargetCache.CombatTargetBuildingsFor` when they have a main weapon module installed.
- Abyssal monster brain and ranged shooter comps should consider hostile combat buildings through shared helpers, not by scanning `AllThings`.
- Do not make every decorative or passive building a monster target; only turret-like/weaponized defenses should be promoted to combat-building threat targets by default.

In-game checks:

- Install a main weapon module on an Abyssal modular turret, spawn melee Abyssal pawns nearby, and verify at least some of them path to attack the turret when it is the nearest/most relevant threat.
- Spawn Hexgun Thralls, Rift Sappers, and Siege Idols against a base with modular turrets and colonists; verify they can target/fire at modular turrets without ignoring colonists forever.
- Remove the main weapon module or depower the turret and verify enemies do not over-prioritize it as a combat threat.

## 2026-05-19 — Hidden utility structure targeting regression

Observed behavior:

- Breach-oriented Abyssal pawns could choose invisible/hidden utility structures such as hidden power cables as their breach target.
- These structures can be indestructible or functionally non-combat, causing monsters to waste time attacking a target that should never be a tactical objective.

Cause:

- Some breach and structure-damage paths validated buildings only through broad `useHitPoints` / player-home checks instead of the shared hostile building target filter.
- The modular turret aggro fix correctly promoted weaponized defenses as targets, but breach logic still allowed passive/hidden structures in its separate all-building scan.

Regression rules:

- New monster, boss, projectile, or breach code must use `AbyssalThreatPawnUtility.IsValidHostileBuildingTarget(...)` or `ShouldIgnoreAsHostileBuildingTarget(...)` before assigning an AttackMelee job or applying special anti-structure damage.
- Hidden/invisible/conduit/cable/wire utility buildings are never valid Abyssal tactical targets unless they are explicitly combat-capable turrets.
- Doors, walls, barricades, sandbags, barriers, and real turret-like defenses remain valid targets.

In-game checks:

- Spawn Breach Brutes/Chain Zealots near hidden cables and visible walls/doors. They should choose walls, doors, turrets, barricades, colonists, or other real targets, not hidden cables.
- Spawn Reactor Saint / Rift Sappers / Siege Idols near hidden cables and player defenses. Their bonus structure damage should not repeatedly target hidden utility objects.
- Large combat mod stacks can throw inside damage/downed/job reactions after Abyssal projectiles call vanilla Bullet.Impact. Keep custom high-impact projectile classes wrapped with ABY_ProjectileImpactSafetyUtility when they are known to trigger external TargetInvocationException/NullReferenceException paths.

## 2026-05-20 — Projectile impact safety regression rules

Observed behavior:

- In large combat modpacks, Abyssal projectiles can become the visible top-level source of red errors even when the exception is thrown deeper inside vanilla/external `Bullet.Impact -> TakeDamage -> DamageWorker -> HealthTracker -> Lord/ThinkNode` chains.
- Known examples included Choir Arc Pulse and Sepulcher Rail Spike hitting pawns while CombatAI/Yayo/VEF/Hospitality/MVCF/HAR hooks were active.

Regression rules:

- New custom projectiles that override `Impact(...)` and call `base.Impact(...)` must use `ABY_ProjectileImpactSafetyUtility.TryRunBaseImpact(...)`.
- New projectile post-impact direct damage should use `ABY_ProjectileImpactSafetyUtility.TryApplyDamage(...)` or `ABY_ProjectileProcUtility.ApplyDamage(...)`, not raw `Thing.TakeDamage(...)`, unless there is a strong reason and the call is otherwise guarded.
- Projectile explosions that are likely to touch pawns/buildings in combat mod stacks should run through `TryRunPostImpactAction(...)`.
- Do not hide exceptions with empty catch blocks. Use the shared safety utility so warnings are throttled and searchable.
- If a projectile fails during base impact, it should vanish rather than continue ticking forever.

In-game checks:

- Test Choir Arc, Sepulcher Rail, Reactor Saint projectiles, Crownfire micro-rockets, Oblivion Choir, Rift Sapper spike, Ashen Scatter, and modular turret projectiles in a combat-heavy modpack.
- Confirm impact VFX/sounds still play when base impact succeeds.
- Confirm red `Exception ticking ABY_* projectile` spam does not recur when external combat hooks throw during damage resolution.

## 2026-05-20 — Abyssal pawn classification ownership rule

Abyssal pawn role, construct physiology, residue-sintering eligibility, and boss/miniboss protection should not be reintroduced as scattered local C# `HashSet<string>` lists.

Rules:

- New non-boss abyssal enemies that should be processed by the Sintering Crucible must carry `AbyssalProtocol.ABY_ResidueSinteringExtension` on their `PawnKindDef` with a positive `residueValue`.
- New bosses or minibosses should carry `AbyssalProtocol.ABY_AbyssalPawnClassificationExtension` with `isBoss` or `isMiniBoss` so generic corpse/reward/Harvester logic protects them.
- New mechanical, semi-mechanical, or construct-like abyssal pawns should carry the same classification extension with `constructPhysiology` and `blockBloodLoss` so the construct physiology helper suppresses vanilla BloodLoss safely.
- Gameplay systems should prefer `ABY_AbyssalPawnClassificationUtility` instead of local `defName` checks when asking whether a pawn is abyssal, protected, boss-like, or construct-like.
- Legacy hardcoded fallbacks may remain for old saves and older XML, but they should not be the primary source of truth for new content.

In-game checks:

- Spawn each construct-like pawn type and verify it does not accumulate vanilla BloodLoss while still taking normal injuries/damage.
- Kill non-boss abyssal enemies and verify the Sintering Crucible sees/processes their corpses for the expected residue values.
- Kill boss/miniboss pawns and verify generic sintering/Harvester corpse logic does not consume or treat them as normal enemy corpses.

## 2026-05-20 — Legacy spawn-composition hardcoded lists remain by design

Some early/T1/Dominion/fallback wave systems still contain explicit pawn-kind names for encounter composition. These lists are not the same risk class as residue/physiology membership because they affect balance, pacing, and encounter identity, not only classification.

Rules:

- Do not mass-replace legacy wave composition with generic auto-pools without an in-game balance pass.
- When adding new enemies, first decide whether they belong in `ABY_EncounterTemplateDef` / `DefModExtension_AbyssalDifficultyScaling` pools, a special scripted wave, or both.
- Use the encounter director for scalable/new content where possible, but preserve deliberately authored T1/fallback compositions until they are playtested.
- If a new enemy should appear in older hardcoded T1/Dominion waves, update those wave builders explicitly and test pacing.

This is an intentional remaining architecture item, not a load/compile bug.

## 2026-05-20 — Encounter validator / shadow-mode safety boundary

A diagnostic-only layer now exists for future migration toward more data-driven encounter composition.

Important rules:

- `ABY_EncounterValidationUtility` may validate XML/Def consistency and log warnings, but it must not rewrite defs, change runtime state, or block a summon by itself.
- `ABY_EncounterShadowPlannerUtility` may compare an authored/legacy pack against a directed plan when `enableEncounterShadowPlanning` is enabled, but the real spawned wave must remain the authored pack.
- Shadow-mode logs are for balance comparison only. Do not treat shadow output as proof that the directed planner is ready to replace a ritual.
- Any migration from authored wave composition to directed templates must be done ritual-by-ritual with in-game playtests, save/load checks, and reward pacing review.
- If validator warnings appear after adding content, fix the XML ownership first: pool ids, role names, difficulty refs, budget costs, boss profile refs, and template coverage.

## 2026-05-20 — Dominion pocket music load-order warning guard

Observed behavior:

- Loading a save while the player camera/current map is inside a Dominion pocket can trigger `[Abyssal Protocol] Could not start Dominion pocket music; will retry.` even when the rest of the Dominion runtime is functioning.
- This warning is usually a music-manager readiness/load-order issue, not evidence that the pocket map, encounter, or save state failed.

Regression rules:

- Do not reintroduce immediate warning logs on the first failed hell-music start attempt after load.
- Keep a short post-load grace window and repeated-failure threshold before warning, because RimWorld and music-related mods may need several realtime seconds after save load before accepting forced track changes.
- Real music-start failures should still be visible through throttled warnings after repeated failures; do not replace the system with permanently silent catches.

In-game checks:

- Save inside an active Dominion pocket, reload, and verify no immediate music warning appears during the first few realtime seconds.
- Remain inside the pocket long enough to confirm the hell-pocket theme either starts or retries without log spam.
- Exit/close the pocket and verify normal music restoration still occurs without repeated restore warnings.

## 2026-05-20 — Projectile base-impact interop warnings should stay non-fatal

Observed behavior:

- In large combat modpacks, `Bullet.Impact(...)` can throw `NullReferenceException` inside vanilla or externally patched combat chains while an Abyssal projectile is the visible top-level caller.
- Known visible context: `Projectile_HexgunBurst base impact` logged through `ABY_ProjectileImpactSafetyUtility`.

Regression rules:

- Expected `NullReferenceException` from `base.Impact(...)` should remain non-fatal and should not produce repeated stack-trace warnings during combat.
- The projectile should be safely removed after a suppressed base-impact failure so it does not continue ticking.
- Unexpected post-impact/direct-damage exceptions should still use throttled warnings so real Abyssal regressions remain visible.
- Do not replace this path with a completely silent catch; use throttled messages for expected external base-impact failures and warnings for unexpected stages.

In-game checks:

- Fire Hexgun/Hexgun Thrall projectiles in the large modpack and confirm the old warning stack no longer appears repeatedly.
- Confirm normal Hex Mark application and impact VFX still occur when base impact succeeds.
- Confirm gameplay continues if an external combat stack still throws during `base.Impact(...)`.

## Dominion pocket return + modpack IMGUI scroll stack

Observed in a large modpack: after returning from the Dominion pocket, RimWorld could log `Mouse position stack is not empty. There were more calls to BeginScrollView than EndScrollView.` The stack only points to `Widgets.EnsureMousePositionStackEmpty`, so the leaking scroll owner is not directly visible. Video evidence showed the warning appears immediately after Dominion extraction while third-party UI overlays are present.

Mitigation added: Dominion pocket enter/jump/return actions triggered from UI now defer the actual map-transfer/collapse work by one Unity frame via `ABY_DeferredUIActionGameComponent`, and core Abyssal diagnostic/settings scroll views use try/finally EndScrollView guards. Do not reintroduce direct Dominion map transfers from IMGUI button delegates unless this regression has been retested in a large modpack.


## 2026-05-20 — DLL/source synchronization can cause XML class lookup storms

If XML defs reference a new `AbyssalProtocol.*` `DefModExtension`, comp, worker, hediff comp, incident worker, projectile, or UI class, the shipped `Assemblies/AbyssalProtocol.dll` must be rebuilt from the same source tree before packaging. Otherwise RimWorld loads XML first, cannot resolve the missing runtime type, and reports many repeated red errors such as `Could not find type named AbyssalProtocol.ABY_ResidueSinteringExtension` across every def that uses the class.

Regression guard:

```text
- After adding XML-referenced C# classes, rebuild the DLL before packaging.
- Verify the rebuilt assembly contains the new class name.
- Do not ship XML references to classes that only exist in source but not in Assemblies/AbyssalProtocol.dll.
- Keep using Framework-style RimWorld/Unity/Harmony references for emergency Roslyn builds; do not compile against .NET 9 reference assemblies.
```


## 2026-05-20 — Passive modular turret module integration risks

- Passive modules with negative `extraPowerDraw` require `ResolvedModulePowerDraw` to preserve the signed module sum; do not re-clamp module draw to zero before combining with base chassis draw, or Blackout Power Regulator becomes a fake module.
- Passive target-priority modules must keep using the existing throttled turret target scan and cached combat pawn list. Do not add per-tick global pawn scans for prioritizer/scanner modules.
- When adding passive module effects, expose them in both item info cards and the turret ITab. Otherwise the module may work but look like an unexplained black-box stat change.
- Final integrated turret module item icons must be true alpha PNGs under `Textures/Things/Item/TurretModules/`; green chromakey belongs only to source sheets, not runtime textures.

## 2026-05-20 — Modular turret passive aegis shield risk
- Passive turret shield modules use a custom shield pool inside `CompAbyssalModularTurret`; future damage-related changes must preserve `PostPreApplyDamage` absorption order, save/load fields and recharge clamping.
- Do not represent shield modules only as incoming damage multipliers: players need a visible, rechargeable aegis pool to understand why the module is different from armor/stabilizer passives.
- Turret module item icons should stay optimized for UI use; avoid reintroducing 512x512+ inventory icons unless a module needs large overlay art.

## 2026-05-21 — Encounter validator must fail soft but stay actionable

Observed behavior: a diagnostic startup scan in `ABY_EncounterValidationUtility` could throw a generic `NullReferenceException` and log `[Abyssal Protocol] Encounter validation failed...` without identifying the bad data or stage. This is especially easy to misread after unrelated content additions such as passive turret modules.

Regression rules:

- Do not wrap the whole validator in a single user-facing warning catch again.
- Keep validation split into named stages and keep per-def access null-safe.
- Unexpected diagnostic-stage exceptions should become actionable report notes/verbose diagnostics, not generic startup warning stacks.
- New content families that can affect startup/data integrity, such as `ABY_TurretModuleDef`, should have explicit validation rather than relying on unrelated encounter scans to fail.
- The validator must remain diagnostic-only and must never change encounter composition, turret module behavior, saved data, or player rewards.

In-game checks:

- Load with encounter data validation enabled and confirm there is no generic `Encounter validation failed: NullReferenceException` warning.
- Use the diagnostics window or "Validate encounters" button to confirm concrete warnings/notes are visible if data is malformed.
- Install passive/aegis turret modules and verify their gameplay behavior is unchanged by the validator.

## 2026-05-21 — Cross-save runtime target cache hardening
- Hardened `ABY_RuntimeTargetCache` against map `uniqueID` reuse across save switches by binding each cache entry to the actual `Map` instance, not just the numeric ID.
- Startup diagnostics now clears runtime target caches on game finalization so stale pawns/buildings from a previously loaded save cannot be reused by turrets or combat helpers.
- Modular turret runtime burst targets are no longer restored from saves; turrets reacquire targets after load instead of carrying serialized pawn references that may be stale or partially initialized.
- This addresses reports of modular turrets firing at empty cells and killing an apparently invisible `ABY_EmberHound` after switching to a different save.

## 2026-05-21 — Static per-map runtime state must be cleared on game load

Observed audit risk: `ABY_VfxBudget` stored per-map budget windows in a static dictionary keyed only by `map.uniqueID`. Like combat target caches, this can survive switching saves inside the same RimWorld process and can reuse state if a new save has a map with the same numeric id or a lower game tick.

Regression rules:

- Static per-map dictionaries must bind entries to the actual `Map` instance, not only `map.uniqueID`.
- Runtime-only static state should expose `ClearAll()` and be cleared from `ABY_StabilityDiagnosticsGameComponent.FinalizeInit()` when a new game/save finalizes.
- Tick-window state must reset if `TicksGame` moves backwards after a save switch.
- Gameplay must not depend on VFX budget state; budget failures may skip optional visuals only.

## 2026-05-21 — Dominion slice reference recovery must not scan AllThings every tick

Dominion slice reference recovery may need a full `map.listerThings.AllThings` scan after save/load or after older save migrations, but this should remain a fallback path.

Regression rules:

- Keep `CleanupReferences()` allocation-free in active tick/update paths.
- Use reverse `for` loops instead of `RemoveAll` lambdas that capture `map`.
- Do not call full `RestoreReferencesFromMap()` every tick while the encounter is active; throttle fallback scans and force them only on load or explicit recovery/spawn paths.
- If new Dominion actors are added, register references directly at spawn time whenever possible instead of relying on global map scans.

## 2026-05-21 — Miniboss overhead HP bars must use RimWorld map-label projection

Observed behavior: the first visible miniboss HP bar implementation appeared to drift or stay anchored around a fixed map/screen point instead of remaining attached to Choir Engine while the camera panned. The cause was using raw Unity `Camera.WorldToScreenPoint` inside RimWorld IMGUI/map UI drawing.

Regression rules:

- Do not use raw `Camera.WorldToScreenPoint` for RimWorld overhead IMGUI labels or bars unless the result is explicitly converted and tested against RimWorld UI scaling.
- Miniboss HP bars should use `GenMapUI.LabelDrawPosFor(pawn, offset)` so they share vanilla map-label projection behavior.
- Large sprite offsets for Choir Engine and other oversized minibosses should remain conservative; do not let drawSize alone push the bar many cells away from the visible sprite.
- Re-test miniboss bars while panning, zooming, and using non-default UI scale before considering future overhead UI changes stable.

In-game checks:

- Spawn or summon Choir Engine and pan the camera across it; the bar should follow the pawn/sprite rather than staying near the map center or screen edge.
- Repeat with Warden of Ash to verify smaller miniboss placement still reads correctly.
- Confirm the full boss HUD remains unchanged for Archon/Reactor-class bosses.

## Implant grid expansion risks — 2026-05-21

- Positive `ShootingAccuracyPawn`, `MeleeHitChance`, and `MeleeDodgeChance` values below `0.10` render as `0.0` in stat cards and look broken. Keep visible positive entries at `0.10` or higher.
- Craftable implant ThingDefs need matching surgery RecipeDefs and installed HediffDefs; missing one of the three silently creates reward items that cannot be installed or hediffs that cannot be recovered.
- Added body part hediffs must include `spawnThingOnRemoved` so extracted/replaced implants return the correct item.
- New implant ThingDefs must use real `Textures/Things/Implant/*.png` assets, not missing texPaths or duplicated placeholder art.

## Crafting economy risk — boss drops must not tax every slot

When adding new gear, every craftable reward should normally require Abyssal Residue plus vanilla resources. Boss-drop resources should gate signature or upper-tier pieces, not every implant slot or passive module. Avoid making complete pawn loadouts require one boss drop per organ; this discourages experimentation and makes multi-pawn gearing feel punitive.
