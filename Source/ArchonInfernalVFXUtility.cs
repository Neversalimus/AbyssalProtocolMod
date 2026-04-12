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

            TrySpawnMote(map, pos, "ABY_Mote_ArchonDashExit", 3.4f);
            TrySpawnMote(map, pos + new Vector3(0f, 0f, 0.12f), "ABY_Mote_ArchonDashEntry", 2.6f);

            SpawnTrailRing(map, center, 2.1f, 1.25f, 8);
            FilthMaker.TryMakeFilth(center, map, ThingDefOf.Filth_Ash, 8);
        }

        public static void DoDeathVFX(Map map, IntVec3 center)
        {
            if (map == null || !center.IsValid)
                return;

            Vector3 pos = center.ToVector3Shifted();

            TrySpawnMote(map, pos, "ABY_Mote_ArchonDashExit", 4.2f);
            TrySpawnMote(map, pos + new Vector3(0f, 0f, 0.15f), "ABY_Mote_ArchonDashEntry", 3.0f);

            SpawnTrailRing(map, center, 2.8f, 1.55f, 12);
            FilthMaker.TryMakeFilth(center, map, ThingDefOf.Filth_Ash, 12);
        }

        private static void SpawnTrailRing(Map map, IntVec3 center, float radius, float scale, int count)
        {
            if (map == null || !center.IsValid || count <= 0)
                return;

            Vector3 basePos = center.ToVector3Shifted();

            for (int i = 0; i < count; i++)
            {
                float angle = (360f / count) * i;
                Vector3 offset = Quaternion.AngleAxis(angle, Vector3.up) * new Vector3(radius, 0f, 0f);
                Vector3 pos = basePos + new Vector3(offset.x, 0f, offset.z);
                TrySpawnMote(map, pos, "ABY_Mote_ArchonDashTrail", scale);
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
