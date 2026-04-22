using RimWorld.Planet;
using System.Linq;
using Verse;

namespace AbyssalProtocol
{
    public class WorldObject_ABY_DominionSliceSite : MapParent
    {
        public override MapGeneratorDef MapGeneratorDef
        {
            get
            {
                return DefDatabase<MapGeneratorDef>.GetNamedSilentFail("Encounter")
                    ?? DefDatabase<MapGeneratorDef>.AllDefsListForReading.FirstOrDefault();
            }
        }
    }
}
