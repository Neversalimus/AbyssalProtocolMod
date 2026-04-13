using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    public static class ABY_HeraldFragmentAnalysisUtility
    {
        public const string AnalysisProjectDefName = "ABY_HeraldFragmentAnalysis";
        public const string ImplantProjectDefName = "ABY_HeraldImplantIntegration";
        public const string WeaponProjectDefName = "ABY_HeraldWeaponIntegration";
        public const string FragmentDefName = "ABY_HeraldCoreFragment";

        public static void ResolveAnalysisPacket(Thing packet)
        {
            if (packet == null || packet.Destroyed)
            {
                return;
            }

            Map map = packet.MapHeld;
            IntVec3 cell = packet.PositionHeld;
            ResearchProjectDef analysisProject = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(AnalysisProjectDefName);

            if (analysisProject == null || Find.ResearchManager == null)
            {
                ReturnFragment(packet, map, cell);
                Messages.Message("ABY_HeraldAnalysisFailedMissingProject".Translate(), MessageTypeDefOf.RejectInput, false);
                packet.Destroy(DestroyMode.Vanish);
                return;
            }

            if (analysisProject.IsFinished)
            {
                ReturnFragment(packet, map, cell);
                Messages.Message("ABY_HeraldAnalysisAlreadyKnown".Translate(), MessageTypeDefOf.NeutralEvent, false);
                packet.Destroy(DestroyMode.Vanish);
                return;
            }

            Find.ResearchManager.FinishProject(analysisProject, false, null);

            string implantLabel = GetProjectLabel(ImplantProjectDefName, "herald implant integration");
            string weaponLabel = GetProjectLabel(WeaponProjectDefName, "herald weapon integration");

            if (Find.LetterStack != null && map != null && cell.IsValid)
            {
                Find.LetterStack.ReceiveLetter(
                    "ABY_HeraldAnalysisCompleteLabel".Translate(),
                    "ABY_HeraldAnalysisCompleteDesc".Translate(weaponLabel, implantLabel),
                    LetterDefOf.PositiveEvent,
                    new TargetInfo(cell, map));
            }

            packet.Destroy(DestroyMode.Vanish);
        }

        private static string GetProjectLabel(string defName, string fallback)
        {
            ResearchProjectDef project = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(defName);
            if (project?.label != null)
            {
                return project.label.CapitalizeFirst();
            }

            return fallback.CapitalizeFirst();
        }

        private static void ReturnFragment(Thing packet, Map map, IntVec3 cell)
        {
            if (map == null || !cell.IsValid)
            {
                return;
            }

            ThingDef fragmentDef = DefDatabase<ThingDef>.GetNamedSilentFail(FragmentDefName);
            if (fragmentDef == null)
            {
                return;
            }

            Thing fragment = ThingMaker.MakeThing(fragmentDef);
            fragment.stackCount = 1;
            GenSpawn.Spawn(fragment, cell, map);
        }
    }
}
