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

        public static List<ABY_ProtocolResearchCategoryDef> AllCategories()
        {
            return DefDatabase<ABY_ProtocolResearchCategoryDef>.AllDefsListForReading
                .OrderBy(def => def.displayOrder)
                .ThenBy(def => def.label)
                .ToList();
        }

        public static List<ABY_ProtocolResearchDef> ProjectsFor(ABY_ProtocolResearchCategoryDef category)
        {
            if (category == null)
            {
                return new List<ABY_ProtocolResearchDef>();
            }

            return DefDatabase<ABY_ProtocolResearchDef>.AllDefsListForReading
                .Where(def => def.category == category)
                .OrderBy(def => def.displayOrder)
                .ThenBy(def => def.label)
                .ToList();
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

            string raw = project.previewState ?? string.Empty;
            if (raw.Equals("Completed", StringComparison.OrdinalIgnoreCase))
            {
                return ABY_ProtocolResearchState.Completed;
            }

            if (raw.Equals("Active", StringComparison.OrdinalIgnoreCase))
            {
                return ABY_ProtocolResearchState.Active;
            }

            if (raw.Equals("Locked", StringComparison.OrdinalIgnoreCase))
            {
                return ABY_ProtocolResearchState.Locked;
            }

            return ABY_ProtocolResearchState.Available;
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
