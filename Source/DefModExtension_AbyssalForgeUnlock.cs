using Verse;

namespace AbyssalProtocol
{
    public class DefModExtension_AbyssalForgeUnlock : DefModExtension
    {
        public int requiredResidue;
        public string category = AbyssalForgeProgressUtility.CoreCategory;

        // Optional Protocol Nexus bridge. Kept as strings so the Forge core can safely ignore
        // or survive removal of the experimental ProtocolResearch defs/source layer.
        public string requiredProtocolResearchDefName;
        public string unknownLabel;
        public string unknownHint;
        public bool revealCategoryWhenUnknown = true;
        public bool revealTierWhenUnknown = true;
    }
}
