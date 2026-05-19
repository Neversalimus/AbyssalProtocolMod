#!/usr/bin/env python3
"""Build a clean Abyssal Protocol release zip.

This copies only playable mod payload folders and excludes dev/source asset folders.
It does not compile C#; run dotnet build separately when code changed.
"""
from __future__ import annotations

import argparse
import fnmatch
import json
import zipfile
from pathlib import Path
from typing import Any, Dict, Iterable


def load_rules(path: Path) -> Dict[str, Any]:
    with path.open("r", encoding="utf-8") as handle:
        return json.load(handle)


def rel(path: Path, root: Path) -> str:
    return path.relative_to(root).as_posix()


def match_any(path: str, patterns: Iterable[str]) -> bool:
    return any(fnmatch.fnmatchcase(path, pattern) for pattern in patterns)


def should_include(path: Path, root: Path, rules: Dict[str, Any]) -> bool:
    relative = rel(path, root)
    if path.is_dir():
        relative += "/"
    excludes = rules.get("release_excludes", [])
    includes = rules.get("release_includes", [])
    if match_any(relative, excludes):
        return False
    return match_any(relative, includes)


def iter_release_files(root: Path, rules: Dict[str, Any]) -> Iterable[Path]:
    for path in sorted(root.rglob("*")):
        if not path.is_file():
            continue
        relative = rel(path, root)
        parts = relative.split("/")
        if any(match_any("/".join(parts[:i]) + "/", rules.get("release_excludes", [])) for i in range(1, len(parts) + 1)):
            continue
        if should_include(path, root, rules):
            yield path


def main() -> None:
    parser = argparse.ArgumentParser(description="Build clean Abyssal Protocol release package.")
    parser.add_argument("--root", default=".", help="Repository root. Defaults to current directory.")
    parser.add_argument("--rules", default="Tools/texture_budget_rules.json", help="Rules JSON path.")
    parser.add_argument("--out", default="BuildOutput/AbyssalProtocolMod-release.zip", help="Release zip path.")
    parser.add_argument("--name", default="AbyssalProtocolMod", help="Top-level folder name inside the release zip.")
    args = parser.parse_args()

    root = Path(args.root).resolve()
    rules = load_rules((root / args.rules).resolve() if not Path(args.rules).is_absolute() else Path(args.rules))
    out = (root / args.out).resolve() if not Path(args.out).is_absolute() else Path(args.out)
    out.parent.mkdir(parents=True, exist_ok=True)

    files = list(iter_release_files(root, rules))
    with zipfile.ZipFile(out, "w", zipfile.ZIP_DEFLATED, compresslevel=9) as archive:
        for file in files:
            archive.write(file, arcname=f"{args.name}/{rel(file, root)}")

    report = out.with_suffix(".report.md")
    total = sum(file.stat().st_size for file in files)
    report.write_text(
        "# Abyssal Protocol Release Package Report\n\n"
        f"Output: `{out.name}`\n\n"
        f"Files included: **{len(files)}**\n\n"
        f"Uncompressed payload: **{total / 1024 / 1024:.2f} MiB**\n\n"
        "Excluded by design: `SourceAssets/`, `Tools/`, `BuildOutput/`, VCS files, temp/source art formats, and Python caches.\n",
        encoding="utf-8",
    )
    print(f"Release zip: {out}")
    print(f"Included files: {len(files)}")
    print(f"Report: {report}")


if __name__ == "__main__":
    main()
