using System.Collections.Generic;
using Verse;

namespace AbyssalProtocol
{
    /// <summary>
    /// Restricts an apparel definition to specific RimWorld body types without changing its XML apparel layers.
    /// Used for body armor whose directional worn overlays are authored for standard humanoid silhouettes.
    /// </summary>
    public class DefModExtension_ABY_ApparelBodyTypeRestriction : DefModExtension
    {
        public List<string> allowedBodyTypes;
        public List<string> disallowedBodyTypes;
        public string rejectMessageKey = "ABY_ApparelBodyTypeRestriction_Incompatible";
        public string removedMessageKey = "ABY_ApparelBodyTypeRestriction_Removed";
        public bool showRejectMessage = true;
        public bool showRemovalMessage = true;
    }
}
