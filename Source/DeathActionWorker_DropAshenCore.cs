using RimWorld;
using Verse;
using Verse.AI.Group;

namespace AbyssalProtocol
{
    public class DeathActionWorker_DropAshenCore : DeathActionWorker
    {
        private const int MinResidueDrop = 10;
        private const int MaxResidueDrop = 16;

        public override void PawnDied(Corpse corpse, Lord prevLord)
        {
            if (corpse == null || corpse.Map == null)
            {
                return;
            }

            Map map = corpse.Map;
            IntVec3 cell = corpse.Position;

            bool droppedAny = false;

            ThingDef coreDef = DefDatabase<ThingDef>.GetNamedSilentFail("ABY_AshenCore");
            if (coreDef != null)
            {
                Thing core = ThingMaker.MakeThing(coreDef);
                if (core != null && GenPlace.TryPlaceThing(core, cell, map, ThingPlaceMode.Near) != null)
                {
                    droppedAny = true;
                }
            }

            ThingDef residueDef = DefDatabase<ThingDef>.GetNamedSilentFail("ABY_AbyssalResidue");
            if (residueDef != null)
            {
                Thing residue = ThingMaker.MakeThing(residueDef);
                if (residue != null)
                {
                    residue.stackCount = Mathf.Min(residueDef.stackLimit, Rand.RangeInclusive(MinResidueDrop, MaxResidueDrop));
                    if (GenPlace.TryPlaceThing(residue, cell, map, ThingPlaceMode.Near) != null)
                    {
                        droppedAny = true;
                    }
                }
            }

            if (droppedAny)
            {
                Messages.Message(
                    "ABY_WardenRewardRecovered".Translate(),
                    new TargetInfo(cell, map),
                    MessageTypeDefOf.PositiveEvent,
                    false);
            }
        }
    }
}
