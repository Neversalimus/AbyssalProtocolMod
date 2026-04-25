using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class DefModExtension_NullbrandFracture : DefModExtension
    {
        public string hediffDefName = "ABY_NullbrandFracture";
        public float chance = 0.24f;
        public int durationTicks = 360;
        public float visualScale = 0.52f;
    }

    public class DamageWorker_ABY_NullbrandFracture : DamageWorker
    {
        public override DamageResult Apply(DamageInfo dinfo, Thing victim)
        {
            DamageResult result = new DamageResult();
            Pawn pawn = victim as Pawn;
            if (pawn == null || pawn.Dead || pawn.health == null)
            {
                return result;
            }

            DefModExtension_NullbrandFracture extension = def?.GetModExtension<DefModExtension_NullbrandFracture>();
            float chance = Mathf.Clamp01(extension?.chance ?? 0.24f);
            if (chance <= 0f || !Rand.Chance(chance))
            {
                return result;
            }

            string hediffDefName = extension?.hediffDefName ?? "ABY_NullbrandFracture";
            HediffDef hediffDef = DefDatabase<HediffDef>.GetNamedSilentFail(hediffDefName);
            if (hediffDef == null)
            {
                return result;
            }

            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef);
            if (hediff == null)
            {
                hediff = HediffMaker.MakeHediff(hediffDef, pawn);
                if (hediff == null)
                {
                    return result;
                }

                pawn.health.AddHediff(hediff);
            }

            hediff.Severity = Mathf.Max(hediff.Severity, 1f);

            HediffComp_Disappears disappears = hediff.TryGetComp<HediffComp_Disappears>();
            if (disappears != null)
            {
                int durationTicks = Mathf.Max(60, extension?.durationTicks ?? 360);
                disappears.ticksToDisappear = durationTicks;
            }

            pawn.health.hediffSet.DirtyCache();

            if (pawn.Spawned && pawn.MapHeld != null)
            {
                float visualScale = Mathf.Max(0f, extension?.visualScale ?? 0.52f);
                if (visualScale > 0f)
                {
                    FleckMaker.ThrowLightningGlow(pawn.DrawPos, pawn.MapHeld, visualScale);
                }
            }

            return result;
        }
    }
}
