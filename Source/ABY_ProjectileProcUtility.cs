using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public static class ABY_ProjectileProcUtility
    {
        public static Hediff ApplyOrRefreshHediff(
            Pawn pawn,
            string hediffDefName,
            float severityGain = 0f,
            float minSeverity = 0.01f,
            float maxSeverity = 0.99f,
            int disappearsTicks = -1)
        {
            if (pawn?.health == null || hediffDefName.NullOrEmpty())
            {
                return null;
            }

            HediffDef hediffDef = DefDatabase<HediffDef>.GetNamedSilentFail(hediffDefName);
            if (hediffDef == null)
            {
                return null;
            }

            return ApplyOrRefreshHediff(pawn, hediffDef, severityGain, minSeverity, maxSeverity, disappearsTicks);
        }

        public static Hediff ApplyOrRefreshHediff(
            Pawn pawn,
            HediffDef hediffDef,
            float severityGain = 0f,
            float minSeverity = 0.01f,
            float maxSeverity = 0.99f,
            int disappearsTicks = -1)
        {
            if (pawn?.health == null || hediffDef == null)
            {
                return null;
            }

            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef);
            if (hediff == null)
            {
                hediff = HediffMaker.MakeHediff(hediffDef, pawn);
                if (hediff == null)
                {
                    return null;
                }
                pawn.health.AddHediff(hediff);
            }

            if (severityGain != 0f)
            {
                hediff.Severity = Mathf.Clamp(hediff.Severity + severityGain, minSeverity, maxSeverity);
            }
            else if (hediff.Severity < minSeverity)
            {
                hediff.Severity = minSeverity;
            }

            ResetDisappearTicks(hediff, disappearsTicks);
            pawn.health.hediffSet.DirtyCache();
            return hediff;
        }

        public static Hediff ApplyOrRefreshFixedHediff(Pawn pawn, string hediffDefName, float severity, int disappearsTicks = -1)
        {
            Hediff hediff = ApplyOrRefreshHediff(pawn, hediffDefName, 0f, Mathf.Max(0.001f, severity), 999f, disappearsTicks);
            if (hediff != null)
            {
                hediff.Severity = Mathf.Max(hediff.Severity, severity);
                ResetDisappearTicks(hediff, disappearsTicks);
                pawn.health.hediffSet.DirtyCache();
            }
            return hediff;
        }

        public static void RemoveHediff(Pawn pawn, Hediff hediff)
        {
            if (pawn?.health == null || hediff == null)
            {
                return;
            }

            if (pawn.health.hediffSet.hediffs.Contains(hediff))
            {
                pawn.health.RemoveHediff(hediff);
            }
        }

        public static void ApplyDamage(
            Thing target,
            DamageDef damageDef,
            float amount,
            float armorPenetration,
            Thing instigator,
            ThingDef weaponDef = null)
        {
            if (target == null || target.Destroyed || damageDef == null || amount <= 0f)
            {
                return;
            }

            DamageInfo damageInfo = new DamageInfo(
                damageDef,
                amount,
                armorPenetration,
                -1f,
                instigator,
                null,
                weaponDef,
                DamageInfo.SourceCategory.ThingOrUnknown);

            target.TakeDamage(damageInfo);
        }

        private static void ResetDisappearTicks(Hediff hediff, int disappearsTicks)
        {
            if (hediff == null || disappearsTicks <= 0)
            {
                return;
            }

            HediffComp_Disappears disappears = hediff.TryGetComp<HediffComp_Disappears>();
            if (disappears != null)
            {
                disappears.ticksToDisappear = disappearsTicks;
            }
        }
    }
}
