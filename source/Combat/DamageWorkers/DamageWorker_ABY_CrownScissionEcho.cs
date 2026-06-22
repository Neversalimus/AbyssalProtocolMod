using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    /// <summary>
    /// Zero-damage extra-melee trigger for Crown Scission Array. The direct base melee hit is still
    /// owned by RimWorld; this worker only spends previously stored echoes and records direct kills.
    /// </summary>
    public class DamageWorker_ABY_CrownScissionEcho : DamageWorker
    {
        public override DamageResult Apply(DamageInfo dinfo, Thing victim)
        {
            DamageResult result = new DamageResult();

            Pawn primaryTarget = victim as Pawn;
            Pawn wielder = dinfo.Instigator as Pawn;
            if (primaryTarget == null || !IsValidWielder(wielder))
            {
                return result;
            }

            CompABY_CrownScissionArray array = ResolveArray(wielder, dinfo.Weapon);
            if (array == null)
            {
                return result;
            }

            Pawn echoTarget;
            if (array.TryConsumeEcho(wielder, primaryTarget, out echoTarget))
            {
                ApplyEchoSeverance(array, wielder, echoTarget, dinfo);
            }

            // Extra melee damage is processed after the tool's direct hit. A dead victim here is a
            // confirmed direct kill from this weapon, not damage from an echo (which uses vanilla Cut).
            if (array.TryStoreDirectKill(wielder, primaryTarget))
            {
                ShowChargeFeedback(array, primaryTarget, wielder.MapHeld);
            }

            return result;
        }

        private static CompABY_CrownScissionArray ResolveArray(Pawn wielder, ThingDef sourceWeaponDef)
        {
            if (wielder?.equipment == null || sourceWeaponDef == null)
            {
                return null;
            }

            ThingWithComps primary = wielder.equipment.Primary;
            if (primary == null || primary.def != sourceWeaponDef)
            {
                return null;
            }

            return primary.GetComp<CompABY_CrownScissionArray>();
        }

        private static void ApplyEchoSeverance(
            CompABY_CrownScissionArray array,
            Pawn wielder,
            Pawn echoTarget,
            DamageInfo sourceDamage)
        {
            if (array == null
                || echoTarget == null
                || echoTarget.Dead
                || echoTarget.Destroyed
                || echoTarget.health == null)
            {
                return;
            }

            CompProperties_ABY_CrownScissionArray props = array.Props;
            float damageAmount = Mathf.Max(1f, props.echoDamage);
            float armorPenetration = Mathf.Max(0f, props.echoArmorPenetration);
            Map map = echoTarget.MapHeld;
            Vector3 drawPos = echoTarget.DrawPos;
            IntVec3 impactCell = echoTarget.PositionHeld;

            echoTarget.TakeDamage(new DamageInfo(
                DamageDefOf.Cut,
                damageAmount,
                armorPenetration,
                -1f,
                wielder,
                null,
                sourceDamage.Weapon,
                DamageInfo.SourceCategory.ThingOrUnknown,
                echoTarget));

            if (map == null)
            {
                return;
            }

            FleckMaker.ThrowLightningGlow(drawPos, map, Mathf.Max(0.20f, props.echoVisualScale));
            FleckMaker.ThrowMicroSparks(drawPos, map);
            FleckMaker.Static(
                impactCell,
                map,
                FleckDefOf.ExplosionFlash,
                Mathf.Max(0.15f, props.echoFlashScale));

            if (!props.echoSoundDefName.NullOrEmpty())
            {
                ABY_SoundUtility.PlayAt(props.echoSoundDefName, impactCell, map);
            }
        }

        private static void ShowChargeFeedback(CompABY_CrownScissionArray array, Pawn target, Map fallbackMap)
        {
            Map map = target?.MapHeld ?? fallbackMap;
            if (map == null)
            {
                return;
            }

            Vector3 drawPos = target != null ? target.DrawPos : Vector3.zero;
            FleckMaker.ThrowLightningGlow(drawPos, map, Mathf.Max(0.15f, array.Props.chargeVisualScale));
        }

        private static bool IsValidWielder(Pawn pawn)
        {
            return pawn != null
                && !pawn.Dead
                && !pawn.Destroyed
                && pawn.equipment != null
                && pawn.MapHeld != null;
        }
    }
}
