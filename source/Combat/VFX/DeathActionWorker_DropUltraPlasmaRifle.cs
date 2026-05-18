using RimWorld;
using Verse;
using Verse.AI.Group;

namespace AbyssalProtocol
{
    public class DeathActionWorker_DropUltraPlasmaRifle : DeathActionWorker
    {
        public override void PawnDied(Corpse corpse, Lord prevLord)
        {
            if (corpse == null || corpse.Map == null)
                return;

            ThingDef rifleDef = DefDatabase<ThingDef>.GetNamedSilentFail("ABY_UltraPlasmaRifle");
            if (rifleDef == null)
                return;

            Thing rifle = ThingMaker.MakeThing(rifleDef);
            if (rifle == null)
                return;

            GenPlace.TryPlaceThing(rifle, corpse.Position, corpse.Map, ThingPlaceMode.Near);
        }
    }
}
