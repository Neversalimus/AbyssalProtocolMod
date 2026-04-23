using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace AbyssalProtocol
{
    public class Thing_CrownshardStormNode : Thing
    {
        private const int DefaultDurationTicks = 230;
        private const int ShieldDampenedDurationTicks = 160;
        private const int PulseIntervalTicks = 20;
        private const int MaxTargetsPerPulse = 3;
        private const float Radius = 3.35f;
        private const float PulseDamage = 7.0f;
        private const float PulseArmorPenetration = 0.48f;
        private const float MechOrBuildingDamageMultiplier = 1.30f;

        private Thing launcher;
        private ThingDef weaponDef;
        private int ageTicks;
        private int durationTicks = DefaultDurationTicks;
        private int nextPulseTick = 8;
        private bool shieldDampened;

        public static Thing_CrownshardStormNode SpawnStorm(IntVec3 cell, Map map, Thing launcher, ThingDef weaponDef, bool shieldDampened)
        {
            if (map == null || !cell.InBounds(map))
            {
                return null;
            }

            ThingDef nodeDef = DefDatabase<ThingDef>.GetNamedSilentFail("ABY_CrownshardStormNode");
            if (nodeDef == null)
            {
                return null;
            }

            Thing_CrownshardStormNode node = ThingMaker.MakeThing(nodeDef) as Thing_CrownshardStormNode;
            if (node == null)
            {
                return null;
            }

            node.Initialize(launcher, weaponDef, shieldDampened);
            GenSpawn.Spawn(node, cell, map, WipeMode.Vanish);
            CrownshardStormVfxUtility.SpawnStormOpen(cell, map, shieldDampened);
            return node;
        }

        public void Initialize(Thing newLauncher, ThingDef newWeaponDef, bool newShieldDampened)
        {
            launcher = newLauncher;
            weaponDef = newWeaponDef;
            shieldDampened = newShieldDampened;
            durationTicks = shieldDampened ? ShieldDampenedDurationTicks : DefaultDurationTicks;
            nextPulseTick = shieldDampened ? 12 : 8;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref launcher, "launcher");
            Scribe_Defs.Look(ref weaponDef, "weaponDef");
            Scribe_Values.Look(ref ageTicks, "ageTicks", 0);
            Scribe_Values.Look(ref durationTicks, "durationTicks", DefaultDurationTicks);
            Scribe_Values.Look(ref nextPulseTick, "nextPulseTick", 8);
            Scribe_Values.Look(ref shieldDampened, "shieldDampened", false);
        }

        protected override void Tick()
        {
            base.Tick();

            if (Map == null)
            {
                return;
            }

            ageTicks++;
            if (ageTicks >= durationTicks)
            {
                Destroy(DestroyMode.Vanish);
                return;
            }

            if (ageTicks >= nextPulseTick)
            {
                nextPulseTick = ageTicks + PulseIntervalTicks;
                DoPulse();
            }
        }

        private void DoPulse()
        {
            List<Thing> targets = GatherTargets();
            if (targets.Count == 0)
            {
                CrownshardStormVfxUtility.SpawnIdlePulse(Position, Map, shieldDampened);
                return;
            }

            CrownshardStormVfxUtility.SpawnPulseCore(Position, Map, shieldDampened);

            for (int i = 0; i < targets.Count; i++)
            {
                ApplyPulseDamage(targets[i]);
            }
        }

        private List<Thing> GatherTargets()
        {
            List<Thing> targets = new List<Thing>();

            foreach (Thing thing in GenRadial.RadialDistinctThingsAround(Position, Map, Radius, true))
            {
                if (!CanAffectTarget(thing))
                {
                    continue;
                }

                targets.Add(thing);
            }

            targets.Sort((a, b) => ScoreTarget(b).CompareTo(ScoreTarget(a)));

            if (targets.Count > MaxTargetsPerPulse)
            {
                targets.RemoveRange(MaxTargetsPerPulse, targets.Count - MaxTargetsPerPulse);
            }

            return targets;
        }

        private bool CanAffectTarget(Thing thing)
        {
            if (thing == null || thing == this || thing.Destroyed || !thing.Spawned || thing.Map != Map)
            {
                return false;
            }

            if (!IsHostileToLauncher(thing))
            {
                return false;
            }

            Pawn pawn = thing as Pawn;
            if (pawn != null)
            {
                return !pawn.Dead && !pawn.Downed;
            }

            if (thing.def == null || thing.def.category != ThingCategory.Building || !thing.def.useHitPoints)
            {
                return false;
            }

            return thing.HitPoints > 0;
        }

        private bool IsHostileToLauncher(Thing thing)
        {
            Faction sourceFaction = launcher != null ? launcher.Faction : null;
            if (sourceFaction == null)
            {
                sourceFaction = Faction.OfPlayer;
            }

            return thing.Faction != null && thing.Faction.HostileTo(sourceFaction);
        }

        private int ScoreTarget(Thing thing)
        {
            int score = 0;
            Pawn pawn = thing as Pawn;
            if (pawn != null)
            {
                score += 100;
                if (IsMechanoidPawn(pawn))
                {
                    score += 35;
                }
            }
            else if (thing.def != null && thing.def.category == ThingCategory.Building)
            {
                score += 65;
            }

            int dx = thing.Position.x - Position.x;
            int dz = thing.Position.z - Position.z;
            score -= dx * dx + dz * dz;
            return score;
        }

        private void ApplyPulseDamage(Thing target)
        {
            if (target == null || target.Destroyed)
            {
                return;
            }

            Pawn pawn = target as Pawn;
            bool wasAlivePawn = pawn != null && !pawn.Dead;
            bool wasDestroyed = target.Destroyed;
            bool denseTarget = IsDenseTarget(target);

            float damageAmount = PulseDamage;
            if (denseTarget)
            {
                damageAmount *= MechOrBuildingDamageMultiplier;
            }

            DamageInfo damageInfo = new DamageInfo(
                DamageDefOf.Cut,
                damageAmount,
                PulseArmorPenetration,
                -1f,
                launcher,
                null,
                weaponDef);

            target.TakeDamage(damageInfo);
            CrownshardStormVfxUtility.SpawnShardImpact(Position, target, denseTarget);

            if ((!wasDestroyed && target.Destroyed) || (pawn != null && wasAlivePawn && pawn.Dead))
            {
                CrownshardStormVfxUtility.SpawnExecutionFlare(target.Position, Map);
            }
        }

        private static bool IsDenseTarget(Thing target)
        {
            Pawn pawn = target as Pawn;
            if (pawn != null)
            {
                return IsMechanoidPawn(pawn);
            }

            return target.def != null && target.def.category == ThingCategory.Building;
        }

        private static bool IsMechanoidPawn(Pawn pawn)
        {
            return pawn != null && pawn.RaceProps != null && pawn.RaceProps.IsMechanoid;
        }
    }
}
