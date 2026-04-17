using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class HediffCompProperties_ABY_TestImmortality : HediffCompProperties
    {
        public int stabilizeIntervalTicks = 1;
        public bool aggressiveCleansing = true;

        public HediffCompProperties_ABY_TestImmortality()
        {
            compClass = typeof(HediffComp_ABY_TestImmortality);
        }
    }

    public class HediffComp_ABY_TestImmortality : HediffComp
    {
        private const string GizmoIconPath = "UI/AbyssalForge/ABY_Category_Implants";

        private Pawn Pawn => parent?.pawn;

        public HediffCompProperties_ABY_TestImmortality Props =>
            (HediffCompProperties_ABY_TestImmortality)props;

        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            ABY_TestImmortalityUtility.StabilizePawn(Pawn, Props.aggressiveCleansing);
        }

        public override void Notify_PawnPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            base.Notify_PawnPostApplyDamage(dinfo, totalDamageDealt);
            ABY_TestImmortalityUtility.StabilizePawn(Pawn, Props.aggressiveCleansing);
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            Pawn pawn = Pawn;
            if (pawn == null || pawn.health == null || pawn.Dead)
            {
                return;
            }

            if (!pawn.IsHashIntervalTick(Mathf.Max(1, Props.stabilizeIntervalTicks)))
            {
                return;
            }

            ABY_TestImmortalityUtility.StabilizePawn(pawn, Props.aggressiveCleansing);
        }

        public override IEnumerable<Gizmo> CompGetGizmos()
        {
            foreach (Gizmo gizmo in base.CompGetGizmos())
            {
                yield return gizmo;
            }

            Pawn pawn = Pawn;
            if (!Prefs.DevMode || pawn == null || pawn.Dead)
            {
                yield break;
            }

            yield return new Command_Action
            {
                defaultLabel = "ABY_TestImmortalityPawnGizmoLabel".Translate(),
                defaultDesc = "ABY_TestImmortalityPawnGizmoDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get(GizmoIconPath),
                action = delegate
                {
                    ABY_TestImmortalityUtility.RemoveImmortality(pawn);
                }
            };
        }
    }
}
