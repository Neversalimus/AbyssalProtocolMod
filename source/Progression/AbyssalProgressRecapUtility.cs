using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    public static class AbyssalProgressRecapUtility
    {
        private const string ChoirSigilRecipeDefName = "ABY_CraftChoirEngineSigil";
        private const string ReactorSigilRecipeDefName = "ABY_CraftReactorSaintSigil";
        private const string DominionSigilRecipeDefName = "ABY_CraftDominionSigil";
        private const string VesperLanceThingDefName = "ABY_VesperLance";
        private const string UltraPlasmaThingDefName = "ABY_UltraPlasmaRifle";
        private const string DominionResearchDefName = "ABY_DominionGateBootstrapping";

        public static void SendFirstBossRecap(Map map, IntVec3 cell)
        {
            try
            {
                if (Find.LetterStack == null)
                {
                    return;
                }

                string choirLabel = GetRecipeLabel(ChoirSigilRecipeDefName, "choir engine sigil");
                string reactorLabel = GetRecipeLabel(ReactorSigilRecipeDefName, "reactor saint sigil");
                int choirResidue = GetRecipeResidue(ChoirSigilRecipeDefName);
                int reactorResidue = GetRecipeResidue(ReactorSigilRecipeDefName);

                Find.LetterStack.ReceiveLetter(
                    "ABY_ProgressRecap_Archon_Label".Translate(),
                    "ABY_ProgressRecap_Archon_Desc".Translate(choirLabel, choirResidue, reactorLabel, reactorResidue),
                    LetterDefOf.PositiveEvent,
                    map != null && cell.IsValid ? new LookTargets(new TargetInfo(cell, map)) : null);
            }
            catch
            {
                // Milestone progression must not be invalidated by LetterStack or localization failures.
            }
        }

        public static void SendReactorRecap(Map map, IntVec3 cell)
        {
            try
            {
                if (Find.LetterStack == null)
                {
                    return;
                }

                string vesperLabel = GetThingLabel(VesperLanceThingDefName, "Vesper Lance");
                string plasmaLabel = GetThingLabel(UltraPlasmaThingDefName, "Ultra Plasma Rifle");
                string dominionResearchLabel = GetResearchLabel(DominionResearchDefName, "dominion gate bootstrapping");
                int dominionResidue = GetRecipeResidue(DominionSigilRecipeDefName);

                Find.LetterStack.ReceiveLetter(
                    "ABY_ProgressRecap_Reactor_Label".Translate(),
                    "ABY_ProgressRecap_Reactor_Desc".Translate(vesperLabel, plasmaLabel, dominionResearchLabel, dominionResidue),
                    LetterDefOf.PositiveEvent,
                    map != null && cell.IsValid ? new LookTargets(new TargetInfo(cell, map)) : null);
            }
            catch
            {
                // Milestone progression must not be invalidated by LetterStack or localization failures.
            }
        }

        private static string GetRecipeLabel(string defName, string fallback)
        {
            RecipeDef recipe = ABY_DefCache.RecipeDefNamed(defName);
            if (recipe?.label != null)
            {
                return recipe.label.CapitalizeFirst();
            }

            return fallback.CapitalizeFirst();
        }

        private static int GetRecipeResidue(string defName)
        {
            RecipeDef recipe = ABY_DefCache.RecipeDefNamed(defName);
            return AbyssalForgeProgressUtility.GetRequiredResidue(recipe);
        }

        private static string GetThingLabel(string defName, string fallback)
        {
            ThingDef thingDef = ABY_DefCache.ThingDefNamed(defName);
            if (thingDef?.label != null)
            {
                return thingDef.label.CapitalizeFirst();
            }

            return fallback.CapitalizeFirst();
        }

        private static string GetResearchLabel(string defName, string fallback)
        {
            ResearchProjectDef project = ABY_DefCache.ResearchProjectDefNamed(defName);
            if (project?.label != null)
            {
                return project.label.CapitalizeFirst();
            }

            return fallback.CapitalizeFirst();
        }
    }
}
