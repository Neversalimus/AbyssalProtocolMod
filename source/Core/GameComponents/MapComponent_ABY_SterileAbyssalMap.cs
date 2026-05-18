using Verse;

namespace AbyssalProtocol
{
    /// <summary>
    /// Marker for temporary dominion pocket maps.
    /// Passive on normal maps; active only when a Dominion slice marks a map sterile.
    /// Package 13 exposes a shared check for optional large-modpack map-generation guards.
    /// </summary>
    public sealed class MapComponent_ABY_SterileAbyssalMap : MapComponent
    {
        private bool sterileDominionPocket;

        public bool IsSterileDominionPocket => sterileDominionPocket;

        public MapComponent_ABY_SterileAbyssalMap(Map map) : base(map)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref sterileDominionPocket, "sterileDominionPocket", false);
        }

        public void MarkSterileDominionPocket()
        {
            sterileDominionPocket = true;
        }

        public static bool IsSterile(Map map)
        {
            return map?.GetComponent<MapComponent_ABY_SterileAbyssalMap>()?.IsSterileDominionPocket == true;
        }

        public static bool IsSterileOrDominionSlice(Map map)
        {
            return IsSterile(map) || AbyssalDominionSterileMapUtility.IsDominionSliceMap(map);
        }
    }
}
