using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class Projectile_RuptureSentence : Bullet
    {
        private const string MarkDefName = "ABY_RuptureSentenceMark";

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map map = Map;
            IntVec3 cell = Position;

            if (hitThing is Pawn pawn && pawn.health != null)
            {
                ApplyMark(pawn);
            }

            if (map != null)
            {
                ABY_SoundUtility.PlayAt("ABY_RuptureImpact", cell, map);

                ThingDef mote = DefDatabase<ThingDef>.GetNamedSilentFail("ABY_Mote_RuptureHaloCore");
                if (mote != null)
                {
                    MoteMaker.MakeStaticMote(ExactPosition + new Vector3(0f, 0f, 0.05f), map, mote, 0.85f);
                }
            }

            Destroy(DestroyMode.Vanish);
        }

        private static void ApplyMark(Pawn pawn)
        {
            HediffDef markDef = DefDatabase<HediffDef>.GetNamedSilentFail(MarkDefName);
            if (markDef == null || pawn.health == null)
            {
                return;
            }

            Hediff existing = pawn.health.hediffSet.GetFirstHediffOfDef(markDef);
            if (existing == null)
            {
                existing = HediffMaker.MakeHediff(markDef, pawn);
                existing.Severity = 0f;
                pawn.health.AddHediff(existing);
            }

            existing.Severity = Mathf.Max(existing.Severity, 1f);
        }
    }
}
