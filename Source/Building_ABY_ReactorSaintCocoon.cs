using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    public sealed class Building_ABY_ReactorSaintCocoon : Building
    {
        private const string BossPawnKindDefName = "ABY_ReactorSaint";
        private const string BossLabel = "Infernal Reactor Saint";
        private const string ArrivalSoundDefName = "ABY_ReactorSaintCharge";
        private const string CompletionLetterLabelKey = "ABY_ReactorSaintSummonSuccessLabel";
        private const string CompletionLetterDescKey = "ABY_ReactorSaintSummonSuccessDesc";
        private const string DepartureSkyfallerDefName = "ABY_Manifestation_ReactorSaintDeparture";
        private const int ReleaseDelayTicks = 834;
        private const int PostReleaseTicks = 417;

        private int ticksSinceImpact;
        private bool bossReleased;
        private bool releaseFailedPermanently;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ticksSinceImpact, "ticksSinceImpact", 0);
            Scribe_Values.Look(ref bossReleased, "bossReleased", false);
            Scribe_Values.Look(ref releaseFailedPermanently, "releaseFailedPermanently", false);
        }

        public override void Tick()
        {
            base.Tick();

            if (Map == null || Destroyed)
            {
                return;
            }

            ticksSinceImpact++;

            if (!bossReleased)
            {
                TickDormantCocoon();

                if (!releaseFailedPermanently && ticksSinceImpact >= ReleaseDelayTicks)
                {
                    TryReleaseBoss();
                }

                return;
            }

            TickSpentCocoon();

            if (ticksSinceImpact >= ReleaseDelayTicks + PostReleaseTicks)
            {
                LaunchAway();
            }
        }

        private void TickDormantCocoon()
        {
            if (ticksSinceImpact % 11 == 0)
            {
                FleckMaker.ThrowMicroSparks(DrawPos, Map);
            }

            if (ticksSinceImpact % 30 == 0)
            {
                FleckMaker.ThrowHeatGlow(Position, Map, 1.20f);
            }

            if (ticksSinceImpact % 60 == 0)
            {
                FleckMaker.ThrowLightningGlow(DrawPos, Map, 1.65f);
                FilthMaker.TryMakeFilth(Position, Map, ThingDefOf.Filth_Ash, 1);
            }
        }

        private void TickSpentCocoon()
        {
            if (ticksSinceImpact % 24 == 0)
            {
                FleckMaker.ThrowMicroSparks(DrawPos, Map);
            }

            if (ticksSinceImpact % 52 == 0)
            {
                FleckMaker.ThrowLightningGlow(DrawPos, Map, 1.10f);
            }
        }

        private void TryReleaseBoss()
        {
            PawnKindDef kindDef = DefDatabase<PawnKindDef>.GetNamedSilentFail(BossPawnKindDefName);
            Faction faction = AbyssalBossSummonUtility.ResolveHostileFaction();

            if (kindDef == null || faction == null)
            {
                releaseFailedPermanently = true;
                Log.Warning("[AbyssalProtocol] Reactor Saint cocoon could not resolve boss kind or hostile faction.");
                return;
            }

            if (!AbyssalBossSummonUtility.TryGenerateBoss(Map, kindDef, faction, BossLabel, out Pawn pawn, out string failReason))
            {
                if (ticksSinceImpact % 60 == 0 && !failReason.NullOrEmpty())
                {
                    Log.Warning("[AbyssalProtocol] Reactor Saint cocoon failed to generate boss: " + failReason);
                }

                return;
            }

            IntVec3 releaseCell = FindReleaseCell();
            AbyssalBossSummonUtility.FinalizeBossArrival(
                pawn,
                faction,
                Map,
                releaseCell,
                BossLabel,
                ArrivalSoundDefName,
                CompletionLetterLabelKey,
                CompletionLetterDescKey);

            bossReleased = true;
            FleckMaker.ThrowLightningGlow(DrawPos, Map, 2.30f);
            FleckMaker.ThrowHeatGlow(Position, Map, 1.55f);
            FilthMaker.TryMakeFilth(Position, Map, ThingDefOf.Filth_Ash, 2);
        }

        private IntVec3 FindReleaseCell()
        {
            if (IsValidReleaseCell(Position))
            {
                return Position;
            }

            for (int i = 0; i < GenRadial.NumCellsInRadius(2.9f); i++)
            {
                IntVec3 candidate = Position + GenRadial.RadialPattern[i];
                if (IsValidReleaseCell(candidate))
                {
                    return candidate;
                }
            }

            return Position;
        }

        private bool IsValidReleaseCell(IntVec3 cell)
        {
            return cell.IsValid && cell.InBounds(Map) && cell.Standable(Map) && !cell.Fogged(Map);
        }

        private void LaunchAway()
        {
            Map map = Map;
            IntVec3 position = Position;

            FleckMaker.ThrowLightningGlow(DrawPos, map, 1.85f);
            FleckMaker.ThrowMicroSparks(DrawPos, map);

            ThingDef departureSkyfallerDef = DefDatabase<ThingDef>.GetNamedSilentFail(DepartureSkyfallerDefName);
            Destroy(DestroyMode.Vanish);

            if (departureSkyfallerDef != null && map != null && position.IsValid && position.InBounds(map))
            {
                SkyfallerMaker.SpawnSkyfaller(departureSkyfallerDef, position, map);
            }
        }
    }
}
