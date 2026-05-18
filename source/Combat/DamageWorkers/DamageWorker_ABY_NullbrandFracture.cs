using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class DefModExtension_NullbrandFracture : DefModExtension
    {
        public string hediffDefName = "ABY_NullbrandFracture";
        public float fractureChance = 0.25f;
        public int durationTicks = 360;
        public float fractureVisualScale = 0.52f;
        public float shearChance = 0.34f;
        public float shearDamage = 8f;
        public float shearArmorPenetration = 0.82f;
        public int shearCooldownTicks = 100;
        public float shearVisualScale = 0.82f;
    }

    public class DamageWorker_ABY_NullbrandFracture : DamageWorker
    {
        private static readonly Dictionary<int, int> nextShearTickByInstigator = new Dictionary<int, int>();

        public override DamageResult Apply(DamageInfo dinfo, Thing victim)
        {
            DamageResult result = new DamageResult();
            Pawn pawn = victim as Pawn;
            if (pawn == null || pawn.Dead || pawn.health == null)
            {
                return result;
            }

            DefModExtension_NullbrandFracture extension = def?.GetModExtension<DefModExtension_NullbrandFracture>() ?? new DefModExtension_NullbrandFracture();
            HediffDef hediffDef = DefDatabase<HediffDef>.GetNamedSilentFail(extension.hediffDefName ?? "ABY_NullbrandFracture");
            if (hediffDef == null)
            {
                return result;
            }

            bool hadFractureBeforeHit = pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef) != null;
            if (hadFractureBeforeHit)
            {
                TryApplyNullShear(pawn, dinfo, extension);
                if (pawn.Dead || pawn.Destroyed)
                {
                    return result;
                }
            }

            float fractureChance = Mathf.Clamp01(extension.fractureChance);
            if (fractureChance <= 0f || !Rand.Chance(fractureChance))
            {
                return result;
            }

            ApplyOrRefreshFracture(pawn, hediffDef, extension);
            return result;
        }

        private static void ApplyOrRefreshFracture(Pawn pawn, HediffDef hediffDef, DefModExtension_NullbrandFracture extension)
        {
            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef);
            if (hediff == null)
            {
                hediff = HediffMaker.MakeHediff(hediffDef, pawn);
                if (hediff == null)
                {
                    return;
                }

                pawn.health.AddHediff(hediff);
            }

            hediff.Severity = Mathf.Max(hediff.Severity, 1f);
            HediffComp_Disappears disappears = hediff.TryGetComp<HediffComp_Disappears>();
            if (disappears != null)
            {
                disappears.ticksToDisappear = Mathf.Max(60, extension.durationTicks);
            }

            pawn.health.hediffSet.DirtyCache();

            if (pawn.Spawned && pawn.MapHeld != null && extension.fractureVisualScale > 0f)
            {
                FleckMaker.ThrowLightningGlow(pawn.DrawPos, pawn.MapHeld, extension.fractureVisualScale);
            }
        }

        private static void TryApplyNullShear(Pawn pawn, DamageInfo sourceDamage, DefModExtension_NullbrandFracture extension)
        {
            float shearChance = Mathf.Clamp01(extension.shearChance);
            if (shearChance <= 0f || extension.shearDamage <= 0f || !Rand.Chance(shearChance))
            {
                return;
            }

            int currentTick = Find.TickManager?.TicksGame ?? 0;
            Thing instigator = sourceDamage.Instigator;
            int cooldownKey = instigator != null ? instigator.thingIDNumber : pawn.thingIDNumber;
            if (nextShearTickByInstigator.TryGetValue(cooldownKey, out int nextTick) && currentTick < nextTick)
            {
                return;
            }

            nextShearTickByInstigator[cooldownKey] = currentTick + Mathf.Max(1, extension.shearCooldownTicks);
            PruneCooldownCache(currentTick);

            Map map = pawn.MapHeld;
            Vector3 drawPos = pawn.DrawPos;
            pawn.TakeDamage(new DamageInfo(
                DamageDefOf.Cut,
                extension.shearDamage,
                Mathf.Max(0f, extension.shearArmorPenetration),
                -1f,
                instigator,
                null,
                sourceDamage.Weapon,
                DamageInfo.SourceCategory.ThingOrUnknown));

            if (map != null && extension.shearVisualScale > 0f)
            {
                FleckMaker.ThrowLightningGlow(drawPos, map, extension.shearVisualScale);
                FleckMaker.ThrowMicroSparks(drawPos, map);
            }
        }

        private static void PruneCooldownCache(int currentTick)
        {
            if (nextShearTickByInstigator.Count <= 512)
            {
                return;
            }

            List<int> staleKeys = null;
            foreach (KeyValuePair<int, int> entry in nextShearTickByInstigator)
            {
                if (entry.Value < currentTick - 600)
                {
                    if (staleKeys == null)
                    {
                        staleKeys = new List<int>();
                    }

                    staleKeys.Add(entry.Key);
                }
            }

            if (staleKeys == null)
            {
                return;
            }

            for (int i = 0; i < staleKeys.Count; i++)
            {
                nextShearTickByInstigator.Remove(staleKeys[i]);
            }
        }
    }
}
