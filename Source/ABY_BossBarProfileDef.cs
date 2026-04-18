using System.Collections.Generic;
using Verse;

namespace AbyssalProtocol
{
    public sealed class ABY_BossBarProfileDef : Def
    {
        public List<string> bossThingDefNames = new List<string>();
        public List<string> bossPawnKindDefNames = new List<string>();

        public string displayLabel;
        public string displayLabelKey;
        public string iconTexPath;
        public string styleId = "default";
        public string phaseSourceMode;
        public string secondaryBarSource;

        public bool showPhaseMarkers = true;
        public bool showWhenDowned = true;
        public int priority;

        public List<ABY_BossBarPhaseEntry> phaseEntries = new List<ABY_BossBarPhaseEntry>();

        public bool Matches(Pawn pawn)
        {
            if (pawn == null)
            {
                return false;
            }

            string thingDefName = pawn.def?.defName;
            if (!thingDefName.NullOrEmpty() && ContainsString(bossThingDefNames, thingDefName))
            {
                return true;
            }

            string pawnKindDefName = pawn.kindDef?.defName;
            return !pawnKindDefName.NullOrEmpty() && ContainsString(bossPawnKindDefNames, pawnKindDefName);
        }

        public string ResolveDisplayLabel(Pawn pawn, string overrideLabel)
        {
            if (!overrideLabel.NullOrEmpty())
            {
                return overrideLabel;
            }

            if (!displayLabelKey.NullOrEmpty())
            {
                return displayLabelKey.Translate();
            }

            if (!displayLabel.NullOrEmpty())
            {
                return displayLabel;
            }

            return pawn?.LabelCap ?? defName;
        }

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors())
            {
                yield return error;
            }

            if ((bossThingDefNames == null || bossThingDefNames.Count == 0) &&
                (bossPawnKindDefNames == null || bossPawnKindDefNames.Count == 0))
            {
                yield return defName + " does not define any bossThingDefNames or bossPawnKindDefNames.";
            }

            if (phaseEntries == null)
            {
                yield break;
            }

            for (int i = 0; i < phaseEntries.Count; i++)
            {
                ABY_BossBarPhaseEntry phaseEntry = phaseEntries[i];
                if (phaseEntry == null)
                {
                    yield return defName + " contains a null phase entry at index " + i + ".";
                    continue;
                }

                if (phaseEntry.phaseIndex < 1)
                {
                    yield return defName + " contains a phase entry with phaseIndex < 1 at index " + i + ".";
                }

                if (phaseEntry.triggerHealthPct < 0f || phaseEntry.triggerHealthPct > 1f)
                {
                    yield return defName + " contains a phase entry with triggerHealthPct outside 0..1 at index " + i + ".";
                }
            }
        }

        private static bool ContainsString(List<string> list, string value)
        {
            if (list == null || list.Count == 0 || value.NullOrEmpty())
            {
                return false;
            }

            for (int i = 0; i < list.Count; i++)
            {
                if (string.Equals(list[i], value))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
