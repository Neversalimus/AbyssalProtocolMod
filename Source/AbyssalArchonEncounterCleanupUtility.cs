using System.Collections.Generic;
using Verse;

namespace AbyssalProtocol
{
    public static class AbyssalArchonEncounterCleanupUtility
    {
        private const string ImpPortalDefName = "ABY_ImpPortal";
        private const string RupturePortalDefName = "ABY_RupturePortal";
        private const string RiftImpDefName = "ABY_RiftImp";
        private const string EmberHoundDefName = "ABY_EmberHound";
        private const string HexgunThrallDefName = "ABY_HexgunThrall";
        private const string ChainZealotDefName = "ABY_ChainZealot";
        private const string NullPriestDefName = "ABY_NullPriest";

        public static void HandleArchonBeastDeath(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            Map map = pawn.Corpse?.Map ?? pawn.MapHeld;
            if (map == null)
            {
                return;
            }

            Current.Game?.GetComponent<AbyssalBossScreenFXGameComponent>()?.ClearBoss(pawn);
            DestroyPortals(map, ImpPortalDefName);
            DestroyPortals(map, RupturePortalDefName);
            DismissEscortPawns(map, pawn.Faction);
        }

        private static void DestroyPortals(Map map, string defName)
        {
            ThingDef portalDef = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            if (map?.listerThings == null || portalDef == null)
            {
                return;
            }

            List<Thing> portals = map.listerThings.ThingsOfDef(portalDef);
            if (portals == null || portals.Count == 0)
            {
                return;
            }

            List<Thing> snapshot = new List<Thing>(portals);
            for (int i = 0; i < snapshot.Count; i++)
            {
                Thing portal = snapshot[i];
                if (portal != null && portal.Spawned && !portal.Destroyed)
                {
                    portal.Destroy(DestroyMode.Vanish);
                }
            }
        }

        private static void DismissEscortPawns(Map map, Faction faction)
        {
            if (map?.mapPawns?.AllPawnsSpawned == null)
            {
                return;
            }

            List<Pawn> snapshot = new List<Pawn>(map.mapPawns.AllPawnsSpawned);
            for (int i = 0; i < snapshot.Count; i++)
            {
                Pawn pawn = snapshot[i];
                if (pawn == null || pawn.Dead || pawn.Destroyed || !pawn.Spawned)
                {
                    continue;
                }

                if (faction != null && pawn.Faction != faction)
                {
                    continue;
                }

                if (!IsDismissedWithArchonDeath(pawn.def?.defName))
                {
                    continue;
                }

                pawn.Destroy(DestroyMode.Vanish);
            }
        }

        private static bool IsDismissedWithArchonDeath(string defName)
        {
            return defName == RiftImpDefName
                || defName == EmberHoundDefName
                || defName == HexgunThrallDefName
                || defName == ChainZealotDefName
                || defName == NullPriestDefName;
        }
    }
}
