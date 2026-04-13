using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class RuptureHaloGameComponent : GameComponent
    {
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

            if (ringDef == null)
            {
                ringDef = DefDatabase<ThingDef>.GetNamedSilentFail("ABY_Mote_RuptureHaloRing");
            }

            HediffDef bearerDef = RuptureCrownUtility.BearerHediffDef;
            AbilityDef abilityDef = RuptureCrownUtility.AbilityDef;
            if (ringDef == null || bearerDef == null || abilityDef == null)
            {
                return;
            }

            HashSet<int> activePawnIds = new HashSet<int>();
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                Map map = maps[i];
                if (map?.mapPawns == null)
                {
                    continue;
                }

                var pawns = map.mapPawns.AllPawnsSpawned;
                for (int j = 0; j < pawns.Count; j++)
                {
                    Pawn pawn = pawns[j];
                    if (pawn == null || pawn.health == null)
                    {
                        continue;
                    }

                    CompRuptureCrown crownComp = RuptureCrownUtility.GetWornCrownComp(pawn);
                    if (crownComp != null)
                    {
                        activePawnIds.Add(pawn.thingIDNumber);
                        EnsureBearerHediff(pawn, bearerDef);
                        SyncCooldownFromCrown(pawn, crownComp);

                        int nextTick;
                        if (!nextHaloRefreshTickByPawn.TryGetValue(pawn.thingIDNumber, out nextTick) || ticksGame >= nextTick)
                        {
                            RefreshHaloFor(pawn, ticksGame);
                            nextHaloRefreshTickByPawn[pawn.thingIDNumber] = ticksGame + 150;
                        }
                    }
                    else
                    {
                        RemoveBearerHediff(pawn, bearerDef);
                    }
                }
            }

            if (nextHaloRefreshTickByPawn.Count > 0)
            {
                List<int> stalePawnIds = null;
                foreach (KeyValuePair<int, int> pair in nextHaloRefreshTickByPawn)
                {
                    if (!activePawnIds.Contains(pair.Key))
                    {
                        if (stalePawnIds == null)
                        {
                            stalePawnIds = new List<int>();
                        }

                        stalePawnIds.Add(pair.Key);
                    }
                }

                if (stalePawnIds != null)
                {
                    for (int i = 0; i < stalePawnIds.Count; i++)
                    {
                        nextHaloRefreshTickByPawn.Remove(stalePawnIds[i]);
                    }
                }
            }
        }

        private static void EnsureBearerHediff(Pawn pawn, HediffDef bearerDef)
        {
            Hediff existing = pawn.health.hediffSet.GetFirstHediffOfDef(bearerDef);
            if (existing == null)
            {
                existing = HediffMaker.MakeHediff(bearerDef, pawn);
                pawn.health.AddHediff(existing);
            }
        }

        private static void RemoveBearerHediff(Pawn pawn, HediffDef bearerDef)
        {
            Hediff existing = pawn.health.hediffSet.GetFirstHediffOfDef(bearerDef);
            if (existing != null)
            {
                pawn.health.RemoveHediff(existing);
            }
        }

        private static void SyncCooldownFromCrown(Pawn pawn, CompRuptureCrown crownComp)
        {
            Ability ability = RuptureCrownUtility.GetGrantedAbility(pawn);
            if (ability == null)
            {
                return;
            }

            int remaining = crownComp.TicksUntilRecharged;
            if (remaining > 0)
            {
                int abilityRemaining = ability.CooldownTicksRemaining;
                if (abilityRemaining <= 0 || Mathf.Abs(abilityRemaining - remaining) > 120)
                {
                    ability.StartCooldown(remaining);
                }
            }
            else if (ability.OnCooldown)
            {
                ability.ResetCooldown();
            }
        }

        private void RefreshHaloFor(Pawn pawn, int ticksGame)
        {
            float pulse = (Mathf.Sin((ticksGame + pawn.thingIDNumber) * 0.03f) + 1f) * 0.5f;
            float scale = Mathf.Lerp(0.92f, 1.02f, pulse);
            MoteMaker.MakeAttachedOverlay(pawn, ringDef, Vector3.zero, scale, 3.20f);
        }
    }
}
