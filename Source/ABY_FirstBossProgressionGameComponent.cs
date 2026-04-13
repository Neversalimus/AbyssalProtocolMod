using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    public class ABY_FirstBossProgressionGameComponent : GameComponent
    {
        private const string ArchonBeastRaceDefName = "ABY_ArchonBeast";
        private const string AbyssalAugmentationResearchDefName = "ABY_AbyssalAugmentation";
        private const string HeraldIntegrationResearchDefName = "ABY_HeraldImplantIntegration";
        private const int ScanIntervalTicks = 90;

        private bool firstBeastKillRecorded;
        private bool heraldIntegrationGranted;
        private bool pendingHeraldIntegrationGrant;
        private int nextScanTick;
        private List<int> processedArchonPawnIds = new List<int>();

        public ABY_FirstBossProgressionGameComponent(Game game)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref firstBeastKillRecorded, "firstBeastKillRecorded", false);
            Scribe_Values.Look(ref heraldIntegrationGranted, "heraldIntegrationGranted", false);
            Scribe_Values.Look(ref pendingHeraldIntegrationGrant, "pendingHeraldIntegrationGrant", false);
            Scribe_Values.Look(ref nextScanTick, "nextScanTick", 0);
            Scribe_Collections.Look(ref processedArchonPawnIds, "processedArchonPawnIds", LookMode.Value);

            if (Scribe.mode == LoadSaveMode.PostLoadInit && processedArchonPawnIds == null)
            {
                processedArchonPawnIds = new List<int>();
            }
        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();

            if (Find.TickManager == null || Find.Maps == null)
            {
                return;
            }

            int ticksGame = Find.TickManager.TicksGame;
            if (ticksGame < nextScanTick)
            {
                return;
            }

            nextScanTick = ticksGame + ScanIntervalTicks;
            TryRecordFirstBeastKill();
            TryGrantHeraldIntegration();
        }

        private void TryRecordFirstBeastKill()
        {
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                Map map = maps[i];
                if (map?.listerThings == null)
                {
                    continue;
                }

                List<Thing> corpses = map.listerThings.ThingsInGroup(ThingRequestGroup.Corpse);
                if (corpses == null)
                {
                    continue;
                }

                for (int j = 0; j < corpses.Count; j++)
                {
                    if (!(corpses[j] is Corpse corpse) || corpse.InnerPawn == null)
                    {
                        continue;
                    }

                    Pawn deadPawn = corpse.InnerPawn;
                    if (deadPawn.def?.defName != ArchonBeastRaceDefName)
                    {
                        continue;
                    }

                    int pawnId = deadPawn.thingIDNumber;
                    if (processedArchonPawnIds.Contains(pawnId))
                    {
                        continue;
                    }

                    processedArchonPawnIds.Add(pawnId);
                    OnArchonBeastKilled(map, corpse.PositionHeld);
                }
            }
        }

        private void OnArchonBeastKilled(Map map, IntVec3 position)
        {
            if (!firstBeastKillRecorded)
            {
                firstBeastKillRecorded = true;
                pendingHeraldIntegrationGrant = true;

                LookTargets lookTargets = map != null && position.IsValid
                    ? new LookTargets(new TargetInfo(position, map))
                    : LookTargets.Invalid;

                Find.LetterStack.ReceiveLetter(
                    "ABY_FirstBossKillLabel".Translate(),
                    "ABY_FirstBossKillDesc".Translate(),
                    LetterDefOf.PositiveEvent,
                    lookTargets);
            }
        }

        private void TryGrantHeraldIntegration()
        {
            if (!pendingHeraldIntegrationGrant || heraldIntegrationGranted)
            {
                return;
            }

            ResearchProjectDef augmentationProject = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(AbyssalAugmentationResearchDefName);
            ResearchProjectDef heraldProject = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(HeraldIntegrationResearchDefName);
            if (augmentationProject == null || heraldProject == null)
            {
                return;
            }

            if (!augmentationProject.IsFinished)
            {
                return;
            }

            if (heraldProject.IsFinished)
            {
                heraldIntegrationGranted = true;
                pendingHeraldIntegrationGrant = false;
                return;
            }

            if (!TryFinishProject(heraldProject))
            {
                return;
            }

            heraldIntegrationGranted = true;
            pendingHeraldIntegrationGrant = false;

            Find.LetterStack.ReceiveLetter(
                "ABY_HeraldIntegrationUnlockedLabel".Translate(),
                "ABY_HeraldIntegrationUnlockedDesc".Translate(),
                LetterDefOf.PositiveEvent);
        }

        private static bool TryFinishProject(ResearchProjectDef projectDef)
        {
            if (projectDef == null)
            {
                return false;
            }

            if (projectDef.IsFinished)
            {
                return true;
            }

            ResearchManager researchManager = Find.ResearchManager;
            if (researchManager == null)
            {
                return false;
            }

            MethodInfo finishMethod = researchManager.GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault(method => method.Name == "FinishProject");

            if (finishMethod == null)
            {
                return false;
            }

            ParameterInfo[] parameters = finishMethod.GetParameters();

            try
            {
                switch (parameters.Length)
                {
                    case 1:
                        finishMethod.Invoke(researchManager, new object[] { projectDef });
                        break;
                    case 2:
                        finishMethod.Invoke(researchManager, new object[] { projectDef, false });
                        break;
                    default:
                        object[] args = new object[parameters.Length];
                        args[0] = projectDef;
                        for (int i = 1; i < parameters.Length; i++)
                        {
                            Type parameterType = parameters[i].ParameterType;
                            if (parameterType == typeof(bool))
                            {
                                args[i] = false;
                            }
                            else if (parameterType.IsValueType)
                            {
                                args[i] = Activator.CreateInstance(parameterType);
                            }
                            else
                            {
                                args[i] = null;
                            }
                        }

                        finishMethod.Invoke(researchManager, args);
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[Abyssal Protocol] Failed to auto-complete research project '" + projectDef.defName + "': " + ex);
                return false;
            }

            return projectDef.IsFinished;
        }
    }
}
