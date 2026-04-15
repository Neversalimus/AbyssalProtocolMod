using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace AbyssalProtocol
{
    public static class AbyssalBossSummonUtility
    {
        private const string ArchonBeastRaceDefName = "ABY_ArchonBeast";
        private const string ArchonOfRuptureRaceDefName = "ABY_ArchonOfRupture";
        private const string RiftImpRaceDefName = "ABY_RiftImp";
        private const string EmberHoundRaceDefName = "ABY_EmberHound";
        private const string RupturePortalDefName = "ABY_RupturePortal";
        private const string ImpPortalDefName = "ABY_ImpPortal";

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

            FactionDef pirateDef = DefDatabase<FactionDef>.GetNamedSilentFail("Pirate")
                ?? DefDatabase<FactionDef>.GetNamedSilentFail("Pirates");
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
            string bossLabel,
            string arrivalSoundDefName = "ABY_ArchonBossArrive")
        {
            if (pawn == null || faction == null || map == null || !spawnCell.IsValid)
            {
                return;
            }

            GenSpawn.Spawn(pawn, spawnCell, map, Rot4.Random);
            ArchonInfernalVFXUtility.DoSummonVFX(map, spawnCell);
            ABY_SoundUtility.PlayAt(arrivalSoundDefName, spawnCell, map);

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

            if (HasActiveAbyssalEncounter(map))
            {
                failReason = "ABY_BossSummonFail_EncounterActive".Translate();
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
            bool foundBlocked = false;

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

                if (!candidate.HasValidInteractionCell(out _) || !candidate.HasClearRitualFocus(out _))
                {
                    foundBlocked = true;
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
                    failReason = "ABY_BossSummonFail_NoCircle".Translate();
                }
                else if (foundBusy && !foundUnpowered && !foundBlocked)
                {
                    failReason = "ABY_BossSummonFail_AllBusy".Translate();
                }
                else if (foundUnpowered && !foundBusy && !foundBlocked)
                {
                    failReason = "ABY_BossSummonFail_AllUnpowered".Translate();
                }
                else if (foundBlocked && !foundBusy && !foundUnpowered)
                {
                    failReason = "ABY_BossSummonFail_AllBlocked".Translate();
                }
                else
                {
                    failReason = "ABY_BossSummonFail_NoCircleAvailable".Translate();
                }

                return false;
            }

            circle = best;
            return true;
        }

        public static bool HasActiveAbyssalEncounter(Map map)
        {
            if (map == null)
            {
                return false;
            }

            MapComponent_AbyssalPortalWave portalWave = map.GetComponent<MapComponent_AbyssalPortalWave>();
            if (portalWave != null && portalWave.IsWaveActive)
            {
                return true;
            }

            if (map.mapPawns != null)
            {
                foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
                {
                    if (pawn == null || pawn.Destroyed || pawn.Dead)
                    {
                        continue;
                    }

                    if (IsActiveEncounterPawn(pawn.def?.defName))
                    {
                        return true;
                    }
                }
            }

            if (HasActivePortalOfDef(map, RupturePortalDefName))
            {
                return true;
            }

            if (HasActivePortalOfDef(map, ImpPortalDefName))
            {
                return true;
            }

            return false;
        }

        private static bool IsActiveEncounterPawn(string defName)
        {
            return defName == ArchonBeastRaceDefName
                || defName == ArchonOfRuptureRaceDefName
                || defName == RiftImpRaceDefName
                || defName == EmberHoundRaceDefName;
        }

        private static bool HasActivePortalOfDef(Map map, string defName)
        {
            ThingDef portalDef = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            if (portalDef == null)
            {
                return false;
            }

            List<Thing> portals = map.listerThings.ThingsOfDef(portalDef);
            if (portals == null)
            {
                return false;
            }

            for (int i = 0; i < portals.Count; i++)
            {
                Thing portal = portals[i];
                if (portal != null && portal.Spawned && !portal.Destroyed)
                {
                    return true;
                }
            }

            return false;
        }

        private static void PrepareBoss(Pawn pawn, string bossLabel)
        {
            if (pawn == null)
            {
                return;
            }

            pawn.Name = new NameSingle(bossLabel);
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
            if (pawn == null)
            {
                return;
            }

            string raceDefName = pawn.def?.defName;
            string coreDefName = raceDefName == "ABY_ArchonOfRupture" ? "ABY_RuptureCore" : "ABY_ArchonCore";
            string carapaceDefName = raceDefName == "ABY_ArchonOfRupture" ? "ABY_RuptureCarapace" : "ABY_ArchonCarapace";

            HediffDef core = DefDatabase<HediffDef>.GetNamedSilentFail(coreDefName);
            HediffDef carapace = DefDatabase<HediffDef>.GetNamedSilentFail(carapaceDefName);
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
