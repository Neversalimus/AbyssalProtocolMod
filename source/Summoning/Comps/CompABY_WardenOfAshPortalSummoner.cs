using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    public class CompABY_WardenOfAshPortalSummoner : ThingComp
    {
        private bool triggered75;
        private bool triggered40;
        private bool triggered15;
        private int spawnTick = -1;
        private float lastHealthPct = 1f;

        public CompProperties_ABY_WardenOfAshPortalSummoner Props => (CompProperties_ABY_WardenOfAshPortalSummoner)props;

        private Pawn PawnParent => parent as Pawn;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);

            Pawn pawn = PawnParent;
            spawnTick = Find.TickManager.TicksGame;
            lastHealthPct = GetHealthPct(pawn);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref triggered75, "triggered75", false);
            Scribe_Values.Look(ref triggered40, "triggered40", false);
            Scribe_Values.Look(ref triggered15, "triggered15", false);
            Scribe_Values.Look(ref spawnTick, "spawnTick", -1);
            Scribe_Values.Look(ref lastHealthPct, "lastHealthPct", 1f);
        }

        public override void CompTick()
        {
            base.CompTick();

            Pawn pawn = PawnParent;
            if (!ShouldOperateNow(pawn))
            {
                return;
            }

            if (spawnTick < 0)
            {
                spawnTick = Find.TickManager.TicksGame;
                lastHealthPct = GetHealthPct(pawn);
                return;
            }

            float healthPct = GetHealthPct(pawn);

            // Ignore the exact spawn tick so freshly spawned bosses do not instantly fire threshold events
            // due to transient initialization values.
            if (Find.TickManager.TicksGame <= spawnTick)
            {
                lastHealthPct = healthPct;
                return;
            }

            if (!triggered75 && CrossedThreshold(lastHealthPct, healthPct, 0.75f))
            {
                triggered75 = true;
                TriggerThresholdFeedback(pawn, 0.08f);
                TriggerPortalBurst(pawn, Props.threshold75Count);
            }

            if (!triggered40 && CrossedThreshold(lastHealthPct, healthPct, 0.40f))
            {
                triggered40 = true;
                TriggerThresholdFeedback(pawn, 0.10f);
                TriggerPortalBurst(pawn, Props.threshold40Count);
            }

            if (!triggered15 && CrossedThreshold(lastHealthPct, healthPct, 0.15f))
            {
                triggered15 = true;
                TriggerThresholdFeedback(pawn, 0.12f);
                TriggerPortalBurst(pawn, Props.threshold15Count);
            }

            lastHealthPct = healthPct;
        }

        private static bool CrossedThreshold(float previousPct, float currentPct, float threshold)
        {
            return previousPct > threshold && currentPct <= threshold;
        }

        private static float GetHealthPct(Pawn pawn)
        {
            if (pawn == null || pawn.health == null)
            {
                return 1f;
            }

            return pawn.health.summaryHealth.SummaryHealthPercent;
        }

        private bool ShouldOperateNow(Pawn pawn)
        {
            return pawn != null && pawn.Spawned && pawn.MapHeld != null && !pawn.Dead;
        }


        private void TriggerThresholdFeedback(Pawn pawn, float pulseStrength)
        {
            if (pawn?.MapHeld == null)
            {
                return;
            }

            FleckMaker.ThrowLightningGlow(pawn.DrawPos, pawn.MapHeld, Props.portalFlashScale * 0.82f);
            FleckMaker.Static(pawn.PositionHeld, pawn.MapHeld, FleckDefOf.ExplosionFlash, Props.portalFlashScale * 0.58f);
            Current.Game?.GetComponent<AbyssalBossScreenFXGameComponent>()?.RegisterRitualPulse(pawn.MapHeld, pulseStrength);
            if (!string.IsNullOrWhiteSpace(Props.portalSoundDefName))
            {
                ABY_SoundUtility.PlayAt(Props.portalSoundDefName, pawn.PositionHeld, pawn.MapHeld);
            }
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
            PawnKindDef impKindDef = DefDatabase<PawnKindDef>.GetNamedSilentFail("ABY_RiftImp");
            if (impKindDef != null
                && ABY_Phase2PortalUtility.TryGenerateImp(impKindDef, pawn.Faction, map, out Pawn imp)
                && CellFinder.TryFindRandomCellNear(center, map, 2, c => c.InBounds(map) && c.Standable(map) && c.GetFirstPawn(map) == null, out IntVec3 spawnCell))
            {
                GenSpawn.Spawn(imp, spawnCell, map, Rot4.Random);
                ABY_Phase2PortalUtility.GiveAssaultLord(imp);
                FleckMaker.Static(spawnCell, map, FleckDefOf.ExplosionFlash, Props.portalFlashScale * 0.65f);
            }
        }
    }
}
