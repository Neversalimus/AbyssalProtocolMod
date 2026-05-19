# Abyssal Protocol — Release Packager

This document explains the repository-side clean release packager.

## Purpose

The development repository can contain source assets, generation sheets, tools, and audit reports. The Workshop/release package should contain only playable mod payload.

## File

```text
Tools/ABY_BuildReleasePackage.py
```

## Build a release zip

From the repository root:

```bash
python Tools/ABY_BuildReleasePackage.py
```

Default output:

```text
BuildOutput/AbyssalProtocolMod-release.zip
BuildOutput/AbyssalProtocolMod-release.report.md
```

## What is included

The packager includes:

```text
About/
Assemblies/
Defs/
Languages/
Patches/
Sounds/
Textures/
Docs/
```

## What is excluded

The packager excludes development-only folders and files such as:

```text
SourceAssets/
Tools/
BuildOutput/
.git/
*.psd
*.kra
*.xcf
*.blend
*.tmp
*.bak
```

The exact include/exclude rules are stored in:

```text
Tools/texture_budget_rules.json
```

## Important build note

The packager does not compile C#. If code changed, build first and only package after `Assemblies/AbyssalProtocol.dll` is verified.

Typical sequence after C# work:

```bash
dotnet build source/AbyssalProtocol.csproj -c Release
python Tools/ABY_BuildReleasePackage.py
```

For asset/XML-only work, no rebuild is required.
