using System;
using System.Linq;
using System.Reflection;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;

namespace AbyssalProtocol
{
    public class CompHexgunThrallShooter : ThingComp
    {
        private int nextSearchTick;
        private int nextReadyTick;
        private int warmupCompleteTick = -1;
        private int nextBurstShotTick = -1;
        private int nextWarmupTelegraphTick = -1;
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
            Scribe_Values.Look(ref nextWarmupTelegraphTick, "nextWarmupTelegraphTick", -1);
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
            if (TryMaintainSpacing(pawn))
            {
                ResetBurst();
                return;
            }

            if (TryPanicMelee(pawn))
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

                if (ShouldShowTargetLockFX(pawn, currentTarget) && ticksGame >= nextWarmupTelegraphTick)
                {
                    ShowTargetLockFX(pawn, currentTarget, false);
                    nextWarmupTelegraphTick = ticksGame + 12;
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
            Pawn target = FindBestTarget(pawn);
            if (target == null)
            {
                return;
            }

            currentTarget = target;
            warmupCompleteTick = ticksGame + Math.Max(1, Props.warmupTicks);
            nextWarmupTelegraphTick = ticksGame + 12;
            if (!Props.aimSoundDefName.NullOrEmpty())
            {
                ABY_SoundUtility.PlayChargeAt(Props.aimSoundDefName, pawn.Position, pawn.Map);
            }

            if (Props.holdPositionWhenTargeting)
            {
                pawn.pather?.StopDead();
            }

            pawn.rotationTracker?.FaceTarget(target.Position);
            if (ShouldShowTargetLockFX(pawn, target))
            {
                ShowTargetLockFX(pawn, target, true);
            }
        }

        public override string CompInspectStringExtra()
        {
            if (!IsSniperProfile() || warmupCompleteTick < 0 || currentTarget == null || Find.TickManager == null)
            {
                return null;
            }

            int totalWarmup = Math.Max(1, Props.warmupTicks);
            int remaining = Math.Max(0, warmupCompleteTick - Find.TickManager.TicksGame);
            int elapsed = Math.Max(0, totalWarmup - remaining);
            float progress = Mathf.Clamp01(elapsed / (float)totalWarmup);
            return "Target lock: " + progress.ToString("P0");
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

        private Pawn FindBestTarget(Pawn pawn)
        {
            float minimumTargetRange = Props.targetMinRange >= 0f
                ? Props.targetMinRange
                : Mathf.Max(0f, Props.preferredMinRange);

            return AbyssalThreatPawnUtility.FindBestTarget(
                pawn,
                minimumTargetRange,
                Props.range,
                Props.preferFarthestTargets,
                Props.preferRangedTargets,
                false,
                4.5f,
                0f);
        }

        private bool CanFireAt(Pawn shooter, Thing target)
        {
            Pawn targetPawn = target as Pawn;
            if (!AbyssalThreatPawnUtility.CanFireAt(shooter, targetPawn))
            {
                return false;
            }

            return shooter.Position.DistanceTo(targetPawn.Position) <= Props.range;
        }

        private bool TryMaintainSpacing(Pawn pawn)
        {
            return AbyssalThreatPawnUtility.TryMaintainSpacing(
                pawn,
                currentTarget,
                Props.preferredMinRange,
                Props.retreatSearchRadius,
                Props.holdPositionWhenTargeting);
        }

        private bool TryPanicMelee(Pawn pawn)
        {
            if (pawn == null || pawn.jobs == null || Props.panicMeleeRange <= 0f)
            {
                return false;
            }

            Pawn nearestThreat = AbyssalThreatPawnUtility.FindClosestThreatWithin(pawn, Props.panicMeleeRange);
            if (nearestThreat == null)
            {
                return false;
            }

            if (AbyssalThreatPawnUtility.TryFindRetreatCell(
                pawn,
                nearestThreat,
                Props.preferredMinRange,
                Props.retreatSearchRadius,
                out IntVec3 retreatCell) && retreatCell.IsValid && retreatCell != pawn.Position)
            {
                return false;
            }

            if (pawn.CurJob != null && pawn.CurJob.def == JobDefOf.AttackMelee && pawn.CurJob.targetA.Thing == nearestThreat)
            {
                return true;
            }

            currentTarget = nearestThreat;
            pawn.rotationTracker?.FaceTarget(nearestThreat.Position);

            Job meleeJob = JobMaker.MakeJob(JobDefOf.AttackMelee, nearestThreat);
            meleeJob.expiryInterval = Math.Max(60, Props.panicMeleeJobExpiryTicks);
            meleeJob.checkOverrideOnExpire = true;
            meleeJob.collideWithPawns = true;
            pawn.jobs.TryTakeOrderedJob(meleeJob, JobTag.Misc);
            return true;
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
                ABY_SoundUtility.PlayOneShotAt(Props.castSoundDefName, pawn.Position, pawn.Map);
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
                if (!TryBuildLaunchArgs(methods[i], pawn, target, out object[] args))
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

        private bool IsSniperProfile()
        {
            return string.Equals(Props.projectileDefName, "ABY_RiftSniperBolt", StringComparison.OrdinalIgnoreCase)
                || (Props.burstShotCount <= 1 && Props.warmupTicks >= 60 && Props.range >= 30f && Props.preferFarthestTargets);
        }

        private bool ShouldShowTargetLockFX(Pawn pawn, Thing target)
        {
            return IsSniperProfile()
                && pawn?.Map != null
                && target is Pawn targetPawn
                && targetPawn.Spawned
                && targetPawn.Map == pawn.Map
                && !targetPawn.Dead;
        }

        private void ShowTargetLockFX(Pawn pawn, Thing target, bool initial)
        {
            Pawn targetPawn = target as Pawn;
            if (pawn?.Map == null || targetPawn == null || !targetPawn.Spawned || targetPawn.Map != pawn.Map || targetPawn.Dead)
            {
                return;
            }

            FleckMaker.ThrowLightningGlow(targetPawn.DrawPos, pawn.Map, initial ? 1.35f : 0.75f);
            if (initial)
            {
                FleckMaker.Static(targetPawn.PositionHeld, pawn.Map, FleckDefOf.ExplosionFlash, 0.85f);
            }
        }

        private void ResetBurst()
        {
            currentTarget = null;
            warmupCompleteTick = -1;
            nextBurstShotTick = -1;
            nextWarmupTelegraphTick = -1;
            burstShotsRemaining = 0;
        }
    }
}
