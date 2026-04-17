using System;
using Verse;

namespace AbyssalProtocol
{
    public sealed class ABY_HostileManifestEntry : IExposable
    {
        public PawnKindDef KindDef;
        public int Count = 1;

        public ABY_HostileManifestEntry()
        {
        }

        public ABY_HostileManifestEntry(PawnKindDef kindDef, int count)
        {
            KindDef = kindDef;
            Count = Math.Max(1, count);
        }

        public void ExposeData()
        {
            Scribe_Defs.Look(ref KindDef, "kindDef");
            Scribe_Values.Look(ref Count, "count", 1);
        }
    }
}
