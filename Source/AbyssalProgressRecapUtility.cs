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
            string choirLabel = GetRecipeLabel(ChoirSigilRecipeDefName, "choir engine sigil");
            string reactorLabel = GetRecipeLabel(ReactorSigilRecipeDefName, "reactor saint sigil");
            int choirResidue = GetRecipeResidue(ChoirSigilRecipeDefName);
            int reactorResidue = GetRecipeResidue(ReactorSigilRecipeDefName);

            Find.LetterStack.ReceiveLetter(
                "ABY_ProgressRecap_Archon_Label".Translate(),
                "ABY_ProgressRecap_Archon_Desc".Translate(choirLabel, choirResidue, reactorLabel, reactorResidue),
                LetterDefOf.PositiveEvent,
                new LookTargets(new TargetInfo(cell, map)));
        }

        public static void SendReactorRecap(Map map, IntVec3 cell)
        {
            string vesperLabel = GetThingLabel(VesperLanceThingDefName, "Vesper Lance");
            string plasmaLabel = GetThingLabel(UltraPlasmaThingDefName, "Ultra Plasma Rifle");
            string dominionResearchLabel = GetResearchLabel(DominionResearchDefName, "dominion gate bootstrapping");
            int dominionResidue = GetRecipeResidue(DominionSigilRecipeDefName);

            Find.LetterStack.ReceiveLetter(
                "ABY_ProgressRecap_Reactor_Label".Translate(),
                "ABY_ProgressRecap_Reactor_Desc".Translate(vesperLabel, plasmaLabel, dominionResearchLabel, dominionResidue),
                LetterDefOf.PositiveEvent,
                new LookTargets(new TargetInfo(cell, map)));
        }

        private static string GetRecipeLabel(string defName, string fallback)
        {
            RecipeDef recipe = DefDatabase<RecipeDef>.GetNamedSilentFail(defName);
            if (recipe?.label != null)
            {
                return recipe.label.CapitalizeFirst();
            }

            return fallback.CapitalizeFirst();
        }

        private static int GetRecipeResidue(string defName)
        {
            RecipeDef recipe = DefDatabase<RecipeDef>.GetNamedSilentFail(defName);
            return AbyssalForgeProgressUtility.GetRequiredResidue(recipe);
        }

        private static string GetThingLabel(string defName, string fallback)
        {
            ThingDef thingDef = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            if (thingDef?.label != null)
            {
                return thingDef.label.CapitalizeFirst();
            }

            return fallback.CapitalizeFirst();
        }

        private static string GetResearchLabel(string defName, string fallback)
        {
            ResearchProjectDef project = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(defName);
            if (project?.label != null)
            {
                return project.label.CapitalizeFirst();
            }

            return fallback.CapitalizeFirst();
        }
    }
}
