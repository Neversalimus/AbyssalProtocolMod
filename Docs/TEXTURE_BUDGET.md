# Abyssal Protocol — Texture Budget System

This document explains the repository-side texture budget tools added for Abyssal Protocol release maintenance.

## Purpose

The mod has a large custom texture payload: UI skins, VFX frames, pawns, Dominion setpieces, buildings, projectiles, and item art. Manual downscale passes helped, but future additions can easily grow the payload again. The texture budget system keeps future assets within predictable limits before Workshop packaging.

## Files

```text
Tools/texture_budget_rules.json
Tools/ABY_TextureAudit.py
Tools/ABY_OptimizeTextures.py
```

## Requirements

Python 3 with Pillow:

```bash
python -m pip install pillow
```

## Audit only

From the repository root:

```bash
python Tools/ABY_TextureAudit.py
```

This writes:

```text
BuildOutput/texture_audit_report.md
```

The report lists:

- total PNG disk payload;
- estimated RGBA32 VRAM without mipmaps;
- estimated RGBA32 VRAM with mipmaps;
- largest textures;
- warning/manual-review candidates.

## Dry-run optimization

```bash
python Tools/ABY_OptimizeTextures.py
```

This does not edit files. It writes:

```text
BuildOutput/texture_optimize_report.md
```

## Apply safe optimization

```bash
python Tools/ABY_OptimizeTextures.py --apply
```

Only rules with `auto_resize: true` and a `safe_max_side` are changed. Manual-review/high-risk groups, such as boss sprites and Dominion fissure sheets, are reported but not changed automatically.

## Rule file

The budget rules are in:

```text
Tools/texture_budget_rules.json
```

Use it to tune paths such as:

- `Textures/UI/**`
- `Textures/Things/VFX/**`
- `Textures/Pawn/**`
- `Textures/Things/Projectile/**`
- `Textures/Things/Building/**/*Overlay*.png`

Boss/hero sprite groups are intentionally configured as manual-review only.

## Recommended workflow before release

```bash
python Tools/ABY_TextureAudit.py
python Tools/ABY_OptimizeTextures.py
python Tools/ABY_OptimizeTextures.py --apply
python Tools/ABY_TextureAudit.py --out BuildOutput/texture_audit_after.md
```

Review the reports before committing changed PNGs.
