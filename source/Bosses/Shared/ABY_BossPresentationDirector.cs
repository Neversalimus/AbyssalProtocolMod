using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public static class ABY_BossPresentationDirector
    {
        public const int IntroTitleDurationTicks = 270;
        public const int PhaseTitleDurationTicks = 180;
        public const int OutroTitleDurationTicks = 230;

        public static string ResolveIntroTitle(Pawn boss, ABY_BossBarProfileDef profile)
        {
            string defName = ResolveBossDefName(boss);
            switch (defName)
            {
                case "ABY_ReactorSaint":
                    return Translate("ABY_BossPresentation_Title_ReactorSaint", "INFERNAL REACTOR SAINT");
                case "ABY_ArchonOfRupture":
                    return Translate("ABY_BossPresentation_Title_Rupture", "ARCHON OF RUPTURE");
                case "ABY_ReliquaryArchonBeast":
                    return Translate("ABY_BossPresentation_Title_ReliquaryArchonBeast", "RELIQUARY ARCHON BEAST");
                case "ABY_ArchonBeast":
                    return Translate("ABY_BossPresentation_Title_ArchonBeast", "ARCHON BEAST");
                case "ABY_WardenOfAsh":
                    return Translate("ABY_BossPresentation_Title_WardenOfAsh", "WARDEN OF ASH");
                case "ABY_ChoirEngine":
                    return Translate("ABY_BossPresentation_Title_ChoirEngine", "CHOIR ENGINE");
                default:
                    return profile?.ResolveDisplayLabel(boss, null) ?? boss?.LabelCap ?? Translate("ABY_BossPresentation_Title_Default", "ABYSSAL NODE");
            }
        }

        public static string ResolveIntroSubtitle(Pawn boss, ABY_BossBarProfileDef profile)
        {
            string defName = ResolveBossDefName(boss);
            switch (defName)
            {
                case "ABY_ReactorSaint":
                    return Translate("ABY_BossPresentation_Subtitle_ReactorSaint", "Reactor lattice online. Aegis protocol engaged.");
                case "ABY_ArchonOfRupture":
                    return Translate("ABY_BossPresentation_Subtitle_Rupture", "The breach remembers its crown.");
                case "ABY_ReliquaryArchonBeast":
                    return Translate("ABY_BossPresentation_Subtitle_ReliquaryArchonBeast", "A heavier law of the first gate enters the map.");
                case "ABY_ArchonBeast":
                    return Translate("ABY_BossPresentation_Subtitle_ArchonBeast", "The first refusal of the gate has arrived.");
                case "ABY_WardenOfAsh":
                    return Translate("ABY_BossPresentation_Subtitle_WardenOfAsh", "Ash discipline anchors the field.");
                case "ABY_ChoirEngine":
                    return Translate("ABY_BossPresentation_Subtitle_ChoirEngine", "The hymn becomes a command channel.");
                default:
                    return Translate("ABY_BossPresentation_Subtitle_Default", "Abyssal authority enters the map.");
            }
        }

        public static string ResolvePhaseTitle(Pawn boss, ABY_BossBarProfileDef profile, int phase)
        {
            string label = AbyssalBossBarUtility.ResolvePhaseLabel(Mathf.Max(1, phase));
            return Translate("ABY_BossPresentation_PhaseTitle", "PHASE {0}", label);
        }

        public static string ResolvePhaseSubtitle(Pawn boss, ABY_BossBarProfileDef profile, int phase)
        {
            string defName = ResolveBossDefName(boss);
            switch (defName)
            {
                case "ABY_ReactorSaint":
                    return phase >= 3
                        ? Translate("ABY_BossPresentation_Phase_ReactorSaint_Final", "Containment limits exceeded. Reactor hymn accelerating.")
                        : Translate("ABY_BossPresentation_Phase_ReactorSaint", "Aegis geometry recalibrates under fire.");
                case "ABY_ArchonOfRupture":
                    return phase >= 4
                        ? Translate("ABY_BossPresentation_Phase_Rupture_Final", "The veil fails. Rupture authority is unbound.")
                        : Translate("ABY_BossPresentation_Phase_Rupture", "A deeper law opens beneath the breach.");
                case "ABY_ReliquaryArchonBeast":
                    return phase >= 3
                        ? Translate("ABY_BossPresentation_Phase_ReliquaryArchonBeast_Final", "The reliquary law closes around the field.")
                        : Translate("ABY_BossPresentation_Phase_ReliquaryArchonBeast", "The shrine-beast unfolds its stored authority.");
                case "ABY_ArchonBeast":
                    return phase >= 3
                        ? Translate("ABY_BossPresentation_Phase_ArchonBeast_Final", "The beast answers with total refusal.")
                        : Translate("ABY_BossPresentation_Phase_ArchonBeast", "The gate-beast sheds restraint.");
                case "ABY_ChoirEngine":
                    return Translate("ABY_BossPresentation_Phase_ChoirEngine", "The chorus retunes the battlefield.");
                default:
                    return Translate("ABY_BossPresentation_Phase_Default", "The abyssal pattern escalates.");
            }
        }

        public static string ResolveOutroTitle(Pawn boss, ABY_BossBarProfileDef profile)
        {
            return Translate("ABY_BossPresentation_OutroTitle", "NODE COLLAPSE");
        }

        public static string ResolveOutroSubtitle(Pawn boss, ABY_BossBarProfileDef profile)
        {
            string defName = ResolveBossDefName(boss);
            switch (defName)
            {
                case "ABY_ReactorSaint":
                    return Translate("ABY_BossPresentation_Outro_ReactorSaint", "The reactor saint falls silent. The aegis geometry unthreads.");
                case "ABY_ArchonOfRupture":
                    return Translate("ABY_BossPresentation_Outro_Rupture", "Rupture authority collapses back into the wound.");
                case "ABY_ReliquaryArchonBeast":
                    return Translate("ABY_BossPresentation_Outro_ReliquaryArchonBeast", "The reliquary shell collapses. The gate loses a heavier claim.");
                case "ABY_ArchonBeast":
                    return Translate("ABY_BossPresentation_Outro_ArchonBeast", "The gate rejects its own beast.");
                case "ABY_ChoirEngine":
                    return Translate("ABY_BossPresentation_Outro_ChoirEngine", "The command hymn drops into dead static.");
                default:
                    return Translate("ABY_BossPresentation_Outro_Default", "The abyssal presence recedes, but the mark remains.");
            }
        }

        private static string ResolveBossDefName(Pawn boss)
        {
            return boss?.def?.defName ?? boss?.kindDef?.defName;
        }

        private static string Translate(string key, string fallback)
        {
            return AbyssalSummoningConsoleUtility.TranslateOrFallback(key, fallback);
        }

        private static string Translate(string key, string fallback, string arg)
        {
            return AbyssalSummoningConsoleUtility.TranslateOrFallback(key, fallback, arg);
        }
    }
}
