using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class CompProperties_ABY_AorticHeartSeverance : CompProperties
    {
        public string guardianKindDefName = "ABY_AorticChainHarrower";
        public string heartDefName = "ABY_DominionSliceHeart";
        public string severanceSoundDefName = "ABY_SigilChargePulse";
        public int rememberPositionIntervalTicks = 45;

        public CompProperties_ABY_AorticHeartSeverance()
        {
            compClass = typeof(CompABY_AorticHeartSeverance);
        }
    }

    /// <summary>
    /// One-shot death severance notifier for Aortic Chain Harrowers.
    ///
    /// This comp is intentionally conservative:
    /// - it never reads Pawn.DrawPos during ticks;
    /// - it does not install any renderer or tweener hook;
    /// - it only stores a safe map cell and fires once when the pawn is killed.
    ///
    /// The previous animated/body overlay pipeline could put PawnTweener into a bad state during
    /// dev-spawn/despawn. This component is deliberately limited to PositionHeld + PostDestroy.
    /// </summary>
    public class CompABY_AorticHeartSeverance : ThingComp
    {
        private IntVec3 lastKnownCell = IntVec3.Invalid;
        private bool severanceNotified;

        public CompProperties_ABY_AorticHeartSeverance Props => (CompProperties_ABY_AorticHeartSeverance)props;

        private Pawn PawnParent => parent as Pawn;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref lastKnownCell, "lastKnownCell");
            Scribe_Values.Look(ref severanceNotified, "severanceNotified", false);
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            RememberSafeCell();
        }

        public override void CompTick()
        {
            base.CompTick();

            if (parent == null || parent.Destroyed || !parent.Spawned)
            {
                return;
            }

            int interval = Props != null ? Mathf.Max(15, Props.rememberPositionIntervalTicks) : 45;
            if (parent.IsHashIntervalTick(interval))
            {
                RememberSafeCell();
            }
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            Pawn pawn = PawnParent;
            bool killed = mode == DestroyMode.KillFinalize || (pawn != null && pawn.Dead);
            if (!severanceNotified && killed)
            {
                severanceNotified = true;
                NotifySeverance(previousMap, pawn);
            }

            base.PostDestroy(mode, previousMap);
        }

        private void RememberSafeCell()
        {
            if (parent == null)
            {
                return;
            }

            IntVec3 cell = parent.PositionHeld;
            if (cell.IsValid)
            {
                lastKnownCell = cell;
            }
        }

        private void NotifySeverance(Map map, Pawn pawn)
        {
            if (map == null)
            {
                return;
            }

            IntVec3 cell = lastKnownCell;
            if (!cell.IsValid && pawn != null && pawn.PositionHeld.IsValid)
            {
                cell = pawn.PositionHeld;
            }
            if (!cell.IsValid)
            {
                cell = map.Center;
            }

            if (!Props.severanceSoundDefName.NullOrEmpty())
            {
                ABY_SoundUtility.PlayAt(Props.severanceSoundDefName, cell, map);
            }

            MapComponent_DominionSliceEncounter encounter = map.GetComponent<MapComponent_DominionSliceEncounter>();
            if (encounter != null)
            {
                encounter.NotifyHeartGuardianKilled(pawn, cell, Props.guardianKindDefName);
                return;
            }

            Vector3 pos = cell.ToVector3Shifted();
            FleckMaker.ThrowLightningGlow(pos, map, 1.65f);
            FleckMaker.ThrowMicroSparks(pos, map);
        }
    }
}
