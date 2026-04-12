using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace AbyssalProtocol
{
    public static class AbyssalBossSummonUtility
    {
        public static Faction ResolveHostileFaction()
        {
            FactionDef ancientsHostileDef = DefDatabase<FactionDef>.GetNamedSilentFail("AncientsHostile");
            if (ancientsHostileDef != null)
            {
                Faction ancients = Find.FactionManager.FirstFactionOfDef(ancientsHostileDef);
                if (ancients != null)
                {
                    return ancients;
                }
            }

            Faction randomEnemy = Find.FactionManager.RandomEnemyFaction(false, false, false, TechLevel.Spacer);
            if (randomEnemy != null)
            {
                return randomEnemy;
            }

            FactionDef pirateDef =
                DefDatabase<FactionDef>.GetNamedSilentFail("Pirate") ??
                DefDatabase<FactionDef>.GetNamedSilentFail("Pirates");

            if (pirateDef != null)
            {
                return Find.FactionManager.FirstFactionOfDef(pirateDef);
            }

            return null;
        }

        public static bool TryFindBossArrivalCell(Map map, out IntVec3 cell)
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

        public static bool TryGenerateBoss(
            Map map,
            PawnKindDef kindDef,
            Faction faction,
            string bossLabel,
            out Pawn pawn,
            out string failReason)
        {
            pawn = null;
            failReason = null;

            if (map == null)
            {
                failReason = "No map available for summoned boss.";
                return false;
            }

            if (kindDef == null)
            {
                failReason = "Missing PawnKindDef for summoned boss.";
                return false;
            }

            if (faction == null)
            {
                failReason = "No hostile faction available for summoned boss.";
                return false;
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
                developmentalStages: DevelopmentalStage.Adult);

            pawn = PawnGenerator.GeneratePawn(request);
            if (pawn == null)
            {
                failReason = "Failed to generate the summoned boss pawn.";
                return false;
            }

            PrepareBoss(pawn, bossLabel);
            return true;
        }

        public static void FinalizeBossArrival(
            Pawn pawn,
            Faction faction,
            Map map,
            IntVec3 spawnCell,
            string bossLabel)
        {
            if (pawn == null || faction == null || map == null || !spawnCell.IsValid)
            {
                return;
            }

            GenSpawn.Spawn(pawn, spawnCell, map, Rot4.Random);
            ArchonInfernalVFXUtility.DoSummonVFX(map, spawnCell);
            ABY_SoundUtility.PlayAt("ABY_ArchonBossArrive", spawnCell, map);

            AbyssalBossScreenFXGameComponent fxComp = Current.Game?.GetComponent<AbyssalBossScreenFXGameComponent>();
            fxComp?.RegisterBoss(pawn);

            LordJob lordJob = new LordJob_AssaultColony(
                faction,
                canKidnap: false,
                canTimeoutOrFlee: false,
                sappers: true,
                useAvoidGridSmart: true,
                canSteal: false);

            LordMaker.MakeNewLord(faction, lordJob, map, new List<Pawn> { pawn });

            Find.LetterStack.ReceiveLetter(
                "ABY_BossSummonSuccessLabel".Translate(),
                "ABY_BossSummonSuccessDesc".Translate(bossLabel),
                LetterDefOf.ThreatBig,
                new TargetInfo(spawnCell, map));
        }

        public static bool TryFindNearestAvailableCircle(
            Map map,
            IntVec3 origin,
            out Building_AbyssalSummoningCircle circle,
            out string failReason)
        {
            circle = null;
            failReason = null;

            if (map == null)
            {
                failReason = "No map available.";
                return false;
            }

            ThingDef circleDef = DefDatabase<ThingDef>.GetNamedSilentFail("ABY_SummoningCircle");
            if (circleDef == null)
            {
                failReason = "Missing ThingDef: ABY_SummoningCircle";
                return false;
            }

            IEnumerable<Thing> circles = map.listerThings.ThingsOfDef(circleDef);
            Building_AbyssalSummoningCircle best = null;
            float bestDistance = float.MaxValue;
            bool foundAny = false;
            bool foundBusy = false;
            bool foundUnpowered = false;

            foreach (Thing thing in circles)
            {
                if (!(thing is Building_AbyssalSummoningCircle candidate) || !candidate.Spawned || candidate.Destroyed)
                {
                    continue;
                }

                foundAny = true;

                if (candidate.RitualActive)
                {
                    foundBusy = true;
                    continue;
                }

                if (!candidate.IsPoweredForRitual)
                {
                    foundUnpowered = true;
                    continue;
                }

                float distance = candidate.Position.DistanceToSquared(origin);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = candidate;
                }
            }

            if (best == null)
            {
                if (!foundAny)
                {
                    failReason = "Build an Abyssal Summoning Circle before using the sigil.";
                }
                else if (foundBusy && foundUnpowered)
                {
                    failReason = "All abyssal circles are either busy or unpowered.";
                }
                else if (foundBusy)
                {
                    failReason = "All abyssal circles on this map are already occupied by another ritual.";
                }
                else if (foundUnpowered)
                {
                    failReason = "All abyssal circles on this map are unpowered.";
                }
                else
                {
                    failReason = "No available abyssal circle was found.";
                }

                return false;
            }

            circle = best;
            return true;
        }

        private static void PrepareBoss(Pawn pawn, string bossLabel)
        {
            pawn.Name = new NameSingle("ABY_BossName".Translate());

            if (pawn.story != null)
            {
                pawn.story.title = bossLabel;
            }

            if (pawn.RaceProps != null && pawn.RaceProps.Humanlike)
            {
                PrepareHumanlikeBoss(pawn);
            }
            else
            {
                PrepareMonsterBoss(pawn);
            }
        }

        private static void PrepareHumanlikeBoss(Pawn pawn)
        {
            if (pawn.equipment != null)
            {
                pawn.equipment.DestroyAllEquipment();
            }

            ThingDef weaponDef = DefDatabase<ThingDef>.GetNamedSilentFail("ABY_RiftCarbine");
            if (weaponDef != null && pawn.equipment != null)
            {
                Thing weapon = ThingMaker.MakeThing(weaponDef);
                if (weapon is ThingWithComps thingWithComps)
                {
                    pawn.equipment.AddEquipment(thingWithComps);
                }
            }

            HediffDef eyeDef = DefDatabase<HediffDef>.GetNamedSilentFail("ABY_InfernalEye_Implant");
            if (eyeDef != null)
            {
                BodyPartRecord eyePart = pawn.health?.hediffSet?.GetNotMissingParts()?.FirstOrDefault(part => part.def == BodyPartDefOf.Eye);
                if (eyePart != null)
                {
                    pawn.health.AddHediff(eyeDef, eyePart);
                }
            }

            if (pawn.skills != null)
            {
                SkillRecord shooting = pawn.skills.GetSkill(SkillDefOf.Shooting);
                if (shooting != null)
                {
                    shooting.Level = 16;
                }

                SkillRecord melee = pawn.skills.GetSkill(SkillDefOf.Melee);
                if (melee != null)
                {
                    melee.Level = 12;
                }

                SkillRecord intellectual = pawn.skills.GetSkill(SkillDefOf.Intellectual);
                if (intellectual != null)
                {
                    intellectual.Level = 8;
                }
            }
        }

        private static void PrepareMonsterBoss(Pawn pawn)
        {
            HediffDef core = DefDatabase<HediffDef>.GetNamedSilentFail("ABY_ArchonCore");
            HediffDef carapace = DefDatabase<HediffDef>.GetNamedSilentFail("ABY_ArchonCarapace");

            if (core != null)
            {
                pawn.health?.AddHediff(core);
            }

            if (carapace != null)
            {
                pawn.health?.AddHediff(carapace);
            }
        }
    }
}
