using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class RuptureHaloGameComponent : GameComponent
    {
        private const string CrownDefName = "ABY_CrownOfRupture";
        private readonly Dictionary<int, int> nextHaloSpawnTickByPawn = new Dictionary<int, int>();
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
            if (ticksGame % 10 != 0)
            {
                return;
            }

            ringDef ??= DefDatabase<ThingDef>.GetNamedSilentFail("ABY_Mote_RuptureHaloRing");
            coreDef ??= DefDatabase<ThingDef>.GetNamedSilentFail("ABY_Mote_RuptureHaloCore");
            if (ringDef == null)
            {
                return;
            }

            HashSet<int> activePawnIds = new HashSet<int>();

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

                    activePawnIds.Add(pawn.thingIDNumber);
                    if (nextHaloSpawnTickByPawn.TryGetValue(pawn.thingIDNumber, out int nextTick) && ticksGame < nextTick)
                    {
                        continue;
                    }

                    SpawnHaloFor(pawn, ticksGame);
                    nextHaloSpawnTickByPawn[pawn.thingIDNumber] = ticksGame + 12;
                }
            }

            if (nextHaloSpawnTickByPawn.Count > 0)
            {
                List<int> staleKeys = new List<int>();
                foreach (KeyValuePair<int, int> pair in nextHaloSpawnTickByPawn)
                {
                    if (!activePawnIds.Contains(pair.Key))
                    {
                        staleKeys.Add(pair.Key);
                    }
                }

                for (int i = 0; i < staleKeys.Count; i++)
                {
                    nextHaloSpawnTickByPawn.Remove(staleKeys[i]);
                }
            }
        }

        private static bool HasCrownEquipped(Pawn pawn)
        {
            return pawn?.Spawned == true && pawn.equipment?.Primary != null && pawn.equipment.Primary.def?.defName == CrownDefName;
        }

        private void SpawnHaloFor(Pawn pawn, int ticksGame)
        {
            float pulse = (Mathf.Sin((ticksGame + pawn.thingIDNumber) * 0.055f) + 1f) * 0.5f;
            float ringScale = Mathf.Lerp(0.84f, 0.94f, pulse);
            MoteMaker.MakeAttachedOverlay(pawn, ringDef, Vector3.zero, ringScale, 0.24f);

            if (coreDef != null && ticksGame % 30 == 0)
            {
                float coreScale = Mathf.Lerp(0.28f, 0.34f, pulse);
                MoteMaker.MakeAttachedOverlay(pawn, coreDef, Vector3.zero, coreScale, 0.18f);
            }
        }
    }
}
