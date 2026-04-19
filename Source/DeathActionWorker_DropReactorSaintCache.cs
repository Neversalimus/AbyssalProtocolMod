using System;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace AbyssalProtocol
{
    public class DeathActionWorker_DropReactorSaintCache : DeathActionWorker
    {
        private const int MinBonusResidueDrop = 14;
        private const int MaxBonusResidueDrop = 20;

        public override void PawnDied(Corpse corpse, Lord prevLord)
        {
            if (corpse == null || corpse.Map == null)
            {
                return;
            }

            Map map = corpse.Map;
            IntVec3 cell = corpse.Position;
            bool droppedAny = false;

            droppedAny |= TrySpawnSingle(map, cell, "ABY_ReactorSaintCore");
            droppedAny |= TrySpawnSingle(map, cell, "ComponentSpacer");

            if (Rand.Chance(Mathf.Clamp01(0.35f * AbyssalDifficultyUtility.CurrentProfile.RewardMultiplier)))
            {
                droppedAny |= TrySpawnSingle(map, cell, "ComponentSpacer");
            }

            ThingDef residueDef = DefDatabase<ThingDef>.GetNamedSilentFail("ABY_AbyssalResidue");
            if (residueDef != null)
            {
                Thing residue = ThingMaker.MakeThing(residueDef);
                if (residue != null)
                {
                    residue.stackCount = Math.Min(residueDef.stackLimit, AbyssalDifficultyUtility.ScaleRewardRoll(MinBonusResidueDrop, MaxBonusResidueDrop));
                    if (GenPlace.TryPlaceThing(residue, cell, map, ThingPlaceMode.Near))
                    {
                        droppedAny = true;
                    }
                }
            }

            if (droppedAny)
            {
                Messages.Message(
                    "ABY_ReactorSaintRewardRecovered".Translate(),
                    new TargetInfo(cell, map),
                    MessageTypeDefOf.PositiveEvent,
                    false);
            }
        }

        private static bool TrySpawnSingle(Map map, IntVec3 nearCell, string defName)
        {
            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            if (def == null)
            {
                return false;
            }

            Thing thing = ThingMaker.MakeThing(def);
            return thing != null && GenPlace.TryPlaceThing(thing, nearCell, map, ThingPlaceMode.Near);
        }
    }
}
