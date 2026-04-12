using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    public static class AbyssalSecretBossUtility
    {
        private const string RupturePortalDefName = "ABY_RupturePortal";
        private const string RupturePawnKindDefName = "ABY_ArchonOfRupture";

        public static void TrySpawnRupturePortal(Map map, IntVec3 origin, Faction faction)
        {
            if (map == null || !origin.IsValid)
            {
                return;
            }

            if (!TryFindPortalCell(map, origin, out IntVec3 portalCell))
            {
                return;
            }

            ThingDef portalDef = DefDatabase<ThingDef>.GetNamedSilentFail(RupturePortalDefName);
            PawnKindDef ruptureKind = DefDatabase<PawnKindDef>.GetNamedSilentFail(RupturePawnKindDefName);
            if (portalDef == null || ruptureKind == null)
            {
                return;
            }

            Building_AbyssalRupturePortal portal = ThingMaker.MakeThing(portalDef) as Building_AbyssalRupturePortal;
            if (portal == null)
            {
                return;
            }

            GenSpawn.Spawn(portal, portalCell, map, Rot4.Random);
            portal.Initialize(faction, ruptureKind, 90, 300, "Archon of Rupture");
            ArchonInfernalVFXUtility.DoSummonVFX(map, portalCell);
            ABY_SoundUtility.PlayAt("ABY_RupturePortalOpen", portalCell, map);

            Find.LetterStack.ReceiveLetter(
                "ABY_SecretBossRevealLabel".Translate(),
                "ABY_SecretBossRevealDesc".Translate(),
                LetterDefOf.ThreatBig,
                new TargetInfo(portalCell, map));
        }

        private static bool TryFindPortalCell(Map map, IntVec3 origin, out IntVec3 cell)
        {
            cell = IntVec3.Invalid;

            foreach (IntVec3 candidate in GenRadial.RadialCellsAround(origin, 4.9f, true))
            {
                if (!candidate.InBounds(map) || !candidate.Standable(map))
                {
                    continue;
                }

                if (candidate.GetFirstPawn(map) != null)
                {
                    continue;
                }

                cell = candidate;
                return true;
            }

            return ABY_Phase2PortalUtility.TryFindPortalSpawnCell(map, out cell);
        }
    }
}
