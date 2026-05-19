using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    public enum ABY_ProtocolResearchState
    {
        Locked,
        Available,
        Active,
        Completed
    }

    public static class ABY_ProtocolResearchUtility
    {
        public const string FeatureId = "ExperimentalProtocolResearch";

        private static List<ABY_ProtocolResearchCategoryDef> cachedCategories;
        private static List<ABY_ProtocolResearchDef> cachedAllProjects;
        private static readonly Dictionary<ABY_ProtocolResearchCategoryDef, List<ABY_ProtocolResearchDef>> CachedProjectsByCategory = new Dictionary<ABY_ProtocolResearchCategoryDef, List<ABY_ProtocolResearchDef>>();
        private static ThingDef cachedProtocolNexusDef;

        public static List<ABY_ProtocolResearchCategoryDef> AllCategories()
        {
            if (cachedCategories != null)
            {
                return cachedCategories;
            }

            cachedCategories = DefDatabase<ABY_ProtocolResearchCategoryDef>.AllDefsListForReading
                .OrderBy(def => def.displayOrder)
                .ThenBy(def => def.label)
                .ToList();
            return cachedCategories;
        }

        public static List<ABY_ProtocolResearchDef> AllProjects()
        {
            if (cachedAllProjects != null)
            {
                return cachedAllProjects;
            }

            cachedAllProjects = DefDatabase<ABY_ProtocolResearchDef>.AllDefsListForReading
                .OrderBy(project => project?.displayOrder ?? 0)
                .ThenBy(project => project?.label ?? string.Empty)
                .ToList();
            return cachedAllProjects;
        }

        public static List<ABY_ProtocolResearchDef> ProjectsFor(ABY_ProtocolResearchCategoryDef category)
        {
            if (category == null)
            {
                return new List<ABY_ProtocolResearchDef>();
            }

            if (CachedProjectsByCategory.TryGetValue(category, out List<ABY_ProtocolResearchDef> cached))
            {
                return cached;
            }

            List<ABY_ProtocolResearchDef> projects = AllProjects()
                .Where(def => def.category == category)
                .ToList();
            CachedProjectsByCategory[category] = projects;
            return projects;
        }

        public static ABY_ProtocolResearchState GetState(ABY_ProtocolResearchDef project)
        {
            if (project == null)
            {
                return ABY_ProtocolResearchState.Locked;
            }

            if (!PrerequisitesMet(project))
            {
                return ABY_ProtocolResearchState.Locked;
            }

            if (project.autoDecodeWhenPrerequisitesMet || IsDecoded(project))
            {
                return ABY_ProtocolResearchState.Completed;
            }

            if (AnyNexusActivelyDecoding(project))
            {
                return ABY_ProtocolResearchState.Active;
            }

            string raw = project.previewState ?? string.Empty;
            if (raw.Equals("Completed", StringComparison.OrdinalIgnoreCase))
            {
                return ABY_ProtocolResearchState.Completed;
            }

            if (raw.Equals("Active", StringComparison.OrdinalIgnoreCase))
            {
                return ABY_ProtocolResearchState.Active;
            }

            if (raw.Equals("Locked", StringComparison.OrdinalIgnoreCase) || project.futureReserve)
            {
                return ABY_ProtocolResearchState.Locked;
            }

            return ABY_ProtocolResearchState.Available;
        }

        public static bool IsDecoded(ABY_ProtocolResearchDef project)
        {
            if (project == null)
            {
                return false;
            }

            if (project.autoDecodeWhenPrerequisitesMet && PrerequisitesMet(project))
            {
                return true;
            }

            return ABY_ProtocolResearchProgressGameComponent.Current?.IsDecoded(project.defName) ?? false;
        }

        public static int ResolveDecodeWorkTicks(ABY_ProtocolResearchDef project)
        {
            if (project == null)
            {
                return 2500;
            }

            return Math.Max(300, project.decodeWorkTicks);
        }

        public static bool CanStartDecode(ABY_ProtocolResearchDef project, out string reason)
        {
            reason = null;
            if (project == null)
            {
                reason = "ABY_ProtocolResearch_DecodeNoProject".Translate();
                return false;
            }

            if (!AbyssalProtocolMod.Settings.enableProtocolNexusGating)
            {
                reason = "ABY_ProtocolResearch_DecodeGatingDisabled".Translate();
                return false;
            }

            if (project.futureReserve)
            {
                reason = "ABY_ProtocolResearch_DecodeFutureReserve".Translate();
                return false;
            }

            if (!PrerequisitesMet(project))
            {
                reason = "ABY_ProtocolResearch_DecodePrerequisites".Translate();
                return false;
            }

            if (IsDecoded(project) || project.autoDecodeWhenPrerequisitesMet)
            {
                reason = "ABY_ProtocolResearch_DecodeAlready".Translate();
                return false;
            }

            return true;
        }

        public static void MarkDecoded(ABY_ProtocolResearchDef project)
        {
            if (project != null)
            {
                ABY_ProtocolResearchGateUtility.MarkDecoded(project.defName);
            }
        }

        private static bool AnyNexusActivelyDecoding(ABY_ProtocolResearchDef project)
        {
            if (project == null || Current.Game == null)
            {
                return false;
            }

            List<Map> maps = Find.Maps;
            if (maps == null)
            {
                return false;
            }

            for (int i = 0; i < maps.Count; i++)
            {
                Map map = maps[i];
                if (map?.listerThings == null)
                {
                    continue;
                }

                cachedProtocolNexusDef = cachedProtocolNexusDef ?? DefDatabase<ThingDef>.GetNamedSilentFail("ABY_ProtocolNexus");
                if (cachedProtocolNexusDef == null)
                {
                    continue;
                }

                List<Thing> nexuses = map.listerThings.ThingsOfDef(cachedProtocolNexusDef);
                for (int j = 0; j < nexuses.Count; j++)
                {
                    Building_ABY_ProtocolNexus nexus = nexuses[j] as Building_ABY_ProtocolNexus;
                    if (nexus != null && nexus.ActiveDecodeProjectDefName == project.defName)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool PrerequisitesMet(ABY_ProtocolResearchDef project)
        {
            if (project?.requiredResearchProjects == null || project.requiredResearchProjects.Count == 0)
            {
                return true;
            }

            for (int i = 0; i < project.requiredResearchProjects.Count; i++)
            {
                ResearchProjectDef prerequisite = project.requiredResearchProjects[i];
                if (prerequisite != null && !prerequisite.IsFinished)
                {
                    return false;
                }
            }

            return true;
        }

        public static int CountVisibleCompleted(ABY_ProtocolResearchCategoryDef category)
        {
            List<ABY_ProtocolResearchDef> projects = ProjectsFor(category);
            int count = 0;
            for (int i = 0; i < projects.Count; i++)
            {
                ABY_ProtocolResearchState state = GetState(projects[i]);
                if (state == ABY_ProtocolResearchState.Completed)
                {
                    count++;
                }
            }

            return count;
        }

        public static int CountAvailable(ABY_ProtocolResearchCategoryDef category)
        {
            List<ABY_ProtocolResearchDef> projects = ProjectsFor(category);
            int count = 0;
            for (int i = 0; i < projects.Count; i++)
            {
                ABY_ProtocolResearchState state = GetState(projects[i]);
                if (state == ABY_ProtocolResearchState.Available || state == ABY_ProtocolResearchState.Active)
                {
                    count++;
                }
            }

            return count;
        }

        public static string GetStateLabel(ABY_ProtocolResearchState state)
        {
            switch (state)
            {
                case ABY_ProtocolResearchState.Completed:
                    return "ABY_ProtocolResearch_StateCompleted".Translate();
                case ABY_ProtocolResearchState.Active:
                    return "ABY_ProtocolResearch_StateActive".Translate();
                case ABY_ProtocolResearchState.Available:
                    return "ABY_ProtocolResearch_StateAvailable".Translate();
                default:
                    return "ABY_ProtocolResearch_StateLocked".Translate();
            }
        }
    }
}
