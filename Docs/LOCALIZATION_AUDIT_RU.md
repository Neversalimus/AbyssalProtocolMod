# Abyssal Protocol — Russian Localization Audit Report

## Scope

This pass audited the Russian localization files under `Languages/Russian/` against the current local archive and checked the matching English orphan state under `Languages/English/`.

Live GitHub and latest commits were checked before the local archive was patched. The working base for this patch is the user-provided `AbyssalProtocolMod-main.zip` archive.

## Initial findings

```text
Russian language keys: 2074
Russian duplicate flat keys: 5
Russian orphan DefInjected keys: 1
English orphan DefInjected keys: 1
Missing Russian DefInjected fields checked by audit: 622
Russian Latin-only visible values found: 58
Russian visible label/title/jobString values containing Latin text: 67
XML parse errors before patch: 0
```

The duplicate keys came from same-name `PawnKindDef` and `ThingDef` label translations. RimWorld language data is effectively flat by key, so these produced duplicate translation entries.

The orphan key was `Make_ABY_CrownshardStormcaster.description` in both English and Russian RecipeDef language files. The matching RecipeDef no longer exists in the audited archive.

## Fixes applied

```text
- Removed orphan CrownshardStormcaster recipe translation entries in English and Russian.
- Removed duplicate Russian PawnKind label language entries where the same flat key already exists through ThingDef localization.
- Added Russian DefInjected coverage for missing AbilityDef, FactionDef, HediffDef, IncidentDef, PawnKindDef, RecipeDef, ResearchProjectDef, TerrainDef, and ThingDef fields.
- Translated visible Russian labels, headers, buttons, boss presentation titles, difficulty labels, horde labels, recipe labels, job strings, and common enemy/item names that were still English.
- Added this report and updated regression/recent-work notes so future localization passes repeat the validation checks.
```

For many previously missing DefInjected descriptions, the patch uses concise Russian fallback descriptions in the Abyssal Protocol tone. These are intentionally functional and load-safe first-pass localizations; they can be hand-polished later without reintroducing English fallback UI.

## Final validation

```text
XML parse errors: 0
Russian duplicate flat keys: 0
Russian orphan DefInjected keys: 0
English duplicate flat keys: 0
English orphan DefInjected keys: 0
Missing audited Russian DefInjected fields: 0
Russian Latin-only visible values: 0
Russian visible label/title/jobString values containing Latin text: 0
Russian remaining Latin text values after cleanup: 0
```

## Build/runtime status

No C# files were changed. DLL rebuild was not required and was not performed.

RimWorld runtime language report generation was not run in this environment, so the in-game Russian language loader warning is not directly smoke-tested here. The static XML/language-data validation pass is clean.

## 2026-05-19 Forge UI follow-up

A follow-up pass was applied after an in-game Forge console screenshot exposed remaining Russian UI problems. This pass is narrower than the full language-data cleanup above and focuses on Forge browser usability and specific incorrect Russian item names.

Applied fixes:

```text
- Localized Forge search row, Clear button, subcategory buttons, status chips, selected-pattern header, empty selected-pattern state, and Forge pattern unavailable/research text.
- Added Russian plural-aware requirement count formatting in Forge UI: 1 требование, 2-4 требования, 5+/11-14 требований.
- Corrected reported bad Russian names: Рифт клинок, Рифт карабин, ультра плазменная винтовка, Хор Забвения, Панцирь святого носителя Эгиды, Сигила угольных гончих.
- Replaced the cramped `пеплосвязанный модуль-конденсатор` label with a shorter `пепельный конденсатор` form for Forge card readability.
- Corrected `Уровень настройка` to `Уровень настройки`.
```

Build status for this follow-up: C# was changed in `source/UI/Forge/Window_AbyssalForgeConsole.cs`; manual Roslyn compilation against the local `Libraries/` references succeeded and regenerated `Assemblies/AbyssalProtocol.dll`. RimWorld runtime language report was not run in-game.

## 2026-05-19 glossary follow-up

Added `Docs/LOCALIZATION_GLOSSARY_RU.md` to prevent future Russian localization drift after the Forge UI screenshot review exposed correct-but-awkward machine-style translations.

The glossary is now the first document to check for Russian terminology before editing item names, weapon names, boss names, sigil names, Forge/Summoning UI labels, requirement counters, and research/protocol text.

Specific correction captured: `Oblivion Choir` is a weapon/proper name and should be localized as `Хор Забвения`, not treated as an enemy/unit and not translated as `Забвение хоровой`.


## 2026-05-19 glossary-driven editorial pass

A broader Russian editorial localization pass was applied after the glossary was added. This pass focused on player-facing natural Russian, compact Forge-readable labels, and removal of machine-translated/transliterated wording from Russian XML.

Applied fixes:

```text
- Shortened Forge-sensitive item/module labels where long strings would crowd cards or selected-pattern panels.
- Replaced remaining awkward transliterations such as веил, слинг, харнесс, пайплайн, кэши, релаи, scanline/sweep text, and old Great Hell Gate wording.
- Standardized Dominion wording around Великие инфернальные врата and Доминион.
- Standardized choir, rift, sigil, aegis, residue, and horde terminology against Docs/LOCALIZATION_GLOSSARY_RU.md.
- Kept descriptions more loreful while making UI labels shorter and easier to read in Abyssal Forge.
```

Validation after this pass:

```text
Russian XML parse errors: 0
Russian visible Latin text values: 0
Known bad machine-translation phrases checked: 0 remaining
C# changed: no
Build required: no
RimWorld runtime language report: not run in-game
```



## 2026-05-19 — Turret module and technical-description follow-up

Scope:

```text
- Russian Forge/turret module cards and tooltips.
- English and Russian modular turret module descriptions.
- Oblivion Choir and Breach Cannon player-facing weapon descriptions.
- Selected base English descriptions that could leak technical implementation text through fallback.
```

Validation focus:

```text
- No remaining player-facing `Primary gun module` / `Primary missile module` turret descriptions.
- No remaining player-facing `projectile is animated`, `animated breach`, `external choir-cell`, `runtime`, `def`, `save/load`, or `системах мода` wording in audited text nodes.
- Russian custom `ABY_TurretModuleDef` fields now have DefInjected localization coverage.
```

