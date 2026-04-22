using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    public static class AbyssalDominionSliceWaveDirector
    {
        public sealed class DominionSliceWavePlan
        {
            public string labelKey;
            public string fallbackLabel;
            public IntVec3 FocusCell = IntVec3.Invalid;
            public int MinSpawnRadius = 7;
            public int MaxSpawnRadius = 18;
            public readonly List<PawnKindDef> PawnKinds = new List<PawnKindDef>();

            public string GetLabel()
            {
                if (!labelKey.NullOrEmpty() && labelKey.CanTranslate())
                {
                    return labelKey.Translate();
                }

                return fallbackLabel ?? "dominion support wave";
            }
        }

        public static DominionSliceWavePlan BuildPlan(
            Map map,
            MapComponent_DominionSliceEncounter.SlicePhase phase,
            int wavesTriggered,
            int hazardPressure,
            int liveAnchorCount,
            List<Building_ABY_DominionSliceAnchor> anchors,
            Building_ABY_DominionSliceHeart heart,
            ABY_DominionPocketSession session)
        {
            DominionSliceWavePlan plan = new DominionSliceWavePlan();
            IntVec3 entryCell = session != null && session.pocketEntryCell.IsValid ? session.pocketEntryCell : map.Center;
            IntVec3 heartCell = heart != null && !heart.Destroyed
                ? heart.PositionHeld
                : session != null && session.heartCell.IsValid ? session.heartCell : map.Center;

            switch (phase)
            {
                case MapComponent_DominionSliceEncounter.SlicePhase.Breach:
                    BuildBreachPlan(plan, wavesTriggered, hazardPressure, entryCell, heartCell);
                    break;
                case MapComponent_DominionSliceEncounter.SlicePhase.Anchorfall:
                    BuildAnchorfallPlan(plan, wavesTriggered, hazardPressure, liveAnchorCount, anchors, entryCell, heartCell);
                    break;
                case MapComponent_DominionSliceEncounter.SlicePhase.HeartExposed:
                case MapComponent_DominionSliceEncounter.SlicePhase.Collapse:
                    BuildHeartPlan(plan, wavesTriggered, hazardPressure, anchors, entryCell, heartCell);
                    break;
                default:
                    return null;
            }

            return plan.PawnKinds.Count > 0 ? plan : null;
        }

        public static int GetNextWaveDelayTicks(
            MapComponent_DominionSliceEncounter.SlicePhase phase,
            int wavesTriggered,
            int hazardPressure,
            int liveAnchorCount)
        {
            int baseTicks;
            switch (phase)
            {
                case MapComponent_DominionSliceEncounter.SlicePhase.Breach:
                    baseTicks = 560;
                    break;
                case MapComponent_DominionSliceEncounter.SlicePhase.Anchorfall:
                    baseTicks = 760;
                    break;
                case MapComponent_DominionSliceEncounter.SlicePhase.HeartExposed:
                case MapComponent_DominionSliceEncounter.SlicePhase.Collapse:
                    baseTicks = 840;
                    break;
                default:
                    return 900;
            }

            int reduction = System.Math.Min(220, hazardPressure * 18) + System.Math.Min(120, wavesTriggered * 12);
            if (phase == MapComponent_DominionSliceEncounter.SlicePhase.Anchorfall && liveAnchorCount <= 1)
            {
                reduction += 80;
            }

            return System.Math.Max(360, baseTicks - reduction);
        }

        private static void BuildBreachPlan(DominionSliceWavePlan plan, int wavesTriggered, int hazardPressure, IntVec3 entryCell, IntVec3 heartCell)
        {
            plan.FocusCell = Midpoint(entryCell, heartCell);
            plan.MinSpawnRadius = 7;
            plan.MaxSpawnRadius = 18;

            if (wavesTriggered % 2 == 0)
            {
                plan.labelKey = "ABY_DominionSliceWaveLabel_ThresholdHunters";
                plan.fallbackLabel = "threshold hunters";
                AddMany(plan.PawnKinds, "ABY_RiftImp", 2);
                AddMany(plan.PawnKinds, "ABY_EmberHound", 1);
                AddMany(plan.PawnKinds, "ABY_HexgunThrall", 1);
                AddMany(plan.PawnKinds, "ABY_ChainZealot", 1);
            }
            else
            {
                plan.labelKey = "ABY_DominionSliceWaveLabel_BreachCell";
                plan.fallbackLabel = "breach cell";
                AddMany(plan.PawnKinds, "ABY_RiftImp", 3);
                AddMany(plan.PawnKinds, "ABY_EmberHound", 2);
                AddMany(plan.PawnKinds, "ABY_ChainZealot", 1);
            }

            if (hazardPressure >= 2)
            {
                AddMany(plan.PawnKinds, "ABY_EmberHound", 1);
            }
        }

        private static void BuildAnchorfallPlan(
            DominionSliceWavePlan plan,
            int wavesTriggered,
            int hazardPressure,
            int liveAnchorCount,
            List<Building_ABY_DominionSliceAnchor> anchors,
            IntVec3 entryCell,
            IntVec3 heartCell)
        {
            plan.FocusCell = ResolveAnchorFocus(anchors, wavesTriggered, heartCell, entryCell);
            plan.MinSpawnRadius = 8;
            plan.MaxSpawnRadius = 20;

            int variant = wavesTriggered % 3;
            if (variant == 0)
            {
                plan.labelKey = "ABY_DominionSliceWaveLabel_ChoirEscort";
                plan.fallbackLabel = "choir escort";
                AddMany(plan.PawnKinds, "ABY_GateWarden", 1);
                AddMany(plan.PawnKinds, "ABY_NullPriest", 1);
                AddMany(plan.PawnKinds, "ABY_EmberHound", 1);
                AddMany(plan.PawnKinds, "ABY_ChainZealot", 1);
            }
            else if (variant == 1)
            {
                plan.labelKey = "ABY_DominionSliceWaveLabel_LawPunishers";
                plan.fallbackLabel = "law punishers";
                AddMany(plan.PawnKinds, "ABY_GateWarden", 1);
                AddMany(plan.PawnKinds, "ABY_RiftSniper", 1);
                AddMany(plan.PawnKinds, "ABY_HexgunThrall", 1);
                AddMany(plan.PawnKinds, "ABY_EmberHound", 1);
            }
            else
            {
                plan.labelKey = "ABY_DominionSliceWaveLabel_AnchorWardens";
                plan.fallbackLabel = "anchor wardens";
                AddMany(plan.PawnKinds, "ABY_GateWarden", 1);
                AddMany(plan.PawnKinds, "ABY_ChainZealot", 2);
                AddMany(plan.PawnKinds, "ABY_EmberHound", 1);
            }

            if (liveAnchorCount <= 1)
            {
                AddMany(plan.PawnKinds, "ABY_NullPriest", 1);
            }

            if (hazardPressure >= 3)
            {
                AddMany(plan.PawnKinds, "ABY_RiftSniper", 1);
            }
        }

        private static void BuildHeartPlan(
            DominionSliceWavePlan plan,
            int wavesTriggered,
            int hazardPressure,
            List<Building_ABY_DominionSliceAnchor> anchors,
            IntVec3 entryCell,
            IntVec3 heartCell)
        {
            plan.FocusCell = ResolveHeartFocus(anchors, wavesTriggered, entryCell, heartCell);
            plan.MinSpawnRadius = 9;
            plan.MaxSpawnRadius = 22;

            if (wavesTriggered % 2 == 0)
            {
                plan.labelKey = "ABY_DominionSliceWaveLabel_CrownRetinue";
                plan.fallbackLabel = "crown retinue";
                AddMany(plan.PawnKinds, "ABY_GateWarden", 2);
                AddMany(plan.PawnKinds, "ABY_NullPriest", 1);
                AddMany(plan.PawnKinds, "ABY_RiftSniper", 1);
            }
            else
            {
                plan.labelKey = "ABY_DominionSliceWaveLabel_CrownEnforcers";
                plan.fallbackLabel = "crown enforcers";
                AddMany(plan.PawnKinds, "ABY_BreachBruteEscort", 1);
                AddMany(plan.PawnKinds, "ABY_GateWarden", 1);
                AddMany(plan.PawnKinds, "ABY_EmberHound", 2);
                AddMany(plan.PawnKinds, "ABY_ChainZealot", 1);
            }

            if (hazardPressure >= 4)
            {
                AddMany(plan.PawnKinds, "ABY_RiftSniper", 1);
            }

            if (wavesTriggered >= 4)
            {
                AddMany(plan.PawnKinds, "ABY_GateWarden", 1);
            }
        }

        private static IntVec3 ResolveAnchorFocus(List<Building_ABY_DominionSliceAnchor> anchors, int wavesTriggered, IntVec3 heartCell, IntVec3 entryCell)
        {
            List<IntVec3> liveAnchorCells = new List<IntVec3>();
            if (anchors != null)
            {
                for (int i = 0; i < anchors.Count; i++)
                {
                    Building_ABY_DominionSliceAnchor anchor = anchors[i];
                    if (anchor != null && !anchor.Destroyed && anchor.Spawned)
                    {
                        liveAnchorCells.Add(anchor.PositionHeld);
                    }
                }
            }

            if (liveAnchorCells.Count > 0)
            {
                return liveAnchorCells[wavesTriggered % liveAnchorCells.Count];
            }

            return Midpoint(entryCell, heartCell);
        }

        private static IntVec3 ResolveHeartFocus(List<Building_ABY_DominionSliceAnchor> anchors, int wavesTriggered, IntVec3 entryCell, IntVec3 heartCell)
        {
            if (anchors != null && anchors.Count > 0 && wavesTriggered % 3 == 2)
            {
                for (int i = 0; i < anchors.Count; i++)
                {
                    Building_ABY_DominionSliceAnchor anchor = anchors[i];
                    if (anchor != null && !anchor.Destroyed && anchor.Spawned)
                    {
                        return Midpoint(anchor.PositionHeld, heartCell);
                    }
                }
            }

            if (wavesTriggered % 2 == 1)
            {
                return Midpoint(entryCell, heartCell);
            }

            return heartCell;
        }

        private static IntVec3 Midpoint(IntVec3 a, IntVec3 b)
        {
            return new IntVec3((a.x + b.x) / 2, 0, (a.z + b.z) / 2);
        }

        private static void AddMany(List<PawnKindDef> list, string defName, int count)
        {
            PawnKindDef def = DefDatabase<PawnKindDef>.GetNamedSilentFail(defName);
            if (def == null || list == null || count <= 0)
            {
                return;
            }

            for (int i = 0; i < count; i++)
            {
                list.Add(def);
            }
        }
    }
}
