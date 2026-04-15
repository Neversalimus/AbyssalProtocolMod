using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    public class AbyssalLegacySigilMigrationGameComponent : GameComponent
    {
        private bool migrated;

        public AbyssalLegacySigilMigrationGameComponent(Game game)
        {
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            TryMigrate();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref migrated, "abyLegacySigilsMigrated", false);
        }

        private void TryMigrate()
        {
            if (migrated)
            {
                return;
            }

            ThingDef fromDef = DefDatabase<ThingDef>.GetNamedSilentFail("ABY_HexgunRelaySigil");
            ThingDef toDef = DefDatabase<ThingDef>.GetNamedSilentFail("ABY_EmberHoundSigil");
            if (fromDef == null || toDef == null)
            {
                migrated = true;
                return;
            }

            int converted = 0;
            List<Map> maps = Find.Maps;
            for (int mapIndex = 0; mapIndex < maps.Count; mapIndex++)
            {
                Map map = maps[mapIndex];
                if (map == null)
                {
                    continue;
                }

                List<Thing> spawned = map.listerThings.ThingsOfDef(fromDef);
                for (int i = spawned.Count - 1; i >= 0; i--)
                {
                    Thing legacy = spawned[i];
                    if (legacy == null || legacy.Destroyed)
                    {
                        continue;
                    }

                    int stackCount = Math.Max(1, legacy.stackCount);
                    IntVec3 pos = legacy.Position;
                    legacy.Destroy(DestroyMode.Vanish);
                    SpawnReplacementStacks(map, pos, toDef, stackCount);
                    converted += stackCount;
                }

                ThingDef vaultDef = DefDatabase<ThingDef>.GetNamedSilentFail("ABY_SigilVault");
                if (vaultDef != null)
                {
                    List<Thing> vaultThings = map.listerThings.ThingsOfDef(vaultDef);
                    for (int i = 0; i < vaultThings.Count; i++)
                    {
                        if (vaultThings[i] is Building_ABY_SigilVault vault)
                        {
                            converted += vault.ConvertStoredSigils(fromDef, toDef);
                        }
                    }
                }
            }

            migrated = true;
            if (converted > 0)
            {
                Messages.Message(
                    AbyssalSummoningConsoleUtility.TranslateOrFallback(
                        "ABY_LegacySigilMigration_Message",
                        "Converted {0} retired hexgun relay sigils into ember hound sigils.",
                        converted),
                    MessageTypeDefOf.PositiveEvent,
                    false);
            }
        }

        private static void SpawnReplacementStacks(Map map, IntVec3 cell, ThingDef def, int count)
        {
            int remaining = Math.Max(0, count);
            while (remaining > 0)
            {
                Thing replacement = ThingMaker.MakeThing(def);
                replacement.stackCount = Math.Min(def.stackLimit, remaining);
                GenPlace.TryPlaceThing(replacement, cell, map, ThingPlaceMode.Near);
                remaining -= replacement.stackCount;
            }
        }
    }
}
