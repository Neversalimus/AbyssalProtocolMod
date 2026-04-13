using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public static class RuptureCrownUtility
    {
        public const string CrownDefName = "ABY_CrownOfRupture";
        public const string AbilityDefName = "ABY_RuptureSentence";
        public const string BearerHediffDefName = "ABY_RuptureCrownBearer";
        public const string MarkHediffDefName = "ABY_RuptureSentenceMark";
        public const float DefaultVerdictRadius = 30f;
        public const int DefaultMarkTicks = 4320;
        public const string CrownIconPath = "Things/Item/ABY_CrownOfRupture";

        private static AbilityDef cachedAbilityDef;
        private static HediffDef cachedBearerHediffDef;
        private static HediffDef cachedMarkHediffDef;
        private static Texture2D cachedCommandIcon;

        public static AbilityDef AbilityDef
        {
            get
            {
                if (cachedAbilityDef == null)
                {
                    cachedAbilityDef = DefDatabase<AbilityDef>.GetNamedSilentFail(AbilityDefName);
                }

                return cachedAbilityDef;
            }
        }

        public static HediffDef BearerHediffDef
        {
            get
            {
                if (cachedBearerHediffDef == null)
                {
                    cachedBearerHediffDef = DefDatabase<HediffDef>.GetNamedSilentFail(BearerHediffDefName);
                }

                return cachedBearerHediffDef;
            }
        }

        public static HediffDef MarkHediffDef
        {
            get
            {
                if (cachedMarkHediffDef == null)
                {
                    cachedMarkHediffDef = DefDatabase<HediffDef>.GetNamedSilentFail(MarkHediffDefName);
                }

                return cachedMarkHediffDef;
            }
        }

        public static Texture2D CommandIcon
        {
            get
            {
                if (cachedCommandIcon == null)
                {
                    cachedCommandIcon = ContentFinder<Texture2D>.Get(CrownIconPath, false) ?? BaseContent.BadTex;
                }

                return cachedCommandIcon;
            }
        }

        public static CompRuptureCrown GetWornCrownComp(Pawn pawn)
        {
            if (pawn?.apparel == null)
            {
                return null;
            }

            for (int i = 0; i < pawn.apparel.WornApparel.Count; i++)
            {
                Apparel apparel = pawn.apparel.WornApparel[i];
                if (apparel?.def?.defName != CrownDefName)
                {
                    continue;
                }

                return apparel.TryGetComp<CompRuptureCrown>();
            }

            return null;
        }

        public static bool HasCrown(Pawn pawn)
        {
            return GetWornCrownComp(pawn) != null;
        }

        public static Ability GetGrantedAbility(Pawn pawn)
        {
            if (pawn?.abilities == null)
            {
                return null;
            }

            AbilityDef abilityDef = AbilityDef;
            if (abilityDef == null)
            {
                return null;
            }

            return pawn.abilities.GetAbility(abilityDef, false);
        }

        public static int CountEligibleTargets(Pawn caster, float radius)
        {
            if (!CanScanTargets(caster))
            {
                return 0;
            }

            var pawns = caster.MapHeld.mapPawns.AllPawnsSpawned;
            int count = 0;
            for (int i = 0; i < pawns.Count; i++)
            {
                if (IsEligibleVerdictTarget(caster, pawns[i], radius))
                {
                    count++;
                }
            }

            return count;
        }

        public static int ApplyVerdictWave(Pawn caster, float radius, int markTicks)
        {
            if (!CanScanTargets(caster))
            {
                return 0;
            }

            var pawns = caster.MapHeld.mapPawns.AllPawnsSpawned;
            int affectedCount = 0;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn targetPawn = pawns[i];
                if (!IsEligibleVerdictTarget(caster, targetPawn, radius))
                {
                    continue;
                }

                ApplyMark(targetPawn, markTicks);
                affectedCount++;

                if (targetPawn.MapHeld != null)
                {
                    FleckMaker.ThrowLightningGlow(targetPawn.DrawPos, targetPawn.MapHeld, 1.4f);
                }
            }

            return affectedCount;
        }

        private static bool CanScanTargets(Pawn caster)
        {
            return caster != null && !caster.Dead && caster.Spawned && caster.MapHeld != null && caster.health != null;
        }

        private static bool IsEligibleVerdictTarget(Pawn caster, Pawn targetPawn, float radius)
        {
            if (!CanScanTargets(caster))
            {
                return false;
            }

            if (targetPawn == null || targetPawn == caster || targetPawn.Dead || !targetPawn.Spawned || targetPawn.MapHeld != caster.MapHeld)
            {
                return false;
            }

            if (!targetPawn.PositionHeld.InHorDistOf(caster.PositionHeld, radius))
            {
                return false;
            }

            if (BelongsToPlayerColony(targetPawn))
            {
                return false;
            }

            return true;
        }

        private static bool BelongsToPlayerColony(Pawn pawn)
        {
            if (pawn == null)
            {
                return false;
            }

            if (pawn.Faction == Faction.OfPlayer)
            {
                return true;
            }

            if (pawn.HostFaction == Faction.OfPlayer)
            {
                return true;
            }

            if (pawn.IsPlayerControlled || pawn.IsPrisonerOfColony || pawn.IsSlaveOfColony)
            {
                return true;
            }

            return false;
        }

        private static void ApplyMark(Pawn targetPawn, int markTicks)
        {
            HediffDef markDef = MarkHediffDef;
            if (markDef == null || targetPawn?.health == null)
            {
                return;
            }

            Hediff mark = targetPawn.health.hediffSet.GetFirstHediffOfDef(markDef);
            if (mark == null)
            {
                mark = HediffMaker.MakeHediff(markDef, targetPawn);
                targetPawn.health.AddHediff(mark);
            }

            mark.Severity = Mathf.Max(mark.Severity, 1f);

            HediffComp_Disappears disappears = mark.TryGetComp<HediffComp_Disappears>();
            if (disappears != null)
            {
                disappears.ticksToDisappear = Mathf.Max(1, markTicks);
            }

            targetPawn.health.hediffSet.DirtyCache();
        }
    }

    public class CompProperties_RuptureCrown : CompProperties
    {
        public int rechargeTicks = GenDate.TicksPerDay;
        public float effectRadius = RuptureCrownUtility.DefaultVerdictRadius;
        public int markTicks = RuptureCrownUtility.DefaultMarkTicks;
        public string commandLabel = "Rupture Verdict";
        public string commandDesc = "Discharge the crown in a silent rupture wave. All hostile and neutral non-colony pawns within range are marked without provoking aggression.";

        public CompProperties_RuptureCrown()
        {
            compClass = typeof(CompRuptureCrown);
        }
    }

    public class CompRuptureCrown : ThingComp
    {
        private int lastUseTick = -999999;

        public CompProperties_RuptureCrown Props => (CompProperties_RuptureCrown)props;

        public bool IsReady => TicksUntilRecharged <= 0;

        public int TicksUntilRecharged
        {
            get
            {
                if (Find.TickManager == null)
                {
                    return 0;
                }

                return Mathf.Max(0, Props.rechargeTicks - (Find.TickManager.TicksGame - lastUseTick));
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref lastUseTick, "lastUseTick", -999999);
        }

        public void NotifyUsed()
        {
            if (Find.TickManager == null)
            {
                lastUseTick = 0;
                return;
            }

            lastUseTick = Find.TickManager.TicksGame;
        }

        public override string CompInspectStringExtra()
        {
            if (IsReady)
            {
                return "Rupture charge: ready";
            }

            return "Rupture charge recharging: " + TicksUntilRecharged.ToStringTicksToPeriod();
        }

        public override System.Collections.Generic.IEnumerable<Gizmo> CompGetWornGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetWornGizmosExtra())
            {
                yield return gizmo;
            }

            Apparel apparel = parent as Apparel;
            Pawn wearer = apparel?.Wearer;
            if (wearer == null || !wearer.IsColonistPlayerControlled || wearer.Dead || !wearer.Spawned || wearer.MapHeld == null)
            {
                yield break;
            }

            Command_Action command = new Command_Action
            {
                defaultLabel = Props.commandLabel,
                defaultDesc = Props.commandDesc + "\n\nRadius: " + Mathf.RoundToInt(Props.effectRadius) + " cells.",
                icon = RuptureCrownUtility.CommandIcon,
                iconDrawScale = 1f,
                defaultIconColor = Color.white,
                action = delegate
                {
                    TryDischargeVerdict(wearer);
                }
            };

            if (!IsReady)
            {
                command.Disable("Crown charge is still recharging: " + TicksUntilRecharged.ToStringTicksToPeriod());
            }
            else if (RuptureCrownUtility.CountEligibleTargets(wearer, Props.effectRadius) <= 0)
            {
                command.Disable("No hostile or neutral non-colony pawns are within rupture radius.");
            }

            yield return command;
        }

        public bool TryDischargeVerdict(Pawn wearer)
        {
            if (wearer == null || wearer.Dead || !wearer.Spawned || wearer.MapHeld == null)
            {
                return false;
            }

            if (!IsReady)
            {
                if (wearer.Faction == Faction.OfPlayer)
                {
                    Messages.Message(
                        "Rupture Verdict is still recharging.",
                        wearer,
                        MessageTypeDefOf.RejectInput,
                        false);
                }

                return false;
            }

            int affectedCount = RuptureCrownUtility.ApplyVerdictWave(wearer, Props.effectRadius, Props.markTicks);
            if (affectedCount <= 0)
            {
                if (wearer.Faction == Faction.OfPlayer)
                {
                    Messages.Message(
                        "No hostile or neutral non-colony pawns are within rupture radius.",
                        wearer,
                        MessageTypeDefOf.RejectInput,
                        false);
                }

                return false;
            }

            NotifyUsed();

            Ability grantedAbility = RuptureCrownUtility.GetGrantedAbility(wearer);
            if (grantedAbility != null)
            {
                grantedAbility.StartCooldown(Props.rechargeTicks);
            }

            if (wearer.MapHeld != null)
            {
                ABY_SoundUtility.PlayAt("ABY_RuptureVerdict", wearer.PositionHeld, wearer.MapHeld);
                FleckMaker.ThrowLightningGlow(wearer.DrawPos, wearer.MapHeld, 2.4f);
            }

            if (wearer.Faction == Faction.OfPlayer)
            {
                Messages.Message(
                    "Rupture Verdict collapsed " + affectedCount + " target(s).",
                    new LookTargets(wearer),
                    MessageTypeDefOf.NeutralEvent,
                    false);
            }

            return true;
        }
    }
}
