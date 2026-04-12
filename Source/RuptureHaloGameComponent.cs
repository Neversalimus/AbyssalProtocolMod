using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class RuptureHaloGameComponent : GameComponent
    {
        private const string CrownDefName = "ABY_CrownOfRupture";
        private ThingDef ringDef;
        private ThingDef coreDef;

        public RuptureHaloGameComponent(Game game)
        {
        }

        public override void GameComponentTick()
        {
            if (Find.TickManager == null || Find.Maps == null)
            {
                return;
            }

            int ticksGame = Find.TickManager.TicksGame;
            if (ticksGame % 6 != 0)
            {
                return;
            }

            ringDef ??= DefDatabase<ThingDef>.GetNamedSilentFail("ABY_Mote_RuptureHaloRing");
            coreDef ??= DefDatabase<ThingDef>.GetNamedSilentFail("ABY_Mote_RuptureHaloCore");
            if (ringDef == null || coreDef == null)
            {
                return;
            }

            for (int i = 0; i < Find.Maps.Count; i++)
            {
                Map map = Find.Maps[i];
                if (map == null || map.mapPawns == null)
                {
                    continue;
                }

                foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
                {
                    if (!HasCrownEquipped(pawn))
                    {
                        continue;
                    }

                    SpawnHaloFor(pawn, map, ticksGame);
                }
            }
        }

        private static bool HasCrownEquipped(Pawn pawn)
        {
            return pawn?.equipment?.Primary != null && pawn.equipment.Primary.def?.defName == CrownDefName;
        }

        private void SpawnHaloFor(Pawn pawn, Map map, int ticksGame)
        {
            Vector3 headPos = pawn.DrawPos;
            headPos.y = AltitudeLayer.MoteOverhead.AltitudeFor();
            headPos.z += 0.32f;

            float pulse = (Mathf.Sin((ticksGame + pawn.thingIDNumber) * 0.085f) + 1f) * 0.5f;
            float orbit = (ticksGame + pawn.thingIDNumber) * 0.17f;
            Vector3 ringOffset = new Vector3(Mathf.Cos(orbit) * 0.06f, 0f, Mathf.Sin(orbit) * 0.03f);

            MoteMaker.MakeStaticMote(headPos + ringOffset, map, ringDef, Mathf.Lerp(0.72f, 0.88f, pulse));

            if (ticksGame % 12 == 0)
            {
                MoteMaker.MakeStaticMote(headPos + new Vector3(0f, 0f, 0.02f), map, coreDef, Mathf.Lerp(0.42f, 0.58f, pulse));
            }
        }
    }
}
