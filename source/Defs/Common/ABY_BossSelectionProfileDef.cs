using System.Collections.Generic;
using Verse;

namespace AbyssalProtocol
{
    public sealed class ABY_BossSelectionProfileDef : Def
    {
        public List<string> bossThingDefNames = new List<string>();
        public List<string> bossPawnKindDefNames = new List<string>();

        public float widthCells = 4.2f;
        public float heightCells = 3.8f;
        public float yOffsetCells = 0f;
        public int priority = 40;

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

            if (widthCells <= 0f)
            {
                yield return defName + " has widthCells <= 0.";
            }

            if (heightCells <= 0f)
            {
                yield return defName + " has heightCells <= 0.";
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
