#!/usr/bin/env python3
"""Safe Abyssal Protocol texture optimizer.

Default mode is dry-run. Use --apply to write PNGs.
This script only auto-resizes rules with auto_resize=true and safe_max_side set.
Manual-review/high-risk categories are reported but never changed automatically.
"""
from __future__ import annotations

import argparse
import fnmatch
import json
from pathlib import Path
from typing import Any, Dict, Optional

try:
    from PIL import Image
except ImportError as exc:  # pragma: no cover
    raise SystemExit("Pillow is required: python -m pip install pillow") from exc


def relpath(path: Path, root: Path) -> str:
    return path.relative_to(root).as_posix()


def matches(pattern: str, value: str) -> bool:
    return fnmatch.fnmatchcase(value, pattern)


def load_rules(path: Path) -> Dict[str, Any]:
    with path.open("r", encoding="utf-8") as handle:
        return json.load(handle)


def find_rule(rules: Dict[str, Any], rel: str) -> Optional[Dict[str, Any]]:
    for rule in rules.get("rules", []):
        excludes = rule.get("exclude_contains") or []
        if any(token in rel for token in excludes):
            continue
        if matches(rule.get("pattern", ""), rel):
            return rule
    return None


def resize_to_max(path: Path, max_side: int) -> bool:
    with Image.open(path) as image:
        image = image.convert("RGBA")
        width, height = image.size
        current_max = max(width, height)
        if current_max <= max_side:
            image.save(path, "PNG", optimize=True, compress_level=9)
            return True
        scale = max_side / float(current_max)
        target = (max(1, round(width * scale)), max(1, round(height * scale)))
        resized = image.resize(target, Image.Resampling.LANCZOS)
        resized.save(path, "PNG", optimize=True, compress_level=9)
        return True


def main() -> None:
    parser = argparse.ArgumentParser(description="Optimize safe Abyssal Protocol textures.")
    parser.add_argument("--root", default=".", help="Repository root. Defaults to current directory.")
    parser.add_argument("--rules", default="Tools/texture_budget_rules.json", help="Rules JSON path.")
    parser.add_argument("--apply", action="store_true", help="Actually write optimized PNGs. Without this, only prints planned changes.")
    parser.add_argument("--out", default="BuildOutput/texture_optimize_report.md", help="Markdown report path.")
    args = parser.parse_args()

    root = Path(args.root).resolve()
    rules = load_rules((root / args.rules).resolve() if not Path(args.rules).is_absolute() else Path(args.rules))
    report_lines = ["# Abyssal Protocol Texture Optimization Report", ""]
    planned = 0
    changed = 0
    saved = 0

    for png in sorted((root / "Textures").rglob("*.png")):
        rel = relpath(png, root)
        rule = find_rule(rules, rel)
        if not rule or not rule.get("auto_resize") or rule.get("manual_review"):
            continue
        safe_max = rule.get("safe_max_side")
        if not safe_max:
            continue
        safe_max = int(safe_max)
        with Image.open(png) as image:
            width, height = image.size
        if max(width, height) <= safe_max:
            # Still allow lossless re-save only in apply mode, but do not count as planned resize.
            if args.apply:
                before = png.stat().st_size
                resize_to_max(png, safe_max)
                after = png.stat().st_size
                if after < before:
                    changed += 1
                    saved += before - after
                    report_lines.append(f"- lossless `{rel}`: {before} -> {after} bytes")
            continue

        planned += 1
        before = png.stat().st_size
        scale = safe_max / float(max(width, height))
        target = (max(1, round(width * scale)), max(1, round(height * scale)))
        if args.apply:
            resize_to_max(png, safe_max)
            after = png.stat().st_size
            changed += 1
            saved += before - after
            report_lines.append(f"- resized `{rel}`: {width}x{height} -> {target[0]}x{target[1]}, {before} -> {after} bytes")
        else:
            report_lines.append(f"- would resize `{rel}`: {width}x{height} -> {target[0]}x{target[1]} ({rule.get('name')})")

    report_lines.insert(2, f"Mode: {'APPLY' if args.apply else 'DRY RUN'}")
    report_lines.insert(3, f"Planned resize candidates: {planned}")
    report_lines.insert(4, f"Changed files: {changed}")
    report_lines.insert(5, f"Saved bytes: {saved}")
    report_lines.insert(6, "")

    out = (root / args.out).resolve() if not Path(args.out).is_absolute() else Path(args.out)
    out.parent.mkdir(parents=True, exist_ok=True)
    out.write_text("\n".join(report_lines) + "\n", encoding="utf-8")
    print(f"Mode: {'APPLY' if args.apply else 'DRY RUN'}")
    print(f"Planned resize candidates: {planned}")
    print(f"Changed files: {changed}")
    print(f"Saved bytes: {saved}")
    print(f"Report: {out}")


if __name__ == "__main__":
    main()
