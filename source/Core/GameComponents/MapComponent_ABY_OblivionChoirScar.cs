using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class MapComponent_ABY_OblivionChoirScar : MapComponent
    {
        private const int DamageIntervalTicks = 30;
        private const float DefaultRadius = 2.65f;
        private const float DefaultDamage = 3.5f;
        private const float DefaultArmorPenetration = 0.20f;
        private const int DefaultLifetimeTicks = 210;
        private const int MaxActiveScars = 24;

        private List<ChoirScar> scars = new List<ChoirScar>();

        public MapComponent_ABY_OblivionChoirScar(Map map) : base(map)
        {
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();

            if (map == null || scars.Count == 0)
            {
                return;
            }

            int currentTick = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            for (int i = scars.Count - 1; i >= 0; i--)
            {
                ChoirScar scar = scars[i];
                if (scar == null || currentTick >= scar.expireTick || !scar.cell.IsValid || !scar.cell.InBounds(map))
                {
                    scars.RemoveAt(i);
                    continue;
                }

                if ((currentTick + scar.seed) % 9 == 0)
                {
                    SpawnScarVisuals(scar, currentTick);
                }

                if (currentTick >= scar.nextDamageTick)
                {
                    scar.nextDamageTick = currentTick + DamageIntervalTicks;
                    PulseScarDamage(scar);
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref scars, "oblivionChoirScars", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                scars.RemoveAll(s => s == null);
            }
        }

        public static void AddScar(Map map, IntVec3 cell, Thing instigator, float radius = DefaultRadius, int lifetimeTicks = DefaultLifetimeTicks)
        {
            if (map == null || !cell.IsValid || !cell.InBounds(map))
            {
                return;
            }

            MapComponent_ABY_OblivionChoirScar component = map.GetComponent<MapComponent_ABY_OblivionChoirScar>();
            if (component == null)
            {
                return;
            }

            int currentTick = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            component.PruneInvalidOrExpiredScars(currentTick);
            if (component.scars.Count >= MaxActiveScars)
            {
                component.RemoveOldestScar();
            }

            component.scars.Add(new ChoirScar
            {
                cell = cell,
                expireTick = currentTick + Mathf.Max(30, lifetimeTicks),
                nextDamageTick = currentTick + 8,
                radius = Mathf.Max(0.5f, radius),
                instigatorThingId = instigator != null ? instigator.thingIDNumber : -1,
                instigatorFactionDefName = instigator?.Faction?.def?.defName,
                instigatorWasAbyssal = instigator is Pawn instigatorPawn && ABY_FactionHostilityUtility.IsAbyssalPawn(instigatorPawn),
                seed = Rand.RangeInclusive(1, 999999)
            });
        }

        private void PulseScarDamage(ChoirScar scar)
        {
            if (scar == null || map == null)
            {
                return;
            }

            Thing instigator = ResolveInstigator(scar.instigatorThingId);
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(scar.cell, scar.radius, true))
            {
                if (!cell.InBounds(map))
                {
                    continue;
                }

                List<Thing> things = cell.GetThingList(map);
                for (int i = 0; i < things.Count; i++)
                {
                    Pawn pawn = things[i] as Pawn;
                    if (pawn == null || pawn.Dead || pawn.Destroyed || !pawn.Spawned)
                    {
                        continue;
                    }

                    if (!CanScarDamagePawn(scar, instigator, pawn))
                    {
                        continue;
                    }

                    float distance = Mathf.Max(0f, pawn.Position.DistanceTo(scar.cell));
                    float falloff = Mathf.Clamp01(1f - distance / Mathf.Max(0.1f, scar.radius + 0.15f));
                    if (falloff <= 0f)
                    {
                        continue;
                    }

                    float damage = DefaultDamage * (0.55f + falloff * 0.75f);
                    DamageInfo damageInfo = new DamageInfo(
                        DamageDefOf.Burn,
                        damage,
                        DefaultArmorPenetration,
                        -1f,
                        instigator,
                        null,
                        null,
                        DamageInfo.SourceCategory.ThingOrUnknown);
                    pawn.TakeDamage(damageInfo);
                    ABY_ProjectileProcUtility.ApplyOrRefreshHediff(pawn, "ABY_ChoirResonance", 0.08f, 0.08f, 1.00f, 300);
                }
            }
        }

        private void SpawnScarVisuals(ChoirScar scar, int currentTick)
        {
            if (scar == null || map == null)
            {
                return;
            }

            Vector3 center = scar.cell.ToVector3Shifted();
            float pulse = 0.55f + Mathf.Abs(Mathf.Sin((currentTick + scar.seed) * 0.07f)) * 0.42f;
            FleckMaker.ThrowLightningGlow(center, map, 0.85f * pulse);
            if ((currentTick + scar.seed) % 27 == 0)
            {
                FleckMaker.ThrowMicroSparks(center, map);
            }
        }

        private bool CanScarDamagePawn(ChoirScar scar, Thing instigator, Pawn pawn)
        {
            if (scar == null || pawn == null)
            {
                return false;
            }

            if (instigator != null)
            {
                return ABY_FactionHostilityUtility.SafeHostileTo(instigator, pawn);
            }

            if (scar.instigatorWasAbyssal)
            {
                return !ABY_FactionHostilityUtility.IsAbyssalPawn(pawn);
            }

            Faction faction = ResolveInstigatorFaction(scar.instigatorFactionDefName);
            return faction != null && ABY_FactionHostilityUtility.SafeHostileTo(faction, pawn);
        }

        private Thing ResolveInstigator(int thingId)
        {
            Thing thing;
            return ABY_RuntimeTargetCache.TryFindThingById(map, thingId, out thing) ? thing : null;
        }

        private Faction ResolveInstigatorFaction(string factionDefName)
        {
            if (factionDefName.NullOrEmpty() || Find.FactionManager?.AllFactionsListForReading == null)
            {
                return null;
            }

            List<Faction> factions = Find.FactionManager.AllFactionsListForReading;
            for (int i = 0; i < factions.Count; i++)
            {
                Faction faction = factions[i];
                if (string.Equals(faction?.def?.defName, factionDefName, System.StringComparison.OrdinalIgnoreCase))
                {
                    return faction;
                }
            }

            return null;
        }

        private void PruneInvalidOrExpiredScars(int currentTick)
        {
            for (int i = scars.Count - 1; i >= 0; i--)
            {
                ChoirScar scar = scars[i];
                if (scar == null || currentTick >= scar.expireTick || !scar.cell.IsValid || !scar.cell.InBounds(map))
                {
                    scars.RemoveAt(i);
                }
            }
        }

        private void RemoveOldestScar()
        {
            if (scars.Count == 0)
            {
                return;
            }

            int oldestIndex = 0;
            int oldestExpireTick = scars[0] != null ? scars[0].expireTick : int.MinValue;
            for (int i = 1; i < scars.Count; i++)
            {
                int expireTick = scars[i] != null ? scars[i].expireTick : int.MinValue;
                if (expireTick < oldestExpireTick)
                {
                    oldestExpireTick = expireTick;
                    oldestIndex = i;
                }
            }

            scars.RemoveAt(oldestIndex);
        }

        private class ChoirScar : IExposable
        {
            public IntVec3 cell;
            public int expireTick;
            public int nextDamageTick;
            public float radius;
            public int instigatorThingId;
            public string instigatorFactionDefName;
            public bool instigatorWasAbyssal;
            public int seed;

            public void ExposeData()
            {
                Scribe_Values.Look(ref cell, "cell");
                Scribe_Values.Look(ref expireTick, "expireTick");
                Scribe_Values.Look(ref nextDamageTick, "nextDamageTick");
                Scribe_Values.Look(ref radius, "radius", DefaultRadius);
                Scribe_Values.Look(ref instigatorThingId, "instigatorThingId", -1);
                Scribe_Values.Look(ref instigatorFactionDefName, "instigatorFactionDefName");
                Scribe_Values.Look(ref instigatorWasAbyssal, "instigatorWasAbyssal", false);
                Scribe_Values.Look(ref seed, "seed", 0);
            }
        }
    }
}
