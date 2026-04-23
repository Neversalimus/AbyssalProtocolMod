using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public static class CrownspikeRailVfxUtility
    {
        private const string RailOuterMoteDefName = "ABY_Mote_CrownspikeRailOuter";
        private const string RailCoreMoteDefName = "ABY_Mote_CrownspikeRailCore";
        private const string MuzzleMoteDefName = "ABY_Mote_CrownspikeRailMuzzle";
        private const string ImpactMoteDefName = "ABY_Mote_CrownspikeRailImpact";
        private const string ShieldMoteDefName = "ABY_Mote_CrownspikeRailShieldImpact";
        private const string DenseImpactMoteDefName = "ABY_Mote_CrownspikeRailDenseImpact";

        private static ThingDef railOuterMoteDef;
        private static ThingDef railCoreMoteDef;
        private static ThingDef muzzleMoteDef;
        private static ThingDef impactMoteDef;
        private static ThingDef shieldMoteDef;
        private static ThingDef denseImpactMoteDef;

        private static ThingDef RailOuterMoteDef => railOuterMoteDef ?? (railOuterMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(RailOuterMoteDefName));
        private static ThingDef RailCoreMoteDef => railCoreMoteDef ?? (railCoreMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(RailCoreMoteDefName));
        private static ThingDef MuzzleMoteDef => muzzleMoteDef ?? (muzzleMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(MuzzleMoteDefName));
        private static ThingDef ImpactMoteDef => impactMoteDef ?? (impactMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(ImpactMoteDefName));
        private static ThingDef ShieldMoteDef => shieldMoteDef ?? (shieldMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(ShieldMoteDefName));
        private static ThingDef DenseImpactMoteDef => denseImpactMoteDef ?? (denseImpactMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(DenseImpactMoteDefName));

        public static void SpawnRailDischarge(Vector3 source, Vector3 target, Map map, Thing hitThing, bool blockedByShield)
        {
            if (map == null)
            {
                return;
            }

            source.y = AltitudeLayer.MoteOverhead.AltitudeFor();
            target.y = AltitudeLayer.MoteOverhead.AltitudeFor();

            SpawnMuzzleFlash(source, target, map);
            SpawnRailLine(source, target, map);
            SpawnImpact(target, map, hitThing, blockedByShield);
        }

        private static void SpawnMuzzleFlash(Vector3 source, Vector3 target, Map map)
        {
            ThingDef muzzleDef = MuzzleMoteDef;
            if (muzzleDef != null)
            {
                MakeStaticMote(source, map, muzzleDef, 1.38f);
                MakeStaticMote(Vector3.Lerp(source, target, 0.035f), map, muzzleDef, 0.74f);
            }

            FleckMaker.ThrowLightningGlow(source, map, 1.85f);
            FleckMaker.ThrowMicroSparks(source, map);
            if (Rand.Chance(0.75f))
            {
                FleckMaker.ThrowMicroSparks(source, map);
            }
        }

        private static void SpawnRailLine(Vector3 source, Vector3 target, Map map)
        {
            Vector3 delta = target - source;
            float distance = delta.MagnitudeHorizontal();
            if (distance < 0.18f)
            {
                return;
            }

            int steps = Mathf.Clamp(Mathf.CeilToInt(distance * 3.15f), 5, 42);
            ThingDef outerDef = RailOuterMoteDef;
            ThingDef coreDef = RailCoreMoteDef;

            for (int i = 1; i <= steps; i++)
            {
                float t = i / (float)(steps + 1);
                Vector3 point = Vector3.Lerp(source, target, t);
                point.y = AltitudeLayer.MoteOverhead.AltitudeFor();

                float pulse = 1f + Mathf.Sin(t * Mathf.PI) * 0.28f;
                if (outerDef != null)
                {
                    MakeStaticMote(point, map, outerDef, Mathf.Lerp(0.46f, 0.78f, pulse - 1f));
                }

                if (coreDef != null)
                {
                    MakeStaticMote(point, map, coreDef, Mathf.Lerp(0.22f, 0.34f, pulse - 1f));
                }

                if (i % 4 == 0)
                {
                    FleckMaker.ThrowLightningGlow(point, map, 0.38f * pulse);
                }
            }
        }

        private static void SpawnImpact(Vector3 target, Map map, Thing hitThing, bool blockedByShield)
        {
            bool denseTarget = IsDenseTarget(hitThing);
            ThingDef impactDef = blockedByShield ? ShieldMoteDef : denseTarget ? DenseImpactMoteDef : ImpactMoteDef;
            float baseScale = blockedByShield ? 1.28f : denseTarget ? 1.48f : 1.22f;

            if (impactDef != null)
            {
                MakeStaticMote(target, map, impactDef, baseScale);
                MakeStaticMote(target + new Vector3(Rand.Range(-0.10f, 0.10f), 0f, Rand.Range(-0.10f, 0.10f)), map, impactDef, baseScale * 0.62f);
            }

            FleckMaker.ThrowLightningGlow(target, map, blockedByShield ? 2.00f : denseTarget ? 2.35f : 1.78f);
            FleckMaker.ThrowMicroSparks(target, map);
            FleckMaker.ThrowMicroSparks(target, map);
            if (blockedByShield || denseTarget)
            {
                FleckMaker.ThrowMicroSparks(target, map);
                FleckMaker.ThrowMicroSparks(target, map);
            }
        }

        private static bool IsDenseTarget(Thing hitThing)
        {
            if (hitThing == null || hitThing.Destroyed)
            {
                return false;
            }

            Pawn pawn = hitThing as Pawn;
            if (pawn != null)
            {
                return pawn.RaceProps != null && pawn.RaceProps.IsMechanoid;
            }

            return hitThing.def != null && hitThing.def.category == ThingCategory.Building;
        }

        private static void MakeStaticMote(Vector3 position, Map map, ThingDef moteDef, float scale)
        {
            if (map == null || moteDef == null)
            {
                return;
            }

            MoteMaker.MakeStaticMote(position, map, moteDef, scale);
        }
    }
}
