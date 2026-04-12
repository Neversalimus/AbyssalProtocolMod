using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public static class ArchonInfernalVFXUtility
    {
        public static void DoSummonVFX(Map map, IntVec3 center)
        {
            if (map == null || !center.IsValid)
                return;

            Vector3 pos = center.ToVector3Shifted();

            TrySpawnMote(map, pos, "ABY_Mote_ArchonSummonRing", 1.00f);
            TrySpawnMote(map, pos + new Vector3(0f, 0f, 0.12f), "ABY_Mote_ArchonSummonBurst", 1.00f);

            FilthMaker.TryMakeFilth(center, map, ThingDefOf.Filth_Ash, 8);
            SpawnTrailRing(map, center, 2.2f, 1.05f, 8);
        }

        public static void DoDeathVFX(Map map, IntVec3 center)
        {
            if (map == null || !center.IsValid)
                return;

            Vector3 pos = center.ToVector3Shifted();

            TrySpawnMote(map, pos, "ABY_Mote_ArchonDeathRing", 1.00f);
            TrySpawnMote(map, pos + new Vector3(0f, 0f, 0.15f), "ABY_Mote_ArchonDeathBurst", 1.00f);

            FilthMaker.TryMakeFilth(center, map, ThingDefOf.Filth_Ash, 12);
            SpawnTrailRing(map, center, 2.8f, 1.35f, 12);
        }

        private static void SpawnTrailRing(Map map, IntVec3 center, float radius, float scale, int count)
        {
            if (map == null || !center.IsValid || count <= 0)
                return;

            ThingDef dashTrailDef = DefDatabase<ThingDef>.GetNamedSilentFail("ABY_Mote_ArchonDashTrail");
            if (dashTrailDef == null)
                return;

            Vector3 basePos = center.ToVector3Shifted();

            for (int i = 0; i < count; i++)
            {
                float angle = (360f / count) * i;
                Vector3 offset = Quaternion.AngleAxis(angle, Vector3.up) * new Vector3(radius, 0f, 0f);
                Vector3 pos = basePos + new Vector3(offset.x, 0f, offset.z);
                MoteMaker.MakeStaticMote(pos, map, dashTrailDef, scale);
            }
        }

        private static void TrySpawnMote(Map map, Vector3 pos, string defName, float scale)
        {
            if (map == null || string.IsNullOrEmpty(defName))
                return;

            ThingDef moteDef = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            if (moteDef == null)
                return;

            MoteMaker.MakeStaticMote(pos, map, moteDef, scale);
        }
    }
}
