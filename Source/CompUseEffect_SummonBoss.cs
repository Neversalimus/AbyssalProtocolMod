using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using UnityEngine;

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

            // SpaceSoldier не существует в PawnKindDefOf — берём из DefDatabase
            PawnKindDef kindDef = DefDatabase<PawnKindDef>.GetNamedSilentFail(Props.pawnKindDefName)
                                  ?? DefDatabase<PawnKindDef>.GetNamedSilentFail("SpaceSoldier");

            Faction faction = Find.FactionManager.FirstFactionOfDef(FactionDefOf.AncientsHostile)
                              ?? Find.FactionManager.RandomEnemyFaction(false, false, false, TechLevel.Spacer);
            if (faction == null)
            {
                faction = Find.FactionManager.FirstFactionOfDef(FactionDefOf.Pirate);
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
                forceAddFreeWarmLayerIfNeeded: true,
                allowGay: true,
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

            // LordJob живёт в Verse.AI.Group
            LordJob lordJob = new LordJob_AssaultColony(faction, canKidnap: false, canTimeoutOrFlee: false, sappers: true, useAvoidGridSmart: true, canSteal: false);
            LordMaker.MakeNewLord(faction, lordJob, map, new List<Pawn> { pawn });

            Find.LetterStack.ReceiveLetter(
                "ABY_BossSummonSuccessLabel".Translate(),
                "ABY_BossSummonSuccessDesc".Translate(Props.bossLabel),
                LetterDefOf.ThreatBig,
                new TargetInfo(spawnCell, map));

            parent.SplitOff(1).Destroy();
        }

        private void PrepareBoss(Pawn pawn)
        {
            pawn.Name = new NameSingle("ABY_BossName".Translate());
            pawn.story.title = Props.bossLabel;

            ThingDef weaponDef = DefDatabase<ThingDef>.GetNamedSilentFail("ABY_RiftCarbine");
            if (weaponDef != null)
            {
                Thing weapon = ThingMaker.MakeThing(weaponDef);
                pawn.equipment?.AddEquipment((ThingWithComps)weapon);
            }

            HediffDef eyeDef = DefDatabase<HediffDef>.GetNamedSilentFail("ABY_InfernalEye_Implant");
            if (eyeDef != null)
            {
                // GetNotMissingParts() возвращает IEnumerable — нужен .FirstOrDefault() из System.Linq
                BodyPartRecord eyePart = pawn.health?.hediffSet?.GetNotMissingParts()
                    .FirstOrDefault(p => p.def == BodyPartDefOf.Eye);
                if (eyePart != null)
                {
                    pawn.health.AddHediff(eyeDef, eyePart);
                }
            }

            // HediffDefOf.DrugDesire не существует — берём из DefDatabase
            HediffDef drugDesire = DefDatabase<HediffDef>.GetNamedSilentFail("DrugDesire");
            if (drugDesire != null)
            {
                pawn.health?.AddHediff(drugDesire);
            }

            pawn.skills?.GetSkill(SkillDefOf.Shooting)?.Learn(16f, direct: true);
            pawn.skills?.GetSkill(SkillDefOf.Melee)?.Learn(12f, direct: true);
            pawn.skills?.GetSkill(SkillDefOf.Intellectual)?.Learn(8f, direct: true);

            if (pawn.kindDef != null)
            {
                pawn.kindDef.combatPower = Mathf.Max(pawn.kindDef.combatPower, Props.spawnPoints);
            }
        }

        private bool TryFindSpawnCell(Map map, out IntVec3 cell)
        {
            for (int i = 0; i < 40; i++)
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
