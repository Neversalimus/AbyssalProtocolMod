using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace AbyssalProtocol
{
    public class CompUseEffect_SummonBoss : CompUseEffect
    {
        public CompProperties_UseEffectSummonBoss Props => (CompProperties_UseEffectSummonBoss)props;

        public override void DoEffect(Pawn usedBy)
        {
            base.DoEffect(usedBy);

            Map map = usedBy?.MapHeld;
            if (map == null)
            {
                Messages.Message("ABY_BossSummonFail_NoMap".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            if (!TryFindSpawnCell(map, out IntVec3 spawnCell))
            {
                Messages.Message("ABY_BossSummonFail_NoEdge".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            PawnKindDef kindDef = DefDatabase<PawnKindDef>.GetNamedSilentFail(Props.pawnKindDefName);
            if (kindDef == null)
            {
                Messages.Message($"Missing PawnKindDef: {Props.pawnKindDefName}", MessageTypeDefOf.RejectInput, false);
                return;
            }

            Faction faction = ResolveHostileFaction();
            if (faction == null)
            {
                Messages.Message("No valid hostile faction found.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            PawnGenerationRequest request = new PawnGenerationRequest(
                kindDef,
                faction,
                PawnGenerationContext.NonPlayer,
                map.Tile,
                forceGenerateNewPawn: true,
                allowDead: false,
                allowDowned: false,
                canGeneratePawnRelations: false,
                mustBeCapableOfViolence: true,
                colonistRelationChanceFactor: 0f,
                forceAddFreeWarmLayerIfNeeded: false,
                allowGay: false,
                allowPregnant: false,
                allowFood: false,
                inhabitant: false,
                certainlyBeenInCryptosleep: false,
                forceRedressWorldPawnIfFormerColonist: false,
                worldPawnFactionDoesntMatter: false,
                biocodeWeaponChance: 0f,
                biocodeApparelChance: 0f,
                extraPawnForExtraRelationChance: null,
                relationWithExtraPawnChanceFactor: 0f,
                validatorPreGear: null,
                validatorPostGear: null,
                fixedBirthName: null,
                fixedLastName: null,
                fixedGender: null,
                fixedIdeo: null,
                forceNoIdeo: true,
                developmentalStages: DevelopmentalStage.Adult
            );

            Pawn pawn = PawnGenerator.GeneratePawn(request);
            if (pawn == null)
            {
                Messages.Message("Failed to generate summoned boss pawn.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            PrepareBoss(pawn);

            GenSpawn.Spawn(pawn, spawnCell, map, Rot4.Random);

            AbyssalBossScreenFXGameComponent fxComp =
                Current.Game?.GetComponent<AbyssalBossScreenFXGameComponent>();

            fxComp?.RegisterBoss(pawn);

            LordJob lordJob = new LordJob_AssaultColony(
                faction,
                canKidnap: false,
                canTimeoutOrFlee: false,
                sappers: true,
                useAvoidGridSmart: true,
                canSteal: false
            );

            LordMaker.MakeNewLord(faction, lordJob, map, new List<Pawn> { pawn });

            Find.LetterStack.ReceiveLetter(
                "ABY_BossSummonSuccessLabel".Translate(),
                "ABY_BossSummonSuccessDesc".Translate(Props.bossLabel),
                LetterDefOf.ThreatBig,
                new TargetInfo(spawnCell, map)
            );

            Thing splitThing = parent.SplitOff(1);
            splitThing?.Destroy();
        }

        private static Faction ResolveHostileFaction()
        {
            FactionDef ancientsHostileDef = DefDatabase<FactionDef>.GetNamedSilentFail("AncientsHostile");
            if (ancientsHostileDef != null)
            {
                Faction ancients = Find.FactionManager.FirstFactionOfDef(ancientsHostileDef);
                if (ancients != null)
                    return ancients;
            }

            Faction randomEnemy = Find.FactionManager.RandomEnemyFaction(false, false, false, TechLevel.Spacer);
            if (randomEnemy != null)
                return randomEnemy;

            FactionDef pirateDef =
                DefDatabase<FactionDef>.GetNamedSilentFail("Pirate") ??
                DefDatabase<FactionDef>.GetNamedSilentFail("Pirates");

            if (pirateDef != null)
                return Find.FactionManager.FirstFactionOfDef(pirateDef);

            return null;
        }

        private void PrepareBoss(Pawn pawn)
        {
            pawn.Name = new NameSingle("ABY_BossName".Translate());

            if (pawn.story != null)
                pawn.story.title = Props.bossLabel;

            if (pawn.RaceProps != null && pawn.RaceProps.Humanlike)
                PrepareHumanlikeBoss(pawn);
            else
                PrepareMonsterBoss(pawn);
        }

        private static void PrepareHumanlikeBoss(Pawn pawn)
        {
            if (pawn.equipment != null)
                pawn.equipment.DestroyAllEquipment();

            ThingDef weaponDef = DefDatabase<ThingDef>.GetNamedSilentFail("ABY_RiftCarbine");
            if (weaponDef != null && pawn.equipment != null)
            {
                Thing weapon = ThingMaker.MakeThing(weaponDef);
                if (weapon is ThingWithComps twc)
                    pawn.equipment.AddEquipment(twc);
            }

            HediffDef eyeDef = DefDatabase<HediffDef>.GetNamedSilentFail("ABY_InfernalEye_Implant");
            if (eyeDef != null)
            {
                BodyPartRecord eyePart = pawn.health?.hediffSet?.GetNotMissingParts()
                    ?.FirstOrDefault(p => p.def == BodyPartDefOf.Eye);

                if (eyePart != null)
                    pawn.health.AddHediff(eyeDef, eyePart);
            }

            if (pawn.skills != null)
            {
                SkillRecord shooting = pawn.skills.GetSkill(SkillDefOf.Shooting);
                if (shooting != null)
                    shooting.Level = 16;

                SkillRecord melee = pawn.skills.GetSkill(SkillDefOf.Melee);
                if (melee != null)
                    melee.Level = 12;

                SkillRecord intellectual = pawn.skills.GetSkill(SkillDefOf.Intellectual);
                if (intellectual != null)
                    intellectual.Level = 8;
            }
        }

        private static void PrepareMonsterBoss(Pawn pawn)
        {
            HediffDef core = DefDatabase<HediffDef>.GetNamedSilentFail("ABY_ArchonCore");
            HediffDef carapace = DefDatabase<HediffDef>.GetNamedSilentFail("ABY_ArchonCarapace");

            if (core != null)
                pawn.health?.AddHediff(core);

            if (carapace != null)
                pawn.health?.AddHediff(carapace);
        }

        private static bool TryFindSpawnCell(Map map, out IntVec3 cell)
        {
            for (int i = 0; i < 8; i++)
            {
                if (CellFinder.TryFindRandomEdgeCellWith(
                    c => c.Standable(map) && !c.Fogged(map),
                    map,
                    CellFinder.EdgeRoadChance_Hostile,
                    out cell))
                {
                    return true;
                }
            }

            cell = IntVec3.Invalid;
            return false;
        }
    }
}
