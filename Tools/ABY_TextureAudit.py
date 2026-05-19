#!/usr/bin/env python3
"""Abyssal Protocol texture budget audit.

Usage:
  python Tools/ABY_TextureAudit.py
  python Tools/ABY_TextureAudit.py --root . --rules Tools/texture_budget_rules.json --out BuildOutput/texture_audit_report.md
"""
from __future__ import annotations

import argparse
import fnmatch
import json
from pathlib import Path
from typing import Any, Dict, Iterable, List, Optional

try:
    from PIL import Image
except ImportError as exc:  # pragma: no cover
    raise SystemExit("Pillow is required: python -m pip install pillow") from exc


def relpath(path: Path, root: Path) -> str:
    return path.relative_to(root).as_posix()


def load_rules(path: Path) -> Dict[str, Any]:
    with path.open("r", encoding="utf-8") as handle:
        return json.load(handle)


def matches(pattern: str, value: str) -> bool:
    return fnmatch.fnmatchcase(value, pattern)


def find_rule(rules: Dict[str, Any], rel: str) -> Optional[Dict[str, Any]]:
    for rule in rules.get("rules", []):
        excludes = rule.get("exclude_contains") or []
        if any(token in rel for token in excludes):
            continue
        if matches(rule.get("pattern", ""), rel):
            return rule
    return None


def iter_pngs(root: Path) -> Iterable[Path]:
    textures = root / "Textures"
    if not textures.exists():
        return []
    return sorted(textures.rglob("*.png"))


def audit(root: Path, rules: Dict[str, Any]) -> Dict[str, Any]:
    rows: List[Dict[str, Any]] = []
    totals = {"png_bytes": 0, "rgba_bytes": 0, "count": 0}
    warnings: List[Dict[str, Any]] = []

    default_warn = int(rules.get("global", {}).get("default_warn_above_side") or 512)

    for png in iter_pngs(root):
        try:
            with Image.open(png) as image:
                width, height = image.size
        except Exception as exc:
            warnings.append({"path": relpath(png, root), "warning": f"failed to read PNG: {exc}"})
            continue

        relative = relpath(png, root)
        disk = png.stat().st_size
        rgba = width * height * 4
        totals["png_bytes"] += disk
        totals["rgba_bytes"] += rgba
        totals["count"] += 1

        rule = find_rule(rules, relative) or {}
        warn_above = int(rule.get("warn_above_side") or default_warn)
        safe_max = rule.get("safe_max_side")
        max_side = max(width, height)
        warning_bits: List[str] = []
        if max_side > warn_above:
            warning_bits.append(f"max side {max_side}px > warn {warn_above}px")
        if safe_max and max_side > int(safe_max):
            warning_bits.append(f"candidate over safe max {safe_max}px")
        if rule.get("manual_review"):
            warning_bits.append("manual review")

        row = {
            "path": relative,
            "width": width,
            "height": height,
            "png_kib": disk / 1024,
            "rgba_mib": rgba / 1024 / 1024,
            "rule": rule.get("name", "default"),
            "safe_max_side": safe_max,
            "auto_resize": bool(rule.get("auto_resize")),
            "warning": "; ".join(warning_bits),
        }
        rows.append(row)
        if warning_bits:
            warnings.append(row)

    rows.sort(key=lambda item: (item["rgba_mib"], item["png_kib"]), reverse=True)
    warnings.sort(key=lambda item: (item.get("rgba_mib", 0), item.get("png_kib", 0)), reverse=True)
    return {"totals": totals, "rows": rows, "warnings": warnings}


def write_report(result: Dict[str, Any], out: Path) -> None:
    out.parent.mkdir(parents=True, exist_ok=True)
    totals = result["totals"]
    with out.open("w", encoding="utf-8") as handle:
        handle.write("# Abyssal Protocol Texture Budget Audit\n\n")
        handle.write(f"Texture PNG files: **{totals['count']}**\n\n")
        handle.write(f"PNG disk payload: **{totals['png_bytes'] / 1024 / 1024:.2f} MiB**\n\n")
        handle.write(f"Estimated RGBA32 VRAM without mipmaps: **{totals['rgba_bytes'] / 1024 / 1024:.2f} MiB**\n\n")
        handle.write(f"Estimated RGBA32 VRAM with mipmaps: **{totals['rgba_bytes'] * 4 / 3 / 1024 / 1024:.2f} MiB**\n\n")
        handle.write("## Warnings / review candidates\n\n")
        handle.write("| Path | Size | PNG KiB | RGBA MiB | Rule | Warning |\n")
        handle.write("|---|---:|---:|---:|---|---|\n")
        for row in result["warnings"][:250]:
            handle.write(
                f"| `{row['path']}` | {row.get('width', '?')}x{row.get('height', '?')} | "
                f"{row.get('png_kib', 0):.1f} | {row.get('rgba_mib', 0):.2f} | "
                f"{row.get('rule', '')} | {row.get('warning', '')} |\n"
            )
        handle.write("\n## Largest textures\n\n")
        handle.write("| Path | Size | PNG KiB | RGBA MiB | Rule |\n")
        handle.write("|---|---:|---:|---:|---|\n")
        for row in result["rows"][:150]:
            handle.write(
                f"| `{row['path']}` | {row['width']}x{row['height']} | "
                f"{row['png_kib']:.1f} | {row['rgba_mib']:.2f} | {row['rule']} |\n"
            )


def main() -> None:
    parser = argparse.ArgumentParser(description="Audit Abyssal Protocol texture budget.")
    parser.add_argument("--root", default=".", help="Repository root. Defaults to current directory.")
    parser.add_argument("--rules", default="Tools/texture_budget_rules.json", help="Rules JSON path.")
    parser.add_argument("--out", default="BuildOutput/texture_audit_report.md", help="Markdown report path.")
    args = parser.parse_args()

    root = Path(args.root).resolve()
    rules = load_rules((root / args.rules).resolve() if not Path(args.rules).is_absolute() else Path(args.rules))
    result = audit(root, rules)
    write_report(result, (root / args.out).resolve() if not Path(args.out).is_absolute() else Path(args.out))

    totals = result["totals"]
    print(f"Textures: {totals['count']}")
    print(f"PNG disk payload: {totals['png_bytes'] / 1024 / 1024:.2f} MiB")
    print(f"RGBA32 VRAM estimate: {totals['rgba_bytes'] / 1024 / 1024:.2f} MiB")
    print(f"Warnings: {len(result['warnings'])}")


if __name__ == "__main__":
    main()
