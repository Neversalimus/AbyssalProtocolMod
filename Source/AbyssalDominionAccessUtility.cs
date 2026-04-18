using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    public static class AbyssalDominionAccessUtility
    {
        public const string DominionRitualId = "dominion_gate";
        public const string DominionRecipeDefName = "ABY_CraftDominionSigil";

        public static bool IsUserFacingDominionContentEnabled()
        {
            return Prefs.DevMode;
        }

        public static bool ShouldExposeDominionRitual(Building_AbyssalSummoningCircle circle = null)
        {
            if (Prefs.DevMode)
            {
                return true;
            }

            return circle?.Map?.GetComponent<MapComponent_DominionCrisis>()?.IsActive == true;
        }

        public static bool ShouldExposeForgeRecipe(RecipeDef recipe)
        {
            if (recipe == null)
            {
                return false;
            }

            if (recipe.defName == DominionRecipeDefName)
            {
                return Prefs.DevMode;
            }

            return true;
        }

        public static bool IsDominionRitualId(string ritualId)
        {
            return string.Equals(ritualId, DominionRitualId, System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
