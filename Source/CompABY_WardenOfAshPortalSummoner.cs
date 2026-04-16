using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    public class CompABY_WardenOfAshPortalSummoner : ThingComp
    {
        private bool triggered75;
        private bool triggered40;
        private bool triggered15;

        public CompProperties_ABY_WardenOfAshPortalSummoner Props => (CompProperties_ABY_WardenOfAshPortalSummoner)props;

        private Pawn PawnParent => parent as Pawn;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref triggered75, "triggered75", false);
            Scribe_Values.Look(ref triggered40, "triggered40", false);
            Scribe_Values.Look(ref triggered15, "triggered15", false);
        }

        public override void CompTick()
        {
            base.CompTick();

            Pawn pawn = PawnParent;
            if (!ShouldOperateNow(pawn))
            {
                return;
            }

            float healthPct = pawn.MaxHitPoints > 0 ? (float)pawn.HitPoints / pawn.MaxHitPoints : 0f;

            if (!triggered75 && healthPct <= 0.75f)
            {
                triggered75 = true;
                TriggerPortalBurst(pawn, Props.threshold75Count);
            }

            if (!triggered40 && healthPct <= 0.40f)
            {
                triggered40 = true;
                TriggerPortalBurst(pawn, Props.threshold40Count);
            }

            if (!triggered15 && healthPct <= 0.15f)
            {
                triggered15 = true;
                TriggerPortalBurst(pawn, Props.threshold15Count);
            }
        }

        private bool ShouldOperateNow(Pawn pawn)
        {
            return pawn != null && pawn.Spawned && pawn.MapHeld != null && !pawn.Dead;
        }

        private void TriggerPortalBurst(Pawn pawn, int impCount)
        {
            if (pawn == null || pawn.MapHeld == null || impCount <= 0)
            {
                return;
            }

            Map map = pawn.MapHeld;
            IntVec3 center = pawn.PositionHeld;

            FleckMaker.Static(center, map, FleckDefOf.ExplosionFlash, Props.portalFlashScale);
            if (!string.IsNullOrWhiteSpace(Props.portalSoundDefName))
            {
                ABY_SoundUtility.PlayAt(Props.portalSoundDefName, center, map);
            }

            if (ABY_Phase2PortalUtility.TrySpawnImpPortalNear(
                map,
                pawn.Faction,
                center,
                Props.portalMinRadius,
                Props.portalMaxRadius,
                impCount,
                Props.portalWarmupTicks,
                Props.portalSpawnIntervalTicks,
                Props.portalLingerTicks,
                out Building_AbyssalImpPortal portal))
            {
                if (portal != null)
                {
                    FleckMaker.Static(portal.Position, map, FleckDefOf.ExplosionFlash, Props.portalFlashScale * 0.7f);
                }

                return;
            }

            // Fallback: if portal placement fails, still try to materialize a single nearby imp directly.
            if (ABY_Phase2PortalUtility.TryGenerateImp(PawnKindDef.NamedSilentFail("ABY_RiftImp"), pawn.Faction, map, out Pawn imp)
                && CellFinder.TryFindRandomCellNear(center, map, 2, c => c.InBounds(map) && c.Standable(map) && c.GetFirstPawn(map) == null, out IntVec3 spawnCell))
            {
                GenSpawn.Spawn(imp, spawnCell, map, Rot4.Random);
                ABY_Phase2PortalUtility.GiveAssaultLord(imp);
                FleckMaker.Static(spawnCell, map, FleckDefOf.ExplosionFlash, Props.portalFlashScale * 0.65f);
            }
        }
    }
}
