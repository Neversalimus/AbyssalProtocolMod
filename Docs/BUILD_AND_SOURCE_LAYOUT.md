# Abyssal Protocol — Build and Source Layout

This document defines the current C# source layout and build expectations for the Abyssal Protocol RimWorld mod.
It exists to prevent accidental reintroduction of root-level source files and to make future AI-assisted patches safer.

## Current project path

The C# project is located at:

```text
source/AbyssalProtocol.csproj
```

The source folder is lowercase:

```text
source/
```

Do not create or use a separate uppercase `Source/` directory.
On Windows this may look harmless, but on GitHub/Linux/case-sensitive tooling it can create duplicated or confusing paths.

## Source root rule

The source root must contain the project file and module folders only.

Allowed:

```text
source/AbyssalProtocol.csproj
source/Apparel/
source/Audio/
source/Bosses/
source/Combat/
source/Compatibility/
source/Comps/
source/Core/
source/Defs/
source/Diagnostics/
source/Dominion/
source/Encounters/
source/Experimental/
source/Forge/
source/Hediffs/
source/Legacy/
source/Patches/
source/Pawns/
source/Progression/
source/Summoning/
source/UI/
source/World/
```

Not allowed:

```text
source/SomeNewClass.cs
source/ABY_NewUtility.cs
Source/
```

All `.cs` files must be placed in a module folder under `source/`.

## Current verified source state

At the time this document was added:

```text
Root-level .cs files in source/: 0
Real .cs files under source/ excluding bin/obj: 407
```

The project builds successfully with source files in subfolders because it is an SDK-style C# project and uses the default recursive compile include behavior.
Do not replace this with a non-recursive `source/*.cs` compile pattern.

## Project configuration summary

The project targets RimWorld-compatible .NET Framework:

```xml
<TargetFramework>net472</TargetFramework>
<OutputType>Library</OutputType>
<AssemblyName>AbyssalProtocol</AssemblyName>
<RootNamespace>AbyssalProtocol</RootNamespace>
<LangVersion>latest</LangVersion>
<OutputPath>..\Assemblies\</OutputPath>
```

The compiled assembly output is expected at:

```text
Assemblies/AbyssalProtocol.dll
```

## Build dependencies

The project expects RimWorld/Unity/Harmony DLLs in a local development-only folder:

```text
Libraries/
```

Typical references include:

```text
Libraries/Assembly-CSharp.dll
Libraries/UnityEngine.CoreModule.dll
Libraries/UnityEngine.dll
Libraries/UnityEngine.IMGUIModule.dll
Libraries/UnityEngine.TextRenderingModule.dll
Libraries/0Harmony.dll
Libraries/netstandard.dll
```

`Libraries/` is for local build support and should not be distributed as part of the normal mod patch unless the user explicitly requests development libraries.

## Normal build command

From the repository root:

```bash
dotnet build source/AbyssalProtocol.csproj -c Release
```

Expected result for a clean build:

```text
Build succeeded
Assemblies/AbyssalProtocol.dll updated
```


## External mod compatibility data

Some compatibility data is not C# source and should remain at repository root when the consuming mod expects it there.

```text
WeaponTweakData/
```

`WeaponTweakData/` contains JSON entries consumed by Melee Animation for per-weapon offsets, rotation, hand mode, blade positions, and melee animation type. Do not move these JSON files under `source/`, and do not compile them into `Assemblies/AbyssalProtocol.dll`. Add or update one JSON file when adding a new Abyssal melee weapon that should animate correctly with Melee Animation.

## What not to package

Do not include generated build folders in user-facing delta zips:

```text
source/bin/
source/obj/
```

Do not include dev-only libraries unless explicitly requested:

```text
Libraries/
```

For C# patches, include:

```text
source/<module>/<changed files>.cs
Assemblies/AbyssalProtocol.dll    only if build was actually verified
```

For XML/assets/audio-only patches, do not rebuild `Assemblies/AbyssalProtocol.dll` unless C# changed.

## Adding new source files

Use this placement rule:

```text
UI behavior               -> source/UI/<area>/
Forge runtime             -> source/Forge/
Summoning runtime         -> source/Summoning/
Boss-specific logic       -> source/Bosses/<BossName>/
Shared boss logic         -> source/Bosses/Shared/
Dominion runtime          -> source/Dominion/
Projectile/verb/damage    -> source/Combat/
Pawn AI/comps/death       -> source/Pawns/
Apparel/armor behavior    -> source/Apparel/
Hediff/implant behavior   -> source/Hediffs/
XML Def C# bridge         -> source/Defs/
Harmony patches           -> source/Patches/
Diagnostics/dev tools     -> source/Diagnostics/
Migration/old-save fixes  -> source/Legacy/
```

If a file does not clearly belong anywhere, do not put it in the root. Create or choose the narrowest reasonable module folder.

## Namespace rule

Do not rename namespaces just because a file moved folders.
C# namespaces do not have to match physical folder paths.

Moving files between folders is safe when:

- class names are unchanged;
- namespaces are unchanged;
- XML class references still resolve;
- build succeeds;
- RimWorld runtime smoke test passes.

## Structural refactor checklist

After moving source files or changing module layout:

```text
1. Confirm source root has 0 `.cs` files.
2. Confirm no duplicate `Source/` and `source/` directory split exists.
3. Confirm `source/bin/` and `source/obj/` are not packaged.
4. Run Release build.
5. Launch RimWorld and check for class load errors.
6. Open Forge, Summoning, Protocol Nexus, and boss bar if those systems are touched.
```

## Release tooling layout

Repository maintenance tools are allowed under:

```text
Tools/
```

Current tool files:

```text
Tools/texture_budget_rules.json
Tools/ABY_TextureAudit.py
Tools/ABY_OptimizeTextures.py
Tools/ABY_BuildReleasePackage.py
```

These tools are development/release helpers and are excluded from clean release packages by default. They do not belong under `source/` and should not be compiled into `Assemblies/AbyssalProtocol.dll`.

Clean release package output goes under:

```text
BuildOutput/
```

`BuildOutput/` must not be committed as playable mod content unless explicitly requested for a release artifact.
## Emergency Roslyn build reference rule

When the normal `dotnet build source/AbyssalProtocol.csproj -c Release` path cannot restore/use the .NET Framework reference pack in the sandbox, a direct Roslyn compile is acceptable only if it targets the bundled RimWorld/Unity/Harmony libraries and .NET Framework-style base assemblies.

Required reference shape for the produced DLL:

```text
mscorlib, Version=4.0.0.0
System.Core, Version=4.0.0.0
Assembly-CSharp
UnityEngine.CoreModule
0Harmony
```

Do not compile the mod DLL against `.NET 9` / `Microsoft.NETCore.App.Ref` reference assemblies. A DLL referencing `System.Runtime, Version=9.0.0.0` can load-fail in RimWorld/Mono with `ReflectionTypeLoadException`, which then cascades into many XML `Could not find type named AbyssalProtocol...` red errors.

