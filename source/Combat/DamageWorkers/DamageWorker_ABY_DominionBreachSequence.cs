using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class DamageWorker_ABY_DominionBreachSequence : DamageWorker
    {
        public override DamageResult Apply(DamageInfo dinfo, Thing victim)
        {
            DamageResult result = new DamageResult();

            Pawn target = victim as Pawn;
            Pawn wielder = dinfo.Instigator as Pawn;
            if (!IsValidPawnTarget(target) || !IsValidWielder(wielder))
            {
                return result;
            }

            CompABY_DominionBreachDriver driver = ResolveDriver(wielder, dinfo.Weapon);
            if (driver == null)
            {
                return result;
            }

            int registeredHitCount;
            bool sentenceTriggered = driver.TryRegisterPressureHit(wielder, target, out registeredHitCount);
            if (sentenceTriggered)
            {
                ApplyStructuralSentence(driver, wielder, target, dinfo);
            }
            else if (registeredHitCount >= 2)
            {
                ShowPressureFeedback(driver, target);
            }

            return result;
        }

        private static CompABY_DominionBreachDriver ResolveDriver(Pawn wielder, ThingDef sourceWeaponDef)
        {
            if (wielder?.equipment == null || sourceWeaponDef == null)
            {
                return null;
            }

            ThingWithComps primary = wielder.equipment.Primary;
            if (primary == null)
            {
                return null;
            }

            if (sourceWeaponDef != null && primary.def != sourceWeaponDef)
            {
                return null;
            }

            return primary.GetComp<CompABY_DominionBreachDriver>();
        }

        private static void ShowPressureFeedback(CompABY_DominionBreachDriver driver, Pawn target)
        {
            Map map = target?.MapHeld;
            if (map == null || target.Dead || target.Destroyed)
            {
                return;
            }

            float scale = Mathf.Max(0.15f, driver.Props.pressureVisualScale);
            FleckMaker.ThrowLightningGlow(target.DrawPos, map, scale);
        }

        private static void ApplyStructuralSentence(
            CompABY_DominionBreachDriver driver,
            Pawn wielder,
            Pawn target,
            DamageInfo sourceDamage)
        {
            if (driver == null || !IsValidPawnTarget(target) || !IsValidWielder(wielder))
            {
                return;
            }

            CompProperties_ABY_DominionBreachDriver props = driver.Props;
            float damageAmount = Mathf.Max(1f, props.sentenceDamage);
            float armorPenetration = Mathf.Max(0f, props.sentenceArmorPenetration);

            DamageInfo verdictDamage = new DamageInfo(
                DamageDefOf.Blunt,
                damageAmount,
                armorPenetration,
                -1f,
                wielder,
                null,
                sourceDamage.Weapon,
                DamageInfo.SourceCategory.ThingOrUnknown,
                target);

            target.TakeDamage(verdictDamage);

            Map map = target.MapHeld;
            if (map != null)
            {
                float glowScale = Mathf.Max(0.25f, props.sentenceVisualScale);
                FleckMaker.ThrowLightningGlow(target.DrawPos, map, glowScale);
                FleckMaker.ThrowMicroSparks(target.DrawPos, map);
                FleckMaker.Static(
                    target.PositionHeld,
                    map,
                    FleckDefOf.ExplosionFlash,
                    Mathf.Max(0.20f, props.sentenceFlashScale));

                if (!props.sentenceSoundDefName.NullOrEmpty())
                {
                    ABY_SoundUtility.PlayAt(props.sentenceSoundDefName, target.PositionHeld, map);
                }
            }
        }

        private static bool IsValidWielder(Pawn pawn)
        {
            return pawn != null
                && !pawn.Dead
                && !pawn.Destroyed
                && pawn.equipment != null;
        }

        private static bool IsValidPawnTarget(Pawn pawn)
        {
            return pawn != null
                && !pawn.Dead
                && !pawn.Destroyed
                && pawn.health != null
                && pawn.MapHeld != null;
        }
    }
}
