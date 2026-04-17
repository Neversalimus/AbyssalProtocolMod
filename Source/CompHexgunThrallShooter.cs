using System;
using System.Linq;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class CompHexgunThrallShooter : ThingComp
    {
        private int nextSearchTick;
        private int nextReadyTick;
        private int warmupCompleteTick = -1;
        private int nextBurstShotTick = -1;
        private int burstShotsRemaining;
        private Thing currentTarget;

        private CompProperties_HexgunThrallShooter Props => (CompProperties_HexgunThrallShooter)props;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref nextSearchTick, "nextSearchTick", 0);
            Scribe_Values.Look(ref nextReadyTick, "nextReadyTick", 0);
            Scribe_Values.Look(ref warmupCompleteTick, "warmupCompleteTick", -1);
            Scribe_Values.Look(ref nextBurstShotTick, "nextBurstShotTick", -1);
            Scribe_Values.Look(ref burstShotsRemaining, "burstShotsRemaining", 0);
            Scribe_References.Look(ref currentTarget, "currentTarget");
        }

        public override void CompTick()
        {
            base.CompTick();

            Pawn pawn = parent as Pawn;
            if (!CanOperate(pawn))
            {
                ResetBurst();
                return;
            }

            int ticksGame = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            if (AbyssalThreatPawnUtility.TryMaintainSpacing(
                pawn,
                GetPreferredMinRange(pawn),
                GetRetreatSearchRadius(pawn),
                currentTarget,
                GetHoldPositionWhenTargeting(pawn)))
            {
                ResetBurst();
                return;
            }

            if (burstShotsRemaining > 0)
            {
                if (!CanFireAt(pawn, currentTarget))
                {
                    ResetBurst();
                    return;
                }

                if (ticksGame >= nextBurstShotTick)
                {
                    FireShot(pawn, currentTarget);
                    burstShotsRemaining--;
                    if (burstShotsRemaining > 0)
                    {
                        nextBurstShotTick = ticksGame + Math.Max(1, Props.ticksBetweenBurstShots);
                    }
                    else
                    {
                        currentTarget = null;
                        nextReadyTick = ticksGame + Math.Max(1, Props.cooldownTicks);
                        nextBurstShotTick = -1;
                    }
                }

                return;
            }

            if (warmupCompleteTick >= 0)
            {
                if (!CanFireAt(pawn, currentTarget))
                {
                    ResetBurst();
                    return;
                }

                if (ticksGame >= warmupCompleteTick)
                {
                    warmupCompleteTick = -1;
                    FireShot(pawn, currentTarget);
                    burstShotsRemaining = Math.Max(0, Props.burstShotCount - 1);
                    if (burstShotsRemaining > 0)
                    {
                        nextBurstShotTick = ticksGame + Math.Max(1, Props.ticksBetweenBurstShots);
                    }
                    else
                    {
                        currentTarget = null;
                        nextReadyTick = ticksGame + Math.Max(1, Props.cooldownTicks);
                    }
                }

                return;
            }

            if (ticksGame < nextReadyTick || ticksGame < nextSearchTick)
            {
                return;
            }

            nextSearchTick = ticksGame + Math.Max(5, Props.scanIntervalTicks);
            Pawn target = AbyssalThreatPawnUtility.FindBestTarget(
                pawn,
                Props.range,
                GetPreferRangedTargets(pawn),
                GetPreferFarthestTargets(pawn),
                requireRangedTarget: false);
            if (target == null)
            {
                return;
            }

            currentTarget = target;
            warmupCompleteTick = ticksGame + Math.Max(1, Props.warmupTicks);
            if (!Props.aimSoundDefName.NullOrEmpty())
            {
                ABY_SoundUtility.PlayAt(Props.aimSoundDefName, pawn.Position, pawn.Map);
            }

            if (GetHoldPositionWhenTargeting(pawn))
            {
                pawn.pather?.StopDead();
            }

            pawn.rotationTracker?.FaceTarget(target.Position);
        }

        private bool CanOperate(Pawn pawn)
        {
            if (pawn == null || pawn.Map == null || pawn.Dead || !pawn.Spawned || pawn.Downed)
            {
                return false;
            }

            if (pawn.Faction == null || Faction.OfPlayer == null || !pawn.Faction.HostileTo(Faction.OfPlayer))
            {
                return false;
            }

            return true;
        }

        private bool CanFireAt(Pawn shooter, Thing target)
        {
            return AbyssalThreatPawnUtility.CanFireAt(shooter, target, Props.range);
        }

        private float GetPreferredMinRange(Pawn pawn)
        {
            CompAbyssalPawnController controller = AbyssalThreatPawnUtility.GetController(pawn);
            if (controller != null && controller.Props.preferredMinRange > 0f)
            {
                return controller.Props.preferredMinRange;
            }

            return Props.preferredMinRange;
        }

        private int GetRetreatSearchRadius(Pawn pawn)
        {
            CompAbyssalPawnController controller = AbyssalThreatPawnUtility.GetController(pawn);
            if (controller != null && controller.Props.retreatSearchRadius > 0)
            {
                return controller.Props.retreatSearchRadius;
            }

            return Props.retreatSearchRadius;
        }

        private bool GetPreferRangedTargets(Pawn pawn)
        {
            CompAbyssalPawnController controller = AbyssalThreatPawnUtility.GetController(pawn);
            if (controller != null)
            {
                return controller.Props.preferRangedTargets;
            }

            return Props.preferRangedTargets;
        }

        private bool GetPreferFarthestTargets(Pawn pawn)
        {
            CompAbyssalPawnController controller = AbyssalThreatPawnUtility.GetController(pawn);
            if (controller != null)
            {
                return controller.Props.preferFarthestTargets;
            }

            return Props.preferFarthestTargets;
        }

        private bool GetHoldPositionWhenTargeting(Pawn pawn)
        {
            CompAbyssalPawnController controller = AbyssalThreatPawnUtility.GetController(pawn);
            if (controller != null)
            {
                return controller.Props.holdPositionWhenTargeting;
            }

            return Props.holdPositionWhenTargeting;
        }

        private void FireShot(Pawn pawn, Thing target)
        {
            if (pawn == null || target == null)
            {
                return;
            }

            pawn.rotationTracker?.FaceTarget(target.Position);
            if (!Props.castSoundDefName.NullOrEmpty())
            {
                ABY_SoundUtility.PlayAt(Props.castSoundDefName, pawn.Position, pawn.Map);
            }

            ThingDef projectileDef = DefDatabase<ThingDef>.GetNamedSilentFail(Props.projectileDefName);
            if (projectileDef == null)
            {
                return;
            }

            Projectile projectile = GenSpawn.Spawn(projectileDef, pawn.Position, pawn.Map, WipeMode.Vanish) as Projectile;
            if (projectile == null)
            {
                return;
            }

            if (!TryLaunchProjectile(projectile, pawn, target))
            {
                projectile.Destroy(DestroyMode.Vanish);
            }
        }

        private bool TryLaunchProjectile(Projectile projectile, Pawn pawn, Thing target)
        {
            MethodInfo[] methods = projectile.GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(m => m.Name == "Launch")
                .OrderByDescending(m => m.GetParameters().Length)
                .ToArray();

            for (int i = 0; i < methods.Length; i++)
            {
                object[] args;
                if (!TryBuildLaunchArgs(methods[i], pawn, target, out args))
                {
                    continue;
                }

                try
                {
                    methods[i].Invoke(projectile, args);
                    return true;
                }
                catch
                {
                }
            }

            return false;
        }

        private bool TryBuildLaunchArgs(MethodInfo method, Pawn pawn, Thing target, out object[] args)
        {
            ParameterInfo[] parameters = method.GetParameters();
            args = new object[parameters.Length];
            int thingSlot = 0;
            LocalTargetInfo targetInfo = new LocalTargetInfo(target);

            for (int i = 0; i < parameters.Length; i++)
            {
                Type parameterType = parameters[i].ParameterType;
                if (typeof(Thing).IsAssignableFrom(parameterType))
                {
                    args[i] = thingSlot == 0 ? (object)pawn : null;
                    thingSlot++;
                    continue;
                }

                if (parameterType == typeof(Vector3))
                {
                    args[i] = pawn.DrawPos;
                    continue;
                }

                if (parameterType == typeof(LocalTargetInfo))
                {
                    args[i] = targetInfo;
                    continue;
                }

                if (parameterType == typeof(ProjectileHitFlags))
                {
                    args[i] = ProjectileHitFlags.IntendedTarget;
                    continue;
                }

                if (parameterType == typeof(bool))
                {
                    args[i] = false;
                    continue;
                }

                if (parameterType == typeof(ThingDef))
                {
                    args[i] = null;
                    continue;
                }

                if (parameterType.IsEnum)
                {
                    args[i] = Activator.CreateInstance(parameterType);
                    continue;
                }

                args = null;
                return false;
            }

            return true;
        }

        private void ResetBurst()
        {
            currentTarget = null;
            warmupCompleteTick = -1;
            nextBurstShotTick = -1;
            burstShotsRemaining = 0;
        }
    }
}
