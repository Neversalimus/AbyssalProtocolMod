using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace AbyssalProtocol
{
    [HarmonyPatch(typeof(Verb_Shoot), "TryCastShot")]
    public static class HarmonyPatch_SpecterLashProjector
    {
        private const float MaxRandomMissRadius = 1.65f;

        public static bool Prefix(Verb_Shoot __instance, ref bool __result)
        {
            if (!ShouldReplaceProjectile(__instance))
            {
                return true;
            }

            __result = TryFireBeam(__instance);
            return false;
        }

        private static bool ShouldReplaceProjectile(Verb_Shoot verb)
        {
            if (verb == null)
            {
                return false;
            }

            if (!SpecterLashStreamGameComponent.IsSpecterLashWeapon(verb.EquipmentSource))
            {
                return false;
            }

            Pawn casterPawn = verb.CasterPawn;
            return casterPawn != null && casterPawn.Spawned && casterPawn.MapHeld != null && !casterPawn.Dead && !casterPawn.Downed;
        }

        private static bool TryFireBeam(Verb_Shoot verb)
        {
            Pawn caster = verb.CasterPawn;
            Map map = caster?.MapHeld;
            if (caster == null || map == null)
            {
                return false;
            }

            LocalTargetInfo targetInfo = verb.CurrentTarget;
            if (!targetInfo.IsValid)
            {
                return false;
            }

            if (!verb.CanHitTarget(targetInfo))
            {
                StartMissBeam(caster, targetInfo, false);
                PlayCastSound(verb, caster, map);
                return true;
            }

            Thing targetThing = targetInfo.Thing;
            Vector3 intendedPos = ResolveTargetPosition(targetInfo, map);
            bool canDamage = targetThing != null && RollHitChance(verb, targetInfo);

            SpecterLashStreamGameComponent component = Current.Game != null ? Current.Game.GetComponent<SpecterLashStreamGameComponent>() : null;
            if (component == null)
            {
                return false;
            }

            if (canDamage && targetThing != null && !targetThing.Destroyed)
            {
                component.TryStartStream(caster, targetThing, intendedPos, true, true);
            }
            else
            {
                Vector3 missPoint = GetMissPoint(caster, targetInfo, intendedPos, map);
                component.TryStartStreamToPoint(caster, missPoint, false);
            }

            PlayCastSound(verb, caster, map);
            ThrowMuzzleFlash(caster, intendedPos, map);
            return true;
        }

        private static Vector3 ResolveTargetPosition(LocalTargetInfo targetInfo, Map map)
        {
            if (targetInfo.HasThing && targetInfo.Thing != null && !targetInfo.Thing.Destroyed)
            {
                return targetInfo.Thing.DrawPos;
            }

            if (targetInfo.Cell.IsValid)
            {
                return targetInfo.Cell.ToVector3Shifted();
            }

            return Vector3.zero;
        }

        private static bool RollHitChance(Verb_Shoot verb, LocalTargetInfo targetInfo)
        {
            if (!targetInfo.HasThing)
            {
                return false;
            }

            float hitChance = 1f;
            try
            {
                ShotReport report = ShotReport.HitReportFor(verb.Caster, verb, targetInfo);
                hitChance = Mathf.Clamp01(report.TotalEstimatedHitChance);
            }
            catch
            {
                hitChance = 0.92f;
            }

            return Rand.Chance(hitChance);
        }

        private static Vector3 GetMissPoint(Pawn caster, LocalTargetInfo targetInfo, Vector3 intendedPos, Map map)
        {
            Vector3 missPoint = intendedPos;
            float missRadius = Mathf.Min(MaxRandomMissRadius, Mathf.Max(0.55f, verbForcedMissRadius(targetInfo)));
            Vector2 offset = Rand.InsideUnitCircle.normalized * Rand.Range(0.45f, missRadius);
            missPoint += new Vector3(offset.x, 0f, offset.y);

            IntVec3 missCell = missPoint.ToIntVec3();
            if (!missCell.IsValid || !missCell.InBounds(map))
            {
                missPoint = intendedPos;
            }

            return missPoint;
        }

        private static float verbForcedMissRadius(LocalTargetInfo targetInfo)
        {
            return targetInfo.HasThing ? 1.15f : 0.65f;
        }

        private static void StartMissBeam(Pawn caster, LocalTargetInfo targetInfo, bool blockedByShield)
        {
            Map map = caster?.MapHeld;
            if (map == null)
            {
                return;
            }

            Vector3 targetPos = ResolveTargetPosition(targetInfo, map);
            if (targetPos == Vector3.zero)
            {
                targetPos = caster.DrawPos;
            }

            SpecterLashStreamGameComponent component = Current.Game != null ? Current.Game.GetComponent<SpecterLashStreamGameComponent>() : null;
            component?.TryStartStreamToPoint(caster, targetPos, blockedByShield);
        }

        private static void PlayCastSound(Verb_Shoot verb, Pawn caster, Map map)
        {
            if (verb?.verbProps?.soundCast != null && caster != null && map != null)
            {
                verb.verbProps.soundCast.PlayOneShot(SoundInfo.InMap(new TargetInfo(caster.PositionHeld, map, false), MaintenanceType.None));
            }
        }

        private static void ThrowMuzzleFlash(Pawn caster, Vector3 targetPos, Map map)
        {
            if (caster == null || map == null)
            {
                return;
            }

            Vector3 pos = caster.DrawPos;
            Vector3 direction = targetPos - pos;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.0001f)
            {
                direction.Normalize();
                pos += direction * 0.55f;
            }

            FleckMaker.ThrowLightningGlow(pos, map, 0.52f);
        }
    }
}
