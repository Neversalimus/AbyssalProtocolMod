using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class Thing_ABY_CinderResiduePatch : Thing
    {
        private const int DefaultLifetimeTicks = 240;
        private const int DamageIntervalTicks = 30;
        private const int DamageAmount = 4;
        private const float DamageArmorPenetration = 0.10f;
        private const float DamageRadius = 1.05f;

        private int ticksLeft = DefaultLifetimeTicks;
        private Thing instigatorThing;

        public void Initialize(Thing instigator)
        {
            instigatorThing = instigator;
            ticksLeft = DefaultLifetimeTicks + Rand.RangeInclusive(-30, 50);
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (!respawningAfterLoad && ticksLeft <= 0)
            {
                ticksLeft = DefaultLifetimeTicks + Rand.RangeInclusive(-30, 50);
            }
        }

        protected override void Tick()
        {
            base.Tick();
            ticksLeft--;
            if (ticksLeft <= 0)
            {
                Destroy(DestroyMode.Vanish);
                return;
            }

            if (Spawned && Map != null && ticksLeft % DamageIntervalTicks == 0)
            {
                DamageStandingPawns();
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ticksLeft, "ticksLeft", DefaultLifetimeTicks);
            Scribe_References.Look(ref instigatorThing, "instigatorThing");
        }

        private void DamageStandingPawns()
        {
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(Position, DamageRadius, true))
            {
                if (!cell.InBounds(Map))
                {
                    continue;
                }

                System.Collections.Generic.List<Thing> things = cell.GetThingList(Map);
                for (int i = things.Count - 1; i >= 0; i--)
                {
                    Pawn pawn = things[i] as Pawn;
                    if (pawn == null || pawn.Dead || pawn.Destroyed || !pawn.Spawned)
                    {
                        continue;
                    }

                    DamageInfo dinfo = new DamageInfo(DamageDefOf.Burn, DamageAmount, DamageArmorPenetration, -1f, instigatorThing ?? this);
                    pawn.TakeDamage(dinfo);
                }
            }
        }
    }
}
