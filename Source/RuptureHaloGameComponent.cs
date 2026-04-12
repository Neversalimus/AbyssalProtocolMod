using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class RuptureHaloGameComponent : GameComponent
    {
        private const string CrownDefName = "ABY_CrownOfRupture";
        private readonly Dictionary<int, int> nextHaloRefreshTickByPawn = new Dictionary<int, int>();
        private ThingDef ringDef;

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
            if (ticksGame % 30 != 0)
            {
                return;
            }

            ringDef ??= DefDatabase<ThingDef>.GetNamedSilentFail("ABY_Mote_RuptureHaloRing");
            if (ringDef == null)
            {
                return;
            }

            HashSet<int> activePawnIds = new HashSet<int>();
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                Map map = maps[i];
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

                    int id = pawn.thingIDNumber;
                    activePawnIds.Add(id);

                    if (nextHaloRefreshTickByPawn.TryGetValue(id, out int nextTick) && ticksGame < nextTick)
                    {
                        continue;
                    }

                    RefreshHaloFor(pawn, ticksGame);
                    nextHaloRefreshTickByPawn[id] = ticksGame + 150;
                }
            }

            if (nextHaloRefreshTickByPawn.Count > 0)
            {
                List<int> stale = null;
                foreach (KeyValuePair<int, int> pair in nextHaloRefreshTickByPawn)
                {
                    if (!activePawnIds.Contains(pair.Key))
                    {
                        stale ??= new List<int>();
                        stale.Add(pair.Key);
                    }
                }

                if (stale != null)
                {
                    for (int i = 0; i < stale.Count; i++)
                    {
                        nextHaloRefreshTickByPawn.Remove(stale[i]);
                    }
                }
            }
        }

        private static bool HasCrownEquipped(Pawn pawn)
        {
            return pawn?.Spawned == true && pawn.equipment?.Primary != null && pawn.equipment.Primary.def?.defName == CrownDefName;
        }

        private void RefreshHaloFor(Pawn pawn, int ticksGame)
        {
            float pulse = (Mathf.Sin((ticksGame + pawn.thingIDNumber) * 0.03f) + 1f) * 0.5f;
            float scale = Mathf.Lerp(0.92f, 1.02f, pulse);
            MoteMaker.MakeAttachedOverlay(pawn, ringDef, Vector3.zero, scale, 3.20f);
        }
    }
}
