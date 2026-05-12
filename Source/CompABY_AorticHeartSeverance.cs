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

        public CompProperties_ABY_AorticHeartSeverance()
        {
            compClass = typeof(CompABY_AorticHeartSeverance);
        }
    }

    public class CompABY_AorticHeartSeverance : ThingComp
    {
        private IntVec3 lastKnownCell = IntVec3.Invalid;
        private Vector3 lastKnownDrawPos = Vector3.zero;
        private bool severanceNotified;

        public CompProperties_ABY_AorticHeartSeverance Props => (CompProperties_ABY_AorticHeartSeverance)props;

        private Pawn PawnParent => parent as Pawn;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref lastKnownCell, "lastKnownCell");
            Scribe_Values.Look(ref lastKnownDrawPos, "lastKnownDrawPos");
            Scribe_Values.Look(ref severanceNotified, "severanceNotified", false);
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            RememberPosition(true);
        }

        public override void CompTick()
        {
            base.CompTick();
            // Intentionally do not read Pawn.DrawPos here. Pawn.DrawPos can pass through
            // PawnTweener while the pawn is being generated/spawned/despawned, and a broken
            // tween state caused infinite Root-level Update/OnGUI red errors in dev-spawn.
            // PositionHeld is enough for death-severance VFX and is safe during normal ticks.
            RememberPosition(false);
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            Pawn pawn = PawnParent;
            bool killed = pawn != null && (pawn.Dead || mode == DestroyMode.KillFinalize);
            if (!severanceNotified && killed && previousMap != null)
            {
                severanceNotified = true;
                NotifySeverance(previousMap, pawn);
            }

            base.PostDestroy(mode, previousMap);
        }

        private void RememberPosition(bool allowInitialDrawPos)
        {
            if (parent == null || parent.Destroyed || !parent.Spawned)
            {
                return;
            }

            lastKnownCell = parent.PositionHeld;
            if (lastKnownCell.IsValid)
            {
                lastKnownDrawPos = lastKnownCell.ToVector3Shifted();
            }

            // DrawPos is only attempted once after spawn setup, and even then it is guarded.
            // Runtime ticks must never depend on DrawPos for this comp.
            if (allowInitialDrawPos)
            {
                try
                {
                    lastKnownDrawPos = parent.DrawPos;
                }
                catch
                {
                    if (lastKnownCell.IsValid)
                    {
                        lastKnownDrawPos = lastKnownCell.ToVector3Shifted();
                    }
                }
            }
        }

        private void NotifySeverance(Map map, Pawn pawn)
        {
            if (map == null)
            {
                return;
            }

            if (!Props.severanceSoundDefName.NullOrEmpty() && lastKnownCell.IsValid)
            {
                ABY_SoundUtility.PlayAt(Props.severanceSoundDefName, lastKnownCell, map);
            }

            MapComponent_DominionSliceEncounter encounter = map.GetComponent<MapComponent_DominionSliceEncounter>();
            if (encounter != null)
            {
                encounter.NotifyHeartGuardianKilled(pawn, lastKnownCell, lastKnownDrawPos, Props.guardianKindDefName);
            }
            else if (lastKnownCell.IsValid)
            {
                FleckMaker.ThrowLightningGlow(lastKnownCell.ToVector3Shifted(), map, 1.65f);
                FleckMaker.ThrowMicroSparks(lastKnownCell.ToVector3Shifted(), map);
            }
        }
    }
}
