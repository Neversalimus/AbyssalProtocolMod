using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class CompABY_HaloFracture : ThingComp
    {
        private IntVec3 lastKnownCell = IntVec3.Invalid;

        public CompProperties_ABY_HaloFracture Props => (CompProperties_ABY_HaloFracture)props;

        private Pawn PawnParent => parent as Pawn;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref lastKnownCell, "lastKnownCell");
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (parent.Spawned)
            {
                lastKnownCell = parent.Position;
            }
        }

        public override void CompTick()
        {
            base.CompTick();
            if (parent.Spawned)
            {
                lastKnownCell = parent.Position;
            }
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            Pawn pawn = PawnParent;
            bool killed = pawn != null && (pawn.Dead || mode == DestroyMode.KillFinalize);
            if (killed && previousMap != null && lastKnownCell.IsValid)
            {
                TriggerFracture(previousMap, lastKnownCell, pawn);
            }

            base.PostDestroy(mode, previousMap);
        }

        private void TriggerFracture(Map map, IntVec3 center, Pawn sourcePawn)
        {
            if (!center.InBounds(map))
            {
                return;
            }

            if (!Props.soundDefName.NullOrEmpty())
            {
                ABY_SoundUtility.PlayAt(Props.soundDefName, center, map);
            }

            FleckMaker.ThrowLightningGlow(center.ToVector3Shifted(), map, Props.visualScale);
            FleckMaker.Static(center, map, FleckDefOf.ExplosionFlash, Props.visualScale * 0.55f);

            HediffDef hediffDef = DefDatabase<HediffDef>.GetNamedSilentFail(Props.hediffDefName);
            if (hediffDef == null)
            {
                return;
            }

            IReadOnlyList<Pawn> pawns = map.mapPawns?.AllPawnsSpawned;
            if (pawns == null)
            {
                return;
            }

            Faction sourceFaction = sourcePawn?.Faction;
            int applied = 0;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn target = pawns[i];
                if (!IsValidTarget(target, map, center, sourceFaction, sourcePawn))
                {
                    continue;
                }

                ApplyFracture(target, hediffDef);
                applied++;
                if (applied >= Mathf.Max(1, Props.maxTargets))
                {
                    break;
                }
            }
        }

        private bool IsValidTarget(Pawn target, Map map, IntVec3 center, Faction sourceFaction, Pawn sourcePawn)
        {
            if (target == null || target == sourcePawn || target.Dead || target.Downed || !target.Spawned || target.MapHeld != map)
            {
                return false;
            }

            if (!target.PositionHeld.InHorDistOf(center, Props.radius))
            {
                return false;
            }

            if (sourceFaction != null && target.Faction != null)
            {
                return sourceFaction.HostileTo(target.Faction);
            }

            if (sourcePawn != null)
            {
                return target.HostileTo(sourcePawn);
            }

            return false;
        }

        private void ApplyFracture(Pawn target, HediffDef hediffDef)
        {
            if (target.health == null)
            {
                return;
            }

            Hediff hediff = target.health.hediffSet.GetFirstHediffOfDef(hediffDef);
            if (hediff == null)
            {
                hediff = HediffMaker.MakeHediff(hediffDef, target);
                target.health.AddHediff(hediff);
            }

            hediff.Severity = Mathf.Max(hediff.Severity, 1f);
            HediffComp_Disappears disappears = hediff.TryGetComp<HediffComp_Disappears>();
            if (disappears != null)
            {
                disappears.ticksToDisappear = Mathf.Max(60, Props.hediffDurationTicks);
            }

            target.health.hediffSet.DirtyCache();
            FleckMaker.ThrowLightningGlow(target.DrawPos, target.MapHeld, Props.visualScale * 0.55f);
        }
    }
}
