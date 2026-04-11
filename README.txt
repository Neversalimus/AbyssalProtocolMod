# Abyssal Protocol

Abyssal Protocol is a RimWorld mod prototype focused on infernal ultra-tech progression through controlled summoning.

## Current features

- custom summoning circle workbench
- craftable Infernal Residue
- craftable Archon Sigil
- C# use-effect that summons a hostile boss on the current map
- custom monster boss race: Archon Beast
- directional boss sprites for north / south / east / west
- custom boss race definition with oversized draw size
- custom boss buff hediffs:
  - ABY_ArchonCore
  - ABY_ArchonCarapace
- starter weapon content
- starter implant content
- English and Russian localization
- placeholder / prototype art pipeline for rapid iteration

## Current boss pipeline

1. Research the starting abyssal technologies.
2. Build the Summoning Circle.
3. Craft the Archon Sigil.
4. Use the sigil on the map.
5. The sigil triggers a custom C# use-effect.
6. A hostile summoned boss is generated and spawned at the map edge.
7. The sigil is consumed on use.

## Current boss design

The first boss is the **Archon Beast**:
- non-humanlike monster boss
- quadruped infernal war-beast
- oversized visual presence through drawSize
- natural melee attacks instead of human equipment-based combat
- enhanced by custom abyssal buff hediffs

## Prototype status

This project is still an early prototype.
The current focus is:
- summoning loop
- boss identity
- content structure
- art pipeline
- transitioning from placeholder systems to dedicated custom content

## Planned next steps

- add unique boss drops
- add additional summon sigils
- add more infernal monsters and elites
- add boss-specific sound and visual effects
- add dedicated boss death rewards
- add more abyssal weapons, armor, and implants
- expand progression beyond the first boss
