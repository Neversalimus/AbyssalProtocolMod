using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    public class ABY_ProtocolResearchDef : Def
    {
        public ABY_ProtocolResearchCategoryDef category;
        public string tierLabel;
        public int displayOrder;
        public string previewState = "Available";
        public List<ResearchProjectDef> requiredResearchProjects;
        public List<string> requirements;
        public List<string> reveals;
        public List<string> unlocks;
        public List<string> notes;
        public string loreRecord;
        public bool experimental = true;
        public int decodeWorkTicks = 2500;
        public bool autoDecodeWhenPrerequisitesMet;
        public bool futureReserve;
    }
}
